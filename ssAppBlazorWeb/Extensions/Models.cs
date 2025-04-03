using ssAppModels.EFModels;

namespace ssAppBlazorWeb.Extensions;

public class NewOrder : DailyOrderNews
{
  public string GroupKey { get; set; } = string.Empty;
}
