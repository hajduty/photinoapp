using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JobTracker.Application.Features.JobApplication;

public class ApplicationStatusHistory
{
    [Key]
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    [JsonIgnore]
    public JobApplication JobApplication { get; set; } = null!;
    public ApplicationStatus Status { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
}
