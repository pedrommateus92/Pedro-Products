using Products.Application.Products.Dtos;
using Products.Application.Products.Validators;

namespace Products.UnitTests.Products;

public sealed class ProductValidatorTests
{
    private static readonly string ValidRowVersion = Convert.ToBase64String([0, 0, 0, 0, 0, 0, 7, 209]);

    private readonly CreateProductRequestValidator createValidator = new();
    private readonly UpdateProductRequestValidator updateValidator = new();

    [Fact]
    public void Create_ValidRequest_HasNoErrors()
    {
        var result = createValidator.Validate(new CreateProductRequest("Keyboard", null, 0m, 0));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    public void Create_BlankName_IsInvalid(string name, string expectedProperty)
    {
        var result = createValidator.Validate(new CreateProductRequest(name, null, 1m, 1));

        Assert.Equal([expectedProperty], result.Errors.Select(e => e.PropertyName));
    }

    [Fact]
    public void Create_TooLongNameAndDescription_AreInvalid()
    {
        var request = new CreateProductRequest(new string('n', 201), new string('d', 1001), 1m, 1);

        var result = createValidator.Validate(request);

        Assert.Equal(["Description", "Name"], result.Errors.Select(e => e.PropertyName).Order());
    }

    [Fact]
    public void Create_MaximumLengths_AreValid()
    {
        var request = new CreateProductRequest(new string('n', 200), new string('d', 1000), 1m, 1);

        var result = createValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.001)]
    public void Create_InvalidPrice_IsInvalid(double price)
    {
        var result = createValidator.Validate(new CreateProductRequest("Keyboard", null, (decimal)price, 1));

        Assert.Equal(["Price"], result.Errors.Select(e => e.PropertyName));
    }

    [Fact]
    public void Create_NegativeStock_IsInvalid()
    {
        var result = createValidator.Validate(new CreateProductRequest("Keyboard", null, 1m, -1));

        Assert.Equal(["Stock"], result.Errors.Select(e => e.PropertyName));
    }

    [Fact]
    public void Update_ValidRequest_HasNoErrors()
    {
        var result = updateValidator.Validate(new UpdateProductRequest("Keyboard", "Mechanical", 89.90m, 3, ValidRowVersion));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    public void Update_MissingOrMalformedRowVersion_IsInvalid(string rowVersion)
    {
        var result = updateValidator.Validate(new UpdateProductRequest("Keyboard", null, 1m, 1, rowVersion));

        Assert.Equal(["RowVersion"], result.Errors.Select(e => e.PropertyName).Distinct());
    }
}
