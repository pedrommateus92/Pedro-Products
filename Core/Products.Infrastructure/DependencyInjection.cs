using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Products.Application.Products;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ProductsDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ProductsDbContext>(options => options
            .UseSqlServer(connectionString)
            .UseSeeding((context, _) => ProductSeeder.Seed(context))
            .UseAsyncSeeding((context, _, cancellationToken) => ProductSeeder.SeedAsync(context, cancellationToken)));

        services.AddScoped<IProductRepository, ProductRepository>();
        return services;
    }
}
