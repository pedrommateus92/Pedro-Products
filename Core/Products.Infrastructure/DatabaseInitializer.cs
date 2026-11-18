using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and runs the seeding configured on the DbContext.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
