using JobTracker.Embeddings;
using JobTracker.Embeddings.Services;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace JobTracker.Tests.SentenceTests;

public class JinaMatchingTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _modelsPath;
    private readonly JinaEmbeddingService? _embeddingService;
    private readonly float[]? _rejectionVec;
    private readonly float[]? _interviewVec;
    private readonly float[]? _offerVec;

    public JinaMatchingTests(ITestOutputHelper output)
    {
        _output = output;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _modelsPath = Path.Combine(baseDir, "Models", "jina-embeddings-v5-text-nano-retrieval");

        _output.WriteLine($"Looking for models in: {_modelsPath}");

        if (!Directory.Exists(_modelsPath))
        {
            _output.WriteLine($"Model folder not found at: {_modelsPath}");
            _output.WriteLine("Tests will be skipped.");
            return;
        }

        _embeddingService = new JinaEmbeddingService(maxLength: 2048);

        _rejectionVec = AverageEmbeddings(Data.RejectionPrototypes);
        _interviewVec = AverageEmbeddings(Data.InterviewPrototypes);
        _offerVec = AverageEmbeddings(Data.OfferPrototypes);
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

        var simRejection = Helper.CosineSimilarity(emailVec, _rejectionVec);
        var simInterview = Helper.CosineSimilarity(emailVec, _interviewVec);
        var simOffer = Helper.CosineSimilarity(emailVec, _offerVec);

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
            _output.WriteLine("Skipping test embedding service not initialized");
            return;
        }

        var emailVec = _embeddingService.GenerateEmbeddingFloat("Query: "+ Data.InterviewEmailToClassify);

        var simRejection = Helper.CosineSimilarity(emailVec, _rejectionVec);
        var simInterview = Helper.CosineSimilarity(emailVec, _interviewVec);
        var simOffer = Helper.CosineSimilarity(emailVec, _offerVec);

        _output.WriteLine($"Rejection: {simRejection:F4}");
        _output.WriteLine($"Interview: {simInterview:F4}");
        _output.WriteLine($"Offer: {simOffer:F4}");

        var max = new[] { simRejection, simInterview, simOffer }.Max();
        Assert.Equal(simInterview, max);
    }

    [Fact]
    public void CVAndJob_ProduceSimilarEmbeddings()
    {
        if (_embeddingService == null)
        {
            _output.WriteLine("Skipping test - summarizer not initialized");
            return;
        }

        var jobDescription = "Document: Looking for backend developer with 10 years experience in .NET";

        var cv = "Query: XXX XXX Junior .NET Developer with ~1 year of experience";

        //var jobSummary = _embeddingService.ExtractSummary(jobDescription, maxSentences: 20);
        //var cvSummary = _embeddingService.ExtractSummary(cv, maxSentences: 20);

        var jobEmbedding = _embeddingService.GenerateEmbeddingFloat(jobDescription);
        var cvEmbedding = _embeddingService.GenerateEmbeddingFloat(cv);

        var similarity = jobEmbedding.Zip(cvEmbedding).Sum(p => p.First * p.Second);

        _output.WriteLine($"\nCosine Similarity: {similarity:F4}");

        Assert.True(similarity > 0.5f, $"Expected high similarity for matching CV/job, got {similarity:F4}");
    }

    private float[] AverageEmbeddings(IEnumerable<string> texts)
    {
        var list = texts.Select(t => _embeddingService!.GenerateEmbeddingFloat("Document: " + t)).ToList();
        int length = list[0].Length;
        var avg = new float[length];

        foreach (var vec in list)
            for (int i = 0; i < length; i++)
                avg[i] += vec[i];

        for (int i = 0; i < length; i++)
            avg[i] /= list.Count;

        return avg;
    }

    [Fact]
    public void JobIsSenior()
    {
        if (_embeddingService == null)
        {
            _output.WriteLine("Skipping test - summarizer not initialized");
            return;
        }

        var jobDescription = "Document: Marshall Group is the audio, tech, and design powerhouse uniting musicians and music lovers through genre-breaking innovation. Our flagship brand, Marshall, is uniquely positioned with over 60 years of rock 'n' roll attitude on stage, at home, and on the go. Our iconic products are brought to life by a dedicated team of 800 passionate employees and sold in over 90 markets worldwide. \r\n\r\nRight now, we need to strengthen our Software team with a Senior iOS Engineer who will help shape the next generation of the Marshall app, delivering high-quality features, strengthening our mobile foundations, and raising the bar for engineering excellence across the team. \r\n\r\n\r\n\r\n \r\n\r\n\r\n\r\nWhat you'll do: \r\n\r\n\r\n\r\nAs Senior iOS Engineer, you will design, build, and ship features for the next-generation Marshall mobile app while contributing to the shared architectural foundations that power our entire mobile ecosystem. You will play a key role in consolidating multiple apps into a single, scalable codebase, improving code quality, and accelerating delivery. Working hand-in-hand with Product, Design, Android, Backend, Firmware, and QA, your work will help create a reliable, world-class digital experience that reaches fans across every market. \r\n\r\n\r\n\r\n \r\n\r\n\r\n\r\nRole & Responsibilities: \r\n\r\n\r\n\r\nDeliver high-quality iOS features end-to-end using Swift and SwiftUI, with a focus on performance, reliability, and user experience. \r\n\r\n\r\nContribute to the consolidation of multiple apps into a unified, scalable codebase, helping unwind legacy implementations along the way. \r\n\r\n\r\nImprove and maintain architectural foundations, modularisation, and shared components that support cross-platform reuse, including Kotlin Multiplatform. \r\n\r\n\r\nWrite clean, maintainable code with strong unit and automated test coverage, and champion modern testing practices across the team. \r\n\r\n\r\nCollaborate closely with Product, Design, Android, Backend, Firmware, and QA to deliver cohesive, global experiences. \r\n\r\n\r\nMentor and support other engineers, raising the overall technical quality and engineering culture of the team. \r\n\r\n\r\nParticipate in design discussions, code reviews, and technical decision-making to ensure consistent, high-quality implementation. \r\n\r\n\r\n \r\n\r\n\r\n\r\nWho we're looking for: \r\n\r\n\r\n\r\nIn this role, you combine hands-on engineering excellence with technical leadership and a genuine interest in building products people love. You write code you're proud of, care about the craft, and bring curiosity and clarity to every cross-functional collaboration. You report to the app development manager and work within the Software team as part of our broader Product organisation. We're looking for someone who takes ownership, raises the people around them, and thrives at the intersection of great technology and meaningful user experiences. \r\n\r\n\r\n\r\n \r\n\r\n\r\n\r\nYou probably have the following experiences & skills: \r\n\r\n\r\n\r\nSeveral years of professional experience developing iOS applications using Swift. \r\n\r\n\r\nDemonstrated success delivering complex mobile features end-to-end in cross-functional teams. \r\n\r\n\r\nExperience working with SwiftUI, modern concurrency (async/await), Combine, or similar paradigms. \r\n\r\n\r\nExperience contributing to architectural foundations, modularisation, or multi-module iOS apps. \r\n\r\n\r\nExperience collaborating closely with design, product, QA, hardware/firmware, and backend teams. \r\n\r\n\r\nExperience with Bluetooth / BLE integrations or connected device ecosystems is a strong advantage. \r\n\r\n\r\nStrong problem-solving and technical design skills, with high standards for maintainability and craftsmanship. \r\n\r\n\r\nA user-centric mindset that balances technical considerations with business and product needs. \r\n\r\n\r\n\r\n\r\nOur pledge: \r\n\r\nWe strive to foster an inclusive workplace and we do not discriminate on the basis of race, religion, disability, colour, national origin, gender, sexual orientation, age or marital status. We firmly believe that Marshall thrives when our employees do, leading to better experiences for our consumers. \r\n\r\n";

        var cv = "Query: This is a job description for a junior role";

        //var jobSummary = _embeddingService.ExtractSummary(jobDescription, maxSentences: 20);
        //var cvSummary = _embeddingService.ExtractSummary(cv, maxSentences: 20);

        var jobEmbedding = _embeddingService.GenerateEmbeddingFloat(jobDescription);
        var cvEmbedding = _embeddingService.GenerateEmbeddingFloat(cv);

        var similarity = jobEmbedding.Zip(cvEmbedding).Sum(p => p.First * p.Second);

        _output.WriteLine($"\nCosine Similarity: {similarity:F4}");

        Assert.True(similarity > 0.5f, $"Expected high similarity for matching CV/job, got {similarity:F4}");
    }

    public void Dispose()
    {
        _embeddingService?.Dispose();
    }
}
