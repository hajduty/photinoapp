using JobTracker.Application.Events;

namespace JobTracker.Application.Features.JobTracker;

/// <summary>
/// Domain event raised when jobs are found during tracker processing
/// </summary>
public record JobsFoundEvent(
    int TrackerId,
    string Keyword,
    int JobCount,
    IReadOnlyList<JobInfo> Jobs
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record JobInfo(int Id, string Title, string Company);