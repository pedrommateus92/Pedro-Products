using Microsoft.EntityFrameworkCore;
using Products.Application;
using Products.Application.Products;
using Products.Domain.Entities;

namespace Products.Infrastructure.Persistence;

internal sealed class ProductRepository(ProductsDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken cancellationToken) =>
        await dbContext.Products.AsNoTracking()
            .Where(p => p.Name.Contains(name))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> ListByStockRangeAsync(int min, int max, CancellationToken cancellationToken) =>
        await dbContext.Products.AsNoTracking()
            .Where(p => p.Stock >= min && p.Stock <= max)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Product?> UpdateAsync(int id, Action<Product> applyChanges, byte[] expectedRowVersion, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        if (!product.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
        {
            throw ConcurrencyConflict(id);
        }

        dbContext.Entry(product).Property(p => p.RowVersion).OriginalValue = expectedRowVersion;
        applyChanges(product);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ConcurrencyConflict(id);
        }

        return product;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Products.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(p => p.Id == id, cancellationToken);

    public async Task<bool> TryDecrementStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        await dbContext.Products
            .Where(p => p.Id == id && p.Stock >= quantity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Stock, p => p.Stock - quantity), cancellationToken) > 0;

    public async Task<bool> TryIncrementStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        await dbContext.Products
            .Where(p => p.Id == id && p.Stock <= int.MaxValue - quantity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Stock, p => p.Stock + quantity), cancellationToken) > 0;

    private static ConflictException ConcurrencyConflict(int id) =>
        new($"Product {id} was changed by someone else. Reload it and try again.");
}
