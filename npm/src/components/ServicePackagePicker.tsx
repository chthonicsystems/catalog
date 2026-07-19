import React, { useEffect, useMemo, useState } from 'react';
import type { ServicePackage } from '../types';
import type { ServicePackageService } from '../services/servicePackageService';

export interface ServicePackagePickerProps {
  /** Service instance from `createServicePackageService(http)`. */
  service: ServicePackageService;
  /** Called with the full package (detail shape, items + total) when the user picks one. */
  onApply: (pkg: ServicePackage) => void;
  /** Optional cancel affordance. */
  onCancel?: () => void;
  /** Disables the apply buttons (e.g. while the consumer's apply call is in flight). */
  disabled?: boolean;
  placeholder?: string;
}

/**
 * Typeahead picker over the tenant's active ServicePackages
 * (v0.2.0 / RFC 0034). Plain-DOM implementation (no Ionic dependency)
 * styled via `svc-pkg-picker*` class hooks so each consumer themes it
 * with its own design system.
 *
 * The picker only *selects*; applying the package to a work unit is
 * consumer logic (RFC 0034 § 12b). `onApply` receives the detail shape
 * (items + sum-of-components total) fetched on selection.
 */
export const ServicePackagePicker: React.FC<ServicePackagePickerProps> = ({
  service,
  onApply,
  onCancel,
  disabled,
  placeholder = 'Search packages…',
}) => {
  const [query, setQuery] = useState('');
  const [packages, setPackages] = useState<ServicePackage[]>([]);
  const [loading, setLoading] = useState(false);
  const [applyingId, setApplyingId] = useState<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    const handle = setTimeout(async () => {
      const results = query.trim().length >= 2
        ? await service.searchPackages(query.trim())
        : await service.listPackages({ activeOnly: true });
      if (!cancelled) {
        setPackages(results);
        setLoading(false);
      }
    }, 300);
    return () => {
      cancelled = true;
      clearTimeout(handle);
    };
  }, [query, service]);

  const sorted = useMemo(
    () => [...packages].sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name)),
    [packages],
  );

  const handlePick = async (pkg: ServicePackage) => {
    if (disabled || applyingId !== null) return;
    setApplyingId(pkg.servicePackageId);
    try {
      // Fetch the detail shape so the consumer gets items + total.
      const detail = await service.getPackage(pkg.servicePackageId);
      onApply(detail ?? pkg);
    } finally {
      setApplyingId(null);
    }
  };

  return (
    <div className="svc-pkg-picker">
      <input
        className="svc-pkg-picker__search"
        type="search"
        value={query}
        placeholder={placeholder}
        aria-label="Search service packages"
        onChange={(e) => setQuery(e.target.value)}
      />
      {loading && <div className="svc-pkg-picker__loading">Loading…</div>}
      {!loading && sorted.length === 0 && (
        <div className="svc-pkg-picker__empty">No packages found</div>
      )}
      <ul className="svc-pkg-picker__list" role="listbox" aria-label="Service packages">
        {sorted.map((pkg) => (
          <li key={pkg.servicePackageId} className="svc-pkg-picker__row" role="option" aria-selected="false">
            <div className="svc-pkg-picker__meta">
              <span className="svc-pkg-picker__name">{pkg.name}</span>
              {pkg.description && <span className="svc-pkg-picker__desc">{pkg.description}</span>}
              {typeof pkg.itemCount === 'number' && (
                <span className="svc-pkg-picker__count">{pkg.itemCount} items</span>
              )}
            </div>
            <button
              type="button"
              className="svc-pkg-picker__apply"
              disabled={disabled || applyingId !== null || undefined}
              onClick={() => void handlePick(pkg)}
            >
              {applyingId === pkg.servicePackageId ? 'Applying…' : 'Apply'}
            </button>
          </li>
        ))}
      </ul>
      {onCancel && (
        <button type="button" className="svc-pkg-picker__cancel" onClick={onCancel}>
          Cancel
        </button>
      )}
    </div>
  );
};
