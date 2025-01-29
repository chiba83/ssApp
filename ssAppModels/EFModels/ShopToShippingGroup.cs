﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ssAppModels.EFModels;

[Table("ShopToShippingGroup")]
public partial class ShopToShippingGroup
{
    /// <summary>
    /// 自動インクリメントのプライマリキー
    /// </summary>
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// Shopマスタの一意コード
    /// </summary>
    [Required]
    [StringLength(20)]
    [Unicode(false)]
    public string ShopCode { get; set; }

    /// <summary>
    /// 配送条件グループID
    /// </summary>
    [Required]
    [Column("ShippingGroupID")]
    [StringLength(20)]
    [Unicode(false)]
    public string ShippingGroupId { get; set; }

    [ForeignKey("ShippingGroupId")]
    [InverseProperty("ShopToShippingGroups")]
    public virtual ShippingGroup ShippingGroup { get; set; }

    [ForeignKey("ShopCode")]
    [InverseProperty("ShopToShippingGroups")]
    public virtual Shop ShopCodeNavigation { get; set; }
}