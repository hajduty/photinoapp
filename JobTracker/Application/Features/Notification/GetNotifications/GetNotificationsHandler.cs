using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Notification.GetNotifications;

public sealed class GetNotificationsHandler : RpcHandler<object?, List<Notification>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "notification.getNotifications";

    public GetNotificationsHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<List<Notification>> HandleAsync(object? request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        return await dbContext.Notifications.ToListAsync();
    }
}
