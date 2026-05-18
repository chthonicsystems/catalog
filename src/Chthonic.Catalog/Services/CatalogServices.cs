using Chthonic.Catalog.Domain;
using Chthonic.Catalog.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chthonic.Catalog.Services;

internal sealed class CatalogServiceService : ICatalogServiceService
{
    private readonly IDbContextProvider _provider;
    private readonly ILogger<CatalogServiceService> _logger;

    public CatalogServiceService(IDbContextProvider provider, ILogger<CatalogServiceService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<List<Service>> ListAsync(int systemId, bool activeOnly = false)
    {
        var db = _provider.GetContext();
        var q = db.Set<Service>().Where(s => s.SystemId == systemId);
        if (activeOnly) q = q.Where(s => s.IsActive);
        return await q.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync();
    }

    public async Task<Service?> GetWithItemsAsync(int serviceId, int systemId)
    {
        var db = _provider.GetContext();
        return await db.Set<Service>()
            .Include(s => s.ServiceItems.OrderBy(si => si.DisplayOrder))
                .ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.SystemId == systemId);
    }

    public async Task<List<Service>> SearchAsync(string query, int systemId, int limit = 50)
    {
        var db = _provider.GetContext();
        var q = db.Set<Service>().Where(s => s.SystemId == systemId);
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            q = q.Where(s => s.Name.Contains(query) || (s.Description != null && s.Description.Contains(query)));
        }
        return await q.OrderBy(s => s.Name).Take(limit).ToListAsync();
    }

    public async Task<Service> CreateAsync(int systemId, string name, string? description = null, string? icon = null, bool isActive = true)
    {
        var db = _provider.GetContext();
        var maxOrder = await db.Set<Service>().Where(s => s.SystemId == systemId).MaxAsync(s => (int?)s.DisplayOrder) ?? 0;
        var service = new Service
        {
            SystemId = systemId,
            Name = name,
            Description = description,
            Icon = icon,
            IsActive = isActive,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Set<Service>().Add(service);
        await db.SaveChangesAsync();
        return service;
    }

    public async Task<bool> UpdateAsync(int serviceId, int systemId, string name, string? description = null, string? icon = null, bool? isActive = null)
    {
        var db = _provider.GetContext();
        var service = await db.Set<Service>().FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.SystemId == systemId);
        if (service == null) return false;
        service.Name = name;
        service.Description = description;
        service.Icon = icon;
        if (isActive.HasValue) service.IsActive = isActive.Value;
        service.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int serviceId, int systemId)
    {
        var db = _provider.GetContext();
        var service = await db.Set<Service>().FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.SystemId == systemId);
        if (service == null) return false;
        db.Set<Service>().Remove(service);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderAsync(int serviceId, int systemId, int displayOrder)
    {
        var db = _provider.GetContext();
        var service = await db.Set<Service>().FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.SystemId == systemId);
        if (service == null) return false;
        service.DisplayOrder = displayOrder;
        service.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ServiceItem> CreateItemAsync(int serviceId, int systemId, string name, string? description, decimal? cost, string? icon, int? productId)
    {
        var db = _provider.GetContext();
        var service = await db.Set<Service>().FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.SystemId == systemId)
            ?? throw new KeyNotFoundException($"Service {serviceId} not found");
        var maxOrder = await db.Set<ServiceItem>().Where(si => si.ServiceId == serviceId).MaxAsync(si => (int?)si.DisplayOrder) ?? 0;
        var item = new ServiceItem
        {
            ServiceId = serviceId,
            ProductId = productId,
            Name = name,
            Description = description,
            Cost = cost,
            Icon = icon,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Set<ServiceItem>().Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateItemAsync(int serviceItemId, int systemId, string name, string? description, decimal? cost, string? icon, int? productId)
    {
        var db = _provider.GetContext();
        var item = await db.Set<ServiceItem>()
            .Include(si => si.Service)
            .FirstOrDefaultAsync(si => si.ServiceItemId == serviceItemId && si.Service.SystemId == systemId);
        if (item == null) return false;
        item.Name = name;
        item.Description = description;
        item.Cost = cost;
        item.Icon = icon;
        item.ProductId = productId;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteItemAsync(int serviceItemId, int systemId)
    {
        var db = _provider.GetContext();
        var item = await db.Set<ServiceItem>()
            .Include(si => si.Service)
            .FirstOrDefaultAsync(si => si.ServiceItemId == serviceItemId && si.Service.SystemId == systemId);
        if (item == null) return false;
        db.Set<ServiceItem>().Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderItemAsync(int serviceItemId, int systemId, int displayOrder)
    {
        var db = _provider.GetContext();
        var item = await db.Set<ServiceItem>()
            .Include(si => si.Service)
            .FirstOrDefaultAsync(si => si.ServiceItemId == serviceItemId && si.Service.SystemId == systemId);
        if (item == null) return false;
        item.DisplayOrder = displayOrder;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}

internal sealed class CatalogProductService : ICatalogProductService
{
    private readonly IDbContextProvider _provider;
    private readonly ILogger<CatalogProductService> _logger;

    public CatalogProductService(IDbContextProvider provider, ILogger<CatalogProductService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<List<Product>> ListAsync(int systemId, bool activeOnly = false)
    {
        var db = _provider.GetContext();
        var q = db.Set<Product>().Where(p => p.SystemId == systemId);
        if (activeOnly) q = q.Where(p => p.IsActive);
        return await q.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToListAsync();
    }

    public async Task<Product?> GetWithVariantsAsync(int productId, int systemId)
    {
        var db = _provider.GetContext();
        return await db.Set<Product>()
            .Include(p => p.ProductVariants.OrderBy(v => v.DisplayOrder))
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.SystemId == systemId);
    }

    public async Task<List<Product>> SearchAsync(string query, int systemId, int limit = 50)
    {
        var db = _provider.GetContext();
        var q = db.Set<Product>().Where(p => p.SystemId == systemId);
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            q = q.Where(p => p.Name.Contains(query) || (p.Description != null && p.Description.Contains(query)));
        }
        return await q.OrderBy(p => p.Name).Take(limit).ToListAsync();
    }

    public async Task<Product> CreateAsync(int systemId, string name, string? description = null, string? icon = null, bool isActive = true)
    {
        var db = _provider.GetContext();
        var maxOrder = await db.Set<Product>().Where(p => p.SystemId == systemId).MaxAsync(p => (int?)p.DisplayOrder) ?? 0;
        var product = new Product
        {
            SystemId = systemId,
            Name = name,
            Description = description,
            Icon = icon,
            IsActive = isActive,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    public async Task<bool> UpdateAsync(int productId, int systemId, string name, string? description = null, string? icon = null, bool? isActive = null)
    {
        var db = _provider.GetContext();
        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.ProductId == productId && p.SystemId == systemId);
        if (product == null) return false;
        product.Name = name;
        product.Description = description;
        product.Icon = icon;
        if (isActive.HasValue) product.IsActive = isActive.Value;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int productId, int systemId)
    {
        var db = _provider.GetContext();
        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.ProductId == productId && p.SystemId == systemId);
        if (product == null) return false;
        db.Set<Product>().Remove(product);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ProductVariant> CreateVariantAsync(int productId, int systemId, string name, string? description, string? sku, string? barcode, decimal price, bool isActive = true)
    {
        var db = _provider.GetContext();
        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.ProductId == productId && p.SystemId == systemId)
            ?? throw new KeyNotFoundException($"Product {productId} not found");
        var maxOrder = await db.Set<ProductVariant>().Where(v => v.ProductId == productId).MaxAsync(v => (int?)v.DisplayOrder) ?? 0;
        var variant = new ProductVariant
        {
            ProductId = productId,
            Name = name,
            Description = description,
            Sku = sku,
            Barcode = barcode,
            Price = price,
            IsActive = isActive,
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Set<ProductVariant>().Add(variant);
        await db.SaveChangesAsync();
        return variant;
    }

    public async Task<bool> UpdateVariantAsync(int productVariantId, int systemId, string name, string? description, string? sku, string? barcode, decimal price, bool? isActive)
    {
        var db = _provider.GetContext();
        var variant = await db.Set<ProductVariant>()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.ProductVariantId == productVariantId && v.Product.SystemId == systemId);
        if (variant == null) return false;
        variant.Name = name;
        variant.Description = description;
        variant.Sku = sku;
        variant.Barcode = barcode;
        variant.Price = price;
        if (isActive.HasValue) variant.IsActive = isActive.Value;
        variant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteVariantAsync(int productVariantId, int systemId)
    {
        var db = _provider.GetContext();
        var variant = await db.Set<ProductVariant>()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.ProductVariantId == productVariantId && v.Product.SystemId == systemId);
        if (variant == null) return false;
        db.Set<ProductVariant>().Remove(variant);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProductVariant>> SearchVariantsAsync(string query, int systemId, int limit = 50)
    {
        var db = _provider.GetContext();
        var q = db.Set<ProductVariant>()
            .Include(v => v.Product)
            .Where(v => v.Product.SystemId == systemId);
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            q = q.Where(v =>
                v.Name.Contains(query) ||
                (v.Sku != null && v.Sku.Contains(query)) ||
                (v.Barcode != null && v.Barcode.Contains(query)));
        }
        return await q.OrderBy(v => v.Name).Take(limit).ToListAsync();
    }
}
