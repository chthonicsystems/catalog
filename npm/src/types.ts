/**
 * Public TypeScript types for `@chthonicsystems/catalog`.
 *
 * Mirrors the Chthonic.Catalog .NET domain entities from
 * `src/Chthonic.Catalog/Domain/`.
 */

export interface Service {
  serviceId: number;
  systemId?: number;
  name: string;
  description?: string | null;
  icon?: string | null;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
  items?: ServiceItem[];
}

export interface ServiceItem {
  serviceItemId: number;
  serviceId?: number;
  productId?: number | null;
  name: string;
  description?: string | null;
  cost?: number | null;
  icon?: string | null;
  displayOrder: number;
  productName?: string | null;
  /**
   * TT-specific: when a JobField is auto-wired to this ServiceItem,
   * the field's name surfaces here. Sister products leave null.
   */
  linkedFieldName?: string | null;
}

export interface Product {
  productId: number;
  systemId?: number;
  name: string;
  description?: string | null;
  icon?: string | null;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
  productVariants?: ProductVariant[];
}

export interface ProductVariant {
  productVariantId: number;
  productId?: number;
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  externalAccountingItemId?: string | null;
  price: number;
  isActive: boolean;
  displayOrder: number;
  createdAt?: string;
  updatedAt?: string;
}

// ---- Create / Update request shapes ----

export interface CreateServiceRequest {
  name: string;
  description?: string | null;
  icon?: string | null;
  isActive?: boolean;
}

export interface UpdateServiceRequest {
  name: string;
  description?: string | null;
  icon?: string | null;
  isActive?: boolean;
}

export interface CreateServiceItemRequest {
  name: string;
  description?: string | null;
  cost?: number | null;
  icon?: string | null;
  productId?: number | null;
}

export interface UpdateServiceItemRequest {
  name: string;
  description?: string | null;
  cost?: number | null;
  icon?: string | null;
  productId?: number | null;
}

export interface CreateProductRequest {
  name: string;
  description?: string | null;
  icon?: string | null;
  isActive?: boolean;
}

export interface UpdateProductRequest {
  name: string;
  description?: string | null;
  icon?: string | null;
  isActive?: boolean;
}

export interface CreateProductVariantRequest {
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  price: number;
  isActive?: boolean;
}

export interface UpdateProductVariantRequest {
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  price: number;
  isActive?: boolean;
}

// ---- Search response wire-format (paginated) ----

export interface PagedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

/** Minimal HTTP adapter peer-injected by consumer. Mirrors the same shape as @chthonicsystems/views. */
export interface CatalogHttpAdapter {
  get(url: string): Promise<Response | undefined>;
  post(url: string, body: unknown): Promise<Response | undefined>;
  put(url: string, body: unknown): Promise<Response | undefined>;
  delete(url: string): Promise<Response | undefined>;
}

// ---- Service packages (v0.2.0 / RFC 0034) ----

export interface ServicePackageItem {
  servicePackageItemId: number;
  /** Set when the component is a ServiceItem. XOR with productVariantId. */
  serviceItemId?: number | null;
  /** Set when the component is a ProductVariant. XOR with serviceItemId. */
  productVariantId?: number | null;
  quantity: number;
  displayOrder: number;
  /** Component display name (detail endpoint only). */
  name?: string | null;
  /** Component unit amount — ServiceItem.cost or ProductVariant.price (detail endpoint only). */
  unitAmount?: number | null;
}

export interface ServicePackage {
  servicePackageId: number;
  /** Optional owning Service — null for standalone packages (RFC 0034 § 12h). */
  serviceId?: number | null;
  name: string;
  description?: string | null;
  isActive: boolean;
  displayOrder: number;
  /** List endpoint projects the count; detail endpoint projects items. */
  itemCount?: number;
  /** Sum-of-components total (detail endpoint only; RFC 0034 § 3). */
  totalAmount?: number;
  items?: ServicePackageItem[];
  createdAt: string;
  updatedAt: string;
}

export interface ServicePackageItemInput {
  serviceItemId?: number | null;
  productVariantId?: number | null;
  quantity: number;
}

export interface CreateServicePackageRequest {
  name: string;
  description?: string | null;
  serviceId?: number | null;
  items: ServicePackageItemInput[];
  isActive?: boolean;
}

export interface UpdateServicePackageRequest {
  name: string;
  description?: string | null;
  serviceId?: number | null;
  /** When present, atomically replaces the package's component rows. */
  items?: ServicePackageItemInput[];
  isActive?: boolean;
}
