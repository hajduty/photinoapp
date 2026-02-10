using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace JobTracker.Application.Infrastructure.Discord;

public sealed class DiscordWebhookService : IDiscordWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DiscordWebhookService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _httpClient = new HttpClient();
        _dbFactory = dbFactory;
    }

    private async Task<(string? WebhookUrl, bool Enabled)> GetSettingsAsync()
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        var settings = await dbContext.Settings.FirstOrDefaultAsync();
        
        if (settings == null || !settings.DiscordNotificationsEnabled)
            return (null, false);
            
        return (settings.DiscordWebhookUrl, settings.DiscordNotificationsEnabled);
    }

    public async Task SendNotificationAsync(string title, string description, NotificationType type = NotificationType.Info)
    {
        var (webhookUrl, enabled) = await GetSettingsAsync();
        if (!enabled || string.IsNullOrEmpty(webhookUrl))
            return;

        var color = type switch
        {
            NotificationType.Success => 0x00FF00,
            NotificationType.Warning => 0xFFA500,
            NotificationType.Error => 0xFF0000,
            _ => 0x3498DB
        };

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title,
                    description,
                    color,
                    timestamp = DateTime.UtcNow.ToString("O")
                }
            }
        };

        await SendPayloadAsync(webhookUrl!, payload);
    }

    public async Task SendJobAlertAsync(string keyword, int jobCount, string[]? jobTitles = null)
    {
        var (webhookUrl, enabled) = await GetSettingsAsync();
        if (!enabled || string.IsNullOrEmpty(webhookUrl))
            return;

        var description = jobCount == 1
            ? $"Found **1** new job matching `{keyword}`"
            : $"Found **{jobCount}** new jobs matching `{keyword}`";

        if (jobTitles?.Length > 0)
        {
            var titlesList = string.Join("\n• ", jobTitles.Take(10));
            description += $"\n\n**Jobs:**\n• {titlesList}";
            if (jobTitles.Length > 10)
                description += $"\n... and {jobTitles.Length - 10} more";
        }

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "🔔 Job Alert",
                    description,
                    color = 0x3498DB,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    footer = new { text = "JobTracker" }
                }
            }
        };

        await SendPayloadAsync(webhookUrl!, payload);
    }

    private async Task SendPayloadAsync(string webhookUrl, object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(webhookUrl, content);
        }
        catch (Exception ex)
        {
            // Log error but don't throw webhook failures shouldn't break the app
            Console.WriteLine($"Discord webhook error: {ex.Message}");
        }
    }
}
