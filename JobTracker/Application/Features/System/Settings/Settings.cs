using JobTracker.Application.Features.Tags;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.System.Settings;

[ExportTsInterface]
public class Settings
{
    public int Id { get; set; }
    // Discord Integration
    public string DiscordWebhookUrl { get; set; } = string.Empty;
    public bool DiscordNotificationsEnabled { get; set; } = false;
    public bool GenerateEmbeddings { get; set; } = false;
    // App Info
    public string AppVersion { get; set; } = "1.0.0";
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public bool? FirstStart { get; set; } = true;
    // Preferences
    public byte[]? UserEmbedding { get; set; } = null!;
    public string? UserCV { get; set; }
    public List<Tag>? SelectedTags { get; set; }
    public int? YearsOfExperience { get; set; }
    public List<string>? BlockedKeywords { get; set; }
    public List<string>? MatchedKeywords { get; set; }
    public bool? AlertOnAllMatchingJobs { get; set; }
    public bool? AlertOnHardMatchingJobs { get; set; }
    public string? Location { get; set; }
    public int? MaxJobAgeDays { get; set; }
}
