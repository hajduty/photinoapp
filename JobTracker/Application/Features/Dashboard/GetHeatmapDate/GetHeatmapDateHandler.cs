using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JobTracker.Application.Features.Dashboard.GetHeatmap;

public record GetHeatmapDateRequest(DateTime Date);
public record GetHeatmapDateResponse(HeatmapJobData[] Jobs);
public record HeatmapJobData(int JobId, string JobTitle, string Company, string CompanyImage);

public class GetHeatmapDateHandler : RpcHandler<GetHeatmapDateRequest, GetHeatmapDateResponse>
{
    public override string Command => "dashboard.getHeatmapDate";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetHeatmapDateHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetHeatmapDateResponse> HandleAsync(GetHeatmapDateRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var date = request.Date.Date;

        var nextDate = request.Date.AddDays(1);

        var jobs = await db.JobApplications
            .Where(j => j.AppliedAt >= date && j.AppliedAt < nextDate)
            .Select(j => new HeatmapJobData(
                j.Posting.Id,
                j.Posting.Title,
                j.Posting.Company,
                j.Posting.CompanyImage
            ))
            .ToArrayAsync();

        return new GetHeatmapDateResponse(jobs);
    }
}
