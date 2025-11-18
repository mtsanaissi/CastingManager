namespace CastingManager.Core.Retry;

public sealed class CastingRetryOptions
{
    private const double MinBackoffFactor = 1.0;

    public int MaxAttempts { get; init; } = 0; // 0 => infinite

    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(3);

    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);

    public double BackoffFactor { get; init; } = 1.5;

    public bool UseExponentialBackoff { get; init; } = true;

    public TimeSpan GetDelayForAttempt(int attempt)
    {
        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Attempt count must be at least 1.");
        }

        if (!UseExponentialBackoff || BackoffFactor <= MinBackoffFactor)
        {
            return InitialDelay;
        }

        var exponent = attempt - 1;
        var delayMs = InitialDelay.TotalMilliseconds * Math.Pow(BackoffFactor, exponent);
        delayMs = Math.Min(delayMs, MaxDelay.TotalMilliseconds);
        delayMs = Math.Max(delayMs, 0);
        return TimeSpan.FromMilliseconds(delayMs);
    }
}
