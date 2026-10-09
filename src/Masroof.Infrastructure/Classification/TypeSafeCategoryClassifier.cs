using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Masroof.Application.Abstractions;
using Masroof.Domain.Taxonomy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Masroof.Infrastructure.Classification;

/// <summary>
/// Categorizes a transaction message via TypeSafe's "choice" question, with the taxonomy codes
/// (and their <see cref="CategoryGuidance"/> descriptions) as the criteria. Returns the chosen
/// code and confidence. Multilingual — handles Arabic merchant names the local model misses.
/// Never throws: any transport/parse failure returns null so the pipeline falls back to the LLM.
/// </summary>
public sealed class TypeSafeCategoryClassifier(
    HttpClient http,
    IOptions<TypeSafeOptions> options,
    ILogger<TypeSafeCategoryClassifier> logger)
    : ICategoryClassifier
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly TypeSafeOptions _options = options.Value;

    // The criteria map (code -> description) is identical for every request; build it once.
    private static readonly IReadOnlyDictionary<string, string> Criteria =
        CategoryCodes.All.ToDictionary(code => code, code => CategoryGuidance.Descriptions[code]);

    public bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<CategorySuggestion?> ClassifyAsync(string text, CancellationToken ct)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text))
            return null;

        var request = new
        {
            state = text,
            model = _options.Model,
            questions = new
            {
                category = new
                {
                    type = "choice",
                    instructions = "Which spending category best fits this bank transaction message?",
                    criteria = Criteria
                }
            }
        };

        try
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = JsonContent.Create(request, options: Json)
            };
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var response = await http.SendAsync(msg, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("TypeSafe returned {Status}; falling back to the LLM.", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<TypeSafeResponse>(Json, ct);
            var answer = body?.Answers?.Category;
            if (answer?.Choice is null || !CategoryCodes.IsValid(answer.Choice))
                return null;

            return new CategorySuggestion(answer.Choice.Trim().ToLowerInvariant(), answer.Confidence ?? 0.0);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TypeSafe classification failed; falling back to the LLM.");
            return null;
        }
    }

    private sealed class TypeSafeResponse
    {
        [JsonPropertyName("answers")] public AnswersDto? Answers { get; set; }
    }

    private sealed class AnswersDto
    {
        [JsonPropertyName("category")] public ChoiceAnswer? Category { get; set; }
    }

    private sealed class ChoiceAnswer
    {
        [JsonPropertyName("choice")] public string? Choice { get; set; }
        [JsonPropertyName("confidence")] public double? Confidence { get; set; }
    }
}
