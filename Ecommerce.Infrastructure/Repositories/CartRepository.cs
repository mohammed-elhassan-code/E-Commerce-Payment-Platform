using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CartItem>> GetByCustomerIdAsync(int customerId)
    {
        // نستخدم Include لجلب بيانات المنتج وسعره الحقيقي برمجياً من الـ DB 🛡️
        return await _context.CartItems
        .Include(ci => ci.Product)
        .Where(ci => ci.CustomerId == customerId)
        .ToListAsync();
    }

    public async Task<CartItem?> GetItemAsync(int CustomerId , int productId)
    {
        return await _context.CartItems.FirstOrDefaultAsync(ci => ci.CustomerId == CustomerId && ci.ProductId == productId);
    }

    public void Add(CartItem Item)
    {
        _context.CartItems.Add(Item);
    }

    public void Remove(CartItem item)
    {
        _context.CartItems.Remove(item);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }


}