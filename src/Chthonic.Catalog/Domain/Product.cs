namespace Chthonic.Catalog.Domain;

/// <summary>
/// A product the tenant sells — typically a tangible item with one or
/// more <see cref="ProductVariant"/>s by SKU/barcode/price.
/// </summary>
/// <remarks>
/// Lifted from TorqueTech's <c>Product</c> entity (PR 11.5 / RFC 0021).
///
/// <para>
/// **Inverse-nav drop:** the original TorqueTech <c>Product</c> had
/// an <c>ICollection&lt;JobField&gt; JobFields</c> back-reference.
/// Per RFC 0021 § 3, that inverse is dropped to break the catalog ↔ views
/// cycle. See <see cref="ServiceItem"/> for the same explanation.
/// </para>
/// </remarks>
public class Product
{
    public int ProductId { get; set; }
    public int SystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Chthonic.Tenant.Domain.System System { get; set; } = null!;
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public ICollection<ServiceItem> ServiceItems { get; set; } = new List<ServiceItem>();
}
