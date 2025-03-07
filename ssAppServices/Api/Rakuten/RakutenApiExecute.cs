using ssAppModels.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppServices.Api.Rakuten;

public class RakutenApiExecute(
   RakutenSearchOrder rakutenSearchOrder,
   RakutenGetOrder rakutenGetOrder)
{
   private readonly RakutenSearchOrder _rakutenSearchOrder = rakutenSearchOrder ?? throw new ArgumentNullException(nameof(rakutenSearchOrder));
   private readonly RakutenGetOrder _rakutenGetOrder = rakutenGetOrder ?? throw new ArgumentNullException(nameof(rakutenGetOrder));

   /// <summary>
   /// Rakuten注文明細情報を取得する。
   /// リクエストに対する全オーダーを取得する。ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="searchOrderRequest">注文リストリクエスト</param>
   /// <param name="rakutenShop">楽天ショップ</param>
   /// <returns name="getOrderResponse">注文明細リスト</returns>
   public RakutenGetOrderResponse GetOrder(
      RakutenSearchOrderRequest searchOrderRequest, RakutenShop rakutenShop)
   {
      // RunGetSearchOrderはページング処理を実行します。リクエストに対する全オーダーを取得します。
      var searchOrder = _rakutenSearchOrder.RunGetSearchOrder(searchOrderRequest, rakutenShop);
      var orderNumberList = searchOrder.OrderNumberList ?? [];

      // GetAllOrdersFromSearchはページング処理を実行します。
      // 全オーダー明細を取得します。（上限2000件）
      var getOrderRequest = RakutenGetOrderRequestFactory.LatestVersionRequest(orderNumberList);
      return _rakutenGetOrder.GetAllOrdersFromSearch(searchOrder, getOrderRequest, rakutenShop)
         ?? new RakutenGetOrderResponse();
   }

   /// <summary>
   /// 各ステータスの注文情報リクエストパラメータを取得する。
   /// ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="status">対象ステータス</param>
   /// <param name="startDate">開始日</param>
   /// <param name="endDate">終了日</param>
   /// <param name="rakutenShop">楽天ショップ</param>
   /// <returns name="getOrderResponse">新規注文明細リスト</returns>
   public RakutenGetOrderResponse GetRakutenOrders(
      OrderStatus status, DateTime? startDate, DateTime? endDate, 
      RakutenShop rakutenShop)
   {

      RakutenSearchOrderRequest searchOrderRequest = status switch
      {
         ///<summary>
         /// 新規注文リクエスト詳細
         /// orderProgressListステータス  （300：発送待ち）
         /// subStatusIdListサブステータス（-1：サブステータスなし）
         /// StartDatetime             　（開始日デフォルト：-30日）
         ///</summary>
         OrderStatus.NewOrder => RakutenSearchOrderRequestFactory.NewOrderRequest(startDate, endDate, 1),

         ///<summary>
         /// 梱包処理用リクエスト詳細
         /// orderProgressListステータス  （300：発送待ち）
         /// subStatusIdListサブステータス（273015：処理中）
         /// StartDatetime             　（開始日デフォルト：-30日）
         ///</summary>
         OrderStatus.Packing => RakutenSearchOrderRequestFactory.ShippingProcessRequest(startDate, endDate, 1),

         ///<summary>
         /// 出荷完了リクエスト詳細
         /// orderProgressListステータス  （400：変更確定待ち、500: 発送済、600: 支払手続き中、700: 支払手続き済）
         /// StartDatetime             　（開始日デフォルト：-30日）
         ///</summary>
         OrderStatus.Shipped => RakutenSearchOrderRequestFactory.ShippedOrderRequest(startDate, endDate, 1),

         ///<summary>
         /// プレゼント対象注文リクエスト詳細
         /// orderProgressListステータス  （400：変更確定待ち、500: 発送済、600: 支払手続き中、700: 支払手続き済）
         /// StartDatetime             　（開始日デフォルト：-60日）
         ///</summary>
         OrderStatus.Present => RakutenSearchOrderRequestFactory.PresentTargetOrderRequest(startDate, endDate, 1),

         _ => throw new Exception("楽天注文リクエストを設定してください。"),
      };
      return GetOrder(searchOrderRequest, rakutenShop);
   }
}