using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id);
    Task<List<Category>> GetAllAsync(); 
    void Add(Category category);         
    void Update(Category category);
    void Remove(Category category);  
    Task<bool> SaveChangesAsync();     
}