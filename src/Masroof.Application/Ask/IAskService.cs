namespace Masroof.Application.Ask;

/// <summary>
/// Answers questions by giving the LLM a catalog of safe, user-scoped tools (not raw data)
/// and letting it call them. The final answer is written in the user's language.
/// </summary>
public interface IAskService
{
    Task<AskResponse> AskAsync(AskRequest request, CancellationToken ct);
}
