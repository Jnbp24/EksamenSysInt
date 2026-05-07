using Microsoft.Extensions.Resilience;
using Polly;
using Polly.Retry;

namespace Eaat.Api.Resilience;

public static class ResiliencePipelines
{
    public static ResiliencePipeline CreateRetryPipeline(string operationName)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,

                OnRetry = args =>
                {
                    Console.WriteLine(
                        $"[RESILIENCE] {operationName} retry {args.AttemptNumber} due to {args.Outcome.Exception?.Message}");

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}