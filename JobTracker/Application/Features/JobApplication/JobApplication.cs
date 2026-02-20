using JobTracker.Application.Features.JobSearch;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Application.Features.JobApplication;

public class JobApplication
{
    [Key]
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
    Pending = 0,
    Submitted = 1,
    Interview = 2,
    Offer = 3,
    Accepted = 4,
    Rejected = 5,
    Ghosted = 6
}