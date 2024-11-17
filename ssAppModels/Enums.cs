using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssApp.Models
{
    // ショップコードを管理する列挙型
    public enum YahooShop
    {
        Yahoo_LARAL,
        Yahoo_Yours
    }
    // OAuth 2.0 のグラントタイプを管理する列挙型
    public enum GrantType
    {
        authorization_code,
        refresh_token
    }


}
