# `@chthonic/catalog`

Service & Product Catalog for the Chthonic platform — what does this
tenant sell? Four entities (`Service`, `ServiceItem`, `Product`,
`ProductVariant`) + their CRUD endpoints + frontend Option C feature
shells (managers, search-selects, ConfigHub sections).

See [RFC 0021 — Service & Product Catalog](https://github.com/chthonicsystems/architecture/blob/main/rfcs/0021-catalog.md)
for the design.

## Install

### .NET (NuGet)

```bash
dotnet add package Chthonic.Catalog --version 0.1.0
```

Configure in `Program.cs`:

```csharp
using Chthonic.Catalog;

builder.Services.AddChthonicCatalog();

// ...

app.MapChthonicCatalogEndpoints();
```

`AddChthonicCatalog()` registers:

- `IServiceService` — Service + ServiceItem CRUD
- `IProductService` — Product + ProductVariant CRUD
- `IProductVariantService` — Variant management (lookup, search by SKU/barcode)

Order matters: register **after** `AddChthonicTenant`, `AddChthonicIdentity`,
and `AddChthonicAudit` (this library depends on all three).

`MapChthonicCatalogEndpoints()` mounts:

- `/api/services/*` — Service CRUD + search
- `/api/services/{serviceId}/items/*` — ServiceItem CRUD
- `/api/products/*` — Product CRUD + search
- `/api/products/{productId}/variants/*` — ProductVariant CRUD + search

The library also exposes `Chthonic.Catalog.Extensions.IDbContextProvider` —
consumers register an implementation that returns their own
`DbContext` instance. TorqueTech wires this to its
`TorqueTechDbContext` in PR 11.5.

### npm

```bash
npm install @chthonicsystems/catalog
```

```tsx
import {
  ServiceSearchSelect,
  ProductSearchSelect,
  CatalogServicesManager,
  CatalogProductsManager,
  CatalogServiceScreensManager,
  SectionServices,
  SectionProducts,
  SectionServiceScreens,
  type Service,
  type ServiceItem,
  type Product,
  type ProductVariant,
  type CatalogHttpAdapter,
} from '@chthonicsystems/catalog';

// Use the section pages directly inside Config Hub
<SectionServices http={httpAdapter} />

// Or the search-select primitives in any form
<ServiceSearchSelect
  http={httpAdapter}
  value={selectedServiceId}
  onChange={(id, service) => setForm({ ...form, serviceId: id })}
/>
```

## Polymorphic FK pattern

`ServiceItem.ServiceId` and `ProductVariant.ProductId` are intra-catalog
parent-child FKs. `Service.SystemId` and `Product.SystemId` reference
`Chthonic.Tenant.Domain.System`.

`Chthonic.Views.EntityField.ServiceItemId` / `Chthonic.Views.EntityField.ProductId`
and `Chthonic.Views.EntityFieldValue.ServiceItemId` / `.ProductId` /
`.ProductVariantId` are nullable FKs from views into catalog. Direction
is one-way: `Chthonic.Views → Chthonic.Catalog`. The reverse (catalog
→ views) is deliberately broken to avoid the circular dependency
identified in PR 11b's first attempt.

## Audit

`ServiceItem` carries `[AuditParent("Service", "ServiceId")]` and
`ProductVariant` carries `[AuditParent("Product", "ProductId")]` —
state changes roll up to the parent's audit row via `Chthonic.Audit`'s
string-variant `AuditParentAttribute(string, string)` constructor.

## Public API

Full surface in [`src/Chthonic.Catalog/`](src/Chthonic.Catalog/).

## RFC

[RFC 0021 — Service & Product Catalog](https://github.com/chthonicsystems/architecture/blob/main/rfcs/0021-catalog.md).
