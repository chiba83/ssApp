using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

        // 例外メッセージから情報を抽出し、ログに記録
        public async Task LogErrorAsync(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            // 例外メッセージから情報を抽出
            var extractedData = ExtractData(ex.Message);
            var errorLog = new ErrorLog
            {
                ServiceName = extractedData["Namespace"],
                MethodName = $"{extractedData["Class"]} : {extractedData["Method"]}",
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                AdditionalInfo = extractedData["Info"],
                CreatedAt = DateTime.Now
            };

            _dbContext.ErrorLogs.Add(errorLog);
            await _dbContext.SaveChangesAsync();
        }

        // 例外メッセージから情報を抽出
        private Dictionary<string, string> ExtractData(string message)
        {
            var patterns = new Dictionary<string, string>
        {
            { "Namespace", @"Namespace//(.*?)/" },
            { "Class", @"Class//(.*?)/" },
            { "Method", @"Method//(.*?)/" },
            { "Info", @"Info//(.*?)/" }
        };

            return patterns.Select(pattern => new
            {
                Key = pattern.Key,
                Value = Regex.Match(message, pattern.Value).Success ?
                        Regex.Match(message, pattern.Value).Groups[1].Value : "Unknown"
            }).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}