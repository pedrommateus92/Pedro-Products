namespace Products.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    Task<ProductResponse?> DecreaseStockAsync(int id, int quantity, CancellationToken cancellationToken);

    Task<ProductResponse?> AddStockAsync(int id, int quantity, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> GetStockByRangeAsync(int min, int max, CancellationToken cancellationToken);
}
