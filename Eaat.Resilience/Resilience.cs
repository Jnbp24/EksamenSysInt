using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Eaat.Resilience
{
    public static class ResiliencePipelines
    {
        public static ResiliencePipeline RabbitMQ => new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
               
                MaxRetryAttempts = 3, // Retry 3 times
                Delay = TimeSpan.FromMilliseconds(500), 
                BackoffType = DelayBackoffType.Exponential, // Increase delay exponentially, so 0.5, 1, 2 
                OnRetry = args =>
                {
                    Console.WriteLine($"RabbitMQ operation failed, retrying attempt {args.AttemptNumber}...");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5, // Amount of requests that must fail before opening the circuit
                MinimumThroughput = 3, // Number of requests
                BreakDuration = TimeSpan.FromSeconds(15), // Wait 15 seconds to close circuit again.
                OnOpened = args =>
                {
                    Console.WriteLine("Circuit breaker opened - RabbitMQ may be down");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("Circuit breaker closed - RabbitMQ is back up");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("Circuit breaker half-open - testing RabbitMQ...");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                // Each individual attempt gets 5 seconds before timing out
                Timeout = TimeSpan.FromSeconds(5)
            })
            .Build();

        // Pipeline for API endpoints - shorter timeout, fewer retries since the user is waiting
        public static ResiliencePipeline Api => new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    Console.WriteLine($"API operation failed, retrying attempt {args.AttemptNumber}...");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(10),
                OnOpened = args =>
                {
                    Console.WriteLine("Circuit breaker opened - API may be down");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("Circuit breaker closed - API is back up");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("Circuit breaker half-open - testing API...");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                // Each individual attempt gets 3 seconds before timing out
                Timeout = TimeSpan.FromSeconds(3)
            })
            .Build();
    }
}