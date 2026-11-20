using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Products.Application.Products;

namespace Products.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.InvariantCulture;

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
