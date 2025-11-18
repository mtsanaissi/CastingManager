using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CastingManager.Core.Retry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CastingManager.Tests.Retry;

[TestClass]
public class CastingRetryOrchestratorTests
{
    [TestMethod]
    public async Task ExecuteAsync_CompletesWhenAttemptEventuallySucceeds()
    {
        var delayProvider = new FakeDelayProvider();
        var orchestrator = new CastingRetryOrchestrator(delayProvider);
        var options = new CastingRetryOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(10),
            MaxDelay = TimeSpan.FromMilliseconds(50),
            BackoffFactor = 2,
            MaxAttempts = 5
        };

        var result = await orchestrator.ExecuteAsync(async (attempt, _) => await Task.FromResult(attempt == 3), options);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(3, result.AttemptCount);
        Assert.AreEqual(2, delayProvider.RequestedDelays.Count);
        Assert.AreEqual(TimeSpan.FromMilliseconds(20), delayProvider.RequestedDelays[0]);
        Assert.AreEqual(TimeSpan.FromMilliseconds(40), delayProvider.RequestedDelays[1]);
    }

    [TestMethod]
    public async Task ExecuteAsync_StopsAfterMaxAttemptsWhenNoSuccess()
    {
        var delayProvider = new FakeDelayProvider();
        var orchestrator = new CastingRetryOrchestrator(delayProvider);
        var options = new CastingRetryOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(5),
            MaxDelay = TimeSpan.FromMilliseconds(5),
            BackoffFactor = 1,
            MaxAttempts = 3,
            UseExponentialBackoff = false
        };

        var result = await orchestrator.ExecuteAsync(static (_, __) => Task.FromResult(false), options);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(3, result.AttemptCount);
        Assert.AreEqual(2, delayProvider.RequestedDelays.Count);
        Assert.IsTrue(delayProvider.RequestedDelays.All(delay => delay == TimeSpan.FromMilliseconds(5)));
    }

    [TestMethod]
    public async Task ExecuteAsync_AllowsInfiniteAttemptsWhenMaxIsZero()
    {
        var delayProvider = new FakeDelayProvider();
        var orchestrator = new CastingRetryOrchestrator(delayProvider);
        var options = new CastingRetryOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(1),
            BackoffFactor = 1,
            MaxAttempts = 0,
            UseExponentialBackoff = false
        };

        var result = await orchestrator.ExecuteAsync(async (attempt, _) => await Task.FromResult(attempt >= 4), options);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(4, result.AttemptCount);
        Assert.AreEqual(3, delayProvider.RequestedDelays.Count);
    }

    private sealed class FakeDelayProvider : IDelayProvider
    {
        public List<TimeSpan> RequestedDelays { get; } = new();

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            RequestedDelays.Add(delay);
            return Task.CompletedTask;
        }
    }
}
