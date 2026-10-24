namespace Ecommerce.Application.DTOs;

public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ProductResponse> Products { get; set; } = new();
}
