using System.Text;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Application.Services;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.dto;
using Ecommerce.Infrastructure.Payment;
using System.ComponentModel.DataAnnotations;
using Ecommerce.Domain.Entities;


var builder = WebApplication.CreateBuilder(args);

//   تسجيل خدمات البنية التحتية (Infrastructure Services) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IProductRepository  , ProductRepository>();
    builder.Services.AddScoped<ICartRepository , CartRepository>();
    builder.Services.AddScoped<ICouponRepository, CouponRepository>();
    builder.Services.AddScoped<IIdentityService, IdentityService>();
    builder.Services.AddScoped<IOrderRepository , OrderRepository>();
    builder.Services.AddScoped<IPaymentService , PaymentService>();
    builder.Services.AddScoped<ICategoryRepository , CategoryRepository>();


//  تسجيل خدمات طبقة الـ Application ---
    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<CategoryService>();
    builder.Services.AddScoped<CartService>();
    builder.Services.AddScoped<OrderService>();

//  إعدادات نظام الأمان والتشفير (JWT Authentication) ---
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

//  أدوات التوثيق لواجهة المستندات OpenAPI & Scalar ---
builder.Services.AddOpenApi();

// تفعيل الـ Distributed Cache في الذاكرة المحلية الفائقة السرعة ⚡
builder.Services.AddDistributedMemoryCache();

builder.Services.AddExceptionHandler<Ecommerce.Api.Middlewares.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

//اعداد سياسة CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowStoreFrontend", policy =>
    {
        policy.WithOrigins("https://my-secure-store.com") // رابط واجهة المتجر الحقيقي المعتمد للشركة 🌐
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();

// تفعيل شبكة الأمان للأخطاء العالمية في أول الـ Pipeline 🛡️
app.UseExceptionHandler(); 

//  إعداد خط الإنتاج البرمجي (HTTP Request Pipeline / Middlewares) ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // تفعيل واجهة Scalar الرائعة لـ .NET 10 🔥
}

app.UseHttpsRedirection();

app.UseCors("AllowStoreFrontend"); // تفعيل درع حماية الطلبات الخارجية (تفعيل سياسة CORS) 🛡️


app.UseAuthentication(); // التعرف على الهوية 👤
app.UseAuthorization();  // التحقق من الصلاحيات 🔐

// مسارات المنتجات (Products Endpoints)

// ----جلب جميع المنتجات
app.MapGet("/api/products", async (ProductService service)=>
{
    var products = await service.GetAllAsync();
    
    return products is null? Results.NotFound() : Results.Ok(products);
    
}
 );

 // ----- جلب منتج محدد برقم ال id
 app.MapGet("/api/products/{id}", async (int id , ProductService service) =>
 {
     var product = await service.GetByIdAsync(id);

     return product is null ? Results.NotFound() : Results.Ok(product) ;
    
 });
 
 // -----اضافة منتج جديد
 app.MapPost("/api/products", async (CreateProductRequest request,ProductService service) =>
 {
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(
        request,
        validationContext,
        validationResults,
        validateAllProperties: true);

    if (!isValid)
    {
        return Results.ValidationProblem(
            validationResults
                .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(v => v.ErrorMessage ?? "Invalid value").ToArray()
                ));
    }


    var product = await service.CreateAsync(request);
    
    return Results.Created($"/api/products/{product.Id}", product); 
 }).RequireAuthorization(policy => policy.RequireRole("Admin"));

 // ----تعديل بيانات منتج
  app.MapPut("/api/products/{id}" , async (int id , UpdateProductRequest request , ProductService service) =>
 {
     var product = await service.UpdateAsync(id , request);

     if(product is null)
     {
         return Results.NotFound();
     }

     return Results.Ok(product);
 }).RequireAuthorization(policy => policy.RequireRole("Admin"));

 //--- حذف منتج
 app.MapDelete("/api/products/{id}", async (int id , ProductService service)=>
{
    var deleted= await service.DeleteAsync(id);

    if (!deleted)
    {
        return Results.NotFound();
    }
      return Results.NoContent();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));



// --- مسارات الفئات( Categories Endpoints)

// جلب فئة محددة برقم ال id
app.MapGet("/api/categories/{id}", async (int id , CategoryService service) =>
{
    var category = await service.GetByIdAsync(id);
    return category is null ? Results.NotFound() : Results.Ok(category);
});


// جلب جميع الفئات
app.MapGet("/api/categories", async (CategoryService service)=>
{
    var categories = await service.GetAllAsync();
    
    return categories is null? Results.NotFound() :  Results.Ok(categories);
    
}
 );


// اضافة فئة جديدة
app.MapPost("/api/categories" , async (CreateCategoryRequest request , CategoryService service) =>
{
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();
    var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

    if (!isValid)
    {
        return Results.ValidationProblem(validationResults
            .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(v => v.ErrorMessage ?? "Invalid").ToArray()));
    }

    var category = await service.CreateAsync(request);
    return Results.Created($"/api/cvategories/{category.Id}", category);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));



 // --- مسار تعديل بيانات فئة 
 app.MapPut("/api/categories/{id}" , async (int id , UpdateCategoryRequest request , CategoryService service) =>
{
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();
    var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

    if (!isValid)
    {
        return Results.ValidationProblem(validationResults
            .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(v => v.ErrorMessage ?? "Invalid").ToArray()));
    }

    var category = await service.UpdateAsync(id , request);
    return category is null ? Results.NotFound() : Results.Ok(category);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// مسار حذف فئة
app.MapDelete("/api/categories/{id}" , async (int id , CategoryService service) =>
{
    try
    {
       var success = await service.DeleteAsync(id);
        return success ? Results.Ok(new { message = "تم حذف الفئة بنجاح من النظام" }) : Results.BadRequest("فشلت عملية الحذف");
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
    catch(InvalidOperationException ex)
    {
        // صيد خطأ منع الحذف بسبب وجود المنتجات وإرجاعه بكود 400 للمستخدم 📦
        return Results.BadRequest(new {message = ex.Message});
    }
}).RequireAuthorization(policy => policy.RequireRole("Admin"));





// --- مسارات سلة التسوق المحمية والمؤمنة (Cart Endpoints) ---

// 1. مسار جلب عناصر السلة وحساب الإجمالي على السيرفر 🛡️
app.MapGet("/api/cart" , async ([FromQuery] string? couponCode , [FromServices] CartService cartService) =>
{
    var mockCustomerId = 1; // محاكاة لعميل مسجل دخول مؤقتًا 👤

    try
    {
    var cart = await cartService.GetCartAsync(mockCustomerId , couponCode);
    return Results.Ok(cart);
    }
    catch(ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
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
        // المنتج غير موجود في المتجر أساساً
        return Results.NotFound(new { message = ex.Message });
    }
    catch(InvalidOperationException ex)
    {
       // الكمية المطلوبة تتجاوز المخزن المتاح حالياً
        return Results.BadRequest(new { message = ex.Message});
    }
});



// =================================================================
// --- منظومة مسارات الأمان وإدارة الحسابات (Identity Endpoints) ---
// =================================================================

// مسار تسجيل حساب جديد وتوليد رابط التفعيل 📧
app.MapPost("/api/auth/register" , async ( [FromBody] RegisterRequest request , [FromServices] IIdentityService IdentityService) =>
{
    try
    {
    var result = await IdentityService.RegisterAsync(request);
    return result ? Results.Ok("تم إنشاء الحساب بنجاح! يرجى مراجعة الـ Terminal لنسخ رابط تفعيل الحساب.") : Results.BadRequest("فشلت عملية التسجيل");
    }
    catch(ArgumentException ex)
    {
        return Results.BadRequest( new { Message = ex.Message});
    }

});

// مسار الضغط على رابط الإيميل لتفعيل الحساب برمجياً 🔗
app.MapGet("/api/auth/confirm-email" , async ([FromQuery] string token , [FromServices] IIdentityService IdentityService) =>
{
    var result = await IdentityService.ConfirmEmailAsync(token);
    return result ? Results.Ok(new { message = "مبروك! تم تفعيل حسابك بنجاح. يمكنك الآن تسجيل الدخول." }) : Results.BadRequest("رابط التفعيل غير صالح أو منتهي الصلاحية.");
    
});

// مسار تسجيل الدخول الصارم الصادر للبطاقتين (JWT & Refresh Token) 👤
app.MapPost("/api/auth/login" , async ([FromBody] LoginRequest request , [FromServices] IIdentityService IdentityService) =>
{
    try
    {
    var tokenResponse = await IdentityService.LoginAsync(request);
    return tokenResponse is null ? Results.Unauthorized() : Results.Ok(tokenResponse);
    }
    catch(InvalidOperationException ex)
    {
        // إرجاع رمز 400 مع رسالة تخبره بضرورة تفعيل الإيميل أولاً 🛡️
        return Results.BadRequest(new { message = ex.Message});
    }
});

// مسار تجديد التوكن تلقائياً في الخلفية (من قبل ال frontend) لحماية العميل (Refresh Token) ⏳
app.MapPost("/api/auth/refresh" , async ([FromBody] RefreshTokenRequest request , [FromServices] IIdentityService IdentityService) =>
{
    var tokenResponse = await IdentityService.RefreshTokenAsync(request);
    return tokenResponse is null ? Results.BadRequest("جلسة العمل منتهية الصلاحية , يرجى اعادة تسجيل الدخول") : Results.Ok(tokenResponse);
});

// مسار طلب رابط استعادة كلمة المرور المنسية (Forgot Password)
app.MapPost("/api/auth/forgot-password" , async ([FromBody] ForgotPasswordRequest request , [FromServices] IIdentityService IdentityService) =>
{
    await IdentityService.ForgotPasswordAsync(request);
    return Results.Ok(new {message = "إذا كان الحساب مسجلاً لدينا، فقد تم إرسال رابط استعادة كلمة المرور، تفقد الـ Terminal."});
});

// مسار تعيين كلمة المرور الجديدة عبر التوكن (Reset Password) 🔐
app.MapPost("/api/auth/reset-password" , async ([FromBody] ResetPasswordRequest request, [FromServices] IIdentityService IdentityService) =>
{
    try
    {
    var result = await IdentityService.ResetPasswordAsync(request);
    return result ? Results.Ok(new { message = "تم تغيير كلمة المرور بنجاح! يمكنك الدخول ببياناتك الجديدة." }) : Results.BadRequest("فشلت العملية");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }

});

// مسار إنشاء طلب جديد وحجز المخزون وتطبيق الكوبونات عبر الـ Transaction 💳🛡️
app.MapPost("/api/orders" , async ([FromQuery] String? couponCode , [FromServices] OrderService orderService ) =>
{
    var mockCustomerId = 1;

    try
    {
    var OrderResponse = await orderService.CreateOrderAsync(mockCustomerId , couponCode);
    return Results.Created($"/api/orders/{OrderResponse.OrderId}" , OrderResponse);
    }
    catch (InvalidOperationException ex)
    {
        // صيد أخطاء السلة الفارغة أو نفاد كميات المخزن برمز 400 📦🛑
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        // صيد خطأ الكوبون المنتهي برمز 400
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }

});


// =================================================================
// --- منظومة المدفوعات الإلكترونية والإشعارات الفورية (Payment APIs) ---
// =================================================================

// 1. مسار طلب إنشاء جلسة دفع والحصول على رابط الـ Checkout الخارجي 🔗
app.MapPost("/api/payment/checkout", async ([FromBody] PaymentRequest request, [FromServices] Ecommerce.Application.Interfaces.IPaymentService paymentService) =>
{
    try
    {
        var response = await paymentService.CreateCheckoutSessionAsync(request);
        return Results.Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
});

// 2. بوابة الـ Webhook المستقلة لاستقبال إشارات البنك الفورية وتأكيد الفواتير 🛡️🔥
app.MapPost("/api/payment/webhook", async ([FromBody] WebhookPayload payload, [FromServices] Ecommerce.Application.Interfaces.IPaymentService paymentService) =>
{
    try
    {
        var success = await paymentService.ProcessWebhookAsync(payload);
        return success ? Results.Ok(new { message = "تم معالجة إشعار الدفع وتحديث حالة الطلب بنجاح" }) : Results.BadRequest("فشلت المعالجة");
    }
    catch (UnauthorizedAccessException ex)
    {
        // صيد محاولات التزوير والاختراق السيبراني برمز الرفض الصارم 401 🚫
        return Results.Json(new { error = ex.Message }, statusCode: 401);
    }
});

    

app.Run();
