using Chthonic.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Chthonic.Catalog.Tests;

/// <summary>
/// Schema tests: verify the EF entity-type configurations match the
/// TT-side schema (table names, column names, indexes).
/// </summary>
public class CatalogConfigurationTests
{
    /// <summary>
    /// Test DbContext that applies catalog configurations + provides a
    /// minimal stub for <c>Chthonic.Tenant.Domain.System</c> (a real
    /// consumer would consume tenant's configurations via
    /// <c>ApplyConfigurationsFromAssembly(typeof(TenantModuleMarker).Assembly)</c>).
    /// </summary>
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Minimum viable System config so HasOne(e => e.System) navs
            // resolve. Real consumers consume tenant's configurations.
            b.Entity<Chthonic.Tenant.Domain.System>(e =>
            {
                e.ToTable("system");
                e.HasKey(s => s.SystemId);
            });

            b.ApplyConfigurationsFromAssembly(typeof(CatalogModuleMarker).Assembly);
        }
    }

    private static TestContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"catalog_cfg_{Guid.NewGuid()}")
            .Options;
        return new TestContext(opts);
    }

    [Fact]
    public void Service_MapsToServiceTable()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Service))!;
        Assert.Equal("service", entity.GetTableName());
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "service_id");
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "system_id");
    }

    [Fact]
    public void ServiceItem_MapsToServiceItemTable()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ServiceItem))!;
        Assert.Equal("service_item", entity.GetTableName());
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "service_item_id");
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "cost");
    }

    [Fact]
    public void Product_MapsToProductTable()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Product))!;
        Assert.Equal("product", entity.GetTableName());
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "product_id");
    }

    [Fact]
    public void ProductVariant_MapsToProductVariantTable()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ProductVariant))!;
        Assert.Equal("product_variant", entity.GetTableName());
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "product_variant_id");
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "sku");
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "barcode");
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "external_accounting_item_id");
    }

    [Fact]
    public void Service_HasIndex_OnSystemIdAndDisplayOrder()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Service))!;
        Assert.Contains(entity.GetIndexes(), i => i.GetDatabaseName() == "idx_service_system_order");
    }

    [Fact]
    public void ProductVariant_HasUniqueIndex_OnBarcode()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ProductVariant))!;
        var barcodeIdx = entity.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == "idx_product_variant_barcode");
        Assert.NotNull(barcodeIdx);
        Assert.True(barcodeIdx!.IsUnique);
    }

    [Fact]
    public void ServiceItem_FK_ToProduct_HasSetNullDeleteBehavior()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ServiceItem))!;
        var productFk = entity.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Product));
        Assert.NotNull(productFk);
        Assert.Equal(DeleteBehavior.SetNull, productFk!.DeleteBehavior);
    }
}
