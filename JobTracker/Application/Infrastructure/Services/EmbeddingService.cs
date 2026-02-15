using JobTracker.Application.Features.Postings;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JobTracker.Application.Infrastructure.Services;

public class EmbeddingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly OllamaService _ollama;

    public EmbeddingService(IDbContextFactory<AppDbContext> dbFactory, OllamaService ollama)
    {
        _dbFactory = dbFactory;
        _ollama = ollama;
    }

    public async Task GenerateEmbeddingsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var postings = await db.Postings
            .Where(p => !db.JobEmbeddings.Any(e => e.JobId == p.Id))
            .OrderBy(p => p.Id)
            .ToListAsync();

        Debug.WriteLine($"Found {postings.Count} postings to process");

        var postingTexts = postings.Select(p =>
            $"Location: {p.Location} {p.PostedDate} {p.Title} {p.Description}"
        ).ToList();

        var batchSize = 20;
        var totalProcessed = 0;
        var batchNumber = 0;

        await foreach (var batchEmbeddings in _ollama.GenerateEmbeddingsBatchedAsync(postingTexts))
        {
            var currentBatch = postings
                .Skip(batchNumber * batchSize)
                .Take(batchSize)
                .ToList();

            var successCount = 0;

            // Add embeddings to DB context
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

            await db.SaveChangesAsync();

            totalProcessed += successCount;
            batchNumber++;

            Debug.WriteLine($"Batch {batchNumber} saved: {successCount}/{currentBatch.Count} embeddings");
            Debug.WriteLine($"Progress: {totalProcessed}/{postings.Count} ({(totalProcessed * 100 / postings.Count):F1}%)");
        }

        Debug.WriteLine($"Complete! Generated {totalProcessed} embeddings");
    }

    public async Task<List<Posting>> SearchAsync(string query, int top = 10)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // 1️ Generate query embedding
        var queryVector = await _ollama.GenerateEmbeddingAsync(query);
        var normalizedQuery = Normalize(queryVector);

        // 2️ Load embeddings
        var embeddings = await db.JobEmbeddings.ToListAsync();

        // 3️ Rank them in memory
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

        // 4 Fetch postings
        var postings = await db.Postings
            .Where(p => rankedIds.Contains(p.Id))
            .ToListAsync();

        // 5 Reorder postings based on ranking
        var ordered = rankedIds
            .Join(postings,
                  id => id,
                  p => p.Id,
                  (id, p) => p)
            .ToList();

        return ordered;
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
