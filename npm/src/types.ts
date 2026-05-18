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
