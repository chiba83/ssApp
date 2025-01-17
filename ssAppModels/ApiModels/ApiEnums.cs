using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.ApiModels
{
   // ショップコードを管理する列挙型
   public enum YahooShop
   {
      Yahoo_LARAL,
      Yahoo_Yours
   }
   public enum RakutenShop
   {
      Rakuten_ENZO
   }
   public enum MallShop
   {
      Yahoo_LARAL,
      Yahoo_Yours,
      Rakuten_ENZO
   }
   public enum Mall
   {
      Yahoo,
      Rakuten
   }

   // OAuth 2.0 のグラントタイプを管理する列挙型
   public enum GrantType
   {
      authorization_code,
      refresh_token
   }


}
