using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.System.Settings;

[ExportTsInterface]
public class Settings
{
    public int Id { get; set; }
    public string DiscordWebhookUrl { get; set; } = string.Empty;
    public bool DiscordNotificationsEnabled { get; set; } = false;
    public bool GenerateEmbeddings { get; set; } = false;
    public string AppVersion { get; set; } = "1.0.0";
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public bool? FirstStart { get; set; } = true;
    public int? ActiveProfileId { get; set; }
}
