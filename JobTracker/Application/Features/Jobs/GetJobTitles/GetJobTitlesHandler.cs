using JobTracker.Application.Infrastructure.RPC;
using System.Diagnostics;
using System.Text.Json;

namespace JobTracker.Application.Features.JobSearch.GetJobTitles;

public record GetJobTitlesRequest(string keyword);
public record GetJobTitlesResponse(List<string> JobTitles);

public sealed class GetJobTitlesHandler : RpcHandler<GetJobTitlesRequest, GetJobTitlesResponse>
{
    public override string Command => "jobs.getTitles";

    private readonly HttpClient _httpClient;
    private readonly string apiUrl = "https://jobsearch.api.jobtechdev.se/";

    public GetJobTitlesHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    protected override async Task<GetJobTitlesResponse> HandleAsync(GetJobTitlesRequest request)
    {
        var query = $"complete?q={request.keyword}&limit=10&contextual=true";
        var response = await _httpClient.GetStringAsync(apiUrl + query);
        Console.WriteLine(query);
        Debug.WriteLine(query);

        var titles = Convert(response);
        return titles;
    }

    private GetJobTitlesResponse Convert(string json)
    {
        var doc = JsonDocument.Parse(json);
        var hits = doc.RootElement.GetProperty("typeahead").EnumerateArray();

        var orderedTitles = hits
            .Select(x => x.GetProperty("value").GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        return new GetJobTitlesResponse(orderedTitles);
    }
}
