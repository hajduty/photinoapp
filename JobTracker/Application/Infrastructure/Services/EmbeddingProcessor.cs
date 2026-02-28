using JobTracker.Application.Events;
using JobTracker.Application.Features.JobSearch;
using JobTracker.Application.Features.SemanticSearch;
using JobTracker.Application.Features.System;
using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Services;
using System.Diagnostics;

namespace JobTracker.Application.Infrastructure.Services;

public class EmbeddingProcessor
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEventPublisher _domainEventPublisher;
    private readonly JinaEmbeddingService _embeddingService; // Your new service

    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EmbeddingProcessor(
        IDbContextFactory<AppDbContext> dbFactory,
        IEventPublisher domainEventPublisher,
        JinaEmbeddingService embeddingService)
    {
        _dbFactory = dbFactory;
        _domainEventPublisher = domainEventPublisher;
        _embeddingService = embeddingService;
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

            // Format texts for embedding
            var postingTexts = postings.Select(p =>
                $"Location: {p.Location} {p.PostedDate} {p.Title} {p.Description}"
            ).ToList();

            var batchSize = 16;
            var batchNumber = 0;

            // Process in batches using your new EmbeddingService
            for (int i = 0; i < postingTexts.Count; i += batchSize)
            {
                token.ThrowIfCancellationRequested();

                var textBatch = postingTexts.Skip(i).Take(batchSize).ToList();
                var currentBatch = postings.Skip(i).Take(batchSize).ToList();

                var embeddingFloats = _embeddingService.GenerateEmbeddingsBatch(textBatch.ToArray());

                var successCount = 0;
                for (int j = 0; j < currentBatch.Count; j++)
                {
                    if (j < embeddingFloats.Length && embeddingFloats[j] != null)
                    {
                        var floats = embeddingFloats[j];
                        var bytes = new byte[floats.Length * sizeof(float)];
                        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);

                        db.JobEmbeddings.Add(new JobEmbedding
                        {
                            JobId = currentBatch[j].Id,
                            Data = bytes
                        });
                        successCount++;
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to generate embedding for Job {currentBatch[j].Id}");
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