using Products.Domain.Entities;

namespace Products.Application.Products;

internal static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.Stock, Convert.ToBase64String(product.RowVersion));

    public static IReadOnlyList<ProductResponse> ToResponses(this IReadOnlyList<Product> products) =>
        [.. products.Select(ToResponse)];

    public static Product ToEntity(this CreateProductRequest request) =>
        new()
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            Stock = request.Stock,
        };

    public static void Apply(this UpdateProductRequest request, Product product)
    {
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
    }
}
