namespace Masroof.Application.Ask;

/// <summary>A natural-language question over the user's own ledger.</summary>
public sealed record AskRequest(string Question);

/// <summary>Record of one tool the model invoked while answering, for auditability.</summary>
public sealed record ToolInvocation(string Tool, string Arguments, int ResultCount);

/// <summary>
/// The answer plus the evidence behind it: which safe tools ran and how many rows they
/// returned. There is no text-to-SQL, so the answer is always grounded in user-scoped data.
/// </summary>
public sealed record AskResponse(
    string Answer,
    IReadOnlyList<ToolInvocation> ToolsUsed);
