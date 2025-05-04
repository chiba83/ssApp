using ssAppModels.EFModels;

namespace ssAppBlazorWeb.Extensions;

public class Order : DailyOrderNews
{
  public string GroupKey { get; set; } = string.Empty;
  public string OrderDateForGrid { get; set; } = string.Empty;
  public string OrderIdForGrid { get; set; } = string.Empty;
  public bool MultiOrders { get; set; } = false;
}