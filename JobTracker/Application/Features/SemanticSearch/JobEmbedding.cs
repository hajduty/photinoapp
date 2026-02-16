using System.ComponentModel.DataAnnotations;

namespace JobTracker.Application.Features.SemanticSearch;

public class JobEmbedding
{
    [Key]
    public int JobId { get; set; }
    public byte[] Data { get; set; }
}