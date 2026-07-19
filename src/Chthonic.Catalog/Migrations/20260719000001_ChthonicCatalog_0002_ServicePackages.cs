using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chthonic.Catalog.Migrations;

/// <summary>
/// Idempotent placeholder migration for v0.2.0 (RFC 0034 § 12j). Per
/// the migration coexistence pattern, the library registers EF metadata
/// only — the consumer's migration owns the schema.
/// </summary>
/// <remarks>
/// The consumer applies (TorqueTech:
/// <c>RegisterChthonicCatalog020_ServicePackages</c>):
///
/// <code>
/// CREATE TABLE service_package (
///   service_package_id INT AUTO_INCREMENT PRIMARY KEY,
///   system_id          INT NOT NULL,               -- FK system CASCADE
///   service_id         INT NULL,                   -- FK service SET NULL
///   name               VARCHAR(200) NOT NULL,
///   description        VARCHAR(1000) NULL,
///   is_active          TINYINT(1) NOT NULL DEFAULT 1,
///   display_order      INT NOT NULL DEFAULT 0,
///   created_at         DATETIME(6) NOT NULL,
///   updated_at         DATETIME(6) NOT NULL,
///   INDEX idx_service_package_system_order (system_id, display_order)
/// );
///
/// CREATE TABLE service_package_item (
///   service_package_item_id INT AUTO_INCREMENT PRIMARY KEY,
///   service_package_id      INT NOT NULL,          -- FK service_package CASCADE
///   service_item_id         INT NULL,              -- FK service_item RESTRICT
///   product_variant_id      INT NULL,              -- FK product_variant RESTRICT
///   quantity                DECIMAL(10,2) NOT NULL,
///   display_order           INT NOT NULL DEFAULT 0,
///   INDEX idx_service_package_item_package_order (service_package_id, display_order),
///   CONSTRAINT chk_service_package_item_xor CHECK (
///     (service_item_id IS NULL) &lt;&gt; (product_variant_id IS NULL))
/// );
/// </code>
///
/// The XOR CHECK constraint lives consumer-side because EF's fluent
/// API can't express it portably; <c>ServicePackageService</c> also
/// validates on every write.
/// </remarks>
public partial class ChthonicCatalog_0002_ServicePackages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty. See class XML doc above.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty.
    }
}
