using Chthonic.Catalog.Domain;
using Chthonic.Catalog.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chthonic.Catalog.Services;

internal sealed class ServicePackageService : IServicePackageService
{
    private readonly IDbContextProvider _provider;
    private readonly ILogger<ServicePackageService> _logger;

    public ServicePackageService(IDbContextProvider provider, ILogger<ServicePackageService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<List<ServicePackage>> ListAsync(int systemId, bool activeOnly = false)
    {
        var db = _provider.GetContext();
        var q = db.Set<ServicePackage>()
            .Include(p => p.Items.OrderBy(i => i.DisplayOrder))
            .Where(p => p.SystemId == systemId);
        if (activeOnly) q = q.Where(p => p.IsActive);
        return await q.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToListAsync();
    }

    public async Task<ServicePackage?> GetWithItemsAsync(int servicePackageId, int systemId)
    {
        var db = _provider.GetContext();
        return await db.Set<ServicePackage>()
            .Include(p => p.Items.OrderBy(i => i.DisplayOrder))
                .ThenInclude(i => i.ServiceItem)
            .Include(p => p.Items)
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(p => p.ServicePackageId == servicePackageId && p.SystemId == systemId);
    }

    public async Task<List<ServicePackage>> SearchAsync(string query, int systemId, int limit = 50)
    {
        var db = _provider.GetContext();
        var q = db.Set<ServicePackage>().Where(p => p.SystemId == systemId && p.IsActive);
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            q = q.Where(p => p.Name.Contains(query) || (p.Description != null && p.Description.Contains(query)));
        }
        return await q
            .Include(p => p.Items.OrderBy(i => i.DisplayOrder))
            .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ServicePackage> CreateAsync(int systemId, string name, string? description, int? serviceId, IReadOnlyList<ServicePackageItemInput> items, bool isActive = true)
    {
        var db = _provider.GetContext();
        await ValidateItemsAsync(db, systemId, items);

        if (serviceId.HasValue &&
            !await db.Set<Service>().AnyAsync(s => s.ServiceId == serviceId.Value && s.SystemId == systemId))
        {
            throw new ArgumentException($"Service {serviceId} not found in system {systemId}.");
        }

        var maxOrder = await db.Set<ServicePackage>()
            .Where(p => p.SystemId == systemId)
            .MaxAsync(p => (int?)p.DisplayOrder) ?? 0;

        var package = new ServicePackage
        {
            SystemId = systemId,
            ServiceId = serviceId,
            Name = name,
            Description = description,
            IsActive = isActive,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = items.Select((i, idx) => new ServicePackageItem
            {
                ServiceItemId = i.ServiceItemId,
                ProductVariantId = i.ProductVariantId,
                Quantity = i.Quantity,
                DisplayOrder = idx,
            }).ToList(),
        };

        db.Set<ServicePackage>().Add(package);
        await db.SaveChangesAsync();
        _logger.LogInformation("Created ServicePackage {PackageId} '{Name}' with {Count} items for system {SystemId}",
            package.ServicePackageId, name, package.Items.Count, systemId);
        return package;
    }

    public async Task<bool> UpdateAsync(int servicePackageId, int systemId, string name, string? description, int? serviceId, bool? isActive)
    {
        var db = _provider.GetContext();
        var package = await db.Set<ServicePackage>()
            .FirstOrDefaultAsync(p => p.ServicePackageId == servicePackageId && p.SystemId == systemId);
        if (package == null) return false;

        if (serviceId.HasValue &&
            !await db.Set<Service>().AnyAsync(s => s.ServiceId == serviceId.Value && s.SystemId == systemId))
        {
            throw new ArgumentException($"Service {serviceId} not found in system {systemId}.");
        }

        package.Name = name;
        package.Description = description;
        package.ServiceId = serviceId;
        if (isActive.HasValue) package.IsActive = isActive.Value;
        package.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReplaceItemsAsync(int servicePackageId, int systemId, IReadOnlyList<ServicePackageItemInput> items)
    {
        var db = _provider.GetContext();
        var package = await db.Set<ServicePackage>()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.ServicePackageId == servicePackageId && p.SystemId == systemId);
        if (package == null) return false;

        await ValidateItemsAsync(db, systemId, items);

        db.Set<ServicePackageItem>().RemoveRange(package.Items);
        package.Items = items.Select((i, idx) => new ServicePackageItem
        {
            ServicePackageId = servicePackageId,
            ServiceItemId = i.ServiceItemId,
            ProductVariantId = i.ProductVariantId,
            Quantity = i.Quantity,
            DisplayOrder = idx,
        }).ToList();
        package.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int servicePackageId, int systemId)
    {
        var db = _provider.GetContext();
        var package = await db.Set<ServicePackage>()
            .FirstOrDefaultAsync(p => p.ServicePackageId == servicePackageId && p.SystemId == systemId);
        if (package == null) return false;

        db.Set<ServicePackage>().Remove(package);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderAsync(int servicePackageId, int systemId, int displayOrder)
    {
        var db = _provider.GetContext();
        var package = await db.Set<ServicePackage>()
            .FirstOrDefaultAsync(p => p.ServicePackageId == servicePackageId && p.SystemId == systemId);
        if (package == null) return false;

        package.DisplayOrder = displayOrder;
        package.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> ComputeTotalAsync(int servicePackageId, int systemId)
    {
        var db = _provider.GetContext();
        var items = await db.Set<ServicePackageItem>()
            .Include(i => i.ServiceItem)
            .Include(i => i.ProductVariant)
            .Where(i => i.ServicePackageId == servicePackageId && i.ServicePackage.SystemId == systemId)
            .ToListAsync();

        return items.Sum(i => i.Quantity * (i.ServiceItem?.Cost ?? i.ProductVariant?.Price ?? 0m));
    }

    /// <summary>
    /// Enforces the write-side invariants: XOR component reference,
    /// positive quantity, and system-scoped component ownership
    /// (cross-tenant references are an ArgumentException, not a silent
    /// filter — the caller sent corrupt input).
    /// </summary>
    private static async Task ValidateItemsAsync(DbContext db, int systemId, IReadOnlyList<ServicePackageItemInput> items)
    {
        foreach (var item in items)
        {
            var hasServiceItem = item.ServiceItemId.HasValue;
            var hasVariant = item.ProductVariantId.HasValue;
            if (hasServiceItem == hasVariant)
            {
                throw new ArgumentException(
                    "Each package item must reference exactly one of serviceItemId or productVariantId.");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException("Package item quantity must be greater than zero.");
            }
        }

        var serviceItemIds = items.Where(i => i.ServiceItemId.HasValue).Select(i => i.ServiceItemId!.Value).Distinct().ToList();
        if (serviceItemIds.Count > 0)
        {
            var owned = await db.Set<ServiceItem>()
                .Where(si => serviceItemIds.Contains(si.ServiceItemId) && si.Service.SystemId == systemId)
                .Select(si => si.ServiceItemId)
                .ToListAsync();
            var missing = serviceItemIds.Except(owned).ToList();
            if (missing.Count > 0)
            {
                throw new ArgumentException($"ServiceItem(s) not found in system {systemId}: {string.Join(", ", missing)}");
            }
        }

        var variantIds = items.Where(i => i.ProductVariantId.HasValue).Select(i => i.ProductVariantId!.Value).Distinct().ToList();
        if (variantIds.Count > 0)
        {
            var owned = await db.Set<ProductVariant>()
                .Where(pv => variantIds.Contains(pv.ProductVariantId) && pv.Product.SystemId == systemId)
                .Select(pv => pv.ProductVariantId)
                .ToListAsync();
            var missing = variantIds.Except(owned).ToList();
            if (missing.Count > 0)
            {
                throw new ArgumentException($"ProductVariant(s) not found in system {systemId}: {string.Join(", ", missing)}");
            }
        }
    }
}
