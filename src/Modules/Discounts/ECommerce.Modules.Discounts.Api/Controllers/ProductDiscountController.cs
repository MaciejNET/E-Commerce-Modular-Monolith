using ECommerce.Modules.Discounts.Core.DTO;
using ECommerce.Modules.Discounts.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Modules.Discounts.Api.Controllers;

internal class ProductDiscountController : BaseController
{
    private readonly IProductDiscountService _productDiscountService;

    public ProductDiscountController(IProductDiscountService productDiscountService)
    {
        _productDiscountService = productDiscountService;
    }

    [HttpPost]
    public async Task<ActionResult> Add(ProductDiscountDto dto)
    {
        await _productDiscountService.AddAsync(dto);
        AddResourceIdHeader(dto.Id);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Deleted(Guid id)
    {
        await _productDiscountService.DeleteAsync(id);

        return NoContent();
    }
}