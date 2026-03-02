using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Classifications.GetClassifications;

[ExportTsInterface]
public record GetAllClassificationsResponse(List<Classification> Classifications);

public class GetClassificationsHandler
    : RpcHandler<NoRequest, GetAllClassificationsResponse>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    public override string Command => "classifications.get";

    public GetClassificationsHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<GetAllClassificationsResponse> HandleAsync(NoRequest request)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var classifications = await db.Classifications
            .Include(c => c.Prototypes)
            .ToListAsync();

        return new GetAllClassificationsResponse(classifications);
    }
}