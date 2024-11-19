using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ssAppModels.EFModels;
using ssAppCommon.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ssAppServices.Api
{
    public class ApiClientHandler
    {
        private readonly HttpClient _httpClient;
        private readonly ErrorLogger _errorLogger;
        private readonly ILogger<ApiClientHandler> _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public ApiClientHandler(HttpClient httpClient, ErrorLogger errorLogger, ILogger<ApiClientHandler> logger, int retryCount = 3)
        {
            _httpClient = httpClient;
            _errorLogger = errorLogger;
            _logger = logger;

            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .RetryAsync(retryCount, onRetry: (response, retryNumber) =>
                {
                    _logger.LogWarning($"Retry {retryNumber} for {response.Result.RequestMessage.RequestUri} failed with {response.Result.StatusCode}");
                });
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request));
        }
    }
}