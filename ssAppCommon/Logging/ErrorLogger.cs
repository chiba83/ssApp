using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ssAppModels.EFModels;

namespace ssAppCommon.Logging
{
    public class ErrorLogger
    {
        private readonly ssAppDBContext _dbContext;

        public ErrorLogger(ssAppDBContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        // 非同期版ログ記録メソッド
        public async Task LogErrorAsync
            (
                Exception ex,
                string? additionalInfo = null,
                string? apiEndpoint = null,
                string? httpMethod = null,
                string? reqHeader = null,
                string? reqBody = null,
                int? resStatusCode = null,
                string? resHeader = null,
                string? resBody = null,
                string? userId = null,
                string? apiErrorType = null
            )
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            // サービス名とメソッド名を抽出
            var (serviceName, methodName) = ExtractServiceAndMethod(ex);

            // エラーログを作成
            var errorLog = new ErrorLog
            {
                ServiceName = serviceName,
                MethodName = methodName,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                AdditionalInfo = additionalInfo,
                ApiEndpoint = apiEndpoint,
                HttpMethod = httpMethod,
                ReqHeader = reqHeader,
                ReqBody = reqBody,
                ResStatusCode = resStatusCode,
                ResHeader = resHeader,
                ResBody = resBody,
                ApiErrorType = apiErrorType,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _dbContext.ErrorLogs.Add(errorLog);
            await _dbContext.SaveChangesAsync();
        }

        // 同期版ログ記録メソッド（非同期メソッドを同期的に実行）
        public void LogErrorSync(Exception ex, string? additionalInfo = null)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            LogErrorAsync(ex, additionalInfo).GetAwaiter().GetResult();
        }

        /// <summary>
        /// スタックトレースからサービス名とメソッド名を抽出
        /// </summary>
        private (string ServiceName, string MethodName) ExtractServiceAndMethod(Exception ex)
        {
            if (ex == null || string.IsNullOrEmpty(ex.StackTrace))
                return ("Unknown", "Unknown");

            // スタックトレースの最初の行を解析
            var stackTraceLines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (stackTraceLines.Length == 0) return ("Unknown", "Unknown");

            // 正規表現で "Namespace.Class.Method (行番号付き)" を解析
            var firstLine = stackTraceLines[0];
            var regex = new Regex(@"^\s*at\s+(?<Namespace>[\w.]+)\.(?<Class>\w+)\.(?<Method>\w+)\(.*?\)\s+in\s+.*?:(line\s+(?<Line>\d+))?");
            var match = regex.Match(firstLine);

            if (!match.Success) return ("Unknown", "Unknown");

            var serviceName = match.Groups["Namespace"].Value;
            var methodName = match.Groups["Class"].Value + "." + match.Groups["Method"].Value;
            methodName += match.Groups["Line"].Success ? $" (Line {match.Groups["Line"].Value})" : ""; // 行番号を追加

            return (serviceName, methodName);
        }
    }
}
