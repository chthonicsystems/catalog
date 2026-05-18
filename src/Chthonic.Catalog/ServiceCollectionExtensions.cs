using Chthonic.Catalog.Extensions;
using Chthonic.Catalog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Chthonic.Catalog;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core <c>@chthonic/catalog</c> services. Call after
    /// <c>AddChthonicTenant()</c>, <c>AddChthonicIdentity()</c>, and
    /// <c>AddChthonicAudit()</c>. Consumer must also register an
    /// <see cref="IDbContextProvider"/> implementation that returns
    /// the consumer's <c>DbContext</c> instance.
    /// </summary>
    public static IServiceCollection AddChthonicCatalog(this IServiceCollection services)
    {
        services.AddScoped<ICatalogServiceService, CatalogServiceService>();
        services.AddScoped<ICatalogProductService, CatalogProductService>();
        return services;
    }
}
