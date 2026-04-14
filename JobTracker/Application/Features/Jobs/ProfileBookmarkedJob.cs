using JobTracker.Application.Features.System.Profiles;

namespace JobTracker.Application.Features.Jobs;

public class ProfileBookmarkedJob
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int PostingId { get; set; }
    public DateTime BookmarkedAt { get; set; } = DateTime.UtcNow;

    public JobProfile Profile { get; set; } = null!;
}
