using Chthonic.Catalog.Domain;
using Chthonic.Catalog.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Chthonic.Catalog.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Mounts the catalog HTTP surface for sister products. TorqueTech
    /// keeps its own TT-side wrappers at the same paths (which add
    /// JobField auto-wiring + LinkedFieldName enrichment); sister
    /// products mount these library endpoints directly.
    /// </summary>
    public static IEndpointRouteBuilder MapChthonicCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ServiceEndpoints.Map(app);
        ServiceItemEndpoints.Map(app);
        ProductEndpoints.Map(app);
        ProductVariantEndpoints.Map(app);
        return app;
    }
}

internal static class ServiceEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/services").RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] int? systemId,
            [FromQuery] bool activeOnly,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = systemId ?? GetSystemId(ctx) ?? 0;
            var services = await svc.ListAsync(sid, activeOnly);
            return Results.Ok(services.Select(ToDto).ToList());
        });

        group.MapGet("/search", async (
            [FromQuery] string? query,
            [FromQuery] int? systemId,
            [FromQuery] int limit,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = systemId ?? GetSystemId(ctx) ?? 0;
            var lim = limit > 0 ? Math.Min(limit, 200) : 50;
            var services = await svc.SearchAsync(query ?? "", sid, lim);
            return Results.Ok(services.Select(ToDto).ToList());
        });

        group.MapGet("/{id:int}", async (
            int id,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var service = await svc.GetWithItemsAsync(id, sid);
            return service == null ? Results.NotFound() : Results.Ok(ToDetailDto(service));
        });

        group.MapPost("/", async (
            [FromBody] CreateServiceBody body,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Service name is required" });
            var service = await svc.CreateAsync(sid, body.Name, body.Description, body.Icon, body.IsActive ?? true);
            return Results.Created($"/api/services/{service.ServiceId}", new { serviceId = service.ServiceId });
        });

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdateServiceBody body,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Service name is required" });
            var ok = await svc.UpdateAsync(id, sid, body.Name, body.Description, body.Icon, body.IsActive);
            return ok ? Results.Ok(new { message = "Service updated successfully" }) : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (
            int id,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.DeleteAsync(id, sid);
            return ok ? Results.Ok(new { message = "Service deleted successfully" }) : Results.NotFound();
        });

        group.MapPut("/{id:int}/reorder", async (
            int id,
            [FromBody] ReorderBody body,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.ReorderAsync(id, sid, body.DisplayOrder);
            return ok ? Results.Ok(new { message = "Service order updated successfully" }) : Results.NotFound();
        });
    }

    private static int? GetSystemId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("system_id") ?? ctx.User.FindFirst("systemId");
        return claim != null && int.TryParse(claim.Value, out var sid) ? sid : null;
    }

    private static object ToDto(Service s) => new
    {
        serviceId = s.ServiceId,
        name = s.Name,
        description = s.Description,
        icon = s.Icon,
        isActive = s.IsActive,
        displayOrder = s.DisplayOrder,
        createdAt = s.CreatedAt,
        updatedAt = s.UpdatedAt,
    };

    private static object ToDetailDto(Service s) => new
    {
        serviceId = s.ServiceId,
        name = s.Name,
        description = s.Description,
        icon = s.Icon,
        isActive = s.IsActive,
        displayOrder = s.DisplayOrder,
        createdAt = s.CreatedAt,
        updatedAt = s.UpdatedAt,
        items = s.ServiceItems.OrderBy(si => si.DisplayOrder).Select(si => new
        {
            serviceItemId = si.ServiceItemId,
            name = si.Name,
            description = si.Description,
            icon = si.Icon,
            cost = si.Cost,
            displayOrder = si.DisplayOrder,
            productId = si.ProductId,
            productName = si.Product?.Name,
        }).ToList(),
    };

    public sealed record CreateServiceBody(string Name, string? Description, string? Icon, bool? IsActive);
    public sealed record UpdateServiceBody(string Name, string? Description, string? Icon, bool? IsActive);
    public sealed record ReorderBody(int DisplayOrder);
}

internal static class ServiceItemEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/services/{serviceId:int}/items").RequireAuthorization();

        group.MapPost("/", async (
            int serviceId,
            [FromBody] CreateBody body,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Item name is required" });
            try
            {
                var item = await svc.CreateItemAsync(serviceId, sid, body.Name, body.Description, body.Cost, body.Icon, body.ProductId);
                return Results.Created($"/api/services/{serviceId}/items/{item.ServiceItemId}", new { serviceItemId = item.ServiceItemId });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        group.MapPut("/{id:int}", async (
            int serviceId,
            int id,
            [FromBody] UpdateBody body,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Item name is required" });
            var ok = await svc.UpdateItemAsync(id, sid, body.Name, body.Description, body.Cost, body.Icon, body.ProductId);
            return ok ? Results.Ok(new { message = "Service item updated successfully" }) : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (
            int serviceId,
            int id,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.DeleteItemAsync(id, sid);
            return ok ? Results.Ok(new { message = "Service item deleted successfully" }) : Results.NotFound();
        });

        group.MapPut("/{id:int}/reorder", async (
            int serviceId,
            int id,
            [FromBody] ReorderBody body,
            ICatalogServiceService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.ReorderItemAsync(id, sid, body.DisplayOrder);
            return ok ? Results.Ok(new { message = "Item order updated successfully" }) : Results.NotFound();
        });
    }

    private static int? GetSystemId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("system_id") ?? ctx.User.FindFirst("systemId");
        return claim != null && int.TryParse(claim.Value, out var sid) ? sid : null;
    }

    public sealed record CreateBody(string Name, string? Description, decimal? Cost, string? Icon, int? ProductId);
    public sealed record UpdateBody(string Name, string? Description, decimal? Cost, string? Icon, int? ProductId);
    public sealed record ReorderBody(int DisplayOrder);
}

internal static class ProductEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] int? systemId,
            [FromQuery] bool activeOnly,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = systemId ?? GetSystemId(ctx) ?? 0;
            var products = await svc.ListAsync(sid, activeOnly);
            return Results.Ok(products.Select(ToDto).ToList());
        });

        group.MapGet("/search", async (
            [FromQuery] string? query,
            [FromQuery] int? systemId,
            [FromQuery] int limit,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = systemId ?? GetSystemId(ctx) ?? 0;
            var lim = limit > 0 ? Math.Min(limit, 200) : 50;
            var products = await svc.SearchAsync(query ?? "", sid, lim);
            return Results.Ok(products.Select(ToDto).ToList());
        });

        group.MapGet("/{id:int}", async (
            int id,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var product = await svc.GetWithVariantsAsync(id, sid);
            return product == null ? Results.NotFound() : Results.Ok(ToDetailDto(product));
        });

        group.MapPost("/", async (
            [FromBody] CreateBody body,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Product name is required" });
            var p = await svc.CreateAsync(sid, body.Name, body.Description, body.Icon, body.IsActive ?? true);
            return Results.Created($"/api/products/{p.ProductId}", new { productId = p.ProductId });
        });

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdateBody body,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Product name is required" });
            var ok = await svc.UpdateAsync(id, sid, body.Name, body.Description, body.Icon, body.IsActive);
            return ok ? Results.Ok(new { message = "Product updated successfully" }) : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (
            int id,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.DeleteAsync(id, sid);
            return ok ? Results.Ok(new { message = "Product deleted successfully" }) : Results.NotFound();
        });
    }

    private static int? GetSystemId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("system_id") ?? ctx.User.FindFirst("systemId");
        return claim != null && int.TryParse(claim.Value, out var sid) ? sid : null;
    }

    private static object ToDto(Product p) => new
    {
        productId = p.ProductId,
        name = p.Name,
        description = p.Description,
        icon = p.Icon,
        isActive = p.IsActive,
        displayOrder = p.DisplayOrder,
        createdAt = p.CreatedAt,
        updatedAt = p.UpdatedAt,
    };

    private static object ToDetailDto(Product p) => new
    {
        productId = p.ProductId,
        name = p.Name,
        description = p.Description,
        icon = p.Icon,
        isActive = p.IsActive,
        displayOrder = p.DisplayOrder,
        createdAt = p.CreatedAt,
        updatedAt = p.UpdatedAt,
        productVariants = p.ProductVariants.OrderBy(v => v.DisplayOrder).Select(v => new
        {
            productVariantId = v.ProductVariantId,
            name = v.Name,
            description = v.Description,
            sku = v.Sku,
            barcode = v.Barcode,
            externalAccountingItemId = v.ExternalAccountingItemId,
            price = v.Price,
            isActive = v.IsActive,
            displayOrder = v.DisplayOrder,
        }).ToList(),
    };

    public sealed record CreateBody(string Name, string? Description, string? Icon, bool? IsActive);
    public sealed record UpdateBody(string Name, string? Description, string? Icon, bool? IsActive);
}

internal static class ProductVariantEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/{productId:int}/variants").RequireAuthorization();

        group.MapPost("/", async (
            int productId,
            [FromBody] CreateBody body,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Variant name is required" });
            try
            {
                var v = await svc.CreateVariantAsync(productId, sid, body.Name, body.Description, body.Sku, body.Barcode, body.Price, body.IsActive ?? true);
                return Results.Created($"/api/products/{productId}/variants/{v.ProductVariantId}", new { productVariantId = v.ProductVariantId });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        });

        group.MapPut("/{id:int}", async (
            int productId,
            int id,
            [FromBody] UpdateBody body,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name)) return Results.BadRequest(new { error = "Variant name is required" });
            var ok = await svc.UpdateVariantAsync(id, sid, body.Name, body.Description, body.Sku, body.Barcode, body.Price, body.IsActive);
            return ok ? Results.Ok(new { message = "Variant updated successfully" }) : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (
            int productId,
            int id,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.DeleteVariantAsync(id, sid);
            return ok ? Results.Ok(new { message = "Variant deleted successfully" }) : Results.NotFound();
        });

        group.MapGet("/search", async (
            int productId,
            [FromQuery] string? query,
            [FromQuery] int limit,
            ICatalogProductService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var lim = limit > 0 ? Math.Min(limit, 200) : 50;
            var variants = await svc.SearchVariantsAsync(query ?? "", sid, lim);
            return Results.Ok(variants.Where(v => v.ProductId == productId).Select(v => new
            {
                productVariantId = v.ProductVariantId,
                productId = v.ProductId,
                name = v.Name,
                sku = v.Sku,
                barcode = v.Barcode,
                price = v.Price,
                isActive = v.IsActive,
            }).ToList());
        });
    }

    private static int? GetSystemId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("system_id") ?? ctx.User.FindFirst("systemId");
        return claim != null && int.TryParse(claim.Value, out var sid) ? sid : null;
    }

    public sealed record CreateBody(string Name, string? Description, string? Sku, string? Barcode, decimal Price, bool? IsActive);
    public sealed record UpdateBody(string Name, string? Description, string? Sku, string? Barcode, decimal Price, bool? IsActive);
}
