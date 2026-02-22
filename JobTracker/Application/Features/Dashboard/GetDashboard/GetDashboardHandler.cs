using JobTracker.Application.Features.JobApplication;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Dashboard.GetInfo;

public record GetDashboardResponse(
    int TotalApplications,
    int AppsThisMonth,
    float ResponseRate,
    int AvgResponseDays,
    int AvgRejectionDays,
    int JobsInReview,
    int JobsInterviewStage);

internal class GetDashboardHandler : RpcHandler<NoRequest, GetDashboardResponse>
{
    public override string Command => "dashboard.getInfo";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GetDashboardHandler(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<GetDashboardResponse> HandleAsync(NoRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var applications = await db.JobApplications.ToListAsync();

        int totalApplications = applications.Count;

        var now = DateTime.Now;

        int appsThisMonth = applications.Count(a =>
            a.AppliedAt.Year == now.Year &&
            a.AppliedAt.Month == now.Month);

        // Response Rate
        var respondedStatuses = new[]
        {
            ApplicationStatus.Interview,
            ApplicationStatus.Offer,
            ApplicationStatus.Accepted,
            ApplicationStatus.Rejected
        };

        int respondedCount = applications.Count(a =>
            respondedStatuses.Contains(a.Status));

        float responseRate = totalApplications == 0
            ? 0
            : (float)respondedCount / totalApplications * 100f;

        // Jobs In Review
        int jobsInReview = applications.Count(a =>
            a.Status == ApplicationStatus.Pending ||
            a.Status == ApplicationStatus.Submitted);

        // Interview Stage
        int jobsInterviewStage = applications.Count(a =>
            a.Status == ApplicationStatus.Interview);

        // Average Response Days
        double avgResponseDaysDouble = applications
            .Where(a => respondedStatuses.Contains(a.Status))
            .Select(a =>
            {
                var firstResponse = a.StatusHistory?
                    .Where(s => respondedStatuses.Contains(s.Status))
                    .OrderBy(s => s.ChangedAt)
                    .FirstOrDefault();

                if (firstResponse == null)
                    return (int?)null;

                return (int)(firstResponse.ChangedAt - a.AppliedAt).TotalDays;
            })
            .Where(days => days.HasValue)
            .Select(days => days!.Value)
            .DefaultIfEmpty(0)
            .Average();

        int avgResponseDays = (int)Math.Round(avgResponseDaysDouble);

        // Average rejection days
        var rejectionDurations = applications
            .Select(a =>
            {
                var rejection = a.StatusHistory
                    .Where(s => s.Status == ApplicationStatus.Rejected)
                    .OrderBy(s => s.ChangedAt)
                    .FirstOrDefault();

                return rejection == null
                    ? (double?)null
                    : (rejection.ChangedAt - a.AppliedAt).TotalDays;
            })
            .Where(d => d.HasValue)
            .Select(d => d.Value)
            .ToList();

        int avgRejectionDays = rejectionDurations.Count == 0
            ? 0
            : (int)Math.Round(rejectionDurations.Average());

        return new GetDashboardResponse(
            totalApplications,
            appsThisMonth,
            responseRate,
            avgResponseDays,
            avgRejectionDays,
            jobsInReview,
            jobsInterviewStage
        );
    }
}
