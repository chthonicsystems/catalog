using Chthonic.Catalog.Domain;
using Chthonic.Catalog.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Chthonic.Catalog.Endpoints;

public static class ServicePackageEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Mounts the ServicePackage HTTP surface (v0.2.0 / RFC 0034).
    /// **Deliberately separate** from <c>MapChthonicCatalogEndpoints()</c>
    /// (RFC 0034 § 12a): TorqueTech keeps its own /api/services +
    /// /api/products wrappers but mounts THIS mapper under its
    /// <c>RequireFeature("JobsTemplates")</c> route group. Sister
    /// products may mount both mappers.
    /// </summary>
    public static IEndpointRouteBuilder MapChthonicServicePackageEndpoints(this IEndpointRouteBuilder app)
    {
        ServicePackageEndpoints.Map(app);
        return app;
    }
}

internal static class ServicePackageEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-packages").RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] bool? activeOnly,
            IServicePackageService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var packages = await svc.ListAsync(sid, activeOnly ?? false);
            return Results.Ok(packages.Select(ToDto).ToList());
        });

        group.MapGet("/search", async (
            [FromQuery] string? query,
            [FromQuery] int? limit,
            IServicePackageService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var lim = limit is > 0 ? Math.Min(limit.Value, 200) : 50;
            var packages = await svc.SearchAsync(query ?? "", sid, lim);
            return Results.Ok(packages.Select(ToDto).ToList());
        });

        group.MapGet("/{id:int}", async (
            int id,
            IServicePackageService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var package = await svc.GetWithItemsAsync(id, sid);
            if (package == null) return Results.NotFound();
            var total = await svc.ComputeTotalAsync(id, sid);
            return Results.Ok(ToDetailDto(package, total));
        });

        group.MapPost("/", async (
            [FromBody] CreateServicePackageBody body,
            IServicePackageService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name))
                return Results.BadRequest(new { message = "Name is required" });

            try
            {
                var package = await svc.CreateAsync(
                    sid, body.Name.Trim(), body.Description, body.ServiceId,
                    (body.Items ?? new List<ServicePackageItemBody>())
                        .Select(i => new ServicePackageItemInput(i.ServiceItemId, i.ProductVariantId, i.Quantity))
                        .ToList(),
                    body.IsActive ?? true);
                return Results.Created($"/api/service-packages/{package.ServicePackageId}",
                    new { servicePackageId = package.ServicePackageId });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message, errorCode = "invalid-package-item" });
            }
        });

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdateServicePackageBody body,
            IServicePackageService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            if (string.IsNullOrWhiteSpace(body.Name))
                return Results.BadRequest(new { message = "Name is required" });

            try
            {
                var ok = await svc.UpdateAsync(id, sid, body.Name.Trim(), body.Description, body.ServiceId, body.IsActive);
                if (!ok) return Results.NotFound();

                if (body.Items != null)
                {
                    await svc.ReplaceItemsAsync(id, sid,
                        body.Items.Select(i => new ServicePackageItemInput(i.ServiceItemId, i.ProductVariantId, i.Quantity)).ToList());
                }

                return Results.Ok(new { servicePackageId = id });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message, errorCode = "invalid-package-item" });
            }
        });

        group.MapDelete("/{id:int}", async (
            int id,
            IServicePackageService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.DeleteAsync(id, sid);
            return ok ? Results.Ok() : Results.NotFound();
        });

        group.MapPut("/{id:int}/reorder", async (
            int id,
            [FromBody] ReorderBody body,
            IServicePackageService svc,
            HttpContext ctx) =>
        {
            var sid = GetSystemId(ctx) ?? 0;
            var ok = await svc.ReorderAsync(id, sid, body.DisplayOrder);
            return ok ? Results.Ok() : Results.NotFound();
        });
    }

    private static int? GetSystemId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("system_id") ?? ctx.User.FindFirst("systemId");
        return claim != null && int.TryParse(claim.Value, out var sid) ? sid : null;
    }

    private static object ToDto(ServicePackage p) => new
    {
        servicePackageId = p.ServicePackageId,
        serviceId = p.ServiceId,
        name = p.Name,
        description = p.Description,
        isActive = p.IsActive,
        displayOrder = p.DisplayOrder,
        itemCount = p.Items?.Count ?? 0,
        createdAt = p.CreatedAt,
        updatedAt = p.UpdatedAt,
    };

    private static object ToDetailDto(ServicePackage p, decimal total) => new
    {
        servicePackageId = p.ServicePackageId,
        serviceId = p.ServiceId,
        name = p.Name,
        description = p.Description,
        isActive = p.IsActive,
        displayOrder = p.DisplayOrder,
        totalAmount = total,
        createdAt = p.CreatedAt,
        updatedAt = p.UpdatedAt,
        items = (p.Items ?? new List<ServicePackageItem>()).Select(i => new
        {
            servicePackageItemId = i.ServicePackageItemId,
            serviceItemId = i.ServiceItemId,
            productVariantId = i.ProductVariantId,
            quantity = i.Quantity,
            displayOrder = i.DisplayOrder,
            name = i.ServiceItem?.Name ?? i.ProductVariant?.Name,
            unitAmount = i.ServiceItem?.Cost ?? i.ProductVariant?.Price,
        }).ToList(),
    };

    internal sealed record ServicePackageItemBody(int? ServiceItemId, int? ProductVariantId, decimal Quantity);
    internal sealed record CreateServicePackageBody(string Name, string? Description, int? ServiceId, List<ServicePackageItemBody>? Items, bool? IsActive);
    internal sealed record UpdateServicePackageBody(string Name, string? Description, int? ServiceId, List<ServicePackageItemBody>? Items, bool? IsActive);
    internal sealed record ReorderBody(int DisplayOrder);
}
