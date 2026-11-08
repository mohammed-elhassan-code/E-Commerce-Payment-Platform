namespace Ecommerce.Application.DTOs;

public class OrderItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderResponse
{
    public int OrderId { get; set; }
    public decimal TaxAmount { get; set; } 
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}
