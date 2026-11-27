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
using Ecommerce.Api.Extensions;
using System.Security.Claims;
using Ecommerce.Infrastructure.Services;
using Serilog;
using ecommer.Application.DTOs;


// تهيئة Serilog فور بدء التطبيق
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    
Log.Information("Starting E-Commerce & Payment Platform API...");

var builder = WebApplication.CreateBuilder(args);

// ربط Serilog مع إعدادات appsettings.json
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));


//   تسجيل خدمات البنية التحتية (Infrastructure Services) ---
// تهيئة  AppDbContext ليختار قاعدة البيانات تلقائياً حسب نص الاتصال:
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    // إذا كان نص الاتصال يحتوي على Host= فهو قادم من Neon PostgreSQL في السحابة
    if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Host="))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        // استخدام sql Server المحلي
        options.UseSqlServer(connectionString);
    }
});


    builder.Services.AddScoped<IProductRepository  , ProductRepository>();
    builder.Services.AddScoped<ICartRepository , CartRepository>();
    builder.Services.AddScoped<ICouponRepository, CouponRepository>();
    builder.Services.AddScoped<IIdentityService, IdentityService>();
    builder.Services.AddScoped<IOrderRepository , OrderRepository>();
    builder.Services.AddScoped<IPaymentService , PaymentService>();
    builder.Services.AddScoped<ICategoryRepository , CategoryRepository>();
    builder.Services.AddScoped<IEmailService , EmailService>();
    builder.Services.AddScoped<IInvoiceService , InvoiceService>();

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

// تطبيق هجرات قاعدة البيانات (Migrations) تلقائياً عند تشغيل الحاوية في السحابة
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (app.Environment.IsDevelopment())
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        // إنشاء الجداول تلقائياً بصيغة PostgreSQL في بيئة السحابة
        await dbContext.Database.EnsureCreatedAsync();
    }
}


// إتاحة واجهة التوثيق والاختبار Scalar دائماً (حتى في بيئة Production للـ Demo)
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "E-Commerce & Payment Platform API (Live Demo)";
    options.Theme = ScalarTheme.DeepSpace;
});


// تفعيل شبكة الأمان للأخطاء العالمية في أول الـ Pipeline 🛡️
app.UseExceptionHandler(); 

/* اغلاق تشغيل scalar في بيئة ال development لاتاحتها في بيئة ال Production
//  إعداد خط الإنتاج البرمجي (HTTP Request Pipeline / Middlewares) ---
 if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // تفعيل واجهة Scalar الرائعة لـ .NET 10 🔥
}
*/
app.UseHttpsRedirection();

app.UseCors("AllowStoreFrontend"); // تفعيل درع حماية الطلبات الخارجية (تفعيل سياسة CORS) 🛡️


app.UseAuthentication(); // التعرف على الهوية 👤
app.UseAuthorization();  // التحقق من الصلاحيات 🔐

// يسجل كل طلب وارد (Method, Path, Status Code, Response Time) في سطر واحد نظيف
app.UseSerilogRequestLogging();



// (في بيئة ال production ) توجيه الصفحة الرئيسية تلقائياً إلى واجهة Scalar لسهولة التصفح
app.MapGet("/", () => Results.Redirect("/scalar"));


//============================================//
//--- مسارات المنتجات (Products Endpoints)---//
//============================================//

//---- جلب جميع المنتجات او وفق شروط فلترة معينة
app.MapGet("/api/products" , async ([AsParameters] ProductQueryParameters queryParameters  , [FromServices] ProductService service) =>
{
    var result = await service.GetProductsAsync(queryParameters);
    return result is null? Results.NotFound() : Results.Ok(result);
    
});

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

 // مسار إضافة كمية للمخزون - محمي بصلاحية Admin حصرياً
 app.MapPatch("/api/products/{id}/stock" , async (int id , [FromBody] AddStockERquest request , [FromServices] ProductService service) =>
 {
     try
     {
         var updatedStock = await service.AddStockAsync(id , request.QuantityToAdd);
         return Results.Ok(new
         {
             message = "تم تحديث الكمية بنجاح",
             productId = id,
             newStock = updatedStock
         });
     }
     catch(KeyNotFoundException ex)
     {
         return Results.NotFound(new {message = ex.Message});
     }
     catch(ArgumentException ex)
     {
          return Results.BadRequest(new { message = ex.Message });
     }
 }).RequireAuthorization(polic => polic.RequireRole("Admin"));

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


//==============================================//
// --- مسارات الفئات( Categories Endpoints)--- //
//==============================================//

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
    
});


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
app.MapGet("/api/cart" , async (ClaimsPrincipal user , [FromQuery] string? couponCode , [FromServices] CartService cartService) =>
{
    int customerId = user.GetCustomerId(); // استخراج الرقم الحقيقي من التوكن

    try
    {
    var cart = await cartService.GetCartAsync(customerId , couponCode);
    return Results.Ok(cart);
    }
    catch(ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireAuthorization();

// 2. مسار إضافة منتج إلى السلة (أو زيادة الكمية)
app.MapPost("/api/cart/items" , async (ClaimsPrincipal user ,[FromBody] AddToCartRequest request ,[FromServices] CartService cartservice) =>
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


    int customerId = user.GetCustomerId(); // استخراج الرقم الحقيقي من التوكن

    try
    {
        var result = await cartservice.AddToCartAsync(customerId , request);
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
    catch(UnauthorizedAccessException ex)
    {
        return Results.Json(new {message = ex.Message}, statusCode: 401);
    }
}).RequireAuthorization(); // إجبار المستخدم على إرسال التوكن في الترويسة



// =================================================================
// --- منظومة مسارات الأمان وإدارة الحسابات (Identity Endpoints) ---
// =================================================================

// مسار تسجيل حساب جديد وتوليد رابط التفعيل 📧
app.MapPost("/api/auth/register" , async ( [FromBody] RegisterRequest request , [FromServices] IIdentityService IdentityService) =>
{
    try
    {
    var result = await IdentityService.RegisterAsync(request);
    return result ? Results.Ok("تم إنشاء الحساب بنجاح ! لتفعيل الحساب اضغط على رابط التفعيل الذي ارسلناه الى بريدك الاكتروني. ") : Results.BadRequest("فشلت عملية التسجيل");
    }
    catch(ArgumentException ex)
    {
        return Results.BadRequest( new { Message = ex.Message});
    }
    catch(UnauthorizedAccessException ex)
    {
        return Results.Json(new {message = ex.Message} , statusCode:401);
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
    return Results.Ok(new {message = "إذا كان الحساب مسجلاً لدينا، فقد تم إرسال رابط استعادة كلمة المرورالى بريدك الالكتروني "});
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
app.MapPost("/api/orders" , async (ClaimsPrincipal user ,[FromQuery] String? couponCode , [FromServices] OrderService orderService ) =>
{
    int customerId = user.GetCustomerId(); // استخراج الرقم الحقيقي من التوكن

    try
    {
    var OrderResponse = await orderService.CreateOrderAsync(customerId , couponCode);
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
    catch(UnauthorizedAccessException ex)
    {
        return Results.Json(new { message = ex.Message}, statusCode: 401);
    }

}).RequireAuthorization(); // إجبار المستخدم على إرسال التوكن في الترويسة

// مسار عرض تفاصيل طلب محدد مع التحقق من الملكية
app.MapGet("/api/orders/{id}", async (
    int id,
    ClaimsPrincipal user,
    [FromServices] OrderService orderService) =>
{
    int customerId = user.GetCustomerId();

    try
    {
        var orderResponse = await orderService.GetOrderByIdAsync(id, customerId);
        return Results.Ok(orderResponse);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { message = ex.Message }, statusCode: 403);
    }
})
.RequireAuthorization();



// =================================================================
// --- منظومة المدفوعات الإلكترونية والإشعارات الفورية (Payment APIs) ---
// =================================================================

// 1. مسار طلب إنشاء جلسة دفع والحصول على رابط الـ Checkout الخارجي 🔗
app.MapPost("/api/payment/checkout", async (ClaimsPrincipal user ,[FromBody] PaymentRequest request, [FromServices] Ecommerce.Application.Interfaces.IPaymentService paymentService) =>
{
     int customerId = user.GetCustomerId(); // استخراج رقم العميل من التوكن

    try
    {
        var response = await paymentService.CreateCheckoutSessionAsync(customerId , request);
        return Results.Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
    catch (UnauthorizedAccessException ex)
    {
        // إرجاع رمز 403 Forbidden لأن المستخدم مسجل دخول لكنه يحاول الوصول لمورد لا يملكه
        return Results.Json(new { message = ex.Message }, statusCode: 403);
    }
}).RequireAuthorization(); 

// 2. بوابة استقبال إشعارات Stripe الحقيقية
app.MapPost("/api/payment/webhook", async (
    HttpRequest httpRequest,
    [FromServices] Ecommerce.Application.Interfaces.IPaymentService paymentService) =>
{
    // 1. قراءة النص الخام القادم من Stripe كما هو تماماً
    using var reader = new StreamReader(httpRequest.Body);
    var rawJson = await reader.ReadToEndAsync();

    // 2. استخراج ترويسة التوقيع الأمني
    var stripeSignature = httpRequest.Headers["Stripe-Signature"].ToString();

    if (string.IsNullOrEmpty(stripeSignature))
        return Results.BadRequest(new { error = "ترويسة Stripe-Signature مفقودة!" });

    try
    {
        var success = await paymentService.ProcessStripeWebhookAsync(rawJson, stripeSignature);
        return success ? Results.Ok() : Results.BadRequest();
    }
    catch (UnauthorizedAccessException ex)
    {
       
        return Results.Json(new { error = ex.Message }, statusCode: 401);
        
    }
});

// لتوجيه المتصفح  لصفحة نجاح عملية الدفع
app.MapGet("/payment-success", (int orderId) => Results.Ok($"تم الدفع بنجاح للطلب رقم #{orderId} (في انتظار تأكيد الـ Webhook)."));

// لتوجيه المتصفح  لصفحة الغاء عملية الدفع
app.MapGet("/payment-cancel", (int orderId) => Results.Ok($"تم إلغاء عملية الدفع للطلب رقم #{orderId}."));


app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}

