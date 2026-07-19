using Chthonic.Catalog.Domain;

namespace Chthonic.Catalog.Services;

/// <summary>
/// One component of a package create/replace request. Exactly one of
/// <see cref="ServiceItemId"/> XOR <see cref="ProductVariantId"/> must
/// be set; <see cref="Quantity"/> must be &gt; 0.
/// </summary>
public sealed record ServicePackageItemInput(
    int? ServiceItemId,
    int? ProductVariantId,
    decimal Quantity);

/// <summary>
/// CRUD for tenant <see cref="ServicePackage"/> bundles (v0.2.0 /
/// RFC 0034). Generic implementation sister products consume directly;
/// TorqueTech mounts the endpoints and owns apply-semantics separately
/// (RFC 0034 § 12b — apply is consumer-side).
/// </summary>
/// <remarks>
/// Item writes are whole-list replaces (<see cref="ReplaceItemsAsync"/>)
/// — packages are small (typically &lt; 20 rows) and the atomic-replace
/// shape matches how the Config Hub editor saves.
/// </remarks>
public interface IServicePackageService
{
    Task<List<ServicePackage>> ListAsync(int systemId, bool activeOnly = false);
    Task<ServicePackage?> GetWithItemsAsync(int servicePackageId, int systemId);
    Task<List<ServicePackage>> SearchAsync(string query, int systemId, int limit = 50);

    /// <exception cref="ArgumentException">On XOR violation, non-positive quantity, or a component not belonging to <paramref name="systemId"/>.</exception>
    Task<ServicePackage> CreateAsync(int systemId, string name, string? description, int? serviceId, IReadOnlyList<ServicePackageItemInput> items, bool isActive = true);

    Task<bool> UpdateAsync(int servicePackageId, int systemId, string name, string? description, int? serviceId, bool? isActive);

    /// <summary>Atomically replaces the package's component rows.</summary>
    /// <exception cref="ArgumentException">On XOR violation, non-positive quantity, or a component not belonging to <paramref name="systemId"/>.</exception>
    Task<bool> ReplaceItemsAsync(int servicePackageId, int systemId, IReadOnlyList<ServicePackageItemInput> items);

    Task<bool> DeleteAsync(int servicePackageId, int systemId);
    Task<bool> ReorderAsync(int servicePackageId, int systemId, int displayOrder);

    /// <summary>
    /// Sum-of-components total (RFC 0034 § 3): Σ item.Quantity ×
    /// (ServiceItem.Cost ?? 0 | ProductVariant.Price).
    /// </summary>
    Task<decimal> ComputeTotalAsync(int servicePackageId, int systemId);
}
