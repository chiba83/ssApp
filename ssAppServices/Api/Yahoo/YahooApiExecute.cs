using Microsoft.EntityFrameworkCore;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppModels.EFModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ssAppServices.Api.Yahoo;

public class YahooApiExecute(
   ssAppDBContext dbContext,
   YahooOrderList yahooOrderList,
   YahooOrderInfo yahooOrderInfo)
{
   private readonly ssAppDBContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
   private readonly YahooOrderList _yahooOrderList = yahooOrderList ?? throw new ArgumentNullException(nameof(yahooOrderList));
   private readonly YahooOrderInfo _yahooOrderInfo = yahooOrderInfo ?? throw new ArgumentNullException(nameof(yahooOrderInfo));
   private readonly string _searchOutputFields = string.Join(",", YahooOrderListRequestFactory.OutputFieldsDefault);

   /// <summary>
   /// Yahoo注文明細情報を取得する。
   /// リクエストに対する全オーダーを取得する。ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="yahooOrderListRequest">注文リストリクエスト</param>
   /// <param name="orderInfoOutputFields">注文明細出力フィールドリスト</param>
   /// <param name="yahooShop">Yahooショップ</param>
   /// <returns name="yahooOrderInfoResponse">注文明細リスト</returns>
   public List<YahooOrderInfoResponse> GetOrder(
      YahooOrderListRequest yahooOrderListRequest,
      List<string>? orderInfoOutputFields, YahooShop yahooShop)
   {
      // YahooOrderList API実行
      var yahooOrderListResult = _yahooOrderList.GetOrderSearch(yahooOrderListRequest, yahooShop);

      // YahooOrderInfoはページング処理を実行します。（注文リスト数のループ）
      // 全オーダー明細を取得します。（上限2000件）
      var orderIds = yahooOrderListResult.Search.OrderInfo
        .Select(x => x.Fields["OrderId"]?.ToString() ?? string.Empty).ToList();
      orderInfoOutputFields ??= AppModelHelpers.GetYahooDefaultFields();
      return _yahooOrderInfo.GetOrderInfo(orderIds, orderInfoOutputFields, yahooShop);
   }

   /// <summary>
   /// 各ステータスの注文情報リクエストパラメータを取得する。
   /// ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="status">対象ステータス</param>
   /// <param name="startDate">開始日</param>
   /// <param name="endDate">終了日</param>
   /// <param name="orderInfoOutputFields">注文明細出力フィールドリスト</param>
   /// <param name="yahooShop">Yahooショップ</param>
   /// <returns name="yahooOrderInfoResponse">注文明細リスト</returns>
   public List<YahooOrderInfoResponse> GetYahooOrders(
      OrderStatus status, DateTime? startDate, DateTime? endDate, 
      List<string>? orderInfoOutputFields, YahooShop yahooShop)
   {

      YahooOrderListRequest yahooOrderListRequest = status switch
      {
         ///<summary>
         /// 新規注文リクエスト詳細
         /// OrderStatus オーダーステータス  （2：処理中）
         /// IsSeen 閲覧済みフラグ           （false：未閲覧）
         /// ShipStatus 出荷ステータス       （1：出荷可）
         /// StartDatetime           　   　（開始日デフォルト：-30日）
         ///</summary>
         OrderStatus.NewOrder => YahooOrderListRequestFactory.NewOrderRequest(startDate, endDate, 1, _searchOutputFields, GetSellerId(yahooShop)),

         ///<summary>
         /// 梱包処理用注文リクエスト詳細
         /// OrderStatus オーダーステータス （2：処理中）
         /// IsSeen 閲覧済みフラグ          （true：閲覧）
         /// ShipStatus 出荷ステータス      （1：出荷可）
         /// StartDatetime          　   　（開始日デフォルト：-30日）
         ///</summary>
         OrderStatus.Packing => YahooOrderListRequestFactory.ShippingInProgressRequest(startDate, endDate, 1, _searchOutputFields, GetSellerId(yahooShop)),

         ///<summary>
         /// 出荷処理用注文リクエスト詳細（出荷日・追跡番号設定）
         /// OrderStatus オーダーステータス （2：処理中）
         /// IsSeen 閲覧済みフラグ          （true：閲覧）
         /// ShipStatus 出荷ステータス      （2：出荷処理中）
         /// StartDatetime          　   　（開始日デフォルト：-30日）
         ///</summary>
         OrderStatus.Shipping => YahooOrderListRequestFactory.ShippingProcessingRequest(startDate, endDate, 1, _searchOutputFields, GetSellerId(yahooShop)),

         ///<summary>
         /// 出荷完了リクエスト詳細
         /// ShipStatus 出荷ステータス      （3：出荷済）
         /// StartDatetime          　   　（開始日デフォルト：-30日）
         ///</summary>
         OrderStatus.Shipped => YahooOrderListRequestFactory.ShippedOrderRequest(startDate, endDate, 1, _searchOutputFields, GetSellerId(yahooShop)),

         _ => throw new Exception("ヤフー注文リクエストを設定してください。"),
      };
      return GetOrder(yahooOrderListRequest, orderInfoOutputFields, yahooShop);
   }

   private string GetSellerId(YahooShop yahooShop)
   {
      return ApiHelpers.GetShopToken(_dbContext, yahooShop.ToString()).SellerId;
   }
}
