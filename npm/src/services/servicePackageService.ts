import type {
  CatalogHttpAdapter,
  CreateServicePackageRequest,
  ServicePackage,
  UpdateServicePackageRequest,
} from '../types';

/**
 * ServicePackage HTTP service factory (v0.2.0 / RFC 0034).
 *
 * Same peer-injected adapter pattern as `createCatalogService`. Talks
 * to the library's `/api/service-packages/*` surface (mounted by the
 * consumer — TorqueTech mounts it under a tier-gated route group, so a
 * Free-tier tenant sees 404s; callers should treat a 404 on the list
 * endpoint as "feature not enabled").
 *
 * Apply-to-work-unit is deliberately NOT here — apply semantics are
 * consumer-side (RFC 0034 § 12b). TorqueTech's apply endpoint is
 * `POST /api/jobs/{id}/apply-package`, owned by the TT app.
 */
export function createServicePackageService(http: CatalogHttpAdapter) {
  async function readJson<T>(res: Response | undefined): Promise<T | null> {
    if (!res || !res.ok) return null;
    return (await res.json()) as T;
  }

  return {
    listPackages: async (opts?: { activeOnly?: boolean }): Promise<ServicePackage[]> => {
      const qs = opts?.activeOnly ? '?activeOnly=true' : '';
      return (await readJson<ServicePackage[]>(await http.get(`/api/service-packages${qs}`))) ?? [];
    },

    searchPackages: async (query: string, opts?: { limit?: number }): Promise<ServicePackage[]> => {
      const qs = new URLSearchParams({ query });
      if (opts?.limit) qs.set('limit', String(opts.limit));
      return (await readJson<ServicePackage[]>(await http.get(`/api/service-packages/search?${qs.toString()}`))) ?? [];
    },

    getPackage: async (servicePackageId: number): Promise<ServicePackage | null> => {
      return readJson<ServicePackage>(await http.get(`/api/service-packages/${servicePackageId}`));
    },

    createPackage: async (req: CreateServicePackageRequest): Promise<{ servicePackageId: number } | null> => {
      return readJson<{ servicePackageId: number }>(await http.post('/api/service-packages', req));
    },

    updatePackage: async (servicePackageId: number, req: UpdateServicePackageRequest): Promise<boolean> => {
      const res = await http.put(`/api/service-packages/${servicePackageId}`, req);
      return !!res?.ok;
    },

    deletePackage: async (servicePackageId: number): Promise<boolean> => {
      const res = await http.delete(`/api/service-packages/${servicePackageId}`);
      return !!res?.ok;
    },

    reorderPackage: async (servicePackageId: number, displayOrder: number): Promise<boolean> => {
      const res = await http.put(`/api/service-packages/${servicePackageId}/reorder`, { displayOrder });
      return !!res?.ok;
    },
  };
}

export type ServicePackageService = ReturnType<typeof createServicePackageService>;
