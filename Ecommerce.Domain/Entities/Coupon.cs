namespace Ecommerce.Domain.Entities;

public class Coupon
{
  public int Id {get; set;}
  public string Code {get; set;} = string.Empty;
  public decimal DiscountPercentage {get; set;}
  public DateTime ExpiryDate {get; set;}
  public bool IsActive {get; set;} = true;
}