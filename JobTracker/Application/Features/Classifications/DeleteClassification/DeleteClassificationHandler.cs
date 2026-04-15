using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Classifications.DeleteClassification;

[ExportTsInterface]
public record DeleteClassificationRequest(int ClassificationId);
[ExportTsInterface]
public record DeleteClassificationResponse(bool Success);

public class DeleteClassificationHandler
    : RpcHandler<DeleteClassificationRequest, DeleteClassificationResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    public override string Command => "classifications.delete";

    public DeleteClassificationHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<DeleteClassificationResponse> HandleAsync(DeleteClassificationRequest request)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var classification = await db.Classifications
            .Include(c => c.Prototypes)
            .FirstOrDefaultAsync(c => c.Id == request.ClassificationId);

        if (classification == null)
            return new DeleteClassificationResponse(false);

        db.Classifications.Remove(classification);
        await db.SaveChangesAsync();

        return new DeleteClassificationResponse(true);
    }
}