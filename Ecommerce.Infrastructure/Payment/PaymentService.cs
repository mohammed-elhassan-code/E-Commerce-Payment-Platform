using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Ecommerce.Infrastructure.Payment;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;

        // تهيئة المفتاح السري لبوابة Stripe
        StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
    }

    // 1. إنشاء جلسة دفع حقيقية وإرجاع رابط يعمل في المتصفح
    public async Task<PaymentResponse> CreateCheckoutSessionAsync(int customerId ,PaymentRequest request)
    {
        var order = await _context.Orders.FindAsync(request.OrderId);
        if (order is null)
            throw new KeyNotFoundException("الطلب المالي المحدد غير موجود!");

            // درع التحقق من الملكية (Ownership Guard): منع الدفع لطلب يخص عميلاً آخر
    if (order.CustomerId != customerId)
        throw new UnauthorizedAccessException("غير مصرح لك بالوصول إلى هذا الطلب أو إتمام الدفع له!");

        if (order.IsPaid || order.Status == OrderStatus.Confirmed)
            throw new InvalidOperationException("هذا الطلب مدفوع ومؤكد مسبقاً!");

        var options = new SessionCreateOptions // انشاء كائن من الكلاس stripe.checkout.sessionCreateOptions
        {
          Mode = "payment",
          SuccessUrl = $"http://localhost:5123/payment-success?orderId={order.Id}", // في المشاريع الحقيقية مسؤولية وضع هذا الرابط هو frontend لتوجيهه لصفحة معينة في الموقع مثل https://mystore.com 
          CancelUrl = $"http://localhost:5123/payment-cancel?orderId={order.Id}", // في المشاريع الحقيقية مسؤولية وضع هذا الرابط هو frontend لتوجيهه لصفحة معينة في الموقع مثل https://mystore.com
          ClientReferenceId = order.Id.ToString(),
          ExpiresAt = DateTime.UtcNow.AddMinutes(30),
          LineItems = new List<SessionLineItemOptions>
            {
              new SessionLineItemOptions
               {
                 PriceData = new SessionLineItemPriceDataOptions
                  {
                   Currency = "usd",
                   UnitAmount = (long)(order.TotalAmount * 100), // نقوم بضرب الاجمالي في 100 لان stripe تتعامل بالسنت
                   ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                       Name = $"Order #{order.Id} - E-Commerce Platform"
                    }
                  },
                Quantity = 1
                }
            }
        };

        
        var service = new SessionService(); // انشاء كائن من الكلاس Stripe.checkout.sessionService
        Session session = await service.CreateAsync(options);

        return new PaymentResponse { CheckoutUrl = session.Url };
    }

    // 2. معالجة إشعارات Stripe الحقيقية والتحقق من التوقيع المشفر
    public async Task<bool> ProcessStripeWebhookAsync(string rawJson, string stripeSignature)
    {
        var webhookSecret = _configuration["StripeSettings:WebhookSecret"]
            ?? throw new InvalidOperationException("WebhookSecret is missing in appsettings.json!");

        Event stripeEvent;
        
        try
        {
            // التحقق التشفيري من صحة الإشارة وأنها قادمة فعلاً من خوادم Stripe
            stripeEvent = EventUtility.ConstructEvent(rawJson, stripeSignature, webhookSecret);
        }
        catch (StripeException ex)
        {
            // التوقيع مزور أو غير صالح
            throw new UnauthorizedAccessException($"فشل التحقق من توقيع Stripe Webhook: {ex.Message}");
        }

        // استخراج كائن الجلسة (Session) من الحدث
        var session = stripeEvent.Data.Object as Session;
        if (session is null || string.IsNullOrEmpty(session.ClientReferenceId))
            return false;

        if (!int.TryParse(session.ClientReferenceId, out int orderId))
            return false;

        // جلب الطلب مع عناصره من قاعدة البيانات
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return false;

        // -------------------------------------------------------------
        // السيناريو الأول: نجاح عملية الدفع (checkout.session.completed)
        // -------------------------------------------------------------
        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
        {
            // درع منع التكرار (Idempotency Guard)
            if (order.IsPaid || order.Status == OrderStatus.Confirmed)
                return true;

            order.IsPaid = true;
            order.Status = OrderStatus.Confirmed;
            order.PaymentTransactionId = session.PaymentIntentId ?? session.Id;

            _context.Orders.Update(order);
            return await _context.SaveChangesAsync() > 0;
        }

        // -------------------------------------------------------------
        // السيناريو الثاني: انتهاء صلاحية جلسة الدفع دون سداد (checkout.session.expired)
        // -------------------------------------------------------------
        if (stripeEvent.Type == EventTypes.CheckoutSessionExpired)
        {
            // نتأكد أن الطلب لم يُدفع ولم يُلغَ مسبقاً لمنع إعادة المخزون مرتين
            if (order.IsPaid || order.Status == OrderStatus.Cancelled)
                return true;

            order.Status = OrderStatus.Cancelled;
            _context.Orders.Update(order);

            // إعادة الكميات المحجوزة إلى مخزن المنتجات تلقائياً
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    _context.Products.Update(product);
                }
            }

            return await _context.SaveChangesAsync() > 0;
        }

        return true;
    }
}
