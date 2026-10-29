using System.Text;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Services;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --- 1. تسجيل خدمات البنية التحتية (Infrastructure Services) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IProductRepository  , ProductRepository>();
    builder.Services.AddScoped<ICartRepository , CartRepository>();

// --- 2. تسجيل خدمات طبقة الـ Application ---
    builder.Services.AddScoped<CartService>();

// --- 2. إعدادات نظام الأمان والتشفير (JWT Authentication) ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("Secret key is missing!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// --- 3. أدوات التوثيق لواجهة المستندات OpenAPI & Scalar ---
builder.Services.AddOpenApi();

var app = builder.Build();

// --- 4. إعداد خط الإنتاج البرمجي (HTTP Request Pipeline / Middlewares) ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // تفعيل واجهة Scalar الرائعة لـ .NET 10 🔥
}

app.UseHttpsRedirection();

app.UseAuthentication(); // التعرف على الهوية 👤
app.UseAuthorization();  // التحقق من الصلاحيات 🔐

// --- 5. مسار فحص أولي للتأكد من عمل السيرفر بنجاح ---
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", platform = "E-Commerce & Payment Platform V1" }));


// --- مسارات سلة التسوق المحمية والمؤمنة (Cart Endpoints) ---

// 1. مسار جلب عناصر السلة وحساب الإجمالي على السيرفر 🛡️
app.MapGet("/api/cart" , async (CartService cartService) =>
{
    var mockCustomerId = 1; // محاكاة لعميل مسجل دخول مؤقتًا 👤
    var cart = await cartService.GetCartAsync(mockCustomerId);
    return Results.Ok(cart);
});

// 2. مسار إضافة منتج إلى السلة (أو زيادة الكمية)
app.MapPost("/api/cart/items" , async ([FromBody] AddToCartRequest request ,[FromServices] CartService cartservice) =>
{
     // تفعيل نظام الـ Validation اليدوي للـ Minimal APIs لمنع مدخلات خاطئة 🛑
    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request);
    var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
    var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request, validationContext, validationResults, true);

    if (!isValid)
    {
        return Results.ValidationProblem(validationResults
            .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(v => v.ErrorMessage ?? "Invalid").ToArray()));
    }
    var mockCustomerId = 1;

    try
    {
        var result = await cartservice.AddToCartAsync(mockCustomerId , request);
       return result ? Results.Ok(new{message ="تمت اضافةالمنتج للسلة بنجاح"}) : Results.BadRequest("فشلت عملية الاضافة");

    }
    catch(KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
});


app.Run();
