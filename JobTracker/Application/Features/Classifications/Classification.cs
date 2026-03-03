namespace JobTracker.Application.Features.Classifications;

public class Classification
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Prototype> Prototypes { get; set; } = new List<Prototype>();
    public string? Color { get; set; } = "#ffffff";
}