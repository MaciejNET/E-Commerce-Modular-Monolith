using ECommerce.Modules.Orders.Application.Carts.Exceptions;
using ECommerce.Modules.Orders.Domain.Carts.Repositories;
using ECommerce.Shared.Abstractions.Commands;
using ECommerce.Shared.Abstractions.Kernel.Types;

namespace ECommerce.Modules.Orders.Application.Carts.Commands.Handlers;

public sealed class RemoveProductFromCartHandler : ICommandHandler<RemoveProductFromCart>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICartItemRepository _cartItemRepository;

    public RemoveProductFromCartHandler(ICartRepository cartRepository, IProductRepository productRepository, ICartItemRepository cartItemRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _cartItemRepository = cartItemRepository;
    }

    public async Task HandleAsync(RemoveProductFromCart command)
    {
        var cart = await _cartRepository.GetAsync(new UserId(command.UserId));

        if (cart is null)
        {
            throw new CartNotFoundException(command.UserId);
        }
        
        var product = await _productRepository.GetAsync(command.ProductId);

        if (product is null)
        {
            throw new ProductNotFoundException(command.ProductId);
        }

        var cartItem = cart.Items.SingleOrDefault(x => x.Product == product);
        
        cart.RemoveItem(product);
        await _cartRepository.UpdateAsync(cart);
        await _cartItemRepository.DeleteAsync(cartItem);
    }
}