namespace CastingManager.Core.Retry;

public sealed class RetryAttemptEventArgs : EventArgs
{
    public RetryAttemptEventArgs(int attemptNumber, TimeSpan scheduledDelay)
    {
        AttemptNumber = attemptNumber;
        ScheduledDelay = scheduledDelay;
    }

    public int AttemptNumber { get; }

    public TimeSpan ScheduledDelay { get; }
}
