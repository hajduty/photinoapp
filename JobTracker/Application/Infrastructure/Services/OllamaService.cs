using System.Diagnostics;
using System.Net.Http.Json;

namespace JobTracker.Application.Infrastructure.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly int _batchSize;

    public OllamaService(HttpClient httpClient, int batchSize = 20)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:11434");
        _batchSize = batchSize;
    }

    // Keep original for backward compatibility if needed
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embeddings = await GenerateEmbeddingsAsync(new[] { text });
        return embeddings.FirstOrDefault();
    }

    // New batched method
    public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts)
    {
        var textsList = texts.ToList();
        if (!textsList.Any())
            return new List<float[]>();

        var payload = new
        {
            model = "nomic-embed-text-v2-moe",
            input = textsList
        };

        var response = await _httpClient.PostAsJsonAsync("/api/embed", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaEmbedResponse>();

        if (result?.embeddings != null)
        {
            return result.embeddings.ToList();
        }

        throw new Exception("No embeddings returned from Ollama");
    }

    public async IAsyncEnumerable<List<float[]>> GenerateEmbeddingsBatchedAsync(
        IEnumerable<string> texts,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var textsList = texts.ToList();

        for (int i = 0; i < textsList.Count; i += _batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = textsList.Skip(i).Take(_batchSize).ToList();
            var batchEmbeddings = new List<float[]>();
            bool batchFailed = false;

            try
            {
                batchEmbeddings = await GenerateEmbeddingsAsync(batch);
                Debug.WriteLine($"Batch {i / _batchSize + 1}: {batch.Count} embeddings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Batch failed, falling back to individual: {ex.Message}");
                batchFailed = true;
            }

            if (batchFailed)
            {
                batchEmbeddings = new List<float[]>();
                foreach (var text in batch)
                {
                    try
                    {
                        var embedding = await GenerateEmbeddingAsync(text);
                        batchEmbeddings.Add(embedding);

                        await Task.Delay(50, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($" Individual failed: {ex.Message}");
                        batchEmbeddings.Add(null);
                    }
                }
            }

            yield return batchEmbeddings;
        }
    }
}

public class OllamaEmbedResponse
{
    public float[][] embeddings { get; set; }
}