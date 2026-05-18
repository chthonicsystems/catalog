using Chthonic.Audit;

namespace Chthonic.Catalog.Domain;

/// <summary>
/// A priced line item under a <see cref="Service"/>. Optionally references
/// a <see cref="Product"/> when the item is a tangible part rather than
/// labour. Cost is independently set on each item; the parent service's
/// total cost is the sum across its items.
/// </summary>
/// <remarks>
/// Lifted from TorqueTech's <c>ServiceItem</c> entity (PR 11.5 / RFC 0021).
/// Carries <c>[AuditParent(typeof(Service), nameof(ServiceId))]</c> so
/// state changes on items roll up to a single <c>service.updated</c>
/// audit entry per save.
///
/// <para>
/// **Inverse-nav drop:** the original TorqueTech <c>ServiceItem</c> had
/// an <c>ICollection&lt;JobField&gt; JobFields</c> back-reference. Per
/// RFC 0021 § 3, that inverse is dropped to break the catalog ↔ views
/// cycle. Direction is one-way: <c>Chthonic.Views → Chthonic.Catalog</c>.
/// Anyone needing the reverse ("which fields reference this service item?")
/// queries <c>db.Set&lt;EntityField&gt;().Where(ef =&gt; ef.ServiceItemId == ...)</c>.
/// </para>
/// </remarks>
[AuditParent(typeof(Service), nameof(ServiceId))]
public class ServiceItem
{
    public int ServiceItemId { get; set; }
    public int ServiceId { get; set; }
    public int? ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Cost { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Service Service { get; set; } = null!;
    public Product? Product { get; set; }
}
