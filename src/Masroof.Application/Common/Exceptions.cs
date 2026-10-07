namespace Masroof.Application.Common;

/// <summary>A requested entity does not exist (or is not visible to the current user). → 404.</summary>
public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.")
{
    public string Entity { get; } = entity;
    public object Key { get; } = key;
}

/// <summary>The same message was already saved for this user. → 409, with the existing id.</summary>
public sealed class DuplicateTransactionException(long existingTransactionId)
    : Exception($"Transaction already exists (id {existingTransactionId}).")
{
    public long ExistingTransactionId { get; } = existingTransactionId;
}

/// <summary>The LLM runtime was unreachable or timed out. → 502.</summary>
public sealed class LlmUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>The LLM returned output that could not be bound to a transaction. → 422.</summary>
public sealed class UnparseableLlmOutputException(string message, string? rawOutput = null)
    : Exception(message)
{
    public string? RawOutput { get; } = rawOutput;
}
