using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Settings.UpdateSettings;
[ExportTsInterface]
public record UpdateSettingsRequest(
    string? DiscordWebhookUrl = null,
    bool? DiscordNotificationsEnabled = null,
    bool? GenerateEmbeddings = null
);
[ExportTsInterface]
public record UpdateSettingsResponse(Settings Settings);

public sealed class UpdateSettingsHandler : RpcHandler<UpdateSettingsRequest, UpdateSettingsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "settings.updateSettings";

    public UpdateSettingsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdateSettingsResponse> HandleAsync(UpdateSettingsRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var settings = await dbContext.Settings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            settings = new Settings();
            dbContext.Settings.Add(settings);
        }

        // Update only provided fields
        if (request.DiscordWebhookUrl != null)
            settings.DiscordWebhookUrl = request.DiscordWebhookUrl;
        
        if (request.DiscordNotificationsEnabled.HasValue)
            settings.DiscordNotificationsEnabled = request.DiscordNotificationsEnabled.Value;

        if (request.GenerateEmbeddings.HasValue)
            settings.GenerateEmbeddings = request.GenerateEmbeddings.Value;

        settings.LastUpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return new UpdateSettingsResponse(settings);
    }
}
