using Chthonic.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chthonic.Catalog.Configuration;

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> entity)
    {
        entity.ToTable("service");
        entity.HasKey(e => e.ServiceId);

        entity.Property(e => e.ServiceId).HasColumnName("service_id");
        entity.Property(e => e.SystemId).HasColumnName("system_id").IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(500);
        entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        entity.HasOne(e => e.System)
            .WithMany()
            .HasForeignKey(e => e.SystemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => new { e.SystemId, e.DisplayOrder })
            .HasDatabaseName("idx_service_system_order");
    }
}

internal sealed class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> entity)
    {
        entity.ToTable("service_item");
        entity.HasKey(e => e.ServiceItemId);

        entity.Property(e => e.ServiceItemId).HasColumnName("service_item_id");
        entity.Property(e => e.ServiceId).HasColumnName("service_id").IsRequired();
        entity.Property(e => e.ProductId).HasColumnName("product_id");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(e => e.Cost).HasColumnName("cost").HasColumnType("decimal(10,2)");
        entity.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(500);
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        entity.HasOne(e => e.Service)
            .WithMany(s => s.ServiceItems)
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cross-FK to Product. The reverse nav (Product.ServiceItems collection)
        // stays in Product (intra-catalog inverse — both ends in same library).
        entity.HasOne(e => e.Product)
            .WithMany(p => p.ServiceItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(e => new { e.ServiceId, e.DisplayOrder })
            .HasDatabaseName("idx_service_item_service_order");
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("product");
        entity.HasKey(e => e.ProductId);

        entity.Property(e => e.ProductId).HasColumnName("product_id");
        entity.Property(e => e.SystemId).HasColumnName("system_id").IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(500);
        entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        entity.HasOne(e => e.System)
            .WithMany()
            .HasForeignKey(e => e.SystemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => new { e.SystemId, e.DisplayOrder })
            .HasDatabaseName("idx_product_system_order");
    }
}

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        entity.ToTable("product_variant");
        entity.HasKey(e => e.ProductVariantId);

        entity.Property(e => e.ProductVariantId).HasColumnName("product_variant_id");
        entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(100);
        entity.Property(e => e.Barcode).HasColumnName("barcode").HasMaxLength(100);
        entity.Property(e => e.ExternalAccountingItemId).HasColumnName("external_accounting_item_id").HasMaxLength(255);
        entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(10,2)").IsRequired();
        entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        entity.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        entity.HasOne(e => e.Product)
            .WithMany(p => p.ProductVariants)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => new { e.ProductId, e.DisplayOrder })
            .HasDatabaseName("idx_product_variant_product_order");

        // Barcode unique per product (when set). MySQL ignores HasFilter
        // but we mirror the TT-side configuration verbatim to avoid drift.
        entity.HasIndex(e => new { e.ProductId, e.Barcode })
            .HasDatabaseName("idx_product_variant_barcode")
            .IsUnique()
            .HasFilter("[barcode] IS NOT NULL");

        entity.HasIndex(e => e.ExternalAccountingItemId)
            .HasDatabaseName("idx_product_variant_external_item_id");
    }
}
