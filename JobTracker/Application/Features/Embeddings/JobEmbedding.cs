namespace JobTracker.Application.Features.Embeddings;

public class JobEmbedding
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public byte[] EmbeddingData { get; set; } = Array.Empty<byte>();
}