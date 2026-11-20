using FluentValidation;
using FluentValidation.Results;

namespace Products.Application.Products;

internal sealed class ProductService(
    IProductRepository repository,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).ToResponses();

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        (await repository.GetByIdAsync(id, cancellationToken))?.ToResponse();

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var product = request.ToEntity();
        await repository.AddAsync(product, cancellationToken);
        return product.ToResponse();
    }

    public async Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var expectedRowVersion = Convert.FromBase64String(request.RowVersion);
        var product = await repository.UpdateAsync(id, request.Apply, expectedRowVersion, cancellationToken);
        return product?.ToResponse();
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    public async Task<ProductResponse?> DecreaseStockAsync(int id, int quantity, CancellationToken cancellationToken)
    {
        EnsurePositiveQuantity(quantity);

        if (!await repository.DecreaseStockAsync(id, quantity, cancellationToken))
        {
            if (!await repository.ExistsAsync(id, cancellationToken))
                return null;

            throw new ConflictException($"Insufficient stock to remove {quantity} unit(s) from product {id}.");
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProductResponse?> AddStockAsync(int id, int quantity, CancellationToken cancellationToken)
    {
        EnsurePositiveQuantity(quantity);

        if (!await repository.IncreaseStockAsync(id, quantity, cancellationToken))
        {
            if (!await repository.ExistsAsync(id, cancellationToken))
                return null;

            throw new ConflictException($"Adding {quantity} unit(s) would exceed the maximum stock for product {id}.");
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException([new ValidationFailure("Name", "'Name' must not be empty.")]);

        return (await repository.GetByNameAsync(name.Trim(), cancellationToken)).ToResponses();
    }

    public async Task<IReadOnlyList<ProductResponse>> GetStockByRangeAsync(int min, int max, CancellationToken cancellationToken)
    {
        List<ValidationFailure> failures = [];
        if (min < 0)
            failures.Add(new ValidationFailure("Min", "'Min' must be greater than or equal to 0."));

        if (max < 0)
            failures.Add(new ValidationFailure("Max", "'Max' must be greater than or equal to 0."));

        if (min > max)
            failures.Add(new ValidationFailure("Min", "'Min' must be less than or equal to 'Max'."));

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return (await repository.GetStockByRangeAsync(min, max, cancellationToken)).ToResponses();
    }

    private static void EnsurePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException([new ValidationFailure("Quantity", "'Quantity' must be greater than 0.")]);
    }
}
