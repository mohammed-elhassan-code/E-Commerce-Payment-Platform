using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Services;

public class CategoryService
{
private readonly ICategoryRepository _categoryRepository;

public CategoryService(ICategoryRepository categoryRepository)
{
        _categoryRepository = categoryRepository;
       
}

  public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if(category is null) return null;

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name
            
        };
    }

 public async Task<List<CategoryResponse>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(c => new CategoryResponse { Id=c.Id, Name = c.Name}).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        var category = new Category { Name = request.Name};

        _categoryRepository.Add(category);
        await _categoryRepository.SaveChangesAsync();

        return new CategoryResponse {  Name = category.Name };
    }

    public async Task<CategoryResponse?> UpdateAsync(int id , UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if(category is null)
        {
            return null;
        }

        category.Name = request.Name;
        
        await _categoryRepository.SaveChangesAsync();

        return new CategoryResponse {Id = category.Id , Name = category.Name};
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

         if(category is  null)
        {
            throw new KeyNotFoundException("القسم أو الفئة المطلوبة غير موجودة في النظام!");
        }
        

       if( category.Products != null && category.Products.Any() )
       {
            throw new InvalidOperationException("عذرًا، لا يمكن حذف هذه الفئة لأنها تحتوي على منتجات مرتبطة بها! قم بنقل أو حذف المنتجات أولاً.");
       }

        _categoryRepository.Remove(category);
         return await _categoryRepository.SaveChangesAsync();
         
    }

}