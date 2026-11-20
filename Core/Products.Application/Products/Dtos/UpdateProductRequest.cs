namespace Products.Application.Products.Dtos;

public sealed record UpdateProductRequest(string Name, string? Description, decimal Price, int Stock, string RowVersion);
