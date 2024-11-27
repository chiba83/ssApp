#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppServices.Api
{
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
      public string orderInfo { get; set; }
   }

   public class RakutenSettings
    {
        public RakutenApiEndpoints ApiEndpoints { get; set; }
    }

    public class RakutenApiEndpoints
    {
        public OrderEndpoints Order { get; set; }
        public InventoryEndpoints Inventory { get; set; }
        public CouponEndpoints Coupon { get; set; }
    }

    public class InventoryEndpoints
    {
        public string GetInventory { get; set; }
        public string UpdateInventory { get; set; }
    }

    public class CouponEndpoints
    {
        public string GetCouponList { get; set; }
        public string CreateCoupon { get; set; }
    }
}

#pragma warning restore CS8618
