using Microsoft.EntityFrameworkCore;
using Products.Application;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence;

namespace Products.IntegrationTests.Persistence;

[Collection(SqlServerTestGroup.Name)]
public sealed class ProductRepositoryTests(SqlServerDatabaseFixture database)
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task AddAsync_AssignsConsecutiveSixDigitIdsFromTheSequence()
    {
        var first = await AddProductAsync("Sequence A");
        var second = await AddProductAsync("Sequence B");

        Assert.InRange(first.Id, Product.MinId, Product.MaxId);
        Assert.True(second.Id > first.Id, $"Expected {second.Id} to be greater than {first.Id}.");
        Assert.NotEmpty(first.RowVersion);
    }

    [Fact]
    public async Task AddAsync_ConcurrentInserts_GetUniqueIds()
    {
        var products = await Task.WhenAll(Enumerable.Range(1, 20).Select(i => AddProductAsync($"Parallel {i}")));

        Assert.Equal(20, products.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public async Task TryDecrementStockAsync_ConcurrentRequests_NeverOversell()
    {
        var product = await AddProductAsync("Limited Edition", stock: 10);

        var results = await Task.WhenAll(Enumerable.Range(1, 25).Select(async _ =>
        {
            await using var context = database.CreateContext();
            return await new ProductRepository(context).DecreaseStockAsync(product.Id, 1, cancellationToken);
        }));

        Assert.Equal(10, results.Count(succeeded => succeeded));
        Assert.Equal(0, (await GetAsync(product.Id))?.Stock);
    }

    [Fact]
    public async Task TryDecrementStockAsync_InsufficientStock_ReturnsFalseAndKeepsStock()
    {
        var product = await AddProductAsync("Scarce", stock: 2);

        var succeeded = await WithRepositoryAsync(r => r.DecreaseStockAsync(product.Id, 3, cancellationToken));

        Assert.False(succeeded);
        Assert.Equal(2, (await GetAsync(product.Id))?.Stock);
    }

    [Fact]
    public async Task TryDecrementStockAsync_ChangesRowVersion()
    {
        var product = await AddProductAsync("Versioned", stock: 5);

        await WithRepositoryAsync(r => r.DecreaseStockAsync(product.Id, 1, cancellationToken));

        Assert.NotEqual(product.RowVersion, (await GetAsync(product.Id))?.RowVersion);
    }

    [Fact]
    public async Task TryIncrementStockAsync_AddsQuantity()
    {
        var product = await AddProductAsync("Restock", stock: 5);

        var succeeded = await WithRepositoryAsync(r => r.IncreaseStockAsync(product.Id, 7, cancellationToken));

        Assert.True(succeeded);
        Assert.Equal(12, (await GetAsync(product.Id))?.Stock);
    }

    [Fact]
    public async Task TryIncrementStockAsync_WouldOverflow_ReturnsFalse()
    {
        var product = await AddProductAsync("Overflow", stock: int.MaxValue - 1);

        var succeeded = await WithRepositoryAsync(r => r.IncreaseStockAsync(product.Id, 2, cancellationToken));

        Assert.False(succeeded);
        Assert.Equal(int.MaxValue - 1, (await GetAsync(product.Id))?.Stock);
    }

    [Fact]
    public async Task UpdateAsync_CurrentRowVersion_SavesChanges()
    {
        var product = await AddProductAsync("Before");

        var updated = await WithRepositoryAsync(r =>
            r.UpdateAsync(product.Id, p => p.Name = "After", product.RowVersion, cancellationToken));

        Assert.Equal("After", updated?.Name);
        Assert.Equal("After", (await GetAsync(product.Id))?.Name);
    }

    [Fact]
    public async Task UpdateAsync_StaleRowVersion_ThrowsConflictAndKeepsData()
    {
        var product = await AddProductAsync("Original");
        await WithRepositoryAsync(r => r.IncreaseStockAsync(product.Id, 1, cancellationToken));

        await Assert.ThrowsAsync<ConflictException>(() => WithRepositoryAsync(r =>
            r.UpdateAsync(product.Id, p => p.Name = "Overwritten", product.RowVersion, cancellationToken)));
        Assert.Equal("Original", (await GetAsync(product.Id))?.Name);
    }

    [Fact]
    public async Task UpdateAsync_RowChangedAfterLoad_ThrowsConflict()
    {
        var product = await AddProductAsync("Race");

        await Assert.ThrowsAsync<ConflictException>(() => WithRepositoryAsync(r =>
            r.UpdateAsync(
                product.Id,
                p =>
                {
                    // Another request changes the row between our read and our save.
                    using var other = database.CreateContext();
                    other.Products.Where(x => x.Id == product.Id)
                        .ExecuteUpdate(s => s.SetProperty(x => x.Stock, x => x.Stock + 1));
                    p.Name = "Lost update";
                },
                product.RowVersion,
                cancellationToken)));
        Assert.Equal("Race", (await GetAsync(product.Id))?.Name);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        var updated = await WithRepositoryAsync(r =>
            r.UpdateAsync(Product.MaxId, p => p.Name = "Ghost", [0, 0, 0, 0, 0, 0, 0, 1], cancellationToken));

        Assert.Null(updated);
    }

    [Fact]
    public async Task SearchByNameAsync_MatchesPartialNameIgnoringCase()
    {
        var marker = UniqueMarker();
        await AddProductAsync($"{marker} Mechanical Keyboard");
        await AddProductAsync($"{marker} Wireless Mouse");

        var results = await WithRepositoryAsync(r => r.GetByNameAsync($"{marker} mechanical", cancellationToken));

        Assert.Equal([$"{marker} Mechanical Keyboard"], results.Select(p => p.Name));
    }

    [Fact]
    public async Task SearchByNameAsync_TreatsLikeWildcardsAsLiteralText()
    {
        var marker = UniqueMarker();
        await AddProductAsync($"{marker} 50% off");
        await AddProductAsync($"{marker} 500 pack");

        var results = await WithRepositoryAsync(r => r.GetByNameAsync($"{marker} 50%", cancellationToken));

        Assert.Equal([$"{marker} 50% off"], results.Select(p => p.Name));
    }

    [Fact]
    public async Task ListByStockRangeAsync_IncludesBothBounds()
    {
        var below = await AddProductAsync("Range below", stock: 1_000_000);
        var lower = await AddProductAsync("Range lower", stock: 1_000_001);
        var upper = await AddProductAsync("Range upper", stock: 1_000_003);
        var above = await AddProductAsync("Range above", stock: 1_000_004);

        var results = await WithRepositoryAsync(r => r.GetStockByRangeAsync(1_000_001, 1_000_003, cancellationToken));
        var ids = results.Select(p => p.Id).ToList();

        Assert.Contains(lower.Id, ids);
        Assert.Contains(upper.Id, ids);
        Assert.DoesNotContain(below.Id, ids);
        Assert.DoesNotContain(above.Id, ids);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheProductOnlyOnce()
    {
        var product = await AddProductAsync("Disposable");

        var first = await WithRepositoryAsync(r => r.DeleteAsync(product.Id, cancellationToken));
        var second = await WithRepositoryAsync(r => r.DeleteAsync(product.Id, cancellationToken));

        Assert.True(first);
        Assert.False(second);
        Assert.Null(await GetAsync(product.Id));
    }

    [Fact]
    public async Task Database_RejectsNegativeStock()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() => AddProductAsync("Negative", stock: -1));
    }

    private async Task<Product> AddProductAsync(string name, int stock = 10)
    {
        var product = new Product { Name = name, Price = 9.99m, Stock = stock };
        await WithRepositoryAsync(async r =>
        {
            await r.AddAsync(product, cancellationToken);
            return product;
        });
        return product;
    }

    private Task<Product?> GetAsync(int id) =>
        WithRepositoryAsync(r => r.GetByIdAsync(id, cancellationToken));

    private async Task<T> WithRepositoryAsync<T>(Func<ProductRepository, Task<T>> action)
    {
        await using var context = database.CreateContext();
        return await action(new ProductRepository(context));
    }

    private static string UniqueMarker() => Guid.NewGuid().ToString("N")[..8];
}
