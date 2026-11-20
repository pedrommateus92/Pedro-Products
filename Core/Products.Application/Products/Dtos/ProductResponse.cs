namespace Products.Application.Products.Dtos;

public sealed record ProductResponse(int Id, string Name, string? Description, decimal Price, int Stock, string RowVersion);
