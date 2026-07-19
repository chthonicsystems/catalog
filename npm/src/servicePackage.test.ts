import { describe, it, expect, vi } from 'vitest';
import {
  CATALOG_PACKAGE_VERSION,
  createServicePackageService,
  type CatalogHttpAdapter,
  type ServicePackage,
} from './index';

function jsonResponse(body: unknown, ok = true): Response {
  return {
    ok,
    json: async () => body,
  } as unknown as Response;
}

describe('@chthonicsystems/catalog service packages (v0.2.0)', () => {
  it('bumps CATALOG_PACKAGE_VERSION to 0.2.0', () => {
    expect(CATALOG_PACKAGE_VERSION).toBe('0.2.1');
  });

  it('ServicePackage detail shape round-trips JSON', () => {
    const sample: ServicePackage = {
      servicePackageId: 7,
      serviceId: null,
      name: 'Major Service',
      description: 'The full annual works',
      isActive: true,
      displayOrder: 1,
      totalAmount: 131,
      items: [
        { servicePackageItemId: 1, serviceItemId: 3, productVariantId: null, quantity: 1, displayOrder: 0, name: 'Labour', unitAmount: 100 },
        { servicePackageItemId: 2, serviceItemId: null, productVariantId: 42, quantity: 2, displayOrder: 1, name: '10W-40 1L', unitAmount: 15.5 },
      ],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    const parsed: ServicePackage = JSON.parse(JSON.stringify(sample));
    expect(parsed.items).toHaveLength(2);
    expect(parsed.items![0].serviceItemId).toBe(3);
    expect(parsed.items![1].productVariantId).toBe(42);
  });

  it('listPackages hits /api/service-packages with activeOnly', async () => {
    const http: CatalogHttpAdapter = {
      get: vi.fn(async () => jsonResponse([])),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
    };
    const svc = createServicePackageService(http);
    await svc.listPackages({ activeOnly: true });
    expect(http.get).toHaveBeenCalledWith('/api/service-packages?activeOnly=true');
  });

  it('createPackage posts the mixed-item body and returns the id', async () => {
    const http: CatalogHttpAdapter = {
      get: vi.fn(),
      post: vi.fn(async () => jsonResponse({ servicePackageId: 12 })),
      put: vi.fn(),
      delete: vi.fn(),
    };
    const svc = createServicePackageService(http);
    const created = await svc.createPackage({
      name: 'Brake Job',
      items: [
        { serviceItemId: 1, quantity: 1 },
        { productVariantId: 42, quantity: 2 },
      ],
    });
    expect(created?.servicePackageId).toBe(12);
    expect(http.post).toHaveBeenCalledWith('/api/service-packages', expect.objectContaining({ name: 'Brake Job' }));
  });

  it('getPackage returns null on 404 (feature-not-enabled contract)', async () => {
    const http: CatalogHttpAdapter = {
      get: vi.fn(async () => jsonResponse({}, false)),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
    };
    const svc = createServicePackageService(http);
    expect(await svc.getPackage(999)).toBeNull();
  });
});
