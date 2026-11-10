using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class CategoryRepository
{
     private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

     public async Task<Category?> GetByIdAsync(int id) =>  await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
     public async Task<List<Category>> GetAllAsync() => await _context.Categories.ToListAsync();
     public void Add(Category category) => _context.Categories.Add(category);
     public void Update(Category category) => _context.Categories.Update(category); 
     public void Remove(Category category) => _context.Categories.Remove(category);
     public async Task<bool> SaveChangesAsync() =>  await _context.SaveChangesAsync() > 0;
}