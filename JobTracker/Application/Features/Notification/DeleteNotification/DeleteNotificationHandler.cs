using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Notification.DeleteNotification;

[ExportTsInterface]
public record DeleteNotificationRequest(int NotificationId);
[ExportTsInterface]
public record DeleteNotificationResponse(bool Success);

public sealed class DeleteNotificationHandler : RpcHandler<DeleteNotificationRequest, DeleteNotificationResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public override string Command => "notification.deleteNotification";

    public DeleteNotificationHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<DeleteNotificationResponse> HandleAsync(DeleteNotificationRequest request)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();

        var notification = await dbContext.Notifications.FindAsync(request.NotificationId);
        if (notification == null)
        {
            return new DeleteNotificationResponse(false);
        }

        dbContext.Notifications.Remove(notification);
        await dbContext.SaveChangesAsync();
        return new DeleteNotificationResponse(true);
    }
}