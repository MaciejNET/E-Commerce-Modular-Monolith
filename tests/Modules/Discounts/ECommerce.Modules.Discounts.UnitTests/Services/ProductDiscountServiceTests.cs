using ECommerce.Modules.Discounts.Core.DTO;
using ECommerce.Modules.Discounts.Core.Entities;
using ECommerce.Modules.Discounts.Core.Events;
using ECommerce.Modules.Discounts.Core.Exceptions;
using ECommerce.Modules.Discounts.Core.Repositories;
using ECommerce.Modules.Discounts.Core.Services;
using ECommerce.Modules.Discounts.Core.Validators;
using ECommerce.Modules.Discounts.UnitTests.Time;
using ECommerce.Shared.Abstractions.Kernel.Enums;
using ECommerce.Shared.Abstractions.Kernel.Types;
using ECommerce.Shared.Abstractions.Messaging;
using ECommerce.Shared.Abstractions.Time;
using FluentAssertions;
using Moq;

namespace ECommerce.Modules.Discounts.UnitTests.Services;

public class ProductDiscountServiceTests
{
    [Fact]
    public async Task AddAsync_GivenValidProductDiscount_ShouldBeAddedSuccessfully()
    {
        //Arrange
        var dto = new ProductDiscountDto
        {
            Id = Guid.NewGuid(),
            NewPrice = new Price(14.22M, Currency.PLN),
            ProductId = Guid.NewGuid(),
            ValidFrom = _clock.CurrentDate().AddHours(2),
            ValidTo = _clock.CurrentDate().AddHours(2).AddDays(2)
        };

        var productDiscountRepositoryMock = new Mock<IProductDiscountRepository>();
        var productRepositoryMock = new Mock<IProductRepository>();
        var messageBrokerMock = new Mock<IMessageBroker>();

        productRepositoryMock
            .Setup(x => x.GetAsync(dto.ProductId))
            .ReturnsAsync(new Product{Id = dto.ProductId});

        productDiscountRepositoryMock
            .Setup(x => x.CanAddDiscountForProductAsync(
                dto.ProductId,
                dto.ValidFrom,
                dto.ValidTo))
            .ReturnsAsync(true);

        var productDiscountService = new ProductDiscountService(productDiscountRepositoryMock.Object,
            productRepositoryMock.Object, messageBrokerMock.Object, _dateValidator);
        
        //Act
        await productDiscountService.AddAsync(dto);
        
        //Assert
        productRepositoryMock.Verify(x => x.GetAsync(dto.ProductId), Times.Once);
        productDiscountRepositoryMock.Verify(x => x.CanAddDiscountForProductAsync(
            dto.ProductId,
            dto.ValidFrom,
            dto.ValidTo), Times.Once);
        productDiscountRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ProductDiscount>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_GivenProductDoesNotExists_ShouldThrowProductNotFoundException()
    {
        //Arrange
        var dto = new ProductDiscountDto
        {
            Id = Guid.NewGuid(),
            NewPrice = new Price(14.22M, Currency.PLN),
            ProductId = Guid.NewGuid(),
            ValidFrom = _clock.CurrentDate().AddHours(2),
            ValidTo = _clock.CurrentDate().AddHours(2).AddDays(2)
        };

        var productDiscountRepositoryMock = new Mock<IProductDiscountRepository>();
        var productRepositoryMock = new Mock<IProductRepository>();
        var messageBrokerMock = new Mock<IMessageBroker>();

        productRepositoryMock
            .Setup(x => x.GetAsync(dto.ProductId))
            .ReturnsAsync((Product)null);

        var productDiscountService = new ProductDiscountService(productDiscountRepositoryMock.Object,
            productRepositoryMock.Object, messageBrokerMock.Object, _dateValidator);
        
        //Act
        var exception = await Record.ExceptionAsync(() => productDiscountService.AddAsync(dto));
        
        //Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ProductNotFoundException>();
    }
    
    [Fact]
    public async Task AddAsync_GivenInvalidDate_ShouldThrowInvalidDiscountDateException()
    {
        //Arrange
        var dto = new ProductDiscountDto
        {
            Id = Guid.NewGuid(),
            NewPrice = new Price(14.22M, Currency.PLN),
            ProductId = Guid.NewGuid(),
            ValidFrom = _clock.CurrentDate().AddHours(-2),
            ValidTo = _clock.CurrentDate().AddHours(2).AddDays(2)
        };

        var productDiscountRepositoryMock = new Mock<IProductDiscountRepository>();
        var productRepositoryMock = new Mock<IProductRepository>();
        var messageBrokerMock = new Mock<IMessageBroker>();

        productRepositoryMock
            .Setup(x => x.GetAsync(dto.ProductId))
            .ReturnsAsync(new Product {Id = dto.ProductId});

        var productDiscountService = new ProductDiscountService(productDiscountRepositoryMock.Object,
            productRepositoryMock.Object, messageBrokerMock.Object, _dateValidator);
        
        //Act
        var exception = await Record.ExceptionAsync(() => productDiscountService.AddAsync(dto));
        
        //Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidDiscountDateException>();
    }
    
    [Fact]
    public async Task AddAsync_GivenProductAlreadyHasDiscountInGivenDate_ShouldThrowProductAlreadyHasDiscountException()
    {
        //Arrange
        var dto = new ProductDiscountDto
        {
            Id = Guid.NewGuid(),
            NewPrice = new Price(14.22M, Currency.PLN),
            ProductId = Guid.NewGuid(),
            ValidFrom = _clock.CurrentDate().AddHours(2),
            ValidTo = _clock.CurrentDate().AddHours(2).AddDays(2)
        };

        var productDiscountRepositoryMock = new Mock<IProductDiscountRepository>();
        var productRepositoryMock = new Mock<IProductRepository>();
        var messageBrokerMock = new Mock<IMessageBroker>();

        productRepositoryMock
            .Setup(x => x.GetAsync(dto.ProductId))
            .ReturnsAsync(new Product {Id = dto.ProductId});
        
        productDiscountRepositoryMock
            .Setup(x => x.CanAddDiscountForProductAsync(
                dto.ProductId,
                dto.ValidFrom,
                dto.ValidTo))
            .ReturnsAsync(false);

        var productDiscountService = new ProductDiscountService(productDiscountRepositoryMock.Object,
            productRepositoryMock.Object, messageBrokerMock.Object, _dateValidator);
        
        //Act
        var exception = await Record.ExceptionAsync(() => productDiscountService.AddAsync(dto));
        
        //Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ProductAlreadyHasDiscountException>();
    }

    [Fact]
    public async Task DeleteAsync_GivenExistingDiscount_ShouldDeleteProductDiscountSuccessfully()
    {
        //Arrange
        var id = Guid.NewGuid();

        var productDiscountRepositoryMock = new Mock<IProductDiscountRepository>();
        var productRepositoryMock = new Mock<IProductRepository>();
        var messageBrokerMock = new Mock<IMessageBroker>();

        productDiscountRepositoryMock
            .Setup(x => x.GetAsync(id))
            .ReturnsAsync(new ProductDiscount());
        
        var productDiscountService = new ProductDiscountService(productDiscountRepositoryMock.Object,
            productRepositoryMock.Object, messageBrokerMock.Object, _dateValidator);
        
        //Act
        await productDiscountService.DeleteAsync(id);
        
        //Assert
        productDiscountRepositoryMock.Verify(x => x.GetAsync(id), Times.Once);
        productDiscountRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ProductDiscount>()), Times.Once);
        messageBrokerMock.Verify(x => x.PublishAsync(It.IsAny<ProductDiscountExpired>()), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_GivenNoExistingDiscount_ShouldThrowProductDiscountNotFoundException()
    {
        //Arrange
        var id = Guid.NewGuid();

        var productDiscountRepositoryMock = new Mock<IProductDiscountRepository>();
        var productRepositoryMock = new Mock<IProductRepository>();
        var messageBrokerMock = new Mock<IMessageBroker>();

        productDiscountRepositoryMock
            .Setup(x => x.GetAsync(id))
            .ReturnsAsync((ProductDiscount)null);
        
        var productDiscountService = new ProductDiscountService(productDiscountRepositoryMock.Object,
            productRepositoryMock.Object, messageBrokerMock.Object, _dateValidator);
        
        //Act
        var exception = await Record.ExceptionAsync(() => productDiscountService.DeleteAsync(id));
        
        //Assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ProductDiscountNotFoundException>();
    }

    private readonly IClock _clock;
    private readonly DiscountDateValidator _dateValidator;

    public ProductDiscountServiceTests()
    {
        _clock = new TestClock();
        _dateValidator = new DiscountDateValidator(_clock);
    }
}
