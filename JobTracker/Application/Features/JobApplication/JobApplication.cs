using JobTracker.Application.Features.JobSearch;

namespace JobTracker.Application.Features.JobApplication;

public class JobApplication
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int JobId { get; set; }
    public Posting Posting { get; set; } = null!;
    public string CoverLetter { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
    public DateTime? LastStatusChangeAt { get; set; }
    public ApplicationStatus Status { get; set; }
    public List<ApplicationStatusHistory> StatusHistory { get; set; } = new();
    //public List<Mails> RelatedMails
}

public enum ApplicationStatus
{
    Pending = 0,
    Submitted = 1,
    Interview = 2,
    Offer = 3,
    Accepted = 4,
    Rejected = 5,
    Ghosted = 6
}