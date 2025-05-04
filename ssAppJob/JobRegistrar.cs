using Hangfire;
using ssAppServices.Apps;
using ssAppModels.ApiModels;
using ssAppJob.Jobs.Backup;

namespace ssAppJob;

public static class JobRegistrar
{
  public static void RegisterAllJobs()
  {
    // NewOrders 系
    RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "NewOrders - Yahoo_LARAL",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_LARAL, OrderStatus.NewOrder, null, null, true, UpdateMode.Replace),
      "0,30 5-23 * * *",
      new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
    );

    RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "NewOrders - Yahoo_Yours",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_Yours, OrderStatus.NewOrder, null, null, true, UpdateMode.Replace),
      "0,30 5-23 * * *",
      new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
    );

    RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "NewOrders - Rakuten_ENZO",
      x => x.FetchDailyOrderFromRakuten(RakutenShop.Rakuten_ENZO, OrderStatus.NewOrder, null, null, true, UpdateMode.Replace),
      "0,30 5-23 * * *",
      new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
    );

    // Packing 系（手動）
    RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "Packing - Yahoo_LARAL",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_LARAL, OrderStatus.Packing, null, null, true, UpdateMode.Replace),
      Cron.Never
    );

    RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "Packing - Yahoo_Yours",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_Yours, OrderStatus.Packing, null, null, true, UpdateMode.Replace),
      Cron.Never
    );

    RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "Packing - Rakuten_ENZO",
      x => x.FetchDailyOrderFromRakuten(RakutenShop.Rakuten_ENZO, OrderStatus.Packing, null, null, true, UpdateMode.Replace),
      Cron.Never
    );

    // Backup 系
    RecurringJob.AddOrUpdate<SqlBackupJobs>(
      "Daily-Backup",
      job => job.DailyBackup(),
      "0 3 * * *",
      new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
    );

    RecurringJob.AddOrUpdate<SqlBackupJobs>(
      "zManual-Backup", "default",
      job => job.OnDemandBackup(),
      Cron.Never
    );

    RecurringJob.AddOrUpdate<SqlBackupJobs>(
      "zManual-Restore", "default",
      job => job.RestoreAllFromFolder(),
      Cron.Never
    );
  }
}
