using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Notification.UpdateNotifications;

[ExportTsInterface]
public record UpdateNotificationRequest(int Id, bool IsRead);
[ExportTsInterface]
public record UpdateNotificationResponse(Notification Notification);
public class UpdateNotificationHandler
    : RpcHandler<UpdateNotificationRequest, UpdateNotificationResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "notification.updateNotification";

    public UpdateNotificationHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<UpdateNotificationResponse> HandleAsync(UpdateNotificationRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var notification = await dbContext.Notifications.FirstAsync(n => n.Id == request.Id);
        notification.IsRead = request.IsRead;

        await dbContext.SaveChangesAsync();
        return new UpdateNotificationResponse(notification);
    }
}
