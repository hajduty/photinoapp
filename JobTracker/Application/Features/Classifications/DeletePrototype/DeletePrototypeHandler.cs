using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Classifications.DeletePrototype;
[ExportTsInterface]
public record DeletePrototypeRequest(int PrototypeId);
[ExportTsInterface]
public record DeletePrototypeResponse(bool Success);

public class DeletePrototypeHandler
    : RpcHandler<DeletePrototypeRequest, DeletePrototypeResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    public override string Command => "prototype.delete";

    public DeletePrototypeHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<DeletePrototypeResponse> HandleAsync(DeletePrototypeRequest request)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var prototype = await db.Prototypes
            .FirstOrDefaultAsync(p => p.Id == request.PrototypeId);

        if (prototype == null)
            return new DeletePrototypeResponse(false);

        db.Prototypes.Remove(prototype);
        await db.SaveChangesAsync();

        return new DeletePrototypeResponse(true);
    }
}