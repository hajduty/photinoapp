using JobTracker.Application.Infrastructure.Data;
using JobTracker.Embeddings;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Services;

public class SentenceClassifierService
{
    private readonly IDbContextFactory<AppDbContext> _dbContext;
    private readonly Dictionary<string, List<float[]>> _prototypeEmbeddings;
    private const float SIMILARITY_THRESHOLD = 0.55f;

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
                sum += Helper.DotProductSimilarity(sentenceVector, prototypeVector);
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

    public (float[]? ClosestVector, float Similarity) FindClosestVector(float[] query, IEnumerable<float[]> candidates)
    {
        if (query == null || query.Length == 0)
            throw new ArgumentException("Query vector cannot be null or empty");

        if (candidates == null)
            return (null, -1f);

        float[]? bestVector = null;
        float bestSimilarity = -1f;

        foreach (var candidate in candidates)
        {
            if (candidate == null || candidate.Length != query.Length)
                continue; 

            float sim = Helper.DotProductSimilarity(query, candidate);

            if (sim > bestSimilarity)
            {
                bestSimilarity = sim;
                bestVector = candidate;
            }
        }

        return (bestVector, bestSimilarity);
    }
}