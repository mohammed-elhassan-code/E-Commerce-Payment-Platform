namespace ecommer.Application.DTOs;

public class ProductQueryParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    // 1. Pagination
    public int PageNumber {get; set;} = 1;
    public int PageSize
    {
        get => _pageSize;
        set =>_pageSize = ( value > MaxPageSize ) ? MaxPageSize : (value < 1 ? 10 : value);
    } 

    // 2. Searching (البحث بالاسم أو الوصف)
    public string? SearchTerm {get; set;}

    // 3. Filtering (التصفية حسب الفئة والسعر)
     public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // 4. Sorting (الترتيب: price_asc, price_desc, name_asc, newest)
    public string? SortBy {get; set;}


}