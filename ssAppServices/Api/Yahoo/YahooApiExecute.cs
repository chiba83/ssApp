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

public class YahooApiExecute
{
   private readonly ssAppDBContext _dbContext;
   private readonly YahooOrderList _yahooOrderList;
   private readonly YahooOrderInfo _yahooOrderInfo;
   private readonly string _searchOutputFields;

   public YahooApiExecute(
      ssAppDBContext dbContext,
      YahooOrderList yahooOrderList,
      YahooOrderInfo yahooOrderInfo)
   {
      _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
      _yahooOrderList = yahooOrderList ?? throw new ArgumentNullException(nameof(yahooOrderList));
      _yahooOrderInfo = yahooOrderInfo ?? throw new ArgumentNullException(nameof(yahooOrderInfo));
      _searchOutputFields = string.Join(",", YahooOrderListRequestFactory.OutputFieldsDefault);
   }

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
      orderInfoOutputFields = orderInfoOutputFields ?? AppModelHelpers.GetYahooDefaultFields();
      return _yahooOrderInfo.GetOrderInfo(orderIds, orderInfoOutputFields, yahooShop);
   }

   /// <summary>
   /// 新規注文情報を取得する。
   /// リクエストに対する全オーダーを取得する。ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="startDate">開始日</param>
   /// <param name="endDate">終了日</param>
   /// <param name="orderInfoOutputFields">注文明細出力フィールドリスト</param>
   /// <param name="yahooShop">Yahooショップ</param>
   /// <returns name="yahooOrderInfoResponse">注文明細リスト</returns>
   public List<YahooOrderInfoResponse> GetNewOrders(
      DateTime? startDate, DateTime? endDate, 
      List<string>? orderInfoOutputFields, YahooShop yahooShop)
   {
      ///<summary>
      /// 新規注文リクエスト詳細
      /// OrderStatus オーダーステータス  （2：処理中）
      /// IsSeen 閲覧済みフラグ           （false：未閲覧）
      /// ShipStatus 出荷ステータス       （1：出荷可）
      ///</summary>
      var yahooOrderListRequest = YahooOrderListRequestFactory.NewOrderRequest(startDate, endDate, 1, _searchOutputFields, GetSellerId(yahooShop));
      return GetOrder(yahooOrderListRequest, orderInfoOutputFields, yahooShop);
   }

   /// <summary>
   /// 発送処理中注文情報を取得する。
   /// リクエストに対する全オーダーを取得する。ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="startDate">開始日</param>
   /// <param name="endDate">終了日</param>
   /// <param name="orderInfoOutputFields">注文明細出力フィールドリスト</param>
   /// <param name="yahooShop">Yahooショップ</param>
   /// <returns name="yahooOrderInfoResponse">注文明細リスト</returns>
   public List<YahooOrderInfoResponse> GetProcessingOrdersForShipping(
      DateTime? startDate, DateTime? endDate, 
      List<string>? orderInfoOutputFields, YahooShop yahooShop)
   {
      ///<summary>
      /// 発送処理中注文リクエスト詳細
      /// OrderStatus オーダーステータス （2：処理中）
      /// IsSeen 閲覧済みフラグ          （true：閲覧）
      /// ShipStatus 出荷ステータス      （1：出荷可）
      ///</summary>
      var yahooOrderListRequest = YahooOrderListRequestFactory.ShippingInProgressRequest(startDate, endDate, 1, _searchOutputFields, GetSellerId(yahooShop));
      return GetOrder(yahooOrderListRequest, orderInfoOutputFields, yahooShop);
   }

   private string GetSellerId(YahooShop yahooShop)
   {
      return ApiHelpers.GetShopToken(_dbContext, yahooShop.ToString()).SellerId;
   }
}
