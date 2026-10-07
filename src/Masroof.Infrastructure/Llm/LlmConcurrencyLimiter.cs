namespace Masroof.Infrastructure.Llm;

/// <summary>Caps concurrent LLM calls to match GPU capacity (singleton SemaphoreSlim wrapper).</summary>
public sealed class LlmConcurrencyLimiter(int maxConcurrency) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(Math.Max(1, maxConcurrency));

    public async Task<IDisposable> AcquireAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        return new Releaser(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                semaphore.Release();
        }
    }
}
