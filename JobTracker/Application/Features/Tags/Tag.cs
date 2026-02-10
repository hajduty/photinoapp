using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace JobTracker.Application.Features.Tags;

[Index(nameof(Name), IsUnique = true)]
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#ffffff";

    // Navigation property for many-to-many relationship with JobAlert
    [JsonIgnore] // Prevent circular reference during serialization
    public List<JobTracker.JobTracker> JobTrackers { get; set; } = new List<JobTracker.JobTracker>();
}
