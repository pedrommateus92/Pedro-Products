using Products.Application.Products;

namespace Products.Infrastructure.Products;

/// <summary>
/// Returns fixed sample data so the API can be exercised end to end.
/// Replaced by the EF Core implementation once the database is wired up.
/// </summary>
internal sealed class PlaceholderProductService : IProductService
{
    private static readonly ProductResponse Sample = new(100001, "Sample product", "Placeholder data", 9.99m, 10);

    private static readonly IReadOnlyList<ProductResponse> Samples = [Sample];

    public Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Samples);

    public Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult<ProductResponse?>(Sample with { Id = id });

    public Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new ProductResponse(Sample.Id, request.Name, request.Description, request.Price, request.Stock));

    public Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<ProductResponse?>(new ProductResponse(id, request.Name, request.Description, request.Price, request.Stock));

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<ProductResponse?> DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        Task.FromResult<ProductResponse?>(Sample with { Id = id });

    public Task<ProductResponse?> AddToStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        Task.FromResult<ProductResponse?>(Sample with { Id = id });

    public Task<IReadOnlyList<ProductResponse>> SearchByNameAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Samples);

    public Task<IReadOnlyList<ProductResponse>> GetByStockLevelAsync(int min, int max, CancellationToken cancellationToken) =>
        Task.FromResult(Samples);
}
