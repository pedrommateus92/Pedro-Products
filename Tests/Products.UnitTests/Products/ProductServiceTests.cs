using FluentValidation;
using Products.Application;
using Products.Application.Products;
using Products.Application.Products.Dtos;
using Products.Application.Products.Validators;
using Products.UnitTests.Fakes;

namespace Products.UnitTests.Products;

public sealed class ProductServiceTests
{
    private readonly InMemoryProductRepository repository = new();
    private readonly ProductService service;
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    public ProductServiceTests() =>
        service = new ProductService(repository, new CreateProductRequestValidator(), new UpdateProductRequestValidator());

    [Fact]
    public async Task GetAllAsync_ReturnsEveryProductWithItsStock()
    {
        repository.Seed("Keyboard", stock: 5);
        repository.Seed("Mouse", stock: 0);

        var products = await service.GetAllAsync(cancellationToken);

        Assert.Collection(
            products,
            p => Assert.Equal(("Keyboard", 5), (p.Name, p.Stock)),
            p => Assert.Equal(("Mouse", 0), (p.Name, p.Stock)));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var product = await service.GetByIdAsync(999999, cancellationToken);

        Assert.Null(product);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_StoresTrimmedProductWithSixDigitId()
    {
        var request = new CreateProductRequest("  Monitor  ", "  27 inch ", 279.00m, 8);

        var created = await service.CreateAsync(request, cancellationToken);

        Assert.InRange(created.Id, 100000, 999999);
        Assert.Equal("Monitor", created.Name);
        Assert.Equal("27 inch", created.Description);
        Assert.NotEmpty(created.RowVersion);
        Assert.Equal("Monitor", repository.Find(created.Id)?.Name);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        var request = new CreateProductRequest("", null, -1m, -1);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request, cancellationToken));

        Assert.Equal(["Name", "Price", "Stock"], exception.Errors.Select(e => e.PropertyName).Order());
    }

    [Fact]
    public async Task UpdateAsync_CurrentRowVersion_UpdatesProduct()
    {
        var existing = await service.CreateAsync(new CreateProductRequest("Mouse", null, 10m, 1), cancellationToken);
        var request = new UpdateProductRequest("Wireless Mouse", "USB-C", 12.50m, 3, existing.RowVersion);

        var updated = await service.UpdateAsync(existing.Id, request, cancellationToken);

        Assert.NotNull(updated);
        Assert.Equal(("Wireless Mouse", "USB-C", 12.50m, 3), (updated.Name, updated.Description, updated.Price, updated.Stock));
        Assert.NotEqual(existing.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task UpdateAsync_StaleRowVersion_ThrowsConflict()
    {
        var existing = await service.CreateAsync(new CreateProductRequest("Mouse", null, 10m, 1), cancellationToken);
        await service.UpdateAsync(existing.Id, new UpdateProductRequest("Mouse v2", null, 10m, 1, existing.RowVersion), cancellationToken);

        var staleRequest = new UpdateProductRequest("Mouse v3", null, 10m, 1, existing.RowVersion);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(existing.Id, staleRequest, cancellationToken));
        Assert.Equal("Mouse v2", repository.Find(existing.Id)?.Name);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        var request = new UpdateProductRequest("Mouse", null, 10m, 1, Convert.ToBase64String([1, 2, 3, 4]));

        var updated = await service.UpdateAsync(999999, request, cancellationToken);

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_RemovesIt()
    {
        var product = repository.Seed("Keyboard");

        var deleted = await service.DeleteAsync(product.Id, cancellationToken);

        Assert.True(deleted);
        Assert.Null(repository.Find(product.Id));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var deleted = await service.DeleteAsync(999999, cancellationToken);

        Assert.False(deleted);
    }

    [Fact]
    public async Task DecrementStockAsync_EnoughStock_SubtractsQuantity()
    {
        var product = repository.Seed("Keyboard", stock: 5);

        var updated = await service.DecreaseStockAsync(product.Id, 5, cancellationToken);

        Assert.Equal(0, updated?.Stock);
    }

    [Fact]
    public async Task DecrementStockAsync_InsufficientStock_ThrowsConflictAndKeepsStock()
    {
        var product = repository.Seed("Keyboard", stock: 2);

        await Assert.ThrowsAsync<ConflictException>(() => service.DecreaseStockAsync(product.Id, 3, cancellationToken));
        Assert.Equal(2, repository.Find(product.Id)?.Stock);
    }

    [Fact]
    public async Task DecrementStockAsync_UnknownId_ReturnsNull()
    {
        var updated = await service.DecreaseStockAsync(999999, 1, cancellationToken);

        Assert.Null(updated);
    }

    [Fact]
    public async Task AddToStockAsync_AddsQuantity()
    {
        var product = repository.Seed("Keyboard", stock: 5);

        var updated = await service.AddStockAsync(product.Id, 10, cancellationToken);

        Assert.Equal(15, updated?.Stock);
    }

    [Fact]
    public async Task AddToStockAsync_WouldOverflow_ThrowsConflict()
    {
        var product = repository.Seed("Keyboard", stock: int.MaxValue);

        await Assert.ThrowsAsync<ConflictException>(() => service.AddStockAsync(product.Id, 1, cancellationToken));
    }

    [Fact]
    public async Task AddToStockAsync_UnknownId_ReturnsNull()
    {
        var updated = await service.AddStockAsync(999999, 1, cancellationToken);

        Assert.Null(updated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task StockChanges_NonPositiveQuantity_ThrowValidationException(int quantity)
    {
        var product = repository.Seed("Keyboard", stock: 5);

        await Assert.ThrowsAsync<ValidationException>(() => service.DecreaseStockAsync(product.Id, quantity, cancellationToken));
        await Assert.ThrowsAsync<ValidationException>(() => service.AddStockAsync(product.Id, quantity, cancellationToken));
        Assert.Equal(5, repository.Find(product.Id)?.Stock);
    }

    [Fact]
    public async Task SearchByNameAsync_MatchesPartialNameIgnoringCase()
    {
        repository.Seed("Mechanical Keyboard");
        repository.Seed("Wireless Mouse");
        repository.Seed("Keyboard Cover");

        var results = await service.GetByNameAsync(" keyboard ", cancellationToken);

        Assert.Equal(["Keyboard Cover", "Mechanical Keyboard"], results.Select(p => p.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByNameAsync_BlankName_ThrowsValidationException(string name)
    {
        await Assert.ThrowsAsync<ValidationException>(() => service.GetByNameAsync(name, cancellationToken));
    }

    [Fact]
    public async Task GetByStockLevelAsync_ReturnsProductsWithinInclusiveRange()
    {
        repository.Seed("Empty", stock: 0);
        repository.Seed("Low", stock: 5);
        repository.Seed("Edge", stock: 10);
        repository.Seed("High", stock: 11);

        var results = await service.GetStockByRangeAsync(5, 10, cancellationToken);

        Assert.Equal(["Low", "Edge"], results.Select(p => p.Name));
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(-1, 5)]
    [InlineData(0, -1)]
    public async Task GetByStockLevelAsync_InvalidRange_ThrowsValidationException(int min, int max)
    {
        await Assert.ThrowsAsync<ValidationException>(() => service.GetStockByRangeAsync(min, max, cancellationToken));
    }
}
