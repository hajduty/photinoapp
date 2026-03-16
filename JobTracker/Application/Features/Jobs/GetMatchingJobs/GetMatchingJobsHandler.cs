using JobTracker.Application.Features.System.Settings;
using JobTracker.Application.Features.Tags;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Embeddings;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace JobTracker.Application.Features.JobSearch.GetMatchingJobs;

public record GetMatchingJobsRequest(int Page);

public record GetMatchingJobsResponse(List<ExtendedPosting> Jobs);

public record ExtendedPosting
{
    public Posting Posting { get; init; } = null!;
    public List<Tag> Tags { get; init; } = null!;
}

public class GetMatchingJobsHandler : RpcHandler<GetMatchingJobsRequest, GetMatchingJobsResponse>
{
    public override string Command => "jobs.getMatchingJobs";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly JinaEmbeddingService _embeddingService;

    public GetMatchingJobsHandler(
        IDbContextFactory<AppDbContext> dbFactory,
        JinaEmbeddingService embeddingService)
    {
        _dbFactory = dbFactory;
        _embeddingService = embeddingService;
    }

    protected override async Task<GetMatchingJobsResponse> HandleAsync(GetMatchingJobsRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings == null)
            return new GetMatchingJobsResponse([]);

        var profileText = BuildUserProfile(settings);

        float[] userVector;
        if (settings.UserEmbedding == null)
        {
            userVector = _embeddingService.GenerateEmbeddingFloat(profileText);
            var settingsToUpdate = new Settings { Id = settings.Id };
            db.Settings.Attach(settingsToUpdate);
            settingsToUpdate.UserEmbedding = Helper.ToBytes(userVector);
            await db.SaveChangesAsync();
        }
        else
        {
            userVector = Helper.ToFloatArray(settings.UserEmbedding);
        }

        var maxAge = settings.MaxJobAgeDays ?? 30;

        // Exclude ignored jobs from candidates entirely
        var candidates = await db.Postings
            .AsNoTracking()
            .Where(p => p.Ignored != true && p.PostedDate >= DateTime.UtcNow.AddDays(-maxAge))
            .OrderByDescending(p => p.PostedDate)
            .Take(1000)
            .ToListAsync();

        var embeddings = await db.JobEmbeddings
            .AsNoTracking()
            .Where(e => candidates.Select(c => c.Id).Contains(e.JobId))
            .ToDictionaryAsync(e => e.JobId, e => e.EmbeddingData);

        // Build a centroid of all ignored job embeddings to use as a repulsion vector
        var negativeVector = await BuildNegativeVectorAsync(db);

        var allTags = await db.Tags
            .AsNoTracking()
            .ToListAsync();

        var tagRegexes = allTags.ToDictionary(
            t => t.Id,
            t => new Regex(CreateTagPattern(t.Name), RegexOptions.IgnoreCase | RegexOptions.Compiled)
        );

        var scored = new List<(Posting Job, List<Tag> Tags, float Score)>();

        foreach (var job in candidates)
        {
            var jobTags = ExtractTags(job, allTags, tagRegexes);

            if (!ContainsSelectedTag(jobTags, settings))
                continue;

            if (!PassesHardFilters(job, settings))
                continue;

            if (!embeddings.TryGetValue(job.Id, out var embeddingBytes))
                continue;

            var jobVector = Helper.ToFloatArray(embeddingBytes);

            float semantic = Helper.DotProductSimilarity(userVector, jobVector);
            float keywordBoost = KeywordBoost(job, settings);
            float freshnessBoost = FreshnessBoost(job);

            // Penalise jobs that are semantically similar to ignored ones
            float negativePenalty = negativeVector != null
                ? Helper.DotProductSimilarity(negativeVector, jobVector)
                : 0f;

            float score =
                semantic * 0.65f +
                keywordBoost * 0.25f +
                freshnessBoost * 0.10f -
                negativePenalty * 0.40f; // subtract similarity to ignored jobs

            scored.Add((job, jobTags, score));
        }

        var topJobs = scored
            .OrderByDescending(x => x.Score)
            .Take(15)
            .ToList();

        var extended = topJobs
            .Select(x => new ExtendedPosting
            {
                Posting = x.Job,
                Tags = x.Tags
            })
            .ToList();

        return new GetMatchingJobsResponse(extended);
    }

    /// <summary>
    /// Computes the mean embedding vector across all ignored postings.
    /// Returns null if there are no ignored jobs with embeddings.
    /// </summary>
    private static async Task<float[]?> BuildNegativeVectorAsync(AppDbContext db)
    {
        var ignoredIds = await db.Postings
            .AsNoTracking()
            .Where(p => p.Ignored == true)
            .Select(p => p.Id)
            .ToListAsync();

        if (ignoredIds.Count == 0)
            return null;

        var ignoredEmbeddings = await db.JobEmbeddings
            .AsNoTracking()
            .Where(e => ignoredIds.Contains(e.JobId))
            .Select(e => e.EmbeddingData)
            .ToListAsync();

        if (ignoredEmbeddings.Count == 0)
            return null;

        var first = Helper.ToFloatArray(ignoredEmbeddings[0]);
        var centroid = new float[first.Length];

        foreach (var bytes in ignoredEmbeddings)
        {
            var vec = Helper.ToFloatArray(bytes);
            for (int i = 0; i < centroid.Length; i++)
                centroid[i] += vec[i];
        }

        for (int i = 0; i < centroid.Length; i++)
            centroid[i] /= ignoredEmbeddings.Count;

        return centroid;
    }

    private static string BuildUserProfile(Settings settings)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(settings.UserCV))
            sb.AppendLine(settings.UserCV);

        if (settings.MatchedKeywords != null)
            sb.AppendLine(string.Join(" ", settings.MatchedKeywords));

        if (settings.SelectedTags != null)
            sb.AppendLine(string.Join(" ", settings.SelectedTags.Select(t => t.Name)));

        return sb.ToString();
    }

    private static bool PassesHardFilters(Posting job, Settings settings)
    {
        if (settings.YearsOfExperience is not null && job.YearsOfExperience is not null)
        {
            if (job.YearsOfExperience > settings.YearsOfExperience + 1)
                return false;
        }

        if (settings.BlockedKeywords != null)
        {
            foreach (var k in settings.BlockedKeywords)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;

                var pattern = $@"(?<![a-zA-Z0-9]){Regex.Escape(k)}(?![a-zA-Z0-9])";
                var rx = new Regex(pattern, RegexOptions.IgnoreCase);

                if (rx.IsMatch(job.Title ?? "") || rx.IsMatch(job.Description ?? ""))
                    return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.Location))
        {
            if (!job.Location.Contains(settings.Location, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static float KeywordBoost(Posting job, Settings settings)
    {
        if (settings.MatchedKeywords == null)
            return 0f;

        float boost = 0;

        foreach (var keyword in settings.MatchedKeywords)
        {
            if (job.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                boost += 0.2f;
            else if (job.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                boost += 0.1f;
        }

        return MathF.Min(boost, 0.5f);
    }

    private static float FreshnessBoost(Posting job)
    {
        var ageDays = (DateTime.UtcNow - job.PostedDate).TotalDays;

        if (ageDays <= 1) return 1f;
        if (ageDays <= 7) return 0.8f;
        if (ageDays <= 14) return 0.5f;
        if (ageDays <= 30) return 0.2f;

        return 0f;
    }

    private static List<ExtendedPosting> CreateExtended(
        List<Posting> jobs,
        List<Tag> allTags,
        Dictionary<int, Regex> regexes)
    {
        return jobs.Select(job =>
        {
            var tags = ExtractTags(job, allTags, regexes);

            return new ExtendedPosting
            {
                Posting = job,
                Tags = tags
            };
        }).ToList();
    }

    private static List<Tag> ExtractTags(
        Posting job,
        List<Tag> allTags,
        Dictionary<int, Regex> regexes)
    {
        var title = job.Title ?? "";
        var desc = job.Description ?? "";

        return allTags
            .Where(t =>
            {
                var rx = regexes[t.Id];
                return rx.IsMatch(title) || rx.IsMatch(desc);
            })
            .ToList();
    }

    private static bool ContainsSelectedTag(List<Tag> jobTags, Settings settings)
    {
        if (settings.SelectedTags == null || settings.SelectedTags.Count == 0)
            return true;

        var selectedIds = settings.SelectedTags
            .Select(t => t.Id)
            .ToHashSet();

        return jobTags.Any(t => selectedIds.Contains(t.Id));
    }

    private static string CreateTagPattern(string tagName)
    {
        var escaped = Regex.Escape(tagName);
        return $@"(?:^|[\s,;.!?()\[\]{{}}""'`])({escaped})(?:$|[\s,;.!?()\[\]{{}}""'`])";
    }
}