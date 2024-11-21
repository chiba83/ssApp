using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;

namespace ssAppServices.Api
{
    public class ApiRequestHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public ApiRequestHandler(IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        /// <summary>
        /// HTTPリクエストをポリシー適用のもとで実行
        /// </summary>
        /// <param name="request">HttpRequestMessage</param>
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
