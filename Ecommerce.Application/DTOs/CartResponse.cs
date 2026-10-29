namespace Ecommerce.Application.DTOs;

public class CartItemDto
{
    public int productId {get; set;}
    public string ProductName {get; set;}= string.Empty;
    public decimal price {get; set;}
    public int Quantity {get; set;}
    public decimal TotalPrice => price * Quantity;
}

public class CartResponse
{
    public List<CartItemDto> items {get; set;}= new();
    public decimal GrandTotal {get; set;} // إجمالي السلة بالكامل بعد الحساب على السيرفر
}