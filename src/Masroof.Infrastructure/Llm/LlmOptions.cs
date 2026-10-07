namespace Masroof.Infrastructure.Llm;

/// <summary>Binds the <c>Llm</c> configuration section (endpoint, models, limits).</summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>Base URL of the Ollama (or OpenAI-compatible) runtime.</summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>Model used for parsing, e.g. "qwen3:8b".</summary>
    public string ParseModel { get; set; } = "qwen3:8b";

    /// <summary>Model used for the Ask flow (may be larger), defaults to the parse model.</summary>
    public string AskModel { get; set; } = "qwen3:8b";

    /// <summary>Embedding model, e.g. "bge-m3".</summary>
    public string EmbedModel { get; set; } = "bge-m3";

    /// <summary>bge-m3 produces 1024-dim vectors.</summary>
    public int EmbeddingDimensions { get; set; } = 1024;

    /// <summary>Per-attempt timeout in seconds. Generous enough to absorb a cold model load
    /// (the first call after startup loads the model into VRAM), which can take ~20-40s.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Max concurrent LLM calls, to match GPU capacity.</summary>
    public int MaxConcurrency { get; set; } = 4;
}
