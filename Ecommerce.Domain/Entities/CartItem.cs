namespace Ecommerce.Domain.Entities;

public class CartItem
{
    public int Id {get; set;}
    public int ProductId {get; set;}
    public Product product {get; set;}= null!; // ربط العنصر بالمنتج لقراءة سعره الحقيقي من الـ DB
    public int Quantity {get; set;} // الكمية المطلوبة
    public int CustomerId {get; set;} // العميل صاحب السل
}