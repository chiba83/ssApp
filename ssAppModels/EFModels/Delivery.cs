﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ssAppModels.EFModels;

[Table("Delivery")]
[Index("DeliveryCode", Name = "UK_Delivery_DeliveryCode", IsUnique = true)]
public partial class Delivery
{
    /// <summary>
    /// 配送マスタの一意識別子
    /// </summary>
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 配送会社コード（4桁：XXXX）
    /// </summary>
    [Required]
    [StringLength(4)]
    [Unicode(false)]
    public string DeliveryCompanyCode { get; set; }

    /// <summary>
    /// 日本郵便、ヤマト運輸、佐川急便、YFF
    /// </summary>
    [Required]
    [StringLength(15)]
    public string DeliveryCompanyName { get; set; }

    /// <summary>
    /// 配送の一意コード（3桁：XXX）
    /// </summary>
    [Required]
    [StringLength(3)]
    [Unicode(false)]
    public string DeliveryCode { get; set; }

    /// <summary>
    /// 配送サービス名（クリックポスト、ネコポス、普通郵便・・・）
    /// </summary>
    [Required]
    [StringLength(15)]
    public string DeliveryName { get; set; }

    /// <summary>
    /// 配送料
    /// </summary>
    public int DeliveryFee { get; set; }

    /// <summary>
    /// 配送追跡サービスの有無
    /// </summary>
    public bool TrackingFlag { get; set; }

    /// <summary>
    /// 配送方法が複数ある場合の適用優先順
    /// </summary>
    public int DeliveryPriority { get; set; }

    /// <summary>
    /// 配送方法が有効かどうかを示すフラグ
    /// </summary>
    public bool IsActive { get; set; }

    [InverseProperty("DeliveryCodeNavigation")]
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    [InverseProperty("DeliveryCodeNavigation")]
    public virtual ICollection<ShippingCondition> ShippingConditions { get; set; } = new List<ShippingCondition>();
}