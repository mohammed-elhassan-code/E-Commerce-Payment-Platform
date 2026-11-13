using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Services;
using Ecommerce.Domain.Entities;
using Moq;
using Xunit;

namespace Ecommerce.Tests;

public class CartServiceTests
{
    [Fact]
    public async Task GetCartAsync_WithValidCoupon_CalculatesDiscountedTotalCorrectly()
    {
        // 1. Arrange (تجهيز البيانات الوهمية للمحاكاة دون لمس قاعدة البيانات الحقيقية)
        var mockCartRepo = new Mock<ICartRepository>();
        var mockProductRepo = new Mock<IProductRepository>();
        var mockCouponRepo = new Mock<ICouponRepository>();

        int customerId = 1;
        var fakeCartItems = new List<CartItem>
        {
            new CartItem { ProductId = 1, Quantity = 2, Product = new Product { Id = 1, Name = "Laptop", Price = 1000m } }
        };

        var fakeCoupon = new Coupon
        {
            Code = "SAVE20",
            DiscountPercentage = 20m,
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddDays(10)
        };

        mockCartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(fakeCartItems);
        mockCouponRepo.Setup(r => r.GetByCodeAsync("SAVE20")).ReturnsAsync(fakeCoupon);

        var cartService = new CartService(mockCartRepo.Object, mockProductRepo.Object, mockCouponRepo.Object);

        // 2. Act (تشغيل الدالة الحقيقية المراد فحصها)
        var result = await cartService.GetCartAsync(customerId, "SAVE20");

        // 3. Assert (التحقق الرياضي الصارم من النتيجة: 2000 - 20% = 1600)
        Assert.Equal(1600m, result.GrandTotal);
    }
}
