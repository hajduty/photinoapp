using JobTracker.Application.Events;
using JobTracker.Application.Features.JobTracker;

namespace JobTracker.Application.Features.Jobs;

public record HighMatchJobsFoundEvent(
    int TrackerId,
    string Keyword,
    int JobCount,
    List<JobInfo> Jobs
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
