using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Application.DTOs;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ecommer.Application.DTOs;

namespace Ecommerce.Infrastructure.Repositories;

public class ProductRepository:IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

   public async Task<Product?> GetByIdAsync(int id) =>  await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
    public void Add(Product product) => _context.Products.Add(product);
    public void Update(Product product) => _context.Products.Update(product); 
    public void Remove(Product product) => _context.Products.Remove(product);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetFilteredAsync(ProductQueryParameters parameters)
{
    // بدء الاستعلام بدون تتبع التغييرات لرفع الأداء
    var query = _context.Products
        .Include(p => p.Category)
        .AsNoTracking()
        .AsQueryable();

    // 1. البحث النصي (Searching)
    if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
    {
        var term = parameters.SearchTerm.Trim().ToLower();
        query = query.Where(p => p.Name.ToLower().Contains(term));
    }

    // 2. التصفية (Filtering)
    if (parameters.CategoryId.HasValue)
        query = query.Where(p => p.CategoryId == parameters.CategoryId.Value);

    if (parameters.MinPrice.HasValue)
        query = query.Where(p => p.Price >= parameters.MinPrice.Value);

    if (parameters.MaxPrice.HasValue)
        query = query.Where(p => p.Price <= parameters.MaxPrice.Value);

    // حساب إجمالي العناصر المطابقة قبل تقسيم الصفحات
    int totalCount = await query.CountAsync();

    // 3. الترتيب (Sorting)
    query = parameters.SortBy?.ToLower() switch
    {
        "price_asc" => query.OrderBy(p => p.Price),
        "price_desc" => query.OrderByDescending(p => p.Price),
        "name_desc" => query.OrderByDescending(p => p.Name),
        "name_asc" => query.OrderBy(p => p.Name),
        _ => query.OrderByDescending(p => p.Id) // الافتراضي: الأحدث أولاً
    };

    // 4. تقسيم الصفحات (Pagination)
    var items = await query
        .Skip((parameters.PageNumber - 1) * parameters.PageSize)
        .Take(parameters.PageSize)
        .ToListAsync();

    return (items, totalCount);
}

    
        
}