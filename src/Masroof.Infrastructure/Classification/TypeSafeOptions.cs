namespace Masroof.Infrastructure.Classification;

/// <summary>
/// Binds the <c>TypeSafe</c> config section. TypeSafe (typesafe.ai) is a hosted classification
/// API used, when enabled, for the category step. It is OFF by default: enabling it sends the
/// message text to an external service, which relaxes the app's zero-egress stance, so it is an
/// explicit opt-in. The API key is supplied via configuration/environment, never committed.
/// </summary>
public sealed class TypeSafeOptions
{
    public const string SectionName = "TypeSafe";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://api.typesafe.ai/v1/systemone";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "jev-latest";
    public int TimeoutSeconds { get; set; } = 20;
}
