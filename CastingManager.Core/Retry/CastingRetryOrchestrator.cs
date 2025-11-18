namespace CastingManager.Core.Retry;

public sealed class CastingRetryOrchestrator
{
    private readonly IDelayProvider _delayProvider;

    public CastingRetryOrchestrator(IDelayProvider? delayProvider = null)
    {
        _delayProvider = delayProvider ?? new SystemDelayProvider();
    }

    public event EventHandler<RetryAttemptEventArgs>? AttemptScheduled;

    public async Task<RetryResult> ExecuteAsync(
        Func<int, CancellationToken, Task<bool>> attemptAsync,
        CastingRetryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attemptAsync);
        ArgumentNullException.ThrowIfNull(options);

        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            var isSuccess = await attemptAsync(attempt, cancellationToken).ConfigureAwait(false);
            if (isSuccess)
            {
                return new RetryResult(true, attempt);
            }

            if (options.MaxAttempts > 0 && attempt >= options.MaxAttempts)
            {
                break;
            }

            var delay = options.GetDelayForAttempt(attempt + 1);
            AttemptScheduled?.Invoke(this, new RetryAttemptEventArgs(attempt, delay));
            await _delayProvider.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
        }

        return new RetryResult(false, attempt);
    }
}
