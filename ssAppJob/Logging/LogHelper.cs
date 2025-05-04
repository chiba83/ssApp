using System;
using System.IO;

namespace ssAppJob.Logging;

public static class LogHelper
{
  private static readonly string LogFilePath = @"C:\inetpub\logs\hangfire\hangfire_log.txt";

  public static void Log(string message)
  {
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    var entry = $"[{timestamp}] {message}\n";
    File.AppendAllText(LogFilePath, entry);
  }
}
