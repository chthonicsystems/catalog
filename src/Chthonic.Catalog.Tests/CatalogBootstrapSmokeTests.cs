using Chthonic.Audit;
using Chthonic.Catalog;
using Chthonic.Catalog.Domain;
using Chthonic.Catalog.Extensions;
using Chthonic.Catalog.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace Chthonic.Catalog.Tests;

public class CatalogBootstrapSmokeTests
{
    [Fact]
    public void AddChthonicCatalog_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IDbContextProvider, StubDbContextProvider>();
        services.AddChthonicCatalog();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICatalogServiceService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICatalogProductService>());
    }

    [Fact]
    public void CatalogModuleMarker_IsAccessible()
    {
        var asm = typeof(CatalogModuleMarker).Assembly;
        Assert.NotNull(asm);
        Assert.Equal("Chthonic.Catalog", asm.GetName().Name);
    }

    [Fact]
    public void ServiceItem_HasAuditParentToService()
    {
        var attr = typeof(ServiceItem).GetCustomAttribute<AuditParentAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(Service), attr!.ParentType);
        Assert.Equal(nameof(ServiceItem.ServiceId), attr.ForeignKeyProperty);
    }

    [Fact]
    public void ProductVariant_HasAuditParentToProduct()
    {
        var attr = typeof(ProductVariant).GetCustomAttribute<AuditParentAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(Product), attr!.ParentType);
        Assert.Equal(nameof(ProductVariant.ProductId), attr.ForeignKeyProperty);
    }

    [Fact]
    public void ServiceItem_DoesNotExposeJobFieldsInverseNav()
    {
        // Per RFC 0021 § 3 (cycle-break): the ServiceItem.JobFields inverse
        // collection nav was dropped in the lift to break the catalog ↔ views
        // circular dependency. Direction is one-way: Views → Catalog.
        var hasJobFields = typeof(ServiceItem).GetProperty("JobFields") != null;
        Assert.False(hasJobFields, "ServiceItem must NOT carry an inverse JobFields collection (RFC 0021 § 3 cycle-break).");
    }

    [Fact]
    public void Product_DoesNotExposeJobFieldsInverseNav()
    {
        var hasJobFields = typeof(Product).GetProperty("JobFields") != null;
        Assert.False(hasJobFields, "Product must NOT carry an inverse JobFields collection (RFC 0021 § 3 cycle-break).");
    }

    [Fact]
    public void Service_HasIntraCatalogServiceItemsCollection()
    {
        // Intra-catalog inverse (Service → ServiceItem) IS allowed and required.
        var prop = typeof(Service).GetProperty("ServiceItems");
        Assert.NotNull(prop);
        Assert.Equal(typeof(ICollection<ServiceItem>), prop!.PropertyType);
    }

    [Fact]
    public void Product_HasIntraCatalogProductVariantsCollection()
    {
        var prop = typeof(Product).GetProperty("ProductVariants");
        Assert.NotNull(prop);
        Assert.Equal(typeof(ICollection<ProductVariant>), prop!.PropertyType);
    }

    [Fact]
    public void Product_HasIntraCatalogServiceItemsCollection()
    {
        // Inverse for ServiceItem.ProductId (intra-catalog FK).
        var prop = typeof(Product).GetProperty("ServiceItems");
        Assert.NotNull(prop);
        Assert.Equal(typeof(ICollection<ServiceItem>), prop!.PropertyType);
    }

    private sealed class StubDbContextProvider : IDbContextProvider
    {
        public DbContext GetContext()
        {
            var opts = new DbContextOptionsBuilder<StubContext>().UseInMemoryDatabase("smoke").Options;
            return new StubContext(opts);
        }
    }

    private sealed class StubContext : DbContext
    {
        public StubContext(DbContextOptions<StubContext> opts) : base(opts) { }
    }
}
