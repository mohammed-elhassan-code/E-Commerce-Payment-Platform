using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface ICartRepository
{
    Task<List<CartItem>> GetByCustomerIdAsync(int CustomerId);
    Task<CartItem?> GetItemAsync(int CustomerId, int ProductId);
    void Add(CartItem item);
    void Remove(CartItem item);
    Task<bool> SaveChangesAsync();
}