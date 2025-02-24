﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ssAppModels.EFModels;

[Table("Shipment")]
[Index("ShipmentCode", Name = "UK_Shipment_ShipmentCode", IsUnique = true)]
public partial class Shipment
{
    /// <summary>
    /// 出荷の一意の識別子
    /// </summary>
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 出荷の一意コード（yymmdd-xxx-999：出荷日-配送コード-連番）
    /// </summary>
    [Required]
    [StringLength(14)]
    [Unicode(false)]
    public string ShipmentCode { get; set; }

    /// <summary>
    /// 出荷日
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime ShipmentDate { get; set; }

    /// <summary>
    /// 配送マスタの一意の配送コード
    /// </summary>
    [Required]
    [StringLength(3)]
    [Unicode(false)]
    public string DeliveryCode { get; set; }

    /// <summary>
    /// 追跡用の伝票番号（追跡番号は各社12桁）
    /// </summary>
    [StringLength(15)]
    [Unicode(false)]
    public string TrackingNumber { get; set; }

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
    /// 出荷先住所１
    /// </summary>
    [StringLength(100)]
    public string ShipAddress1 { get; set; }

    /// <summary>
    /// 出荷先住所２
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
    /// 注文商品コードをカンマ区切りで列挙。ソート用。商品毎にピッキングを行わせ効率を上げる
    /// </summary>
    [StringLength(100)]
    [Unicode(false)]
    public string PackingSort { get; set; }

    /// <summary>
    /// 梱包内容1（配送ラベルへの記載用メモ）
    /// </summary>
    [Required]
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
    /// 配送料
    /// </summary>
    public int DeliveryFee { get; set; }

    /// <summary>
    /// 検品担当者
    /// </summary>
    [StringLength(15)]
    public string StaffName { get; set; }

    /// <summary>
    /// レコード作成日
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// レコード更新日
    /// </summary>
    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("DeliveryCode")]
    [InverseProperty("Shipments")]
    public virtual Delivery DeliveryCodeNavigation { get; set; }

    [InverseProperty("ShipmentCodeNavigation")]
    public virtual ICollection<ShipmentDetail> ShipmentDetails { get; set; } = new List<ShipmentDetail>();
}