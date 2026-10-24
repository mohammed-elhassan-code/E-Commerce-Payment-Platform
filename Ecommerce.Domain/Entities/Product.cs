namespace Ecommerce.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // ربط المفتاح الخارجي بالفئة
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
