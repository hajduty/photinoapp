using Microsoft.ML.OnnxRuntimeGenAI;
using System.Diagnostics;
using System.Text;

namespace Services;

public class QwenService : IDisposable
{
    private readonly Model _model;
    private readonly Tokenizer _tokenizer;

    public QwenService(string modelPath = "Models/qwen-2.5-0.5b")
    {
        try
        {
            var config = new Config(modelPath);
            config.AppendProvider("DmlExecutionProvider");

            _model = new Model(config);
            Debug.WriteLine("Using DirectML (GPU)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GPU init failed: {ex.Message}");
            Debug.WriteLine("Falling back to CPU");

            _model = new Model(modelPath);
        }

        _tokenizer = new Tokenizer(_model);
    }

    public string Generate(
        string userPrompt,
        string? systemPrompt = null,
        int maxLength = 1024,
        float temperature = 0.2f,
        float topP = 0.9f)
    {
        var fullPrompt = BuildChatPrompt(userPrompt, systemPrompt);

        var sequences = _tokenizer.Encode(fullPrompt);

        var generatorParams = new GeneratorParams(_model);
        generatorParams.SetSearchOption("max_length", maxLength);
        generatorParams.SetSearchOption("temperature", temperature);
        generatorParams.SetSearchOption("top_p", topP);
        generatorParams.SetSearchOption("repetition_penalty", 1.1f);
        generatorParams.SetSearchOption("no_repeat_ngram_size", 3);

        using var generator = new Generator(_model, generatorParams);
        generator.AppendTokenSequences(sequences);

        var result = new StringBuilder();

        while (!generator.IsDone())
        {
            generator.GenerateNextToken();

            var outputSequences = generator.GetSequence(0);
            var newToken = outputSequences[^1];

            result.Append(_tokenizer.Decode(new[] { newToken }));
        }

        return result.ToString().Trim();
    }

    private static string BuildChatPrompt(string userPrompt, string? systemPrompt)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            sb.Append("<|im_start|>system\n");
            sb.Append(systemPrompt);
            sb.Append("<|im_end|>\n");
        }

        sb.Append("<|im_start|>user\n");
        sb.Append(userPrompt);
        sb.Append("<|im_end|>\n");

        sb.Append("<|im_start|>assistant\n");

        return sb.ToString();
    }

    public void Dispose()
    {
        _tokenizer?.Dispose();
        _model?.Dispose();
    }
}