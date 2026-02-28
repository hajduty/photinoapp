using Xunit;
using Xunit.Abstractions;
using Services;

namespace JobTracker.Tests;

public class QwenServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _modelsPath;
    private readonly QwenService? _qwen;

    public QwenServiceTests(ITestOutputHelper output)
    {
        _output = output;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _modelsPath = Path.Combine(baseDir, "Models", "Qwen");

        _output.WriteLine($"Looking for models in: {_modelsPath}");

        if (!Directory.Exists(_modelsPath))
        {
            _output.WriteLine("Model folder not found. Tests will be skipped.");
            return;
        }

        _qwen = new QwenService(_modelsPath);
    }

    [Fact]
    public void Classify_RejectionEmail_ReturnsRejection()
    {
        if (_qwen == null)
        {
            _output.WriteLine("Skipping test - Qwen not initialized");
            return;
        }

        var email = """
        Dear Hajder,
        After careful review, we have decided to move forward with another candidate.
        Thank you for your interest.
        """;

        var result = _qwen.Generate(
            userPrompt: $"Classify this email as: rejection, interview, offer, other.\n\n{email}",
            systemPrompt: "Return only one word.",
            temperature: 0.01f,
            maxLength: 2000
        );

        _output.WriteLine($"Model output: {result}");

        Assert.Contains("rejection", result.ToLower());
    }

    [Fact]
    public void Generate_LowTemperature_IsDeterministic()
    {
        if (_qwen == null)
        {
            _output.WriteLine("Skipping test - Qwen not initialized");
            return;
        }

        var prompt = "Say hello in one short sentence.";

        var result1 = _qwen.Generate(prompt, temperature: 0.01f, maxLength: 20);
        var result2 = _qwen.Generate(prompt, temperature: 0.01f, maxLength: 20);

        _output.WriteLine($"Run 1: {result1}");
        _output.WriteLine($"Run 2: {result2}");

        Assert.Equal(result1, result2);
    }

    public void Dispose()
    {
        _qwen?.Dispose();
    }
}