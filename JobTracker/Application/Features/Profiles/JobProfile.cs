using JobTracker.Application.Features.Settings;
using JobTracker.Application.Features.Tags;

namespace JobTracker.Application.Features.Profiles;

public class JobProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = "Default";
    public string? UserId { get; set; } // reserved for future multi-user
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public byte[]? UserEmbedding { get; set; }
    public string? UserCV { get; set; }
    public List<Tag>? SelectedTags { get; set; }
    public int? YearsOfExperience { get; set; }
    public List<KeywordRule>? BlockedKeywords { get; set; }
    public List<KeywordRule>? MatchedKeywords { get; set; }
    public bool? AlertOnAllMatchingJobs { get; set; }
    public bool? AlertOnHardMatchingJobs { get; set; }
    public List<string>? Locations { get; set; }
    public int? MaxJobAgeDays { get; set; }

    // Aggregated from ignore actions
    public List<string>? BlockedLocations { get; set; }
    public List<string>? RejectedSeniorityLevels { get; set; }
    public List<RejectedTagRule>? RejectedTechKeywords { get; set; }
}
