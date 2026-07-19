using Chthonic.Audit;

namespace Chthonic.Catalog.Domain;

/// <summary>
/// A named, reusable bundle of catalog components — "Major Service",
/// "Brake Job" — that a consumer application can apply to a work unit
/// in one step. Composition is mixed: each <see cref="ServicePackageItem"/>
/// references a <see cref="ServiceItem"/> XOR a <see cref="ProductVariant"/>.
/// </summary>
/// <remarks>
/// Added in v0.2.0 per RFC 0034 (§ 12 Amendment 1). Pricing is
/// sum-of-components — the package has no price of its own; totals are
/// computed from the underlying <c>ServiceItem.Cost</c> +
/// <c>ProductVariant.Price</c> at apply time (RFC 0034 § 3).
///
/// <para>
/// <see cref="ServiceId"/> is optional (RFC 0034 § 12h): a package can
/// be tied to a Service (rendered alongside it in pickers) or fully
/// standalone.
/// </para>
///
/// <para>
/// **Apply semantics are consumer-side.** The library owns the bundle
/// definition + CRUD only. How a consumer hydrates package items onto
/// its work unit (TorqueTech: Job line items through the JobField
/// machinery, inside the RFC 0030 stock-decrement wrap) is consumer
/// logic — same cross-library FK-only stance as the rest of the
/// platform (the catalog library must not depend on
/// <c>@chthonic/work</c>).
/// </para>
/// </remarks>
public class ServicePackage
{
    public int ServicePackageId { get; set; }
    public int SystemId { get; set; }

    /// <summary>Optional owning Service — null for standalone packages.</summary>
    public int? ServiceId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Chthonic.Tenant.Domain.System System { get; set; } = null!;
    public Service? Service { get; set; }
    public List<ServicePackageItem> Items { get; set; } = new();
}
