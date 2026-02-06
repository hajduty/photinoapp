using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Tags;

[Index(nameof(Name), IsUnique = true)]
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#ffffff";
}