using Microsoft.AspNetCore.Mvc;
using Products.Application.Products;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await productService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(id, request, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await productService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> DecrementStock(int id, int quantity, CancellationToken cancellationToken)
    {
        var product = await productService.DecrementStockAsync(id, quantity, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> AddToStock(int id, int quantity, CancellationToken cancellationToken)
    {
        var product = await productService.AddToStockAsync(id, quantity, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("search")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> SearchByName(
        [FromQuery] string name,
        CancellationToken cancellationToken) =>
        Ok(await productService.SearchByNameAsync(name, cancellationToken));

    [HttpGet("stock-level")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetByStockLevel(
        [FromQuery] int min,
        [FromQuery] int max,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetByStockLevelAsync(min, max, cancellationToken));
}
