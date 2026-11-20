using Products.Application;
using Products.Application.Products;
using Products.Domain.Entities;

namespace Products.UnitTests.Fakes;

/// <summary>
/// Mirrors the contract of the EF repository: IDs from 100000, a new row version on every write,
/// and stock changes that only apply when the guard holds.
/// </summary>
internal sealed class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<int, Product> products = [];
    private int nextId = Product.MinId;
    private long nextRowVersion = 1;

    public Product Seed(string name, int stock = 10, decimal price = 9.99m, string? description = null)
    {
        var product = new Product { Name = name, Description = description, Price = price, Stock = stock };
        Insert(product);
        return Copy(product);
    }

    public Product? Find(int id) => products.TryGetValue(id, out var product) ? Copy(product) : null;

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken) =>
        Result(products.Values.OrderBy(p => p.Id));

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Find(id));

    public Task<IReadOnlyList<Product>> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        Result(products.Values
            .Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id));

    public Task<IReadOnlyList<Product>> GetStockByRangeAsync(int min, int max, CancellationToken cancellationToken) =>
        Result(products.Values
            .Where(p => p.Stock >= min && p.Stock <= max)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Id));

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        Insert(product);
        return Task.CompletedTask;
    }

    public Task<Product?> UpdateAsync(int id, Action<Product> applyChanges, byte[] expectedRowVersion, CancellationToken cancellationToken)
    {
        if (!products.TryGetValue(id, out var stored))
        {
            return Task.FromResult<Product?>(null);
        }

        if (!stored.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
        {
            throw new ConflictException($"Product {id} was changed by someone else.");
        }

        applyChanges(stored);
        stored.RowVersion = NewRowVersion();
        return Task.FromResult<Product?>(Copy(stored));
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(products.Remove(id));

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(products.ContainsKey(id));

    public Task<bool> DecreaseStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        Task.FromResult(TryChangeStock(id, p => p.Stock >= quantity, p => p.Stock - quantity));

    public Task<bool> IncreaseStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        Task.FromResult(TryChangeStock(id, p => p.Stock <= int.MaxValue - quantity, p => p.Stock + quantity));

    private bool TryChangeStock(int id, Func<Product, bool> guard, Func<Product, int> newStock)
    {
        if (!products.TryGetValue(id, out var stored) || !guard(stored))
        {
            return false;
        }

        stored.Stock = newStock(stored);
        stored.RowVersion = NewRowVersion();
        return true;
    }

    private void Insert(Product product)
    {
        product.Id = nextId++;
        product.RowVersion = NewRowVersion();
        products[product.Id] = Copy(product);
    }

    private byte[] NewRowVersion() => BitConverter.GetBytes(nextRowVersion++);

    private static Product Copy(Product product) =>
        new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            RowVersion = [.. product.RowVersion],
        };

    private static Task<IReadOnlyList<Product>> Result(IEnumerable<Product> source) =>
        Task.FromResult<IReadOnlyList<Product>>([.. source.Select(Copy)]);
}
