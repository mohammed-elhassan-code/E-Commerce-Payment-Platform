using System.Security.Cryptography;

namespace Ecommerce.Domain.Entities;

public enum OrederStatus
{
    Pending,
    confirmed,
    Shipped,
    Delivered,
    Cancelled
}

public class Order
{
    public int Id {get; set;}
    public int CustomerId {get; set;}
    public Customer customer {get; set;} = null!;
    public decimal TotalAmount {get; set;} // إجمالي المبلغ المطلوب دفعه
    public OrederStatus Status {get; set;} = OrederStatus.Pending; // حالة الطلب الابتدائية
    public bool IsPaid {get; set;} = false;    // هل تم الدفع بنجاح؟
    public string? PaymentTransactionId {get; set;} // رقم المعاملة القادم من بوابة الدفع

    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    // علاقة واحد لمتعدد: الطلب يحتوي على تفاصيل ومنتجات متعددة
    public ICollection<OrderItem> OrderItems =new List<OrderItem>();
}