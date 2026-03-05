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
    private readonly SemanticChunker _chunker;

    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EmbeddingProcessor(
        IDbContextFactory<AppDbContext> dbFactory,
        IEventPublisher domainEventPublisher,
        JinaEmbeddingService embeddingService,
        SentenceClassifierService classifierService,
        SemanticChunker chunker)
    {
        _dbFactory = dbFactory;
        _domainEventPublisher = domainEventPublisher;
        _embeddingService = embeddingService;
        _classifierService = classifierService;
        _chunker = chunker;
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
            await GenerateChunksForReadyJobsAsync(token);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task GenerateChunksForReadyJobsAsync(CancellationToken token)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(token);

        var jobsToChunk = await db.Postings
            .Where(p => db.JobSentences.Any(s => s.JobId == p.Id)
                     && !db.JobChunks.Any(c => c.JobId == p.Id))
            .Select(p => p.Id)
            .ToListAsync(token);

        if (!jobsToChunk.Any()) return;

        int processed = 0;
        foreach (var jobId in jobsToChunk)
        {
            token.ThrowIfCancellationRequested();

            var sentences = await db.JobSentences
                .Where(s => s.JobId == jobId)
                .OrderBy(s => s.Start)
                .ToListAsync(token);

            if (!sentences.Any()) continue;

            var chunkEntities = _chunker.CreateChunks(sentences, jobId);

            db.JobChunks.AddRange(chunkEntities);
            await db.SaveChangesAsync(token);

            processed++;
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
                .Where(p => !db.JobSentences.Any(e => e.JobId == p.Id))
                .OrderBy(p => p.Id)
                .ToListAsync(token);

            Debug.WriteLine($"Found {postings.Count} postings to process");

            if (postings.Count == 0)
            {
                await _domainEventPublisher.PublishAsync(new EmbeddingsFinished(0));
                return;
            }

            const int batchSize = 110;

            var buffer = new List<(int JobId, JobSentenceDto Sentence)>(batchSize);

            foreach (var posting in postings)
            {
                token.ThrowIfCancellationRequested();

                var fullText = $"{posting.Description}";
                var sentences = JobSentenceExtractor.Extract(posting.Description);

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
        List<(int JobId, JobSentenceDto Sentence)> batch,
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

            //var category = _classifierService.ClassifyWithScore(vector);

            db.JobSentences.Add(new JobSentence
            {
                JobId = jobId,
                Start = sentence.Start,
                Length = sentence.Length,
                Sentence = sentence.Sentence,
                Data = bytes
            });
        }

        await db.SaveChangesAsync(token);

        return batch.Count;
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }
}