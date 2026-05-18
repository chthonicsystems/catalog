namespace Chthonic.Catalog.Domain;

/// <summary>
/// A service offered by a tenant — e.g. "Slip booking", "Annual PM",
/// "Vaccination consult". Has a collection of <see cref="ServiceItem"/>
/// children that capture the priced line items.
/// </summary>
/// <remarks>
/// Lifted from TorqueTech's <c>Service</c> entity (PR 11.5 / RFC 0021).
/// Vertical-agnostic — every Phase-1 product (MarineDeck slip types,
/// FlowLift PM tiers, PetCare consults) creates instances of this same
/// entity for their tenant catalog.
/// </remarks>
public class Service
{
    public int ServiceId { get; set; }
    public int SystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Chthonic.Tenant.Domain.System System { get; set; } = null!;
    public ICollection<ServiceItem> ServiceItems { get; set; } = new List<ServiceItem>();
}
