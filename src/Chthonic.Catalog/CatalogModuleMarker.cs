namespace Chthonic.Catalog;

/// <summary>
/// Marker type for assembly scanning. Public, sealed, intentionally empty.
/// Consumers reference <c>typeof(CatalogModuleMarker).Assembly</c> when registering
/// the library's EF configurations + migrations via assembly scan.
/// </summary>
public sealed class CatalogModuleMarker
{
}
