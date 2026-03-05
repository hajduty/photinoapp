namespace JobTracker.Application.Features.Embeddings;

public class JobChunk
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public byte[] ChunkEmbedding { get; set; } = Array.Empty<byte>();
    public int StartChar { get; set; }
    public int Length { get; set; }
    public string? ChunkType { get; set; }
    public float? Score { get; set; }
    public List<int> SentenceIds { get; set; } = new();
}

public class JobChunkDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public int StartChar { get; set; }
    public int Length { get; set; }
    public string? ChunkType { get; set; }
    public float? Score { get; set; }
    public List<int> SentenceIds { get; set; } = new();
}