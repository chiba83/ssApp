﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ssAppModels.EFModels;

public partial class DailyOrderNews
{
    [Key]
    [Column("ID")]
    public long Id { get; set; }

    /// <summary>
    /// 分割注文の最新注文日時
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime LastOrderDate { get; set; }

    /// <summary>
    /// Shopマスタの一意のショップコード
    /// </summary>
    [Required]
    [StringLength(20)]
    [Unicode(false)]
    public string ShopCode { get; set; }

    /// <summary>
    /// 出荷先郵便番号
    /// </summary>
    [Required]
    [StringLength(7)]
    [Unicode(false)]
    public string ShipZip { get; set; }

    /// <summary>
    /// 出荷先都道府県
    /// </summary>
    [Required]
    [StringLength(20)]
    public string ShipPrefecture { get; set; }

    /// <summary>
    /// 出荷先市区町村
    /// </summary>
    [Required]
    [StringLength(80)]
    public string ShipCity { get; set; }

    /// <summary>
    /// 出荷先住所２
    /// </summary>
    [StringLength(100)]
    public string ShipAddress1 { get; set; }

    /// <summary>
    /// 出荷先住所３
    /// </summary>
    [StringLength(100)]
    public string ShipAddress2 { get; set; }

    /// <summary>
    /// 出荷先氏名
    /// </summary>
    [Required]
    [StringLength(60)]
    public string ShipName { get; set; }

    /// <summary>
    /// 出荷先電話番号
    /// </summary>
    [Column("ShipTEL")]
    [StringLength(15)]
    [Unicode(false)]
    public string ShipTel { get; set; }

    /// <summary>
    /// 出荷先メールアドレス
    /// </summary>
    [StringLength(50)]
    [Unicode(false)]
    public string ShipEmail { get; set; }

    /// <summary>
    /// 注文の一意コード（各モールの注文ID）
    /// </summary>
    [Required]
    [StringLength(35)]
    [Unicode(false)]
    public string OrderId { get; set; }

    /// <summary>
    /// 注文日時（楽天は注文確定日時）
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// 注文明細行番号
    /// </summary>
    public int OrderLineId { get; set; }

    /// <summary>
    /// SKUの一意コード
    /// </summary>
    [Required]
    [Column("SKUCode")]
    [StringLength(30)]
    [Unicode(false)]
    public string Skucode { get; set; }

    /// <summary>
    /// 注文数量（3桁：999）
    /// </summary>
    [Column("OrderQTY")]
    public int OrderQty { get; set; }

    /// <summary>
    /// 配送マスタの一意の配送コード
    /// </summary>
    [StringLength(3)]
    [Unicode(false)]
    public string DeliveryCode { get; set; }

    /// <summary>
    /// 配送料
    /// </summary>
    public int? DeliveryFee { get; set; }

    /// <summary>
    /// 注文商品コードをカンマ区切りで列挙。ソート用。商品毎にピッキングを行わせ効率を上げる
    /// </summary>
    [StringLength(100)]
    [Unicode(false)]
    public string PackingSort { get; set; }

    /// <summary>
    /// 梱包内容1（配送ラベルへの記載用メモ）
    /// </summary>
    [StringLength(50)]
    public string PackingCont1 { get; set; }

    /// <summary>
    /// 梱包内容2（配送ラベルへの記載用メモ）
    /// </summary>
    [StringLength(50)]
    public string PackingCont2 { get; set; }

    /// <summary>
    /// 梱包内容3（配送ラベルへの記載用メモ）
    /// </summary>
    [StringLength(50)]
    public string PackingCont3 { get; set; }

    /// <summary>
    /// 記事（配送ラベルへの記載用メモ）
    /// </summary>
    [StringLength(50)]
    public string ShipNotes { get; set; }

    /// <summary>
    /// 出荷の一意コード（yymmdd-xxx-999：出荷日-配送コード-連番）
    /// </summary>
    [StringLength(14)]
    [Unicode(false)]
    public string ShipmentCode { get; set; }

    /// <summary>
    /// 出荷日
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime? ShipmentDate { get; set; }

    /// <summary>
    /// 追跡用の伝票番号（追跡番号は各社12桁）
    /// </summary>
    [StringLength(15)]
    [Unicode(false)]
    public string TrackingNumber { get; set; }

    /// <summary>
    /// 消費税率
    /// </summary>
    [Column(TypeName = "decimal(5, 3)")]
    public decimal ConsumptionTaxRate { get; set; }

    /// <summary>
    /// オリジナル価格（税込）
    /// </summary>
    [Column(TypeName = "decimal(8, 2)")]
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// クーポン値引き（税込）
    /// </summary>
    [Column(TypeName = "decimal(7, 2)")]
    public decimal CouponDiscount { get; set; }

    /// <summary>
    /// 注文明細合計（税込）=オリジナル価格-クーポン値引き
    /// </summary>
    [Column(TypeName = "decimal(8, 2)")]
    public decimal OrderDetailTotal { get; set; }
}