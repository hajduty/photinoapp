using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Runtime.CompilerServices;
using Tokenizers.DotNet;

namespace JobTracker.Embeddings.Services;

public class JinaEmbeddingService : IDisposable
{
    private readonly object _lock = new object();
    private readonly int _maxLength = 2048;
    private readonly int _hiddenSize;
    private readonly bool _forceCpu;
    private readonly bool _enabled;
    private readonly long[] _inputIdsBuffer;
    private readonly long[] _attentionMaskBuffer;
    private readonly int[] _singleDimensions;
    private readonly string _modelPath;
    private readonly string _tokenizerPath;
    private readonly TimeSpan _idleTimeout;
    private readonly Timer _idleTimer;

    // These are now nullable and can be unloaded
    private InferenceSession? _session;
    private Tokenizer? _tokenizer;
    private DateTime _lastUsedTime;
    private bool _isDisposed;

    public bool Enabled => _enabled;

    public JinaEmbeddingService(
        int maxLength = 2048,
        bool forceCpu = false,
        TimeSpan? idleTimeout = null)
    {
        _maxLength = maxLength;
        _forceCpu = forceCpu;
        _idleTimeout = idleTimeout ?? TimeSpan.FromSeconds(30);

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JobTracker"
        );

        var modelsDir = Path.Combine(appDataDir, "Models", "jina-embeddings-v5-text-nano-classification");
        Directory.CreateDirectory(modelsDir);

        _modelPath = Path.Combine(modelsDir, "model.onnx");
        _tokenizerPath = Path.Combine(modelsDir, "tokenizer.json");

        if (!File.Exists(_modelPath) || !File.Exists(_tokenizerPath))
        {
            Console.WriteLine("Jina embeddings disabled (model files not found)");
            _enabled = false;
            return;
        }

        try
        {
            // Get hidden size without loading full model
            _hiddenSize = GetHiddenSizeFromModel();

            _inputIdsBuffer = new long[_maxLength];
            _attentionMaskBuffer = new long[_maxLength];
            _singleDimensions = new[] { 1, _maxLength };

            // Setup idle timer
            _idleTimer = new Timer(CheckIdle, null, _idleTimeout, _idleTimeout);

            _enabled = true;

            // Don't load model in constructor anymore
            Console.WriteLine("Jina embedding service initialized (model will load on first request)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Jina embeddings disabled: {ex.Message}");
            _enabled = false;
        }
    }

    private int GetHiddenSizeFromModel()
    {
        // Quick load just to get metadata, then unload
        var options = new SessionOptions();
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;

        using var tempSession = new InferenceSession(_modelPath, options);
        return tempSession.OutputMetadata.TryGetValue("last_hidden_state", out var metadata)
            ? (int)metadata.Dimensions[2]
            : 768;
    }

    private void EnsureLoaded()
    {
        if (!_enabled) return;

        lock (_lock)
        {
            if (_session != null)
            {
                _lastUsedTime = DateTime.UtcNow;
                return;
            }

            Console.WriteLine($"[{DateTime.Now:T}] Loading model into memory...");

            _tokenizer = new Tokenizer(_tokenizerPath);

            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            options.EnableCpuMemArena = true;
            options.EnableMemoryPattern = true;
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;

            options.AddSessionConfigEntry("session.intra_op.allow_spinning", "1");
            options.AddSessionConfigEntry("session.inter_op.allow_spinning", "1");
            options.AddSessionConfigEntry("session.set_denormal_as_zero", "1");

            if (!_forceCpu)
            {
                try
                {
                    options.AppendExecutionProvider_DML(0);
                    Console.WriteLine("Using GPU (DirectML)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GPU initialization failed: {ex.Message}, falling back to CPU");
                    options.AppendExecutionProvider_CPU(0);
                }
            }
            else
            {
                options.AppendExecutionProvider_CPU(0);
                Console.WriteLine("Using CPU");
            }

            _session = new InferenceSession(_modelPath, options);
            _lastUsedTime = DateTime.UtcNow;

            Warmup();
        }
    }

    private void CheckIdle(object? state)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            if (_session == null) return;

            var idleTime = DateTime.UtcNow - _lastUsedTime;
            if (idleTime > _idleTimeout)
            {
                Console.WriteLine($"[{DateTime.Now:T}] Unloading model after {idleTime.TotalMinutes:F1} minutes idle");

                _session?.Dispose();
                _session = null;

                // Tokenizer might need disposal depending on implementation
                _tokenizer = null;

                // Force GC to help release resources
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    private void Warmup()
    {
        try
        {
            GenerateEmbeddingInternal("warmup");
            Console.WriteLine("Model warmed up successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warmup failed: {ex.Message}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float[] GenerateEmbeddingInternal(string text)
    {
        if (!_enabled)
            return Array.Empty<float>();

        EnsureLoaded(); // This will load if needed

        // Safe to use null-forgiving operator because EnsureLoaded guarantees non-null if enabled
        uint[] encoded = _tokenizer!.Encode(text);
        int tokenCount = encoded.Length;
        if (tokenCount == 0)
        {
            throw new InvalidOperationException("Input produced no tokens after tokenization.");
        }

        int effectiveLength = Math.Min(tokenCount, _maxLength);

        // Clear buffers
        Array.Clear(_inputIdsBuffer, 0, _maxLength);
        Array.Clear(_attentionMaskBuffer, 0, _maxLength);

        // Fill buffers
        for (int i = 0; i < effectiveLength; i++)
        {
            _inputIdsBuffer[i] = encoded[i];
            _attentionMaskBuffer[i] = 1;
        }

        // Create tensors with proper dimensions
        var inputTensor = new DenseTensor<long>(_inputIdsBuffer, _singleDimensions);
        var maskTensor = new DenseTensor<long>(_attentionMaskBuffer, _singleDimensions);

        var inputs = new[]
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor)
        };

        using var results = _session!.Run(inputs);
        var tokenEmbeddings = results[0].AsTensor<float>();

        return LastTokenPooling(tokenEmbeddings, effectiveLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float[] LastTokenPooling(Tensor<float> embeddings, int effectiveLength)
    {
        float[] result = new float[_hiddenSize];

        int lastIdx = effectiveLength - 1;
        float sumSq = 0f;

        for (int j = 0; j < _hiddenSize; j++)
        {
            float val = embeddings[0, lastIdx, j];
            result[j] = val;
            sumSq += val * val;
        }

        float norm = (float)Math.Sqrt(sumSq);
        if (norm > 1e-12f)
        {
            float invNorm = 1f / norm;
            for (int j = 0; j < _hiddenSize; j++)
            {
                result[j] *= invNorm;
            }
        }

        return result;
    }

    public float[][] GenerateEmbeddingsBatch(string[] texts)
    {
        if (!_enabled)
            return Array.Empty<float[]>();

        if (texts == null || texts.Length == 0)
            return Array.Empty<float[]>();

        EnsureLoaded(); // Load if needed

        int count = texts.Length;
        var allEmbeddings = new float[count][];

        int batchSize = !_forceCpu ? 4 : 2; // Use GPU batch size if not forced to CPU

        for (int i = 0; i < count; i += batchSize)
        {
            int currentBatchSize = Math.Min(batchSize, count - i);
            ProcessBatch(texts, i, currentBatchSize, allEmbeddings);
        }

        return allEmbeddings;
    }

    private void ProcessBatch(string[] texts, int startIdx, int batchSize, float[][] allEmbeddings)
    {
        long[] flatInputs = new long[batchSize * _maxLength];
        long[] flatMasks = new long[batchSize * _maxLength];
        int[] lengths = new int[batchSize];

        try
        {
            for (int b = 0; b < batchSize; b++)
            {
                string text = "Passage: " + texts[startIdx + b];
                uint[] encoded = _tokenizer!.Encode(text);
                int len = Math.Min(encoded.Length, _maxLength);
                lengths[b] = len;

                int offset = b * _maxLength;
                for (int j = 0; j < len; j++)
                {
                    flatInputs[offset + j] = encoded[j];
                    flatMasks[offset + j] = 1;
                }
            }

            var batchInputTensor = new DenseTensor<long>(flatInputs, new[] { batchSize, _maxLength });
            var batchMaskTensor = new DenseTensor<long>(flatMasks, new[] { batchSize, _maxLength });

            var inputs = new[]
            {
                NamedOnnxValue.CreateFromTensor("input_ids", batchInputTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", batchMaskTensor)
            };

            using var results = _session!.Run(inputs);
            var allTokenEmbeddings = results[0].AsTensor<float>();

            // Process results
            for (int b = 0; b < batchSize; b++)
            {
                int effectiveLength = lengths[b];
                int lastIdx = effectiveLength - 1;

                float[] embedding = new float[_hiddenSize];
                float sumSq = 0f;

                for (int j = 0; j < _hiddenSize; j++)
                {
                    float val = allTokenEmbeddings[b, lastIdx, j];
                    embedding[j] = val;
                    sumSq += val * val;
                }

                float norm = (float)Math.Sqrt(sumSq);
                if (norm > 1e-12f)
                {
                    float invNorm = 1f / norm;
                    for (int j = 0; j < _hiddenSize; j++)
                    {
                        embedding[j] *= invNorm;
                    }
                }

                allEmbeddings[startIdx + b] = embedding;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing batch: {ex.Message}");
            throw;
        }
    }

    public byte[] GenerateEmbedding(string text)
    {
        float[] embedding = GenerateEmbeddingFloat(text);
        return Helper.ToBytes(embedding);
    }

    public float[] GenerateEmbeddingFloat(string text)
    {
        return GenerateEmbeddingInternal(text);
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            _idleTimer?.Dispose();
            _session?.Dispose();
            _session = null;
            _tokenizer = null;
            _isDisposed = true;
        }
    }
}