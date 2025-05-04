using Hangfire.Server;
using Hangfire.Common;
using Hangfire.States;
using Hangfire;
using ssAppJob.Logging;

namespace ssAppJob.Logging;

public class LogJobFilter : JobFilterAttribute, IServerFilter
{
  public void OnPerforming(PerformingContext context)
  {
    var jobId = context.BackgroundJob?.Id ?? "(不明)";
    var jobName = context.BackgroundJob?.Job?.Method?.Name ?? "(不明)";
    LogHelper.Log($"ジョブ開始: ID = {jobId}, メソッド = {jobName}");
  }

  public void OnPerformed(PerformedContext context)
  {
    var jobId = context.BackgroundJob?.Id ?? "(不明)";
    var jobName = context.BackgroundJob?.Job?.Method?.Name ?? "(不明)";

    if (context.Exception == null)
    {
      LogHelper.Log($"ジョブ完了: ID = {jobId}, メソッド = {jobName}, 結果 = 成功");
    }
    else
    {
      LogHelper.Log($"ジョブ失敗: ID = {jobId}, メソッド = {jobName}, 例外 = {context.Exception.Message}");
    }
  }
}
