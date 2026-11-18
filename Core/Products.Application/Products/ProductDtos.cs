namespace Products.Application.Products;

public sealed record ProductResponse(int Id, string Name, string? Description, decimal Price, int Stock, string RowVersion);

public sealed record CreateProductRequest(string Name, string? Description, decimal Price, int Stock);

public sealed record UpdateProductRequest(string Name, string? Description, decimal Price, int Stock, string RowVersion);
