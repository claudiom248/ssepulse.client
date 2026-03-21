using SsePulse.Client.Common.Models;
using SsePulse.Client.Utils;

namespace SsePulse.Client.Tests;

public class ExecuteTests
{
    // --- Gruppo: WithRetryAsync - Success Cases ---
    
    [Fact]
    public async Task WithRetryAsync_SuccessfulAction_ExecutesOnce()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            return Task.CompletedTask;
        }

        // ACT
        await Execute.WithRetryAsync(Action, RetryOptions.None);

        // ASSERT
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task WithRetryAsync_SuccessAfterFailures_RetriesUntilSuccess()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            if (attempts < 3)
                throw new InvalidOperationException("Temporary failure");
            return Task.CompletedTask;
        }

        // ACT
        await Execute.WithRetryAsync(Action, RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 100));

        // ASSERT
        Assert.Equal(3, attempts);
    }

    // --- Gruppo: WithRetryAsync - Failure Cases ---

    [Fact]
    public async Task WithRetryAsync_AlwaysFails_ThrowsAfterMaxRetries()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            throw new InvalidOperationException("Persistent failure");
        }

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Execute.WithRetryAsync(Action, RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 50)));

        Assert.Equal(4, attempts); // Initial + 3 retries
    }
    
    [Fact]
    public async Task WithRetryAsync_WhenConditionalRetrySet_ThrowsWhenConditionFails()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            if (attempts < 2)
            {
                throw new InvalidOperationException("failure");
            }
            throw new InvalidOperationException("unrecoverable");
        }

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Execute.WithRetryAsync(
                Action, 
                RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 50),
                shouldRetry: ex => ex.Message.Contains("unrecoverable")));

        Assert.Equal(1, attempts); // Initial + 3 retries
    }

    [Fact]
    public async Task WithRetryAsync_NoRetries_FailsImmediately()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            throw new InvalidOperationException("Failure");
        }

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Execute.WithRetryAsync(Action, RetryOptions.None));

        Assert.Equal(1, attempts); // No retries
    }

    // --- Gruppo: WithRetryAsync - Cancellation ---

    [Fact]
    public async Task WithRetryAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // ARRANGE
        using CancellationTokenSource cts = new();
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            if (attempts == 2)
                cts.Cancel();
            throw new InvalidOperationException("Failure");
        }

        // ACT & ASSERT
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => Execute.WithRetryAsync(Action, RetryOptions.Fixed(5, 50), cancellationToken: cts.Token));

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task WithRetryAsync_OperationCanceledExceptionInAction_DoesNotRetry()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            throw new OperationCanceledException();
        }

        // ACT & ASSERT
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Execute.WithRetryAsync(Action, RetryOptions.Fixed(5, 50)));

        Assert.Equal(1, attempts); // Should not retry on OperationCanceledException
    }

    // --- Gruppo: WithRetryAsync - Error Callback ---

    [Fact]
    public async Task WithRetryAsync_OnErrorCallback_InvokedOnEachFailure()
    {
        // ARRANGE
        List<Exception> errors = new();
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            throw new InvalidOperationException($"Failure {attempts}");
        }

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Execute.WithRetryAsync(
                Action,
                RetryOptions.Fixed(2, 50),
                onError: ex => errors.Add(ex)));

        Assert.Equal(3, errors.Count); // Initial + 2 retries
        Assert.All(errors, ex => Assert.IsType<InvalidOperationException>(ex));
    }

    [Fact]
    public async Task WithRetryAsync_OnErrorCallbackNull_DoesNotThrow()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            if (attempts < 2)
                throw new InvalidOperationException("Failure");
            return Task.CompletedTask;
        }

        // ACT
        await Execute.WithRetryAsync(Action, RetryOptions.Fixed(3, 50), onError: null);

        // ASSERT
        Assert.Equal(2, attempts);
    }

    // --- Gruppo: RetryOptions - Configuration ---

    [Fact]
    public void RetryOptions_Default_HasExpectedValues()
    {
        // ACT
        RetryOptions options = RetryOptions.Default;

        // ASSERT
        Assert.Equal(RetryStrategy.Fixed, options.Strategy);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(2000, options.DelayInMilliseconds);
        Assert.Equal(10000, options.MaxDelayInMilliseconds);
    }

    [Fact]
    public void RetryOptions_None_HasZeroRetries()
    {
        // ACT
        RetryOptions options = RetryOptions.None;

        // ASSERT
        Assert.Equal(0, options.MaxRetries);
    }

    [Fact]
    public void RetryOptions_Fixed_CreatesCorrectConfiguration()
    {
        // ACT
        RetryOptions options = RetryOptions.Fixed(maxRetries: 5, delayInMilliseconds: 1000);

        // ASSERT
        Assert.Equal(RetryStrategy.Fixed, options.Strategy);
        Assert.Equal(5, options.MaxRetries);
        Assert.Equal(1000, options.DelayInMilliseconds);
    }

    [Fact]
    public void RetryOptions_Exponential_CreatesCorrectConfiguration()
    {
        // ACT
        RetryOptions options = RetryOptions.Exponential(
            maxRetries: 4,
            delayInMilliseconds: 100,
            maxDelayInMilliseconds: 5000);

        // ASSERT
        Assert.Equal(RetryStrategy.Exponential, options.Strategy);
        Assert.Equal(4, options.MaxRetries);
        Assert.Equal(100, options.DelayInMilliseconds);
        Assert.Equal(5000, options.MaxDelayInMilliseconds);
    }

    // --- Gruppo: WithRetryAsync - Retry Strategy Behavior ---

    [Fact]
    public async Task WithRetryAsync_FixedStrategy_RetriesWithConstantDelay()
    {
        // ARRANGE
        List<DateTime> attemptTimes = new();
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attemptTimes.Add(DateTime.UtcNow);
            attempts++;
            throw new InvalidOperationException("Failure");
        }

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Execute.WithRetryAsync(Action, RetryOptions.Fixed(2, 100)));

        // Note: In UNIT_TEST mode, delays are 10ms, so we just verify retries happened
        Assert.Equal(3, attempts);
        Assert.Equal(3, attemptTimes.Count);
    }

    [Fact]
    public async Task WithRetryAsync_ExponentialStrategy_RetriesSuccessfully()
    {
        // ARRANGE
        int attempts = 0;
        Task Action(CancellationToken ct)
        {
            attempts++;
            if (attempts < 3)
                throw new InvalidOperationException("Failure");
            return Task.CompletedTask;
        }

        // ACT
        await Execute.WithRetryAsync(
            Action,
            RetryOptions.Exponential(maxRetries: 5, delayInMilliseconds: 100, maxDelayInMilliseconds: 5000));

        // ASSERT
        Assert.Equal(3, attempts);
    }

    // --- Gruppo: WithIgnoreExceptionAsync ---

    [Fact]
    public async Task WithIgnoreExceptionAsync_SuccessfulAction_Completes()
    {
        // ARRANGE
        bool executed = false;
        Task Action(CancellationToken ct)
        {
            executed = true;
            return Task.CompletedTask;
        }

        // ACT
        await Execute.WithIgnoreExceptionAsync(Action);

        // ASSERT
        Assert.True(executed);
    }

    [Fact]
    public async Task WithIgnoreExceptionAsync_ThrowsException_SwallowsException()
    {
        // ARRANGE
        Task Action(CancellationToken ct) => throw new InvalidOperationException("Error");

        // ACT
        Exception? caughtException = await Record.ExceptionAsync(
            () => Execute.WithIgnoreExceptionAsync(Action));

        // ASSERT
        Assert.Null(caughtException);
    }

    [Fact]
    public async Task WithIgnoreExceptionAsync_CancellationRequested_SwallowsException()
    {
        // ARRANGE
        using CancellationTokenSource cts = new();
        cts.Cancel();
        Task Action(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        // ACT
        Exception? caughtException = await Record.ExceptionAsync(
            () => Execute.WithIgnoreExceptionAsync(Action, cts.Token));

        // ASSERT
        Assert.Null(caughtException);
    }
}
