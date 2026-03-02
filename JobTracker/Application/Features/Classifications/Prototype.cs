using System.Text.Json.Serialization;

namespace JobTracker.Application.Features.Classifications;

public class Prototype
{
    public int Id { get; set; }
    public int ClassificationId { get; set; }
    [JsonIgnore]
    public Classification Classification { get; set; } = null!;
    public string Text { get; set; } = null!;
    public byte[] Embedding { get; set; } = null!;
}
