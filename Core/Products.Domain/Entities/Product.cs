namespace Products.Domain.Entities;

public sealed class Product
{
    public const int MinId = 100000;
    public const int MaxId = 999999;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
