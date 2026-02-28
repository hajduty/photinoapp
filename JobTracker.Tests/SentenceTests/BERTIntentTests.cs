using Xunit;
using Xunit.Abstractions;
using Services;

namespace JobTracker.Tests.SentenceTests;

public class SummarizeTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _modelsPath;
    private readonly BertService? _embeddingService;
    private readonly float[]? _rejectionVec;
    private readonly float[]? _interviewVec;
    private readonly float[]? _offerVec;

    public SummarizeTest(ITestOutputHelper output)
    {
        _output = output;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _modelsPath = Path.Combine(baseDir, "Models", "Summarizer");

        _output.WriteLine($"Looking for models in: {_modelsPath}");

        if (!Directory.Exists(_modelsPath))
        {
            _output.WriteLine($"Model folder not found at: {_modelsPath}");
            _output.WriteLine("Tests will be skipped.");
            return;
        }

        _embeddingService = new BertService();

        _rejectionVec = AverageEmbeddings(Data.RejectionPrototypes);
        _interviewVec = AverageEmbeddings(Data.InterviewPrototypes);
        _offerVec = AverageEmbeddings(Data.OfferPrototypes);
    }

    [Fact]
    public void Summarize_CVAndJob_ProduceSimilarEmbeddings()
    {
        if (_embeddingService == null)
        {
            _output.WriteLine("Skipping test - summarizer not initialized");
            return;
        }

        var jobDescription = "";

        var cv = "";
        
        //var jobSummary = _embeddingService.ExtractSummary(jobDescription, maxSentences: 20);
        //var cvSummary = _embeddingService.ExtractSummary(cv, maxSentences: 20);

        var jobEmbedding = _embeddingService.GenerateEmbeddingFloat(jobDescription);
        var cvEmbedding = _embeddingService.GenerateEmbeddingFloat(cv);

        var similarity = jobEmbedding.Zip(cvEmbedding).Sum(p => p.First * p.Second);

        //_output.WriteLine($"Job Summary:\n{jobSummary}");
        //_output.WriteLine($"\nCV Summary:\n{cvSummary}");
        _output.WriteLine($"\nCosine Similarity: {similarity:F4}");

        Assert.True(similarity > 0.5f, $"Expected high similarity for matching CV/job, got {similarity:F4}");
    }

    [Fact]
    public void ClassifyEmail_WithEmbeddings_ReturnsRejection()
    {
        if (_embeddingService == null)
        {
            _output.WriteLine("Skipping test embedding service not initialized");
            return;
        }

        // Generate averaged embeddings
        var emailVec = _embeddingService.GenerateEmbeddingFloat(Data.RejectionEmailToClassify);

        var simRejection = BertService.CosineSimilarity(emailVec, _rejectionVec);
        var simInterview = BertService.CosineSimilarity(emailVec, _interviewVec);
        var simOffer = BertService.CosineSimilarity(emailVec, _offerVec);

        _output.WriteLine($"Rejection: {simRejection:F4}");
        _output.WriteLine($"Interview: {simInterview:F4}");
        _output.WriteLine($"Offer: {simOffer:F4}");

        var max = new[] { simRejection, simInterview, simOffer }.Max();

        Assert.Equal(simRejection, max);
    }

    [Fact]
    public void Classify_OfferEmail_WithEmbeddings_ReturnsOffer()
    {
        if (_embeddingService == null)
        {
            _output.WriteLine("Skipping test - embedding service not initialized");
            return;
        }

        var emailVec = _embeddingService.GenerateEmbeddingFloat(Data.OfferEmailToClassify);

        var simRejection = BertService.CosineSimilarity(emailVec, _rejectionVec);
        var simInterview = BertService.CosineSimilarity(emailVec, _interviewVec);
        var simOffer = BertService.CosineSimilarity(emailVec, _offerVec);

        _output.WriteLine($"Rejection: {simRejection:F4}");
        _output.WriteLine($"Interview: {simInterview:F4}");
        _output.WriteLine($"Offer: {simOffer:F4}");

        var max = new[] { simRejection, simInterview, simOffer }.Max();
        Assert.Equal(simOffer, max);
    }

    private float[] AverageEmbeddings(IEnumerable<string> texts)
    {
        var list = texts.Select(t => _embeddingService!.GenerateEmbeddingFloat(t)).ToList();
        int length = list[0].Length;
        var avg = new float[length];

        foreach (var vec in list)
            for (int i = 0; i < length; i++)
                avg[i] += vec[i];

        for (int i = 0; i < length; i++)
            avg[i] /= list.Count;

        return avg;
    }

    public void Dispose()
    {
        _embeddingService?.Dispose();
    }
}