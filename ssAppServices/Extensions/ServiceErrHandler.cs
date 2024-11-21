using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using ssAppCommon.Logging;

namespace ssAppServices.Extensions
{
    public class ServiceErrHandler
    {
        private readonly ErrorLogger _errorLogger;

        public ServiceErrHandler(ErrorLogger errorLogger)
        {
            _errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
        }

        /// <summary>
        /// 汎用的なHTTPポリシーを構築
        /// </summary>
        /// <param name="retryCount">リトライ回数（デフォルト: 3回）</param>
        /// <param name="exitOnFallback">フォールバック時にプログラム終了するか（デフォルト: false）</param>
        /// <returns>構築済みのポリシー</returns>
        public IAsyncPolicy<HttpResponseMessage> BuildHttpPolicy(int retryCount = 3, bool exitOnFallback = false)
        {
            // リトライポリシー
            var retryPolicy = Policy
                .Handle<HttpRequestException>() // ネットワークエラー
                .OrResult<HttpResponseMessage>(response => (int)response.StatusCode >= 500) // HTTP 500以上のエラー
                .WaitAndRetryAsync(
                    retryCount,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), // 指数バックオフ
                    async (outcome, timespan, retryAttempt, context) =>
                    {
                        var additionalInfo = $"Retry {retryAttempt} after {timespan.TotalSeconds}s.";
                        await LogErrorForPolicy(outcome.Exception, context, additionalInfo, "Retry");
                    });

            // フォールバックポリシー
            var fallbackPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(response => (int)response.StatusCode >= 500)
                .FallbackAsync(
                    fallbackAction: async (context, cancellationToken) =>
                    {
                        if (exitOnFallback) Environment.Exit(-1);
                        return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            Content = new StringContent("{\"message\":\"Fallback executed\"}")
                        };
                    },
                    onFallbackAsync: async (outcome, context) =>
                    {
                        var additionalInfo = "Fallback executed.";
                        await LogErrorForPolicy(outcome.Exception, context, additionalInfo, "Fallback");
                    });

            // ポリシーの統合
            return Policy.WrapAsync(fallbackPolicy, retryPolicy);
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
