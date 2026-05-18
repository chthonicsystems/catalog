using Microsoft.EntityFrameworkCore;

namespace Chthonic.Catalog.Extensions;

/// <summary>
/// Provider for the consumer's <see cref="DbContext"/>. Library services
/// resolve this at runtime so the library never compile-time depends on
/// any product's specific DbContext type. TorqueTech registers an
/// implementation that returns <c>TorqueTechDbContext</c>; sister
/// products register their own.
/// </summary>
/// <remarks>
/// Mirrors the pattern in <c>@chthonic/views</c> and
/// <c>@chthonic/notifications</c> — same shape, separate type so each
/// library is independently scopable and testable.
/// </remarks>
public interface IDbContextProvider
{
    DbContext GetContext();
}
