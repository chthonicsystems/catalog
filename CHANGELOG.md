# Changelog

## v0.2.0 — 2026-07-19

Additive. Per [RFC 0034 — Job templates](https://github.com/chthonicsystems/architecture/blob/main/rfcs/0034-job-templates.md)
(§ 12 Amendment 1).

### .NET (`Chthonic.Catalog`)

- **NEW** `ServicePackage` + `ServicePackageItem` entities — named
  bundle of ServiceItems + ProductVariants (`ServiceItemId` XOR
  `ProductVariantId`, decimal(10,2) Quantity). Optional `ServiceId`
  FK (standalone or service-tied per § 12h).
- **NEW** `IServicePackageService` / `ServicePackageService` — CRUD +
  atomic `ReplaceItemsAsync` + sum-of-components `ComputeTotalAsync`.
  Registered by `AddChthonicCatalog()`.
- **NEW** `MapChthonicServicePackageEndpoints()` — a **separate**
  mapper from `MapChthonicCatalogEndpoints()` (§ 12a) mounting
  `/api/service-packages/*`, so consumers that keep their own
  services/products endpoints (TorqueTech) can mount packages alone,
  e.g. under a tier-gated route group.
- **NEW** empty-placeholder migration
  `ChthonicCatalog_0002_ServicePackages` (coexistence pattern § 12j —
  the consumer migration owns the schema; XML doc carries the DDL).

### npm (`@chthonicsystems/catalog`)

- **NEW** `ServicePackage` / `ServicePackageItem` /
  `ServicePackageItemInput` / `Create-`/`UpdateServicePackageRequest`
  types.
- **NEW** `createServicePackageService(http)` HTTP factory.
- **NEW** `<ServicePackagePicker>` typeahead picker (plain-DOM,
  `svc-pkg-picker*` class hooks; selection only — apply semantics are
  consumer-side per § 12b).

Apply-to-work-unit semantics are deliberately **not** in this library
(cross-library FK-only typing — catalog must not depend on
`@chthonic/work`). TorqueTech owns `POST /api/jobs/{id}/apply-package`.

## v0.1.0 — 2026-05-18

Initial release (RFC 0021 — catalog extraction from TorqueTech PR 11.5):
Service / ServiceItem / Product / ProductVariant entities, EF configs,
CRUD services, sister-product endpoints, npm types + HTTP factory.
