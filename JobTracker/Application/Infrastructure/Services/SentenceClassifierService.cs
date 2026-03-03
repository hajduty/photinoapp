using JobTracker.Application.Infrastructure.Data;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Services;

public class SentenceClassifierService
{
    private readonly IDbContextFactory<AppDbContext> _dbContext;
    private readonly Dictionary<string, List<float[]>> _prototypeEmbeddings;
    private const float SIMILARITY_THRESHOLD = 0.21f;

    public SentenceClassifierService(JinaEmbeddingService embeddingService, IDbContextFactory<AppDbContext> dbContext)
    {
        if (embeddingService == null)
            throw new ArgumentNullException(nameof(embeddingService));

        _dbContext = dbContext;
        _prototypeEmbeddings = new Dictionary<string, List<float[]>>();

        using var db = _dbContext.CreateDbContext();

        _prototypeEmbeddings = db.Prototypes
            .Include(p => p.Classification)
            .AsEnumerable()
            .GroupBy(p => p.Classification.Name)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var list = new List<float[]>();

                    foreach (var p in g)
                    {
                        var floats = new float[p.Embedding.Length / sizeof(float)];
                        Buffer.BlockCopy(p.Embedding, 0, floats, 0, p.Embedding.Length);
                        list.Add(floats);
                    }

                    return list;
                }
            );
    }

    public string Classify(byte[] embeddingBytes)
    {
        var (category, _) = ClassifyWithScore(embeddingBytes);
        return category;
    }

    public (string Category, float Score) ClassifyWithScore(byte[] embeddingBytes)
    {
        if (embeddingBytes == null || embeddingBytes.Length == 0)
            throw new ArgumentException("Embedding cannot be null or empty");

        float[] vector = new float[embeddingBytes.Length / sizeof(float)];
        Buffer.BlockCopy(embeddingBytes, 0, vector, 0, embeddingBytes.Length);

        return GetClosestPrototype(vector);
    }

    public string Classify(float[] vector)
    {
        var (category, _) = GetClosestPrototype(vector);
        return category;
    }

    public (string Category, float Score) ClassifyWithScore(float[] vector)
    {
        return GetClosestPrototype(vector);
    }

    public string ClassifyFromText(string text, JinaEmbeddingService embeddingService)
    {
        var (category, _) = ClassifyFromTextWithScore(text, embeddingService);
        return category;
    }

    public (string Category, float Score) ClassifyFromTextWithScore(string text, JinaEmbeddingService embeddingService)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty");

        var embedding = embeddingService.GenerateEmbeddingFloat(text);
        return GetClosestPrototype(embedding);
    }

    private (string Category, float Score) GetClosestPrototype(float[] sentenceVector)
    {
        string bestCategory = "Unknown";
        float bestScore = float.MinValue;

        foreach (var kvp in _prototypeEmbeddings)
        {
            var prototypes = kvp.Value;

            float sum = 0f;

            foreach (var prototypeVector in prototypes)
            {
                sum += CosineSimilarity(sentenceVector, prototypeVector);
            }

            float avgSim = sum / prototypes.Count;

            if (avgSim > bestScore)
            {
                bestScore = avgSim;
                bestCategory = kvp.Key;
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"Best match: {bestCategory} with avg similarity {bestScore:F4}");

        if (bestScore <= SIMILARITY_THRESHOLD)
            return ("Unknown", bestScore);

        return (bestCategory, bestScore);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Vector length mismatch: {a.Length} vs {b.Length}");

        float dot = 0f;
        float magA = 0f;
        float magB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
            return 0f;

        return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}