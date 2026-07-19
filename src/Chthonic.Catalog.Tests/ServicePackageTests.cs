using Chthonic.Catalog.Domain;
using Chthonic.Catalog.Extensions;
using Chthonic.Catalog.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chthonic.Catalog.Tests;

/// <summary>
/// v0.2.0 (RFC 0034) — schema shape + service invariants for
/// ServicePackage / ServicePackageItem.
/// </summary>
public class ServicePackageTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Chthonic.Tenant.Domain.System>(e =>
            {
                e.ToTable("system");
                e.HasKey(s => s.SystemId);
            });

            b.ApplyConfigurationsFromAssembly(typeof(CatalogModuleMarker).Assembly);
        }
    }

    private sealed class TestProvider : IDbContextProvider
    {
        private readonly DbContext _db;
        public TestProvider(DbContext db) => _db = db;
        public DbContext GetContext() => _db;
    }

    private static TestContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"catalog_pkg_{Guid.NewGuid()}")
            .Options;
        return new TestContext(opts);
    }

    private static IServicePackageService CreateService(TestContext db) =>
        new ServicePackageService(new TestProvider(db), NullLogger<ServicePackageService>.Instance);

    private static async Task<(int serviceItemId, int productVariantId)> SeedComponentsAsync(TestContext db, int systemId = 1)
    {
        db.Add(new Chthonic.Tenant.Domain.System { SystemId = systemId });
        var service = new Service { SystemId = systemId, Name = "Major Service", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var item = new ServiceItem { Service = service, Name = "Labour", Cost = 100m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var product = new Product { SystemId = systemId, Name = "Oil", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var variant = new ProductVariant { Product = product, Name = "10W-40 1L", Price = 15.5m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AddRange(service, item, product, variant);
        await db.SaveChangesAsync();
        return (item.ServiceItemId, variant.ProductVariantId);
    }

    // ---- Schema shape ----

    [Fact]
    public void ServicePackage_MapsToSnakeCaseTable()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(ServicePackage))!;
        Assert.Equal("service_package", entity.GetTableName());
        Assert.Equal("service_package_id", entity.FindProperty(nameof(ServicePackage.ServicePackageId))!.GetColumnName());
        Assert.Equal("system_id", entity.FindProperty(nameof(ServicePackage.SystemId))!.GetColumnName());
        Assert.Equal("service_id", entity.FindProperty(nameof(ServicePackage.ServiceId))!.GetColumnName());
    }

    [Fact]
    public void ServicePackageItem_MapsToSnakeCaseTable_WithDecimalQuantity()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(ServicePackageItem))!;
        Assert.Equal("service_package_item", entity.GetTableName());
        Assert.Equal("quantity", entity.FindProperty(nameof(ServicePackageItem.Quantity))!.GetColumnName());
        Assert.Equal(typeof(decimal), entity.FindProperty(nameof(ServicePackageItem.Quantity))!.ClrType);
        // Relational store type (decimal(10,2)) isn't surfaced by the
        // InMemory provider; assert the configured annotation directly.
        Assert.Equal("decimal(10,2)",
            entity.FindProperty(nameof(ServicePackageItem.Quantity))!.FindAnnotation("Relational:ColumnType")?.Value);
    }

    [Fact]
    public void ServicePackage_ServiceFk_IsSetNull_And_ItemFks_AreRestrict()
    {
        using var db = CreateContext();
        var pkg = db.Model.FindEntityType(typeof(ServicePackage))!;
        var serviceFk = pkg.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Service));
        Assert.Equal(DeleteBehavior.SetNull, serviceFk.DeleteBehavior);

        var item = db.Model.FindEntityType(typeof(ServicePackageItem))!;
        var componentFks = item.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(ServiceItem)
                      || fk.PrincipalEntityType.ClrType == typeof(ProductVariant));
        Assert.All(componentFks, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    // ---- Service invariants ----

    [Fact]
    public async Task Create_WithValidMixedItems_Succeeds_AndComputesSumOfComponents()
    {
        using var db = CreateContext();
        var (serviceItemId, variantId) = await SeedComponentsAsync(db);
        var svc = CreateService(db);

        var pkg = await svc.CreateAsync(1, "Brake Job", null, null, new[]
        {
            new ServicePackageItemInput(serviceItemId, null, 1m),
            new ServicePackageItemInput(null, variantId, 2m),
        });

        Assert.True(pkg.ServicePackageId > 0);
        Assert.Equal(2, pkg.Items.Count);
        // 1 × 100 (labour) + 2 × 15.50 (oil) = 131.00
        Assert.Equal(131m, await svc.ComputeTotalAsync(pkg.ServicePackageId, 1));
    }

    [Fact]
    public async Task Create_XorViolation_BothSet_Throws()
    {
        using var db = CreateContext();
        var (serviceItemId, variantId) = await SeedComponentsAsync(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(1, "Bad", null, null, new[]
            {
                new ServicePackageItemInput(serviceItemId, variantId, 1m),
            }));
    }

    [Fact]
    public async Task Create_XorViolation_NeitherSet_Throws()
    {
        using var db = CreateContext();
        await SeedComponentsAsync(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(1, "Bad", null, null, new[]
            {
                new ServicePackageItemInput(null, null, 1m),
            }));
    }

    [Fact]
    public async Task Create_NonPositiveQuantity_Throws()
    {
        using var db = CreateContext();
        var (serviceItemId, _) = await SeedComponentsAsync(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(1, "Bad", null, null, new[]
            {
                new ServicePackageItemInput(serviceItemId, null, 0m),
            }));
    }

    [Fact]
    public async Task Create_CrossSystemComponent_Throws()
    {
        using var db = CreateContext();
        var (serviceItemId, _) = await SeedComponentsAsync(db, systemId: 1);
        db.Add(new Chthonic.Tenant.Domain.System { SystemId = 2 });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        // System 2 referencing system 1's ServiceItem must fail loudly.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(2, "Sneaky", null, null, new[]
            {
                new ServicePackageItemInput(serviceItemId, null, 1m),
            }));
    }

    [Fact]
    public async Task ReplaceItems_IsAtomicWholeListReplace()
    {
        using var db = CreateContext();
        var (serviceItemId, variantId) = await SeedComponentsAsync(db);
        var svc = CreateService(db);

        var pkg = await svc.CreateAsync(1, "Major Service", null, null, new[]
        {
            new ServicePackageItemInput(serviceItemId, null, 1m),
        });

        var ok = await svc.ReplaceItemsAsync(pkg.ServicePackageId, 1, new[]
        {
            new ServicePackageItemInput(null, variantId, 3m),
        });

        Assert.True(ok);
        var reloaded = await svc.GetWithItemsAsync(pkg.ServicePackageId, 1);
        Assert.Single(reloaded!.Items);
        Assert.Equal(variantId, reloaded.Items[0].ProductVariantId);
        Assert.Equal(3m, reloaded.Items[0].Quantity);
    }

    [Fact]
    public async Task List_IsSystemScoped()
    {
        using var db = CreateContext();
        var (serviceItemId, _) = await SeedComponentsAsync(db, systemId: 1);
        db.Add(new Chthonic.Tenant.Domain.System { SystemId = 2 });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        await svc.CreateAsync(1, "Sys1 Package", null, null, new[]
        {
            new ServicePackageItemInput(serviceItemId, null, 1m),
        });

        Assert.Single(await svc.ListAsync(1));
        Assert.Empty(await svc.ListAsync(2));
    }

    [Fact]
    public async Task Delete_CascadesItems_AndGetReturnsNull()
    {
        using var db = CreateContext();
        var (serviceItemId, _) = await SeedComponentsAsync(db);
        var svc = CreateService(db);

        var pkg = await svc.CreateAsync(1, "To delete", null, null, new[]
        {
            new ServicePackageItemInput(serviceItemId, null, 1m),
        });

        Assert.True(await svc.DeleteAsync(pkg.ServicePackageId, 1));
        Assert.Null(await svc.GetWithItemsAsync(pkg.ServicePackageId, 1));
    }
}
