using Ecommerce.Domain.Entities;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using System.Data.Common;

namespace Ecommerce.Application.Services;

public class CartService
{
    private readonly ICartRepository _cartrepository;
    private readonly IProductRepository _productRepository;

    public CartService(ICartRepository cartRepository , IProductRepository productRepository)
    {
        _cartrepository = cartRepository;
        _productRepository = productRepository;
    }

    // 1. جلب السلة وحساب الإجمالي آليًا على السيرفر 🛡️
    public async Task<CartResponse> GetCartAsync(int customerId)
    {
        var items = await _cartrepository.GetByCustomerIdAsync(customerId);

        var response = new CartResponse();

        foreach (var item in items)
        {
            response.items.Add(new CartItemDto
            {
                productId = item.ProductId,
                ProductName = item.product.Name,
                price = item.product.Price, // جلب السعر الأصلي الحقيقي من قاعدة البيانات
                Quantity = item.Quantity
            });
        }
         // حساب الإجمالي الكلي على السيرفر حصريًا لمنع تلاعب الـ Frontend
        response.GrandTotal = response.items.Sum(x => x.TotalPrice);
        
         return response;   
        
    }

    // 2. إضافة منتج للسلة أو تحديث الكمية إذا كان موجودًا مسبقًا
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