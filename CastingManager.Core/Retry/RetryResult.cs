namespace CastingManager.Core.Retry;

public readonly record struct RetryResult(bool IsSuccess, int AttemptCount);
