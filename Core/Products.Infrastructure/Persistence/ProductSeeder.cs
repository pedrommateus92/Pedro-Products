using Microsoft.EntityFrameworkCore;
using Products.Domain.Entities;

namespace Products.Infrastructure.Persistence;

internal static class ProductSeeder
{
    private static Product[] CreateProducts() =>
    [
        new() { Name = "Mechanical Keyboard", Description = "Tenkeyless keyboard with brown switches", Price = 89.90m, Stock = 25 },
        new() { Name = "Wireless Mouse", Description = "Ergonomic mouse with USB-C charging", Price = 39.50m, Stock = 60 },
        new() { Name = "27\" Monitor", Description = "QHD IPS monitor, 144 Hz", Price = 279.00m, Stock = 8 },
        new() { Name = "USB-C Hub", Description = "7-in-1 hub with HDMI and card reader", Price = 34.99m, Stock = 0 },
        new() { Name = "Noise-Cancelling Headphones", Description = null, Price = 199.00m, Stock = 15 },
    ];

    public static void Seed(DbContext context)
    {
        var products = context.Set<Product>();
        if (products.Any())
        {
            return;
        }

        products.AddRange(CreateProducts());
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var products = context.Set<Product>();
        if (await products.AnyAsync(cancellationToken))
        {
            return;
        }

        products.AddRange(CreateProducts());
        await context.SaveChangesAsync(cancellationToken);
    }
}
