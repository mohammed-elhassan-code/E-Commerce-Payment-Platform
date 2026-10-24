using System.Text;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. تسجيل خدمات البنية التحتية (Infrastructure Services) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.Run();
