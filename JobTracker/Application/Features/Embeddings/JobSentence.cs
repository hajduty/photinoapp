using System.ComponentModel.DataAnnotations;

namespace JobTracker.Application.Features.Embeddings;

public class JobSentence
{
    [Key]
    public int Id { get; set; }
    public int JobId { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string Sentence { get; set; }
    public byte[] Data { get; set; }
    public string? SentenceType { get; set; }
    public float? Score { get; set; }
}

public class JobSentenceDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string Sentence { get; set; }
    public string? SentenceType { get; set; }
    public float? Score { get; set; }
}