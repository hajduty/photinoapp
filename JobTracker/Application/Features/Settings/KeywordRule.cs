using System.Text.Json.Serialization;

namespace JobTracker.Application.Features.Settings;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KeywordScope
{
    Both,
    TitleOnly,
    DescriptionOnly
}

public record KeywordRule(string Keyword, KeywordScope Scope = KeywordScope.Both);

public record RejectedTagRule(int TagId, KeywordScope Scope = KeywordScope.Both);
