using FluentValidation;
using Products.Application.Products.Dtos;
using Products.Domain.Entities;

namespace Products.Application.Products.Validators;

internal sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(r => r.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(r => r.Stock).GreaterThanOrEqualTo(0);
    }
}
