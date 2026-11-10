using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class ProductRepository:IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

   public async Task<Product?> GetByIdAsync(int id) =>  await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
   public async Task<List<Product>> GetAllAsync() => await _context.Products.ToListAsync();
    public void Add(Product product) => _context.Products.Add(product);
    public void Update(Product product) => _context.Products.Update(product); 
    public void Remove(Product product) => _context.Products.Remove(product);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
    
        
}