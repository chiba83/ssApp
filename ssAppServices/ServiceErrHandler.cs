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

        /// <summary>
        /// HTTPリトライポリシー
        /// </summary>
        /// <param name="retryCount">リトライ回数（デフォルト3回）</param>
        /// <param name="exit">リトライオーバー時に継続する場合はtrue（デフォルトはfalse）</param>
        /// <returns>HTTPリトライポリシー</returns>
        public IAsyncPolicy<string> HttpRetryPolicy(int retryCount = 3, bool exit = false)
        {
            return 
            Policy<string>.Handle<Exception>().WaitAndRetryAsync // 全ての例外をキャッチ 
            (
                retryCount: retryCount, // リトライ回数
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)), // 1秒 → 2秒 → 4秒
                onRetry: async (exception, timespan, retryCount, context) =>
                {
                    await _errorLogger.LogErrorAsync(exception.Exception);
                }
            ).WrapAsync 
            (
                Policy<string>.Handle<Exception>().FallbackAsync // 再試行限界超過時
                (
                    fallbackAction: async (context, cancellationToken) =>
                    {
                        if (exit) Environment.Exit(-1); // プログラム終了
                        return await Task.FromResult("continue"); // 継続文字列を返す
                    },
                    onFallbackAsync: async (exception, context) =>
                    {
                        if (exception != null) // リトライオーバー時のエラーログ記録
                            await _errorLogger.LogErrorAsync(exception.Exception);
                    }
                )
            );
        }
    }
}
