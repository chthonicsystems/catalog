using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chthonic.Catalog.Migrations;

/// <summary>
/// Idempotent placeholder migration. Per RFC 0021 § 2 and the migration
/// coexistence pattern in <c>AGENT-CONTEXT.md § 5</c>, the library's
/// <c>_Initial</c> migration registers EF metadata only — the actual
/// schema lives consumer-side and the consumer's own migration owns
/// the <c>CREATE TABLE</c> / <c>ALTER TABLE</c> statements.
/// </summary>
/// <remarks>
/// TorqueTech's <c>_RegisterChthonicCatalog</c> migration (PR 11.5)
/// inserts a row in <c>__EFMigrationsHistory</c> so this migration is
/// registered as already-applied at app boot. The catalog tables
/// (<c>service</c>, <c>service_item</c>, <c>product</c>, <c>product_variant</c>)
/// already exist in TT's schema with bare descriptive names — no
/// rename or recreation is needed.
/// </remarks>
public partial class ChthonicCatalog_0001_Initial : Migration
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
