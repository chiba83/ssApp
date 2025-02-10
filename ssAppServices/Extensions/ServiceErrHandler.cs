#pragma warning disable CS1998, CS8629

using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Polly;
using Polly.Wrap;
using ssAppCommon.Logging;

namespace ssAppServices.Extensions;

public class ServiceErrHandler
 {
     private readonly ErrorLogger _errorLogger;

     public ServiceErrHandler(ErrorLogger errorLogger)
     {
         _errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
     }

     /// <summary>
     /// 非同期用デフォルトポリシー
     /// </summary>
     /// <returns>構築済みのデフォルトポリシー</returns>
     public IAsyncPolicy BuildDefaultPolicy()
     {
         return Policy
             .Handle<Exception>()
             .FallbackAsync(
                 fallbackAction: async (context, cancellationToken) =>
                 {
                     await Task.CompletedTask; // 何もしない
                 },
                 onFallbackAsync: async (exception, context) =>
                 {
                     await _errorLogger.LogErrorAsync(exception ?? new Exception("Unknown error"), additionalInfo: "DefaultAsyncPolicy triggered.");
                 });
     }

     /// <summary>
     /// 同期用デフォルトポリシー
     /// </summary>
     /// <returns>構築済みのデフォルトポリシー</returns>
     public ISyncPolicy BuildDefaultSyncPolicy()
     {
         return Policy
             .Handle<Exception>()
             .Fallback(
                 fallbackAction: context =>
                 {
                 },
                 onFallback: exception =>
                 {
                     _errorLogger.LogErrorSync(exception ?? new Exception("Unknown error"), additionalInfo: "DefaultSyncPolicy triggered.");
                 });
     }

     /// <summary>
     /// 汎用的なHTTPポリシーを構築
     /// </summary>
     /// <param name="retryCount">リトライ回数（デフォルト: 3回）</param>
     /// <param name="exitOnFallback">フォールバック時にプログラム終了するか（デフォルト: false）</param>
     /// <returns>構築済みのHTTPポリシー</returns>
     public IAsyncPolicy<HttpResponseMessage> BuildHttpPolicy(int retryCount = 3, bool exitOnFallback = false)
     {
         var retryPolicy = Policy
             .Handle<HttpRequestException>() // 通信エラー
             .OrResult<HttpResponseMessage>(response => (int)response.StatusCode >= 500) // 500番台のみリトライ対象
             .WaitAndRetryAsync(
                 retryCount,
                 attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), // 指数バックオフ
                 async (outcome, timespan, retryAttempt, context) =>
                 {
                     var additionalInfo = $"Retry {retryAttempt} after {timespan.TotalSeconds}s.";
                     context["HttpResponse"] = outcome.Result; // HttpResponseMessage をコンテキストに追加
                     await LogErrorForPolicy(outcome.Exception, context, additionalInfo, "Retry");
                 });

         var fallbackPolicy = Policy<HttpResponseMessage>
             .Handle<HttpRequestException>() // 通信エラー
             .OrResult(response => (int)response.StatusCode >= 400) // 400番台もキャッチ
             .FallbackAsync(
                 fallbackAction: async (context, cancellationToken) =>
                 {
                     if (exitOnFallback) Environment.Exit(-1); // 必要ならプロセス終了
                     return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                     {
                         Content = new StringContent("{\"message\":\"Error fallback executed.\"}")
                     };
                 },
                 onFallbackAsync: async (outcome, context) =>
                 {
                     // HTTPレスポンスをコンテキストに追加
                     context["HttpResponse"] = outcome.Result;

                     var additionalInfo = (int)outcome.Result?.StatusCode >= 400 && (int)outcome.Result?.StatusCode < 500
                         ? "Client error fallback executed." // 400番台用メッセージ
                         : "Server error fallback executed."; // 500番台用メッセージ
                     await LogErrorForPolicy(outcome.Exception, context, additionalInfo, "Fallback");
                 });

         return Policy.WrapAsync(fallbackPolicy, retryPolicy); // リトライ後にフォールバック実行
     }

     /// <summary>
     /// HTTPポリシーの取得（既存ポリシーを簡単に取得可能）
     /// </summary>
     /// <returns>構築済みのHTTPポリシー</returns>
     public IAsyncPolicy<HttpResponseMessage> GetHttpPolicy()
     {
         return BuildHttpPolicy();
     }

     /// <summary>
     /// ポリシー内でエラーをログ出力
     /// </summary>
     /// <param name="ex">例外情報</param>
     /// <param name="context">Pollyのコンテキスト</param>
     /// <param name="additionalInfo">追加情報</param>
     /// <param name="apiErrorType">エラー種別（例: Retry, Fallback）</param>
     private async Task LogErrorForPolicy(Exception ex, Context context, string additionalInfo, string apiErrorType)
     {
         // Exception が null の場合は HttpResponseMessage から情報を補完
         if (ex == null && context.TryGetValue("HttpResponse", out var responseObj) && responseObj is HttpResponseMessage response)
         {
             var stackTrace = $"StackTrace: HTTP {response.StatusCode} at {response.RequestMessage?.RequestUri}";
             ex = new Exception($"HTTP {response.StatusCode}: {response.ReasonPhrase}\n{stackTrace}");
         }

         // Context から追加情報を取得
         var apiEndpoint = context.TryGetValue("ApiEndpoint", out var endpoint) ? endpoint?.ToString() : null;
         var httpMethod = context.TryGetValue("HttpMethod", out var method) ? method?.ToString() : null;
         var userId = context.TryGetValue("UserId", out var user) ? user?.ToString() : null;

         // リクエスト情報を取得
         string? requestHeaders = null;
         string? requestBody = null;
         if (context.TryGetValue("HttpRequest", out var requestObj) && requestObj is HttpRequestMessage request)
         {
             requestHeaders = string.Join(Environment.NewLine, request.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
             requestBody = request.Content != null ? await request.Content.ReadAsStringAsync() : null;
         }

         // HttpResponseMessage の詳細をログに追加
         int? statusCode = null;
         string? responseBody = null;
         string? responseHeader = null;

         if (context.TryGetValue("HttpResponse", out responseObj) && responseObj is HttpResponseMessage httpResponse)
         {
             statusCode = (int)httpResponse.StatusCode;
             responseBody = await httpResponse.Content.ReadAsStringAsync();
             responseHeader = string.Join(Environment.NewLine, httpResponse.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
         }

         // ログを記録
         await _errorLogger.LogErrorAsync(
             ex: ex ?? new Exception("Unknown error"),
             additionalInfo: additionalInfo,
             apiEndpoint: apiEndpoint,
             httpMethod: httpMethod,
             reqHeader: requestHeaders, // リクエストヘッダー
             reqBody: requestBody,     // リクエストボディ
             resStatusCode: statusCode,
             resHeader: responseHeader,
             resBody: responseBody,
             userId: userId,
             apiErrorType: apiErrorType
         );
     }
 }
