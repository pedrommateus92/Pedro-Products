using FluentValidation;
using Products.Application.Products.Dtos;
using Products.Domain.Entities;

namespace Products.Application.Products.Validators;

internal sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(r => r.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(r => r.Stock).GreaterThanOrEqualTo(0);
        RuleFor(r => r.RowVersion)
            .NotEmpty()
            .Must(BeBase64).WithMessage("'Row Version' must be the value returned when the product was read.");
    }

    private static bool BeBase64(string value) =>
        Convert.TryFromBase64String(value, new byte[value.Length], out var bytesWritten) && bytesWritten > 0;
}
