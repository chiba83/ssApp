#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppServices.Api;

 public class MallSettings
 {
     public YahooSettings Yahoo { get; set; }
     public RakutenSettings Rakuten { get; set; }
 }

 public class YahooSettings
 {
     public YahooEndpoints Endpoints { get; set; }
 }

 public class YahooEndpoints
 {
     public string AccessToken { get; set; }
     public OrderEndpoints Order { get; set; }
 }

 public class OrderEndpoints
 {
   public string OrderList { get; set; }
   public string OrderInfo { get; set; }
}

public class RakutenSettings
{
   public RakutenEndpoints Endpoints { get; set; }
}

public class RakutenEndpoints
{
   public RakutenOrderEndpoints Order { get; set; }
   public RakutenInventoryEndpoints Inventory { get; set; }
   public RakutenCouponEndpoints Coupon { get; set; }
}

public class RakutenOrderEndpoints
{
   public string SearchOrder { get; set; }
   public string GetOrder { get; set; }
}

public class RakutenInventoryEndpoints
{
   public string GetInventory { get; set; }
   public string UpdateInventory { get; set; }
}

public class RakutenCouponEndpoints
{
   public string GetCouponList { get; set; }
   public string CreateCoupon { get; set; }
}
