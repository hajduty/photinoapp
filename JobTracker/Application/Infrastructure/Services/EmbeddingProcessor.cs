using JobTracker.Application.Events;
using JobTracker.Application.Features.Embeddings;
using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.SemanticSearch;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JobTracker.Application.Infrastructure.Services;

public class EmbeddingProcessor
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEventPublisher _domainEventPublisher;
    private readonly JinaEmbeddingService _embeddingService;
    private readonly SentenceClassifierService _classifierService;

    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EmbeddingProcessor(
        IDbContextFactory<AppDbContext> dbFactory,
        IEventPublisher domainEventPublisher,
        JinaEmbeddingService embeddingService,
        SentenceClassifierService classifierService)
    {
        _dbFactory = dbFactory;
        _domainEventPublisher = domainEventPublisher;
        _embeddingService = embeddingService;
        _classifierService = classifierService;
    }

    public async Task GenerateEmbeddingsAsync()
    {
        if (!await _lock.WaitAsync(0))
            return;

        try
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            await GenerateEmbeddingsInternal(token);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task GenerateEmbeddingsInternal(CancellationToken token)
    {
        var totalProcessed = 0;
        await _domainEventPublisher.PublishAsync(new EmbeddingsStarted());

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(token);

            db.ChangeTracker.AutoDetectChangesEnabled = false;

            // Fetch jobs without embeddings
            var postings = await db.Postings
                .Where(p => !db.JobEmbeddings.Any(e => e.JobId == p.Id))
                .OrderBy(p => p.Id)
                .ToListAsync(token);

            Debug.WriteLine($"Found {postings.Count} postings to process");

            if (postings.Count == 0)
            {
                await _domainEventPublisher.PublishAsync(new EmbeddingsFinished(0));
                return;
            }

            const int batchSize = 110;

            var buffer = new List<(int JobId, JobSentence Sentence)>(batchSize);

            foreach (var posting in postings)
            {
                token.ThrowIfCancellationRequested();

                var fullText = $"{posting.Location}. {posting.Title}. {posting.Description}";
                var sentences = ExtractJobSentences(fullText);

                foreach (var sentence in sentences)
                {
                    buffer.Add((posting.Id, sentence));

                    if (buffer.Count >= batchSize)
                    {
                        totalProcessed += await ProcessBatch(buffer, db, token);
                        buffer.Clear();

                        await _domainEventPublisher.PublishAsync(
                            new EmbeddingsProgress(postings.Count, totalProcessed));
                    }
                }
            }

            if (buffer.Count > 0)
            {
                totalProcessed += await ProcessBatch(buffer, db, token);
                buffer.Clear();

                await _domainEventPublisher.PublishAsync(
                    new EmbeddingsProgress(postings.Count, totalProcessed));
            }

            db.ChangeTracker.AutoDetectChangesEnabled = true;

            await _domainEventPublisher.PublishAsync(new EmbeddingsFinished(totalProcessed));
        }
        catch (OperationCanceledException)
        {
            await _domainEventPublisher.PublishAsync(new EmbeddingsCancelled(totalProcessed));
        }
    }

    private async Task<int> ProcessBatch(
        List<(int JobId, JobSentence Sentence)> batch,
        AppDbContext db,
        CancellationToken token)
    {
        var texts = new string[batch.Count];

        for (int i = 0; i < batch.Count; i++)
            texts[i] = batch[i].Sentence.Sentence;

        var embeddings = _embeddingService.GenerateEmbeddingsBatch(texts);

        for (int i = 0; i < batch.Count; i++)
        {
            var (jobId, sentence) = batch[i];
            var vector = embeddings[i];

            var bytes = new byte[vector.Length * sizeof(float)];
            Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);

            var category = _classifierService.ClassifyWithScore(vector);

            db.JobEmbeddings.Add(new JobEmbedding
            {
                JobId = jobId,
                Start = sentence.Start,
                Length = sentence.Length,
                Sentence = sentence.Sentence,
                SentenceType = category.Category,
                Score = category.Score,
                Data = bytes
            });
        }

        await db.SaveChangesAsync(token);

        return batch.Count;
    }

    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
{
    "e.g", "i.e", "etc", "vs", "mr", "mrs", "ms", "dr", "prof", "sr", "jr",
    "dept", "approx", "incl", "excl", "est", "fig", "no", "vol", "p"
};

    public List<JobSentence> ExtractJobSentences(string text)
    {
        var result = new List<JobSentence>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        int start = 0, id = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '.' && IsSentenceEndDot(text, i)) continue;

            if (c is not ('.' or '!' or '?' or '\n')) continue;

            var sentence = text[start..(i + 1)].Trim();

            if (!string.IsNullOrWhiteSpace(sentence))
            {
                int actualStart = text.IndexOf(sentence, start, StringComparison.Ordinal);
                result.Add(new JobSentence
                {
                    Id = id++,
                    Start = actualStart,
                    Length = sentence.Length,
                    Sentence = sentence,
                    SentenceType = null
                });
            }

            start = i + 1;
        }

        // Catch trailing text without punctuation
        if (start < text.Length)
        {
            var trailing = text[start..].Trim();
            if (trailing.Length > 20)
            {
                int actualStart = text.IndexOf(trailing, start, StringComparison.Ordinal);
                result.Add(new JobSentence { Id = id++, Start = actualStart, Length = trailing.Length, Sentence = trailing });
            }
        }

        return result;
    }

    private static bool IsSentenceEndDot(string text, int i)
    {
        // Not end of string — check what follows
        if (i + 1 < text.Length && !char.IsWhiteSpace(text[i + 1]))
            return true; // dot immediately followed by non-space (e.g. "3.5", "U.S.A")

        // Extract the word before the dot
        int wordStart = i - 1;
        while (wordStart > 0 && char.IsLetter(text[wordStart - 1])) wordStart--;
        string wordBefore = text[wordStart..i].ToLower();

        return Abbreviations.Contains(wordBefore); // skip if known abbreviation
    }

    public async Task<List<Posting>> SearchAsync(string query, int top = 10)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Generate query embedding using your new service
        var queryEmbeddingBytes = _embeddingService.GenerateEmbedding(query);
        var normalizedQuery = FromBytes(queryEmbeddingBytes); // Convert back to float for dot product

        // Load embeddings
        var embeddings = await db.JobEmbeddings.ToListAsync();

        // Rank them in memory
        var ranked = embeddings
            .Select(e => new
            {
                e.JobId,
                Score = Dot(normalizedQuery, FromBytes(e.Data))
            })
            .OrderByDescending(x => x.Score)
            .Take(top)
            .ToList();

        var rankedIds = ranked.Select(r => r.JobId).ToList();

        // Fetch postings
        var postings = await db.Postings
            .Where(p => rankedIds.Contains(p.Id))
            .ToListAsync();

        // Reorder postings based on ranking
        var ordered = rankedIds
            .Join(postings,
                  id => id,
                  p => p.Id,
                  (id, p) => p)
            .ToList();

        return ordered;
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    // Keep these helper methods (they're still needed)
    public static float Dot(float[] a, float[] b)
    {
        float sum = 0;
        for (int i = 0; i < a.Length; i++)
            sum += a[i] * b[i];

        return sum;
    }

    public static float[] FromBytes(byte[] blob)
    {
        var floats = new float[blob.Length / sizeof(float)];
        Buffer.BlockCopy(blob, 0, floats, 0, blob.Length);
        return floats;
    }

    // Note: Normalize and ToBytes are no longer needed since EmbeddingService handles them
    // But if other code uses them, you can keep them or remove if unused
    public static float[] Normalize(float[] vec)
    {
        var len = Math.Sqrt(vec.Select(x => x * x).Sum());
        return vec.Select(x => (float)(x / len)).ToArray();
    }

    public static byte[] ToBytes(float[] vec)
    {
        var buffer = new byte[vec.Length * sizeof(float)];
        Buffer.BlockCopy(vec, 0, buffer, 0, buffer.Length);
        return buffer;
    }
}