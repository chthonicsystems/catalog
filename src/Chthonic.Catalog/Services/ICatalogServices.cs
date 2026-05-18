using Chthonic.Catalog.Domain;

namespace Chthonic.Catalog.Services;

/// <summary>
/// CRUD for tenant <see cref="Service"/> + <see cref="ServiceItem"/>.
/// Generic implementation that sister products can consume directly.
/// TorqueTech wraps these for TT-specific JobField auto-wiring; sister
/// products use the library service as-is.
/// </summary>
public interface ICatalogServiceService
{
    Task<List<Service>> ListAsync(int systemId, bool activeOnly = false);
    Task<Service?> GetWithItemsAsync(int serviceId, int systemId);
    Task<List<Service>> SearchAsync(string query, int systemId, int limit = 50);
    Task<Service> CreateAsync(int systemId, string name, string? description = null, string? icon = null, bool isActive = true);
    Task<bool> UpdateAsync(int serviceId, int systemId, string name, string? description = null, string? icon = null, bool? isActive = null);
    Task<bool> DeleteAsync(int serviceId, int systemId);
    Task<bool> ReorderAsync(int serviceId, int systemId, int displayOrder);

    Task<ServiceItem> CreateItemAsync(int serviceId, int systemId, string name, string? description, decimal? cost, string? icon, int? productId);
    Task<bool> UpdateItemAsync(int serviceItemId, int systemId, string name, string? description, decimal? cost, string? icon, int? productId);
    Task<bool> DeleteItemAsync(int serviceItemId, int systemId);
    Task<bool> ReorderItemAsync(int serviceItemId, int systemId, int displayOrder);
}

/// <summary>
/// CRUD for tenant <see cref="Product"/> + <see cref="ProductVariant"/>.
/// Generic implementation, mirrors <see cref="ICatalogServiceService"/>.
/// </summary>
public interface ICatalogProductService
{
    Task<List<Product>> ListAsync(int systemId, bool activeOnly = false);
    Task<Product?> GetWithVariantsAsync(int productId, int systemId);
    Task<List<Product>> SearchAsync(string query, int systemId, int limit = 50);
    Task<Product> CreateAsync(int systemId, string name, string? description = null, string? icon = null, bool isActive = true);
    Task<bool> UpdateAsync(int productId, int systemId, string name, string? description = null, string? icon = null, bool? isActive = null);
    Task<bool> DeleteAsync(int productId, int systemId);

    Task<ProductVariant> CreateVariantAsync(int productId, int systemId, string name, string? description, string? sku, string? barcode, decimal price, bool isActive = true);
    Task<bool> UpdateVariantAsync(int productVariantId, int systemId, string name, string? description, string? sku, string? barcode, decimal price, bool? isActive);
    Task<bool> DeleteVariantAsync(int productVariantId, int systemId);
    Task<List<ProductVariant>> SearchVariantsAsync(string query, int systemId, int limit = 50);
}
