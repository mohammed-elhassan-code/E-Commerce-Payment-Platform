namespace Ecommerce.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // علاقة واحد لمتعدد: الفئة تحتوي على مجموعة منتجات
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
