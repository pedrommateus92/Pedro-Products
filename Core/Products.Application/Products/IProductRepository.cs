using Products.Domain.Entities;

namespace Products.Application.Products;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetStockByRangeAsync(int min, int max, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> UpdateAsync(int id, Action<Product> applyChanges, byte[] expectedRowVersion, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<bool> DecreaseStockAsync(int id, int quantity, CancellationToken cancellationToken);

    Task<bool> IncreaseStockAsync(int id, int quantity, CancellationToken cancellationToken);
}
