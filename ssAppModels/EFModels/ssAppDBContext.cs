﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ssAppModels.EFModels;

public partial class ssAppDBContext : DbContext
{
    public ssAppDBContext(DbContextOptions<ssAppDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderHeader> OrderHeaders { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductSku> ProductSkus { get; set; }

    public virtual DbSet<SetProductSku> SetProductSkus { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<ShipmentDetail> ShipmentDetails { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<ShopToken> ShopTokens { get; set; }

    public virtual DbSet<Skuconversion> Skuconversions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Delivery_ID");

            entity.Property(e => e.Id).HasComment("配送マスタの一意識別子");
            entity.Property(e => e.DeliveryCode)
                .IsFixedLength()
                .HasComment("配送の一意コード（3桁：XXX）");
            entity.Property(e => e.DeliveryCompanyCode)
                .IsFixedLength()
                .HasComment("配送会社コード（4桁：XXXX）");
            entity.Property(e => e.DeliveryCompanyName).HasComment("日本郵便、ヤマト運輸、佐川急便、YFF");
            entity.Property(e => e.DeliveryFee).HasComment("配送料");
            entity.Property(e => e.DeliveryName).HasComment("配送サービス名（クリックポスト、ネコポス、普通郵便・・・）");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasComment("配送方法が有効かどうかを示すフラグ");
            entity.Property(e => e.TrackingFlag).HasComment("配送追跡サービスの有無");
        });

        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ErrorLog__3214EC27C6F6F83E");

            entity.Property(e => e.Id).HasComment("自動インクリメントのプライマリキー");
            entity.Property(e => e.AdditionalInfo).HasComment("任意の補足情報（リクエストデータなど）");
            entity.Property(e => e.CreatedAt).HasComment("レコードの作成日時（記録時点）");
            entity.Property(e => e.ErrorMessage).HasComment("エラーメッセージ");
            entity.Property(e => e.MethodName).HasComment("エラーが発生したメソッド名");
            entity.Property(e => e.ServiceName).HasComment("エラーが発生したサービス名やモジュール名");
            entity.Property(e => e.StackTrace).HasComment("エラーのスタックトレース");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderDetail_ID");

            entity.Property(e => e.Id).HasComment("注文明細の一意識別子");
            entity.Property(e => e.ConsumptionTaxRate).HasComment("消費税率");
            entity.Property(e => e.CostId).HasComment("月次仕入原価の一意コード（yyyymm_xxxxxxx：出荷年月 + \"_\" + SKUCode）");
            entity.Property(e => e.CouponDiscount).HasComment("クーポン値引き（税込）");
            entity.Property(e => e.CreatedAt).HasComment("レコード作成日");
            entity.Property(e => e.OrderCode).HasComment("注文の一意の注文コード（各モールの注文ID）");
            entity.Property(e => e.OrderDetailCode).HasComment("注文明細の一意コード");
            entity.Property(e => e.OrderDetailTotal).HasComment("注文明細合計（税込）=オリジナル価格-クーポン値引き");
            entity.Property(e => e.OrderQty).HasComment("注文数量（3桁：999）");
            entity.Property(e => e.OriginalPrice).HasComment("オリジナル価格（税込）");
            entity.Property(e => e.Skucode).HasComment("SKUの一意コード");
            entity.Property(e => e.UpdatedAt).HasComment("レコード更新日");

            entity.HasOne(d => d.OrderCodeNavigation).WithMany(p => p.OrderDetails)
                .HasPrincipalKey(p => p.OrderCode)
                .HasForeignKey(d => d.OrderCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetail_OrderCode");

            entity.HasOne(d => d.SkucodeNavigation).WithMany(p => p.OrderDetails)
                .HasPrincipalKey(p => p.Skucode)
                .HasForeignKey(d => d.Skucode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetail_SKUCode");
        });

        modelBuilder.Entity<OrderHeader>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderHeader_ID");

            entity.Property(e => e.Id).HasComment("注文の一意の識別子");
            entity.Property(e => e.CreatedAt).HasComment("レコード作成日");
            entity.Property(e => e.OrderCode).HasComment("注文の一意コード（各モールの注文ID）");
            entity.Property(e => e.OrderDate).HasComment("注文日時（楽天は注文確定日時）");
            entity.Property(e => e.ShopCode).HasComment("Shopマスタの一意のショップコード");
            entity.Property(e => e.UpdatedAt).HasComment("レコード更新日");

            entity.HasOne(d => d.ShopCodeNavigation).WithMany(p => p.OrderHeaders)
                .HasPrincipalKey(p => p.ShopCode)
                .HasForeignKey(d => d.ShopCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderHeader_ShopCode");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Product_ID");

            entity.Property(e => e.Id).HasComment("商品マスタの一意識別子");
            entity.Property(e => e.IsActive).HasComment("商品が有効かどうかを示すフラグ");
            entity.Property(e => e.ProductAbbr).HasComment("商品の略称。簡易表示用");
            entity.Property(e => e.ProductCode).HasComment("商品の一意コード");
            entity.Property(e => e.ProductName).HasComment("商品の正式な名称");
            entity.Property(e => e.SortOrder).HasComment("表示順序を決定するための番号");
        });

        modelBuilder.Entity<ProductSku>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductSKU_ID");

            entity.Property(e => e.Id).HasComment("商品SKUマスタの一意識別子");
            entity.Property(e => e.ImageUrl).HasComment("SKUの画像のURL");
            entity.Property(e => e.IsActive).HasComment("SKUが有効かどうかを示すフラグ");
            entity.Property(e => e.ProductCode).HasComment("商品マスタの一意の商品コード");
            entity.Property(e => e.SalesCategory)
                .IsFixedLength()
                .HasComment("販売区分（Set：セット品販売、SKU：単品販売）");
            entity.Property(e => e.Skuabbr).HasComment("SKUの略称。簡易表示用");
            entity.Property(e => e.Skucode).HasComment("SKUの一意コード");
            entity.Property(e => e.Skuname).HasComment("SKUの正式な名称");
            entity.Property(e => e.SortOrder).HasComment("SKUごとの表示順序");
            entity.Property(e => e.SupplierUrl).HasComment("SKUの仕入れ先のURL");

            entity.HasOne(d => d.ProductCodeNavigation).WithMany(p => p.ProductSkus)
                .HasPrincipalKey(p => p.ProductCode)
                .HasForeignKey(d => d.ProductCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSKU_ProductCode");
        });

        modelBuilder.Entity<SetProductSku>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SetProductSKU_ID");

            entity.Property(e => e.Id).HasComment("セット品構成マスタの一意識別子");
            entity.Property(e => e.ComponentQty).HasComment("構成SKUの数量");
            entity.Property(e => e.SetSkucode).HasComment("セット商品のコード（セット商品コードは商品SKUのSKUコードとN対１の関係）");
            entity.Property(e => e.Skucode).HasComment("SKUコードは商品SKUのSKUコードとN対１の関係");

            entity.HasOne(d => d.SetSkucodeNavigation).WithMany(p => p.SetProductSkuSetSkucodeNavigations)
                .HasPrincipalKey(p => p.Skucode)
                .HasForeignKey(d => d.SetSkucode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SetProductSKU_SetSKUCode");

            entity.HasOne(d => d.SkucodeNavigation).WithMany(p => p.SetProductSkuSkucodeNavigations)
                .HasPrincipalKey(p => p.Skucode)
                .HasForeignKey(d => d.Skucode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SetProductSKU_SKUCode");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Shipment_ID");

            entity.Property(e => e.Id).HasComment("出荷の一意の識別子");
            entity.Property(e => e.CreatedAt).HasComment("レコード作成日");
            entity.Property(e => e.DeliveryCode)
                .IsFixedLength()
                .HasComment("配送マスタの一意の配送コード");
            entity.Property(e => e.DeliveryFee).HasComment("配送料");
            entity.Property(e => e.PackingCont1).HasComment("梱包内容1（配送ラベルへの記載用メモ）");
            entity.Property(e => e.PackingCont2).HasComment("梱包内容2（配送ラベルへの記載用メモ）");
            entity.Property(e => e.PackingCont3).HasComment("梱包内容3（配送ラベルへの記載用メモ）");
            entity.Property(e => e.PackingSort).HasComment("注文商品コードをカンマ区切りで列挙。ソート用。商品毎にピッキングを行わせ効率を上げる");
            entity.Property(e => e.ShipAddress1).HasComment("出荷先住所１");
            entity.Property(e => e.ShipAddress2).HasComment("出荷先住所２");
            entity.Property(e => e.ShipCity).HasComment("出荷先市区町村");
            entity.Property(e => e.ShipEmail).HasComment("出荷先メールアドレス");
            entity.Property(e => e.ShipName).HasComment("出荷先氏名");
            entity.Property(e => e.ShipNotes).HasComment("記事（配送ラベルへの記載用メモ）");
            entity.Property(e => e.ShipPrefecture).HasComment("出荷先都道府県");
            entity.Property(e => e.ShipTel).HasComment("出荷先電話番号");
            entity.Property(e => e.ShipZip)
                .IsFixedLength()
                .HasComment("出荷先郵便番号");
            entity.Property(e => e.ShipmentCode)
                .IsFixedLength()
                .HasComment("出荷の一意コード（yymmdd-xxx-999：出荷日-配送コード-連番）");
            entity.Property(e => e.ShipmentDate).HasComment("出荷日");
            entity.Property(e => e.StaffName).HasComment("検品担当者");
            entity.Property(e => e.TrackingNumber).HasComment("追跡用の伝票番号（追跡番号は各社12桁）");
            entity.Property(e => e.UpdatedAt).HasComment("レコード更新日");

            entity.HasOne(d => d.DeliveryCodeNavigation).WithMany(p => p.Shipments)
                .HasPrincipalKey(p => p.DeliveryCode)
                .HasForeignKey(d => d.DeliveryCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipment_DeliveryCode");
        });

        modelBuilder.Entity<ShipmentDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ShipmentDetail_ID");

            entity.Property(e => e.Id).HasComment("出荷明細の一意識別子");
            entity.Property(e => e.CreatedAt).HasComment("レコード作成日");
            entity.Property(e => e.DeliveryDetailFee).HasComment("出荷送料を明細ごとに按分");
            entity.Property(e => e.InspectionFlag).HasComment("検品済みフラグ");
            entity.Property(e => e.OrderDetailCode).HasComment("注文明細の一意のコード");
            entity.Property(e => e.PackingQty).HasComment("梱包数量");
            entity.Property(e => e.ShipmentCode)
                .IsFixedLength()
                .HasComment("出荷の一意の出荷コード");
            entity.Property(e => e.ShipmentDetailCode)
                .IsFixedLength()
                .HasComment("出荷明細の一意コード（yymmdd-xxx-xxx-L99：出荷コード+L連番）");
            entity.Property(e => e.UpdatedAt).HasComment("レコード更新日");

            entity.HasOne(d => d.OrderDetailCodeNavigation).WithMany(p => p.ShipmentDetails)
                .HasPrincipalKey(p => p.OrderDetailCode)
                .HasForeignKey(d => d.OrderDetailCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShipmentDetail_OrderDetailCode");

            entity.HasOne(d => d.ShipmentCodeNavigation).WithMany(p => p.ShipmentDetails)
                .HasPrincipalKey(p => p.ShipmentCode)
                .HasForeignKey(d => d.ShipmentCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShipmentDetail_ShipmentCode");
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Shop_ID");

            entity.Property(e => e.Id).HasComment("Shopマスタの一意識別子");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasComment("モールショップが有効かどうかを示すフラグ");
            entity.Property(e => e.MallCode).HasComment("Yahoo、Rakuten、Amazon");
            entity.Property(e => e.ShopCode).HasComment("Shopマスタの一意コード");
            entity.Property(e => e.ShopName).HasComment("LARAL、Yours、ENZO");
        });

        modelBuilder.Entity<ShopToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ShopTokens_ID");

            entity.Property(e => e.Id).HasComment("認証トークン管理の一意識別子");
            entity.Property(e => e.AccessToken).HasComment("ヤフー専用（アクセストークン）");
            entity.Property(e => e.AppType)
                .IsFixedLength()
                .HasComment("ヤフー専用（client , server）");
            entity.Property(e => e.AtexpiresAt).HasComment("ヤフー専用（アクセストークン有効期限）");
            entity.Property(e => e.AuthCode)
                .IsFixedLength()
                .HasComment("ヤフー専用（許可コード）");
            entity.Property(e => e.CallbackUri).HasComment("コールバックURL");
            entity.Property(e => e.ClientId).HasComment("ヤフー：クライアントID、楽天：LicenseKey");
            entity.Property(e => e.PkexpiresAt).HasComment("ヤフー専用（公開キー有効期限）");
            entity.Property(e => e.PublicKey).HasComment("ヤフー専用（公開キー）");
            entity.Property(e => e.RefreshToken).HasComment("ヤフー専用（リフレッシュトークン）");
            entity.Property(e => e.RtexpiresAt).HasComment("ヤフー専用（リフレッシュトークン有効期限）");
            entity.Property(e => e.Secret).HasComment("ヤフー：シークレット、楽天：サービスシークレット");
            entity.Property(e => e.ShopCode).HasComment("Shopマスタの一意コード");
            entity.Property(e => e.TokenType)
                .IsFixedLength()
                .HasComment("トークンの種類（Basic、Bearer）");
            entity.Property(e => e.UpdatedAt).HasComment("レコード更新日");

            entity.HasOne(d => d.ShopCodeNavigation).WithOne(p => p.ShopToken)
                .HasPrincipalKey<Shop>(p => p.ShopCode)
                .HasForeignKey<ShopToken>(d => d.ShopCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopTokens_ShopCode");
        });

        modelBuilder.Entity<Skuconversion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SKUConversion_ID");

            entity.Property(e => e.Id).HasComment("SKU変換マスタの一意識別子");
            entity.Property(e => e.ProductCode).HasComment("変換元商品コード（商品コード）");
            entity.Property(e => e.ShopCode).HasComment("Shopマスタの一意のショップコード");
            entity.Property(e => e.ShopProductCode).HasComment("変換先商品コード（ショップ商品コード）");
            entity.Property(e => e.ShopSkucode).HasComment("変換先SKUコード（ショップSKUコード）");
            entity.Property(e => e.Skucode).HasComment("変換元SKUコード（商品SKUコード）");

            entity.HasOne(d => d.ProductCodeNavigation).WithMany(p => p.Skuconversions)
                .HasPrincipalKey(p => p.ProductCode)
                .HasForeignKey(d => d.ProductCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SKUConversion_ProductCode");

            entity.HasOne(d => d.ShopCodeNavigation).WithMany(p => p.Skuconversions)
                .HasPrincipalKey(p => p.ShopCode)
                .HasForeignKey(d => d.ShopCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SKUConversion_ShopCode");

            entity.HasOne(d => d.SkucodeNavigation).WithMany(p => p.Skuconversions)
                .HasPrincipalKey(p => p.Skucode)
                .HasForeignKey(d => d.Skucode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SKUConversion_SKUCode");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}