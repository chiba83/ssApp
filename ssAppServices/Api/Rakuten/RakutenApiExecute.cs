using ssAppModels.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppServices.Api.Rakuten;

public class RakutenApiExecute
{
   private readonly RakutenSearchOrder _rakutenSearchOrder;
   private readonly RakutenGetOrder _rakutenGetOrder;

   public RakutenApiExecute(
      RakutenSearchOrder rakutenSearchOrder,
      RakutenGetOrder rakutenGetOrder)
   {
      _rakutenSearchOrder = rakutenSearchOrder ?? throw new ArgumentNullException(nameof(rakutenSearchOrder));
      _rakutenGetOrder = rakutenGetOrder ?? throw new ArgumentNullException(nameof(rakutenGetOrder));
   }

   /// <summary>
   /// Rakuten注文明細情報を取得する。
   /// リクエストに対する全オーダーを取得する。ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="searchOrderRequest">注文リストリクエスト</param>
   /// <param name="rakutenShop">楽天ショップ</param>
   /// <returns name="getOrderResponse">注文明細リスト</returns>
   public RakutenGetOrderResponse GetOrder(
      RakutenSearchOrderRequest searchOrderRequest,
      RakutenShop rakutenShop)
   {
      // RunGetSearchOrderはページング処理を実行します。リクエストに対する全オーダーを取得します。
      var searchOrder = _rakutenSearchOrder.RunGetSearchOrder(searchOrderRequest, rakutenShop);
      var orderNumberList = searchOrder.OrderNumberList ?? new List<string>();

      // GetAllOrdersFromSearchはページング処理を実行します。
      // 全オーダー明細を取得します。（上限2000件）
      var getOrderRequest = RakutenGetOrderRequestFactory.LatestVersionRequest(orderNumberList);
      return _rakutenGetOrder.GetAllOrdersFromSearch(searchOrder, getOrderRequest, rakutenShop)
         ?? new RakutenGetOrderResponse();
   }

   /// <summary>
   /// 新規注文情報を取得する。
   /// リクエストに対する全オーダーを取得する。ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="startDate">開始日</param>
   /// <param name="endDate">終了日</param>
   /// <param name="rakutenShop">楽天ショップ</param>
   /// <returns name="getOrderResponse">新規注文明細リスト</returns>
   public RakutenGetOrderResponse GetNewOrders(DateTime? startDate, DateTime? endDate, RakutenShop rakutenShop)
   {
      ///<summary>
      /// 新規注文リクエスト詳細
      /// orderProgressListステータス  （300：発送待ち）
      /// subStatusIdListサブステータス（-1：サブステータスなし）
      ///</summary>
      var searchOrderRequest = RakutenSearchOrderRequestFactory.NewOrderRequest(startDate, endDate, 1);
      return GetOrder(searchOrderRequest, rakutenShop);
   }

   /// <summary>
   /// 発送処理中注文情報を取得する。
   /// リクエストに対する全オーダーを取得する。ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="startDate">開始日</param>
   /// <param name="endDate">終了日</param>
   /// <param name="rakutenShop">楽天ショップ</param>
   /// <returns name="getOrderResponse">新規注文明細リスト</returns>
   public RakutenGetOrderResponse GetProcessingOrdersForShipping(DateTime? startDate, DateTime? endDate, RakutenShop rakutenShop)
   {
      ///<summary>
      /// 発送処理中リクエスト詳細
      /// orderProgressListステータス  （300：発送待ち）
      /// subStatusIdListサブステータス（273015：処理中）
      ///</summary>
      var searchOrderRequest = RakutenSearchOrderRequestFactory.ShippingProcessRequest(startDate, endDate, 1);
      return GetOrder(searchOrderRequest, rakutenShop);
   }
}