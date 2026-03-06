using JobTracker.Application.Events;
using JobTracker.Application.Features.Embeddings;
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
            await GenerateEmbeddingsInternal(_cts.Token);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task GenerateEmbeddingsInternal(CancellationToken token)
    {
        var totalProcessed = 0;
        await _domainEventPublisher.PublishAsync(new EmbeddingsStarted());

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(token);
            db.ChangeTracker.AutoDetectChangesEnabled = false;

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

            const int batchSize = 4;

            for (int i = 0; i < postings.Count; i += batchSize)
            {
                token.ThrowIfCancellationRequested();

                var batch = postings.Skip(i).Take(batchSize).ToList();
                var texts = batch.Select(p => p.Description).ToArray();

                var embeddings = _embeddingService.GenerateEmbeddingsBatch(texts);

                for (int j = 0; j < batch.Count; j++)
                {
                    var vector = embeddings[j];
                    var bytes = new byte[vector.Length * sizeof(float)];
                    Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);

                    db.JobEmbeddings.Add(new JobEmbedding
                    {
                        JobId = batch[j].Id,
                        EmbeddingData = bytes
                    });
                }

                await db.SaveChangesAsync(token);
                totalProcessed += batch.Count;

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

    public void Cancel() => _cts?.Cancel();
}