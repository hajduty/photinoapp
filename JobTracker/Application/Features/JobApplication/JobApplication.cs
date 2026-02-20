using JobTracker.Application.Features.JobSearch;

namespace JobTracker.Application.Features.JobApplication;

public class JobApplication
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Posting Posting { get; set; } = null!;
    public string CoverLetter { get; set; } = null!;
    public DateTime AppliedAt { get; set; }
    public DateTime? LastStatusChangeAt { get; set; }
    public ApplicationStatus Status { get; set; }
    //public List<Mails> RelatedMails
}

public enum ApplicationStatus
{
    Submitted = 0,
    Interview = 1,
    Offer = 2,
    Accepted = 3,
    Rejected = 4,
    Ghosted = 5
}