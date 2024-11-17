using Microsoft.Extensions.Logging;
using ssAppModels.EFModels;
using System;
using System.Threading.Tasks;

public class ErrorLogger
{
    private readonly ssAppDBContext _dbContext;
    private readonly ILogger<ErrorLogger> _logger;

    public ErrorLogger(ssAppDBContext dbContext, ILogger<ErrorLogger> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogErrorAsync(Exception ex, string source)
    {
        var errorLog = new ErrorLog
        {
            ErrorMessage = ex.Message,
            StackTrace = ex.StackTrace,
            ErrorSource = source,
            CreatedAt = DateTime.Now
        };

        _dbContext.ErrorLogs.Add(errorLog);
        await _dbContext.SaveChangesAsync();

        _logger.LogError(ex, "An error occurred in {Source}", source);
    }
}
