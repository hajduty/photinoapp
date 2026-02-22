using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Dashboard.GetHeatmap;

public record GetHeatmapResponse(List<HeatmapData> Heatmaps);
public record HeatmapData(string Date, int Applications);

internal class GetHeatmapHandler : RpcHandler<NoRequest, GetHeatmapResponse>
{
    public override string Command => "dashboard.getHeatmap";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetHeatmapHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetHeatmapResponse> HandleAsync(NoRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var dateJobs = await db.JobApplications
            .GroupBy(j => j.AppliedAt.Date)
            .Select(g => new HeatmapData(
                g.Key.ToString("yyyy-MM-dd"),
                g.Count()))
            .ToListAsync();

        return new GetHeatmapResponse(dateJobs);
    }
}
