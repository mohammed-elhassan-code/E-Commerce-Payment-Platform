using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IOrderRepository
{
    void Add(Order order);
    Task<Order?> GetByIdAsync(int id);
    Task<IDbContextTransactionWrapper> BeginTransactionAsync(); // لتأمين المعاملات المالية 🌟
    Task<bool> SaveChangesAsync();
}