using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Classifications.GetPrototypes;

[ExportTsInterface]
public record GetPrototypesByClassificationRequest(int ClassificationId);
[ExportTsInterface]
public record GetPrototypesByClassificationResponse(List<Prototype> Prototypes);

public class GetPrototypesByClassificationHandler
    : RpcHandler<GetPrototypesByClassificationRequest, GetPrototypesByClassificationResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    public override string Command => "prototype.getByClassification";

    public GetPrototypesByClassificationHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<GetPrototypesByClassificationResponse> HandleAsync(GetPrototypesByClassificationRequest request)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var prototypes = await db.Prototypes
            .Where(p => p.ClassificationId == request.ClassificationId)
            .ToListAsync();

        return new GetPrototypesByClassificationResponse(prototypes);
    }
}