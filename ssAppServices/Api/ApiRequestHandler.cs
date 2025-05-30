using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using ssAppServices.Extensions;

namespace ssAppServices.Api;

public class ApiRequestHandler
{
  private readonly IAsyncPolicy<HttpResponseMessage> _policy;

  public ApiRequestHandler(ServiceErrHandler errorHandler)
  {
    if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));

    // HTTP専用ポリシーを取得
    _policy = errorHandler.GetHttpPolicy();
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

    /*    return await _policy.ExecuteAsync(async ctx =>
          {
            using var client = new HttpClient();
            return await client.SendAsync(request);
          }, context
        );
    */
    return await _policy.ExecuteAsync(async ctx =>
      {
        HttpClient client;
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

        if (!isProduction)
        {
          var handler = new HttpClientHandler
          {
            Proxy = new WebProxy("http://116.80.74.94:3128"),
            UseProxy = true
          };
          client = new HttpClient(handler);
        }
        else
        {
          client = new HttpClient();
        }

        using (client)
        {
          return await client.SendAsync(request);
        }
      }, 
      context
    );
  }
}
