﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ssAppModels.EFModels;

[Table("Product")]
[Index("ProductCode", Name = "UK_Product_ProductCode", IsUnique = true)]
public partial class Product
{
    /// <summary>
    /// 商品マスタの一意識別子
    /// </summary>
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 商品の一意コード
    /// </summary>
    [Required]
    [StringLength(15)]
    [Unicode(false)]
    public string ProductCode { get; set; }

    /// <summary>
    /// 商品の正式な名称
    /// </summary>
    [Required]
    [StringLength(30)]
    public string ProductName { get; set; }

    /// <summary>
    /// 商品の略称。簡易表示用
    /// </summary>
    [Required]
    [StringLength(15)]
    public string ProductAbbr { get; set; }

    /// <summary>
    /// 表示順序を決定するための番号
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 商品が有効かどうかを示すフラグ
    /// </summary>
    public bool IsActive { get; set; }

    [InverseProperty("ProductCodeNavigation")]
    public virtual ICollection<ProductSku> ProductSkus { get; set; } = new List<ProductSku>();

    [InverseProperty("ProductCodeNavigation")]
    public virtual ICollection<Skuconversion> Skuconversions { get; set; } = new List<Skuconversion>();
}