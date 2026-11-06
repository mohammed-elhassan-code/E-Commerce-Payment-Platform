using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICouponRepository _couponRepository; 

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICouponRepository couponRepository)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
    }

    // إنشاء الطلب وحجز المخزون وتطبيق الكوبونات بأمان عبر الـ Transaction 💳🔥
    public async Task<OrderResponse> CreateOrderAsync(int customerId, string? couponCode = null)
    {
        // 1. جلب عناصر السلة للتأكد من وجود منتجات للشراء
        var cartItems = await _cartRepository.GetByCustomerIdAsync(customerId);
        if (!cartItems.Any())
        {
            throw new InvalidOperationException("لا يمكن إنشاء طلب لأن سلة التسوق فارغة!");
        }

        // 2. إطلاق درع الحماية المالي (بدء المعاملة الصارمة في قاعدة البيانات) 🛡️
        using var transaction = await _orderRepository.BeginTransactionAsync();

        try
        {
            var order = new Order
            {
                CustomerId = customerId,
                
            };

            decimal totalAmount = 0;

            // 3. قراءة كل عنصر وحجز المخزون وتسجيل السعر التاريخي
            foreach (var cartItem in cartItems)
            {
                var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                if (product is null)
                {
                    throw new KeyNotFoundException($"المنتج رقم ({cartItem.ProductId}) لم يعد متوفرًا في النظام!");
                }

                // درع المخزون: منع بيع كميات غير متوفرة تحت الضغط العالي 🛑
                if (product.Stock < cartItem.Quantity)
                {
                    throw new InvalidOperationException($"عذرًا، فشل إتمام الطلب! المنتج '{product.Name}' نفد أو كميته أقل من المطلوبة. المخزون الحالي: {product.Stock}");
                }

                // خصم الكمية المشتراة من مخزن المنتجات فورًا برمجياً 📦
                product.Stock -= cartItem.Quantity;
                _productRepository.Update(product);

                // إنشاء عنصر الفاتورة التاريخي بالسعر الحالي وقت الشراء
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Price = product.Price  // تسجيل السعر بشكل ثابت لحمايته من تغييرات الأسعار لاحقًا 🌟
                };

                order.OrderItems.Add(orderItem);
                totalAmount += orderItem.Quantity * orderItem.Price;
            }

            // --- 🌟 تفعيل كود الخصم واستخدامه برمجياً داخل الفاتورة 🌟 ---
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _couponRepository.GetByCodeAsync(couponCode);
                
                // درع فحص الكوبونات: التحقق من الفعالية وصلاحية التاريخ لعام 2026 ⏳
                if (coupon != null && coupon.IsActive && coupon.ExpiryDate >= DateTime.UtcNow)
                {
                    decimal discountAmount = totalAmount * (coupon.DiscountPercentage / 100);
                    totalAmount -= discountAmount; // خصم القيمة من الفاتورة النهائية للطلب قبل الحفظ بأمان
                }
                else
                {
                    throw new ArgumentException("كوبون الخصم المستخدم منتهي الصلاحية أو غير فعال!");
                }
            }

            order.TotalAmount = totalAmount;

            // 4. حفظ الطلب وتعديلات المخزن في قاعدة البيانات
            _orderRepository.Add(order);
            await _orderRepository.SaveChangesAsync();

            // 5. تصفير سلة تسوق العميل بعد حجز المنتجات بنجاح
            foreach (var cartItem in cartItems)
            {
                _cartRepository.Remove(cartItem);
            }
            await _cartRepository.SaveChangesAsync();

            // 6. تثبيت ونجاح المعاملة ماليًا وقبول التغييرات في السيرفر بشكل نهائي 🎯
            await transaction.CommitAsync();

            // صياغة الرد النهائي المنسق
            return new OrderResponse
            {
                OrderId = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                IsPaid = order.IsPaid,
                Items = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "منتج غير معروف",
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };
        }
        catch (Exception)
        {
            // الكود الحامي: التراجع الفوري عن كل التعديلات وإعادة المخزن والسلة لأصلها إذا فشل أي سطر! 💥🔄
            await transaction.RollBackAsync();
            throw; // إعادة رمي الخطأ لتلتقطه شبكة الأمان العالمية أو الـ Endpoint
        }
    }
}
