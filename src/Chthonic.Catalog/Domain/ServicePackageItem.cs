using Chthonic.Audit;

namespace Chthonic.Catalog.Domain;

/// <summary>
/// One component row of a <see cref="ServicePackage"/> — exactly one of
/// <see cref="ServiceItemId"/> (labour / priced service line) XOR
/// <see cref="ProductVariantId"/> (tangible part) is set.
/// </summary>
/// <remarks>
/// Added in v0.2.0 per RFC 0034. The XOR invariant is enforced three
/// ways: (1) the package service validates on write,
/// (2) consumer-side schema applies a DB CHECK constraint (the library
/// migration is an empty placeholder per the coexistence pattern —
/// RFC 0034 § 12j), (3) apply-time consumers treat a violating row as
/// data corruption and skip it with a warning.
///
/// <para>
/// <see cref="Quantity"/> is decimal(10,2) so fractional quantities
/// work (1.5&#160;L of oil). Carries
/// <c>[AuditParent(typeof(ServicePackage), nameof(ServicePackageId))]</c>
/// so item edits roll up to a single <c>servicepackage.updated</c>
/// audit entry per save.
/// </para>
/// </remarks>
[AuditParent(typeof(ServicePackage), nameof(ServicePackageId))]
public class ServicePackageItem
{
    public int ServicePackageItemId { get; set; }
    public int ServicePackageId { get; set; }

    /// <summary>Set when the component is a ServiceItem. XOR with <see cref="ProductVariantId"/>.</summary>
    public int? ServiceItemId { get; set; }

    /// <summary>Set when the component is a ProductVariant. XOR with <see cref="ServiceItemId"/>.</summary>
    public int? ProductVariantId { get; set; }

    public decimal Quantity { get; set; } = 1m;
    public int DisplayOrder { get; set; }

    public ServicePackage ServicePackage { get; set; } = null!;
    public ServiceItem? ServiceItem { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}
