import type {
  CatalogHttpAdapter,
  Service,
  ServiceItem,
  Product,
  ProductVariant,
  CreateServiceRequest,
  UpdateServiceRequest,
  CreateServiceItemRequest,
  UpdateServiceItemRequest,
  CreateProductRequest,
  UpdateProductRequest,
  CreateProductVariantRequest,
  UpdateProductVariantRequest,
  PagedResponse,
} from '../types';

/**
 * Catalog HTTP service factory.
 *
 * Consumers (TorqueTech, MarineDeck, FlowLift, PetCare) pass their own
 * auth-aware HTTP adapter. Library never imports a TT-specific
 * `httpService` directly.
 */
export function createCatalogService(http: CatalogHttpAdapter) {
  async function readJson<T>(res: Response | undefined): Promise<T | null> {
    if (!res || !res.ok) return null;
    return (await res.json()) as T;
  }

  return {
    // ---- Services ----
    listServices: async (opts?: { systemId?: number; activeOnly?: boolean }): Promise<Service[]> => {
      const qs = new URLSearchParams();
      if (opts?.systemId) qs.set('systemId', String(opts.systemId));
      if (opts?.activeOnly) qs.set('activeOnly', 'true');
      const url = `/api/services${qs.toString() ? '?' + qs.toString() : ''}`;
      return (await readJson<Service[]>(await http.get(url))) ?? [];
    },

    getService: async (serviceId: number): Promise<Service | null> => {
      return readJson<Service>(await http.get(`/api/services/${serviceId}`));
    },

    searchServices: async (query: string, opts?: { systemId?: number; page?: number; pageSize?: number }): Promise<PagedResponse<Service> | Service[] | null> => {
      const qs = new URLSearchParams({ query });
      if (opts?.systemId) qs.set('systemId', String(opts.systemId));
      if (opts?.page) qs.set('page', String(opts.page));
      if (opts?.pageSize) qs.set('pageSize', String(opts.pageSize));
      return readJson<PagedResponse<Service> | Service[]>(await http.get(`/api/services/search?${qs.toString()}`));
    },

    createService: async (req: CreateServiceRequest): Promise<{ serviceId: number } | null> => {
      return readJson<{ serviceId: number }>(await http.post('/api/services', req));
    },

    updateService: async (serviceId: number, req: UpdateServiceRequest): Promise<boolean> => {
      const res = await http.put(`/api/services/${serviceId}`, req);
      return !!res?.ok;
    },

    deleteService: async (serviceId: number): Promise<boolean> => {
      const res = await http.delete(`/api/services/${serviceId}`);
      return !!res?.ok;
    },

    reorderService: async (serviceId: number, displayOrder: number): Promise<boolean> => {
      const res = await http.put(`/api/services/${serviceId}/reorder`, { displayOrder });
      return !!res?.ok;
    },

    // ---- Service items ----
    createServiceItem: async (serviceId: number, req: CreateServiceItemRequest): Promise<{ serviceItemId: number } | null> => {
      return readJson<{ serviceItemId: number }>(await http.post(`/api/services/${serviceId}/items`, req));
    },

    updateServiceItem: async (serviceId: number, serviceItemId: number, req: UpdateServiceItemRequest): Promise<boolean> => {
      const res = await http.put(`/api/services/${serviceId}/items/${serviceItemId}`, req);
      return !!res?.ok;
    },

    deleteServiceItem: async (serviceId: number, serviceItemId: number): Promise<boolean> => {
      const res = await http.delete(`/api/services/${serviceId}/items/${serviceItemId}`);
      return !!res?.ok;
    },

    reorderServiceItem: async (serviceId: number, serviceItemId: number, displayOrder: number): Promise<boolean> => {
      const res = await http.put(`/api/services/${serviceId}/items/${serviceItemId}/reorder`, { displayOrder });
      return !!res?.ok;
    },

    // ---- Products ----
    listProducts: async (opts?: { systemId?: number; activeOnly?: boolean }): Promise<Product[]> => {
      const qs = new URLSearchParams();
      if (opts?.systemId) qs.set('systemId', String(opts.systemId));
      if (opts?.activeOnly) qs.set('activeOnly', 'true');
      const url = `/api/products${qs.toString() ? '?' + qs.toString() : ''}`;
      return (await readJson<Product[]>(await http.get(url))) ?? [];
    },

    getProduct: async (productId: number): Promise<Product | null> => {
      return readJson<Product>(await http.get(`/api/products/${productId}`));
    },

    searchProducts: async (query: string, opts?: { systemId?: number; page?: number; pageSize?: number }): Promise<PagedResponse<Product> | Product[] | null> => {
      const qs = new URLSearchParams({ query });
      if (opts?.systemId) qs.set('systemId', String(opts.systemId));
      if (opts?.page) qs.set('page', String(opts.page));
      if (opts?.pageSize) qs.set('pageSize', String(opts.pageSize));
      return readJson<PagedResponse<Product> | Product[]>(await http.get(`/api/products/search?${qs.toString()}`));
    },

    createProduct: async (req: CreateProductRequest): Promise<{ productId: number } | null> => {
      return readJson<{ productId: number }>(await http.post('/api/products', req));
    },

    updateProduct: async (productId: number, req: UpdateProductRequest): Promise<boolean> => {
      const res = await http.put(`/api/products/${productId}`, req);
      return !!res?.ok;
    },

    deleteProduct: async (productId: number): Promise<boolean> => {
      const res = await http.delete(`/api/products/${productId}`);
      return !!res?.ok;
    },

    // ---- Product variants ----
    createProductVariant: async (productId: number, req: CreateProductVariantRequest): Promise<{ productVariantId: number } | null> => {
      return readJson<{ productVariantId: number }>(await http.post(`/api/products/${productId}/variants`, req));
    },

    updateProductVariant: async (productId: number, productVariantId: number, req: UpdateProductVariantRequest): Promise<boolean> => {
      const res = await http.put(`/api/products/${productId}/variants/${productVariantId}`, req);
      return !!res?.ok;
    },

    deleteProductVariant: async (productId: number, productVariantId: number): Promise<boolean> => {
      const res = await http.delete(`/api/products/${productId}/variants/${productVariantId}`);
      return !!res?.ok;
    },

    searchProductVariants: async (productId: number, query: string, limit = 50): Promise<ProductVariant[]> => {
      const qs = new URLSearchParams({ query, limit: String(limit) });
      return (await readJson<ProductVariant[]>(await http.get(`/api/products/${productId}/variants/search?${qs.toString()}`))) ?? [];
    },
  };
}

export type CatalogService = ReturnType<typeof createCatalogService>;
