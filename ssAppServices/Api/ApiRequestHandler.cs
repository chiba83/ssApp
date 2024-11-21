﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using ssAppServices.Extensions;

namespace ssAppServices.Api
{
    public class ApiRequestHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public ApiRequestHandler(ServiceErrHandler errorHandler, int retryCount = 3, bool exitOnFallback = false)
        {
            if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));
            _policy = errorHandler.BuildHttpPolicy(retryCount, exitOnFallback);
        }

        /// <summary>
        /// HTTPリクエストをポリシー適用のもとで実行
        /// </summary>
        /// <param name="request">HttpRequestMessage</param>
        /// <param name="context">Pollyコンテキスト</param>
        /// <returns>HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Polly.Context context)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));

            return await _policy.ExecuteAsync(async ctx =>
            {
                using var client = new HttpClient();
                return await client.SendAsync(request);
            }, context);
        }
    }
}