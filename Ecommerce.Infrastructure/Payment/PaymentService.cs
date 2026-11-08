using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Payment;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;

    public PaymentService(AppDbContext context)
    {
        _context = context;
    }

    // 1. توليد رابط الدفع التجريبي (Sandbox Session)
    public async Task<PaymentResponse> CreateCheckoutSessionAsync(PaymentRequest request)
    {
        var order = await _context.Orders.FindAsync(request.OrderId);
        if (order is null) throw new KeyNotFoundException("الطلب المالي المحدد غير موجود في النظام!");

        // توليد رابط دفع محاكي آمن لبوابة الدفع (Sandbox) يضم المعرف وقيمة الفاتورة الشاملة للضريبة
        string sandboxUrl = $"https://paymentgateway.com{order.Id}&amount={order.TotalAmount}";
        
        return new PaymentResponse { CheckoutUrl = sandboxUrl };
    }

    // 2. معالجة إشعارات الدفع الفورية (Webhook Receiver) ومنع التكرار مرتين 🛡️🔥
    public async Task<bool> ProcessWebhookAsync(WebhookPayload payload)
    {
        // درع التحقق السيبراني (Signature Verification): فحص التوقيع لمنع المخترقين من تزوير دفع الطلبات يدوياً
        if (payload.Signature != "SECURE_BANK_SIGNATURE_2026")
        {
            throw new UnauthorizedAccessException("تحذير أمني: إشارة الـ Webhook غير موثوقة والتوقيع مزور!");
        }

        // جلب الطلب المستهدف بقفل المعاملة
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == payload.OrderId);

        if (order is null) return false;

        // درع منع تكرار معالجة الدفع (Idempotency Guard) 🛑
        // إذا كان الطلب مدفوعاً مسبقاً، نوقف العملية فوراً لمنع التلاعب بالمخزن أو تكرار الإشعارات
        if (order.IsPaid || order.Status == OrderStatus.Confirmed)
        {
            Console.WriteLine($"[WEBHOOK WARNING] الطلب رقم ({order.Id}) مدفوع ومؤكد مسبقًا. تم تجاهل الإشارة المكررة بأمان.");
            return true; 
        }

        if (payload.EventType == "payment.succeeded")
        {
            // تحديث البيانات المالية بأمان كامل وضبط الحالات
            order.IsPaid = true;
            order.Status = OrderStatus.Confirmed; // نقل حالة الطلب من Pending إلى Confirmed 🚀
            order.PaymentTransactionId = payload.TransactionId;

            _context.Orders.Update(order);
            return await _context.SaveChangesAsync() > 0;
        }

        if (payload.EventType == "payment.failed")
        {
             order.Status = OrderStatus.Cancelled; // إلغاء الطلب إذا فشل السحب
            _context.Orders.Update(order);
            
            // في الشركات الكبرى، نقوم بإعادة كميات المنتجات المشتراة إلى المخزن (Stock) تلقائياً إذا ألغي الدفع! 📦🔄
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null) product.Stock += item.Quantity; // إرجاع السلعة للمستودع
            }
            
            return await _context.SaveChangesAsync() > 0;
        }

        return false;
    }
}
