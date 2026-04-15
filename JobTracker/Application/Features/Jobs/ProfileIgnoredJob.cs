using JobTracker.Application.Features.Profiles;

namespace JobTracker.Application.Features.Jobs;

public class ProfileIgnoredJob
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int PostingId { get; set; }
    public bool SoftIgnore { get; set; } = false;
    public IgnoreReason? Reason { get; set; }
    public DateTime IgnoredAt { get; set; } = DateTime.UtcNow;

    public JobProfile Profile { get; set; } = null!;
}
