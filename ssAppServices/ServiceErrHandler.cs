using System;
using System.Threading.Tasks;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using ssAppCommon.Logging;

namespace ssAppServices
{
    public class ServiceErrHandler
    {
        private readonly ErrorLogger _errorLogger;

        public ServiceErrHandler(ErrorLogger errorLogger)
        {
            _errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
        }

        public IAsyncPolicy<string> ErrDefaultPolicy(bool exit = true)
        {
            return Policy<string>.Handle<Exception>().FallbackAsync(
                fallbackAction: async (cancellationToken) =>
                {
                    if (exit) Environment.Exit(-1);
                    return await Task.FromResult("continue");
                },
                onFallbackAsync: async (exception) =>
                {
                    await _errorLogger.LogErrorAsync(exception.Exception);
                }
            );
        }

        public IAsyncPolicy<string> HttpRetryPolicy(int retryCount = 3, bool exit = false)
        {
            return
            Policy<string>.Handle<Exception>().WaitAndRetryAsync
            (
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                onRetry: async (outcome, timespan, currentRetryCount, context) =>
                {
                    await LogErrorForPolicy(
                        ex: outcome.Exception,
                        context: context,
                        additionalInfo: $"Retry attempt {currentRetryCount} after {timespan.TotalSeconds} seconds.",
                        apiErrorType: "Retry"
                    );
                }
            ).WrapAsync
            (
                Policy<string>.Handle<Exception>().FallbackAsync
                (
                    fallbackAction: async (context, cancellationToken) =>
                    {
                        if (exit) Environment.Exit(-1);
                        return await Task.FromResult("continue");
                    },
                    onFallbackAsync: async (outcome, context) =>
                    {
                        await LogErrorForPolicy(
                            ex: outcome.Exception,
                            context: context,
                            additionalInfo: "Fallback executed after retry limit exceeded.",
                            apiErrorType: "Fallback"
                        );
                    }
                )
            );
        }

        private async Task LogErrorForPolicy(Exception ex, Context context, string additionalInfo, string apiErrorType)
        {
            if (ex == null) ex = new Exception("Unknown error");

            var apiEndpoint = context.ContainsKey("ApiEndpoint") ? context["ApiEndpoint"].ToString() : null;
            var httpMethod = context.ContainsKey("HttpMethod") ? context["HttpMethod"].ToString() : null;
            var userId = context.ContainsKey("UserId") ? context["UserId"].ToString() : null;

            await _errorLogger.LogErrorAsync(
                ex: ex,
                additionalInfo: additionalInfo,
                apiEndpoint: apiEndpoint,
                httpMethod: httpMethod,
                userId: userId,
                apiErrorType: apiErrorType
            );
        }
    }
}
