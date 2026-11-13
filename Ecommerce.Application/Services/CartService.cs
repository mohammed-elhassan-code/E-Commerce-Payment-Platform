using Ecommerce.Domain.Entities;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using System.Data.Common;

namespace Ecommerce.Application.Services;

public class CartService
{
    private readonly ICartRepository _cartrepository;
    private readonly IProductRepository _productRepository;
    private readonly ICouponRepository _couponRepository;

    public CartService(
        ICartRepository cartRepository , 
        IProductRepository productRepository ,
        ICouponRepository couponRepository)
    {
        _cartrepository = cartRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
    }

    // 1. جلب السلة وحساب الإجمالي مع دعم تطبيق الكوبونات 🎫
    public async Task<CartResponse> GetCartAsync(int customerId , string? couponCode = null)
    {
        var items = await _cartrepository.GetByCustomerIdAsync(customerId);

        var response = new CartResponse();

        foreach (var item in items)
        {
            response.items.Add(new CartItemDto
            {
                productId = item.ProductId,
                ProductName = item.Product.Name,
                price = item.Product.Price, // جلب السعر الأصلي الحقيقي من قاعدة البيانات
                Quantity = item.Quantity
            });
        }
          // حساب الإجمالي الأساسي قبل الخصم
        response.GrandTotal = response.items.Sum(x => x.TotalPrice);

          // إذا أرسل العميل كود كوبون، نتحقق منه برمجياً على السيرفر
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(couponCode);

            // درع فحص الكوبونات: التحقق من الفعالية وتاريخ الصلاحية لعام 2026 ⏳
            if( coupon != null && coupon.IsActive && coupon.ExpiryDate >= DateTime.UtcNow)
            {
                decimal discountAmount = response.GrandTotal * (coupon.DiscountPercentage/100);
                response.GrandTotal -= discountAmount ; // تطبيق الخصم بأمان
            }
            else
            {   
                // في الشركات، لا نوقف التطبيق بل نرجع تنبيهاً بأن الكوبون غير صالح
                throw new ArgumentException("كوبون الخصم منتهي الصلاحية أو غير فعال!");
            }

            
        }
        
         return response;   
        
    }

    // 2. إضافة منتج للسلة أو تحديث الكمية إذا كان موجودًا مسبقًا
    // درع المخزون (Inventory Guard): مع منع إضافة كميات أكبر من المستودع 🛡️📦
    public async Task<bool> AddToCartAsync(int customerId , AddToCartRequest request)
    {   
        // التحقق من أن المنتج موجود فعليًا في مخازن الشركة
        var product = await _productRepository.GetByIdAsync(request.ProductId);

        if(product is null)
        {
            throw new KeyNotFoundException("المنتج المطلوب غير موجود في النظام!");
        }
         // فحص هل المنتج مضاف مسبقًا لسلة هذا العميل؟
        var existingItem = await _cartrepository.GetItemAsync(customerId , request.ProductId);
        int targetQuantity = request.Quantity + (existingItem?.Quantity ?? 0);


        // الفحص الصارم للمخزون: منع بيع كميات غير متاحة 🛑
        if(targetQuantity > product.Stock)
        {
           throw new InvalidOperationException($"عذرًا، الكمية المطلوبة غير متوفرة! المخزون المتاح الحالي هو ({product.Stock}) قطع فقط.");
        }

        if(existingItem != null)
        {
            // تحديث الكمية فقط
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            // إنشاء عنصر جديد تمامًا في السلة
            var newItem = new CartItem
            {
                CustomerId = customerId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };
            _cartrepository.Add(newItem);
        }

        return await _cartrepository.SaveChangesAsync();

    }

           
}