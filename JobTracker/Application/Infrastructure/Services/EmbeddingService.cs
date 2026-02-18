using JobTracker.Application.Events;
using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.SemanticSearch;
using JobTracker.Application.Features.System;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JobTracker.Application.Infrastructure.Services;

public class EmbeddingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEventPublisher _domainEventPublisher;
    private readonly OllamaService _ollama;

    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EmbeddingService(IDbContextFactory<AppDbContext> dbFactory, OllamaService ollama, IEventPublisher domainEventPublisher)
    {
        _dbFactory = dbFactory;
        _ollama = ollama;
        _domainEventPublisher = domainEventPublisher;
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

            var postings = await db.Postings
                .Where(p => !db.JobEmbeddings.Any(e => e.JobId == p.Id))
                .OrderBy(p => p.Id)
                .ToListAsync(token);

            Debug.WriteLine($"Found {postings.Count} postings to process");

            var postingTexts = postings.Select(p =>
                $"Location: {p.Location} {p.PostedDate} {p.Title} {p.Description}"
            ).ToList();

            var batchSize = 20;
            var batchNumber = 0;

            await foreach (var batchEmbeddings in _ollama.GenerateEmbeddingsBatchedAsync(postingTexts).WithCancellation(token))
            {
                var currentBatch = postings
                    .Skip(batchNumber * batchSize)
                    .Take(batchSize)
                    .ToList();

                token.ThrowIfCancellationRequested();

                var successCount = 0;

                for (int i = 0; i < currentBatch.Count; i++)
                {
                    if (i < batchEmbeddings.Count && batchEmbeddings[i] != null)
                    {
                        var normalized = Normalize(batchEmbeddings[i]);
                        var blob = ToBytes(normalized);

                        db.JobEmbeddings.Add(new JobEmbedding
                        {
                            JobId = currentBatch[i].Id,
                            Data = blob
                        });
                        successCount++;
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to generate embedding for Job {currentBatch[i].Id}");
                    }
                }

                await db.SaveChangesAsync(token);

                totalProcessed += successCount;
                batchNumber++;

                await _domainEventPublisher.PublishAsync(new EmbeddingsProgress(postings.Count, totalProcessed));

                Debug.WriteLine($"Batch {batchNumber} saved: {successCount}/{currentBatch.Count} embeddings");
                Debug.WriteLine($"Progress: {totalProcessed}/{postings.Count} ({(totalProcessed * 100 / postings.Count):F1}%)");
            }

            Debug.WriteLine($"Complete! Generated {totalProcessed} embeddings");
            await _domainEventPublisher.PublishAsync(new EmbeddingsFinished(totalProcessed));
        }
        catch (OperationCanceledException)
        {
            await _domainEventPublisher.PublishAsync(new EmbeddingsCancelled(totalProcessed));
        }
    }

    public async Task<List<Posting>> SearchAsync(string query, int top = 10)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Generate query embedding
        var queryVector = await _ollama.GenerateEmbeddingAsync(query);
        var normalizedQuery = Normalize(queryVector);

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
