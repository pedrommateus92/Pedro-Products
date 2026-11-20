using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Products.Domain.Entities;

namespace Products.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public const string IdSequenceName = "ProductIds";

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Id_SixDigits", $"[Id] BETWEEN {Product.MinId} AND {Product.MaxId}");
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            table.HasCheckConstraint("CK_Products_Stock_NonNegative", "[Stock] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).UseSequence(IdSequenceName);

        builder.Property(p => p.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => p.Name);
    }
}
