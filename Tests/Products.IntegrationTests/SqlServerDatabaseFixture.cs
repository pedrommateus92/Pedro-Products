using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Products.Infrastructure.Persistence;

namespace Products.IntegrationTests;

/// <summary>
/// Creates a uniquely named database on the local SQL Server, applies the real migrations,
/// and drops the database when the test run ends.
/// </summary>
public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    public const string ServerEnvironmentVariable = "PRODUCTS_TEST_SQLSERVER";
    private const string DefaultServer = "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True";

    public string ConnectionString { get; } = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable(ServerEnvironmentVariable) ?? DefaultServer)
    {
        InitialCatalog = $"ProductsDb_Tests_{Guid.NewGuid():N}",
    }.ConnectionString;

    public ProductsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ProductsDbContext>().UseSqlServer(ConnectionString).Options);

    public async ValueTask InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerTestGroup : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SQL Server";
}
