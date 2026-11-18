using Products.Domain.Entities;

namespace Products.Application.Products;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> SearchByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> ListByStockRangeAsync(int min, int max, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the product only if the stored row version still equals <paramref name="expectedRowVersion"/>.
    /// </summary>
    /// <returns>The updated product, or null when it does not exist.</returns>
    /// <exception cref="ConflictException">The product was changed by someone else.</exception>
    Task<Product?> UpdateAsync(int id, Action<Product> applyChanges, byte[] expectedRowVersion, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically subtracts <paramref name="quantity"/> when at least that much is in stock.
    /// </summary>
    /// <returns>True when the stock was decremented.</returns>
    Task<bool> TryDecrementStockAsync(int id, int quantity, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically adds <paramref name="quantity"/> unless the result would overflow.
    /// </summary>
    /// <returns>True when the stock was incremented.</returns>
    Task<bool> TryIncrementStockAsync(int id, int quantity, CancellationToken cancellationToken);
}
