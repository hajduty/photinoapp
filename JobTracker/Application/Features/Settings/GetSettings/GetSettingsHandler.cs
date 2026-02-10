using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Settings.GetSettings;

public sealed class GetSettingsHandler : RpcHandler<object?, Settings>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "settings.getSettings";

    public GetSettingsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<Settings> HandleAsync(object? request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        
        // Return first settings record or create default if none exists
        var settings = await dbContext.Settings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            settings = new Settings();
            dbContext.Settings.Add(settings);
            await dbContext.SaveChangesAsync();
        }
        
        return settings;
    }
}
