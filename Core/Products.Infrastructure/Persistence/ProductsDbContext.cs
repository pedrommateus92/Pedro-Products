using Microsoft.EntityFrameworkCore;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence.Configurations;

namespace Products.Infrastructure.Persistence;

public sealed class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(ProductConfiguration.IdSequenceName)
            .StartsAt(Product.MinId)
            .IncrementsBy(1)
            .HasMin(Product.MinId)
            .HasMax(Product.MaxId)
            .IsCyclic(false);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductsDbContext).Assembly);
    }
}
