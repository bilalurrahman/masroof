using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Taxonomy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace Masroof.Infrastructure.Llm;

/// <summary>
/// Parses a message with Ollama using schema-constrained output (<c>format</c>). Transport
/// failures surface as <see cref="LlmUnavailableException"/>; unusable output comes back as a
/// failed <see cref="ParseResult"/> with the raw text for the trace.
/// </summary>
public sealed class OllamaTransactionParser(
    HttpClient http,
    PromptBuilder prompts,
    IClock clock,
    LlmConcurrencyLimiter limiter,
    IOptions<LlmOptions> options,
    ILogger<OllamaTransactionParser> logger)
    : ILlmTransactionParser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly LlmOptions _options = options.Value;

    public async Task<ParseResult> ParseAsync(string text, IReadOnlyList<string> hints, string defaultCurrency, CancellationToken ct)
    {
        var model = _options.ParseModel;
        var system = prompts.Build(hints, defaultCurrency, clock.Today);
        var request = new
        {
            model,
            stream = false,
            think = false,
            format = TransactionSchema.Build(),
            options = new { temperature = 0, num_predict = _options.ParseNumPredict },
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = text }
            }
        };

        var sw = Stopwatch.StartNew();
        string? rawContent = null;

        using var gate = await limiter.AcquireAsync(ct);
        try
        {
            using var response = await http.PostAsJsonAsync("/api/chat", request, Json, ct);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await SafeReadAsync(response, ct);
                throw new LlmUnavailableException($"Ollama returned {(int)response.StatusCode}: {detail}");
            }

            var chat = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(Json, ct);
            sw.Stop();
            rawContent = chat?.Message?.Content;

            if (string.IsNullOrWhiteSpace(rawContent))
                return Failed(model, (int)sw.ElapsedMilliseconds, rawContent, "Empty content from model.");

            // Schema-constrained models return a bare JSON object; reasoning models (glm-*) wrap
            // it in <think>…</think> and/or a ```json fence with prose around it. Try the content
            // as-is first, then fall back to extracting the trailing JSON object.
            var dto = TryBind(rawContent) ?? TryBind(ExtractJsonObject(rawContent));
            if (dto is null)
                return Failed(model, (int)sw.ElapsedMilliseconds, rawContent, "Content did not deserialize.");

            var parsed = Map(dto);
            return new ParseResult(true, parsed, model, PromptBuilder.Version, rawContent, (int)sw.ElapsedMilliseconds, null);
        }
        catch (JsonException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Failed to bind Ollama output.");
            return Failed(model, (int)sw.ElapsedMilliseconds, rawContent, ex.Message);
        }
        catch (TimeoutRejectedException ex)
        {
            // Polly's resilience timeout (per-attempt or total) — treat as the LLM being slow/down
            // so the parse pipeline can fall back to a pre-parsed pending row instead of 500-ing.
            throw new LlmUnavailableException("The LLM request timed out.", ex);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new LlmUnavailableException("The LLM request timed out.");
        }
        catch (HttpRequestException ex)
        {
            throw new LlmUnavailableException("The LLM runtime is unreachable.", ex);
        }
    }

    private static ParsedTransaction Map(ParsedDto dto)
    {
        DateOnly? date = null;
        if (!string.IsNullOrWhiteSpace(dto.Date)
            && DateOnly.TryParse(dto.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            date = d;

        return new ParsedTransaction(
            Direction: dto.Direction?.Trim().ToLowerInvariant() ?? "debit",
            Amount: dto.Amount,
            Currency: string.IsNullOrWhiteSpace(dto.Currency) ? "SAR" : dto.Currency.Trim().ToUpperInvariant(),
            Counterparty: NullIfBlank(dto.Counterparty),
            Channel: NullIfBlank(dto.Channel),
            AccountLast4: NullIfBlank(dto.AccountLast4),
            Date: date,
            Category: CategoryCodes.ResolveOrOther(dto.Category),
            Confidence: Math.Clamp(dto.Confidence, 0.0, 1.0));
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static ParsedDto? TryBind(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try { return JsonSerializer.Deserialize<ParsedDto>(json, Json); }
        catch (JsonException) { return null; }
    }

    /// <summary>
    /// Pulls the FIRST complete JSON object out of a reasoning model's answer: drops everything up
    /// to the last <c>&lt;/think&gt;</c>, then brace-balances from the first '{' to its matching
    /// '}' (ignoring braces inside strings). Taking the first balanced object — not first-'{' to
    /// last-'}' — is important because these models sometimes emit two objects back to back.
    /// </summary>
    private static string? ExtractJsonObject(string content)
    {
        var s = content;
        var thinkEnd = s.LastIndexOf("</think>", StringComparison.OrdinalIgnoreCase);
        if (thinkEnd >= 0)
            s = s[(thinkEnd + "</think>".Length)..];

        var start = s.IndexOf('{');
        if (start < 0)
            return null;

        bool inString = false, escaped = false;
        var depth = 0;
        for (var i = start; i < s.Length; i++)
        {
            var ch = s[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (ch == '\\') escaped = true;
                else if (ch == '"') inString = false;
            }
            else if (ch == '"') inString = true;
            else if (ch == '{') depth++;
            else if (ch == '}' && --depth == 0)
                return s[start..(i + 1)];
        }
        return null;
    }

    private static ParseResult Failed(string model, int ms, string? raw, string error) =>
        new(false, null, model, PromptBuilder.Version, raw, ms, error);

    private static async Task<string> SafeReadAsync(HttpResponseMessage r, CancellationToken ct)
    {
        try { return await r.Content.ReadAsStringAsync(ct); }
        catch { return "(no body)"; }
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class ParsedDto
    {
        public string? Direction { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Counterparty { get; set; }
        public string? Channel { get; set; }
        public string? AccountLast4 { get; set; }
        public string? Date { get; set; }
        public string? Category { get; set; }
        public double Confidence { get; set; }
    }
}
