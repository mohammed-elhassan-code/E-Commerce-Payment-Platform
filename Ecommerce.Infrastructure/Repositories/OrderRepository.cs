using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

        public void Add(Order order) => _context.Orders.Add(order);

        public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IDbContextTransactionWrapper> BeginTransactionAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new DbContextTransactionWrapper(transaction); // إطلاق درع الحماية المالي 🛡️
    }

    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

}