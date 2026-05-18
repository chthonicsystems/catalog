using Chthonic.Audit;

namespace Chthonic.Catalog.Domain;

/// <summary>
/// A specific variant of a <see cref="Product"/> — by SKU, barcode, and
/// price. Variants own the price (a Product is just a grouping); a
/// product with no variants represents an unpriced placeholder.
/// </summary>
/// <remarks>
/// Lifted from TorqueTech's <c>ProductVariant</c> entity (PR 11.5 / RFC 0021).
/// Carries <c>[AuditParent(typeof(Product), nameof(ProductId))]</c> so
/// state changes on variants roll up to a single <c>product.updated</c>
/// audit entry per save.
///
/// <para>
/// <see cref="ExternalAccountingItemId"/> is the FK in the tenant's
/// accounting provider (Xero / QuickBooks / etc.). Owned here for one-
/// way mapping; round-trip sync logic lives in <c>@chthonic/billing</c>.
/// </para>
/// </remarks>
[AuditParent(typeof(Product), nameof(ProductId))]
public class ProductVariant
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? ExternalAccountingItemId { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Product Product { get; set; } = null!;
}
