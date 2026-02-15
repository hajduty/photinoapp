using JobTracker.Application.Features.Postings;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Application.Infrastructure.Entities;

public class JobEmbedding
{
    [Key]
    public int JobId { get; set; }
    public byte[] Data { get; set; }
}