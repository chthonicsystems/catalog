/**
 * @chthonicsystems/catalog — npm public surface.
 *
 * v0.1.0 ships the public TypeScript types + HTTP service factory +
 * peer-injectable `CatalogHttpAdapter` interface. Sister products
 * (MarineDeck, FlowLift, PetCare OS) consume these directly.
 *
 * The full UI shells (`<CatalogServicesManager>`, `<CatalogProductsManager>`,
 * `<ServiceSearchSelect>`, `<ProductSearchSelect>`, ConfigHub
 * `<SectionServices>` / `<SectionProducts>` / `<SectionServiceScreens>`)
 * lift from TorqueTech in PR 11.5b — same minimal-atomic-then-strict-RFC
 * pattern PR 10/10b and PR 11/11b followed.
 *
 * For PR 11.5, TorqueTech keeps its existing `web/src/components/managers/*`
 * + ConfigHub section pages. The library shape is established here so
 * the component-side imports flip cleanly in 11.5b without further
 * architecture work.
 */

export * from './types';
export * from './services/catalogService';
export * from './services/servicePackageService';
export * from './components/ServicePackagePicker';

export const CATALOG_PACKAGE_VERSION = '0.2.1';
