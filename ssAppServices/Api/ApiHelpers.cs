using System;
using System.Linq;
using Polly;
using ssAppModels;
using ssAppModels.EFModels;

namespace ssAppServices.Api
{
    public static class ApiHelpers
    {
        /// <summary>
        /// 指定されたShopCodeに対応するShopTokenを取得
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="shopCode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ShopToken GetShopToken(ssAppDBContext dbContext, YahooShop shopCode)
        {
            return dbContext.ShopTokens.FirstOrDefault(st => st.ShopCode == shopCode.ToString())
                ?? throw new Exception($"指定されたShopCode（{shopCode}）に対応するShopTokenが見つかりません。");
        }

        /// <summary>
        /// Polly.Context を生成するヘルパーメソッド
        /// </summary>
        /// <param name="vendor">ベンダー名</param>
        /// <param name="request">HttpRequestMessage</param>
        /// <param name="userId">ユーザーID</param>
        /// <returns>生成された Polly.Context</returns>
        public static Context CreatePollyContext(string vendor, HttpRequestMessage request, string userId)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return new Context
            {
                { "Vendor", vendor },
                { "ApiEndpoint", request.RequestUri?.ToString() },
                { "HttpMethod", request.Method.ToString() },
                { "UserId", userId },
                { "HttpRequest", request }
            };
        }
    }
}
