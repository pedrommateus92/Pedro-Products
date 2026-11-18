using Microsoft.Extensions.DependencyInjection;
using Products.Application.Products;
using Products.Infrastructure.Products;

namespace Products.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductService, PlaceholderProductService>();
        return services;
    }
}
