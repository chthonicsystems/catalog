import { describe, it, expect, vi } from 'vitest';
import {
  CATALOG_PACKAGE_VERSION,
  createCatalogService,
  type Service,
  type ServiceItem,
  type Product,
  type ProductVariant,
  type CatalogHttpAdapter,
} from './index';

describe('@chthonicsystems/catalog package', () => {
  it('exports CATALOG_PACKAGE_VERSION', () => {
    expect(CATALOG_PACKAGE_VERSION).toBe('0.2.0');
  });

  it('Service shape round-trips JSON', () => {
    const sample: Service = {
      serviceId: 1,
      systemId: 1,
      name: 'Annual PM',
      description: 'Annual preventative maintenance',
      icon: 'wrench',
      isActive: true,
      displayOrder: 1,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    const parsed: Service = JSON.parse(JSON.stringify(sample));
    expect(parsed.name).toBe('Annual PM');
    expect(parsed.isActive).toBe(true);
  });

  it('ServiceItem optionally references Product via productId', () => {
    const item: ServiceItem = {
      serviceItemId: 1,
      serviceId: 1,
      productId: 42,
      name: 'Class A — 8m+ powerboat',
      cost: 85,
      displayOrder: 1,
    };
    expect(item.productId).toBe(42);
  });

  it('ProductVariant carries SKU + barcode + accounting external id', () => {
    const variant: ProductVariant = {
      productVariantId: 1,
      productId: 1,
      name: 'Pack of 20',
      sku: 'APO-54-20',
      barcode: '0123456789012',
      externalAccountingItemId: 'XERO-ITEM-9876',
      price: 89.95,
      isActive: true,
      displayOrder: 1,
    };
    expect(variant.sku).toBe('APO-54-20');
    expect(variant.externalAccountingItemId).toBe('XERO-ITEM-9876');
  });

  it('createCatalogService returns a service that calls the injected http adapter', async () => {
    const httpAdapter: CatalogHttpAdapter = {
      get: vi.fn().mockResolvedValue(new Response(JSON.stringify([{ serviceId: 1, name: 'svc', isActive: true, displayOrder: 1, createdAt: '', updatedAt: '' }]), { status: 200 })),
      post: vi.fn().mockResolvedValue(new Response(JSON.stringify({ serviceId: 7 }), { status: 201 })),
      put: vi.fn().mockResolvedValue(new Response(null, { status: 200 })),
      delete: vi.fn().mockResolvedValue(new Response(null, { status: 200 })),
    };

    const svc = createCatalogService(httpAdapter);
    const services = await svc.listServices();
    expect(httpAdapter.get).toHaveBeenCalledWith('/api/services');
    expect(services).toHaveLength(1);

    const created = await svc.createService({ name: 'New service' });
    expect(httpAdapter.post).toHaveBeenCalledWith('/api/services', { name: 'New service' });
    expect(created?.serviceId).toBe(7);
  });

  it('Product shape supports optional productVariants list (detail response)', () => {
    const product: Product = {
      productId: 1,
      systemId: 1,
      name: 'Apoquel 5.4mg',
      isActive: true,
      displayOrder: 1,
      createdAt: '',
      updatedAt: '',
      productVariants: [
        { productVariantId: 1, productId: 1, name: 'Pack of 20', price: 89, isActive: true, displayOrder: 1 },
        { productVariantId: 2, productId: 1, name: 'Pack of 50', price: 199, isActive: true, displayOrder: 2 },
      ],
    };
    expect(product.productVariants).toHaveLength(2);
    expect(product.productVariants?.[1].price).toBe(199);
  });
});
