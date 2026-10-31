using Microsoft.AspNetCore.Diagnostics; 
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. تسجيل الخطأ بالتفصيل في الـ Logs الخلفية ليراه مهندسو الشركة
        _logger.LogError(exception, "حدث خطأ غير متوقع في منصة التجارة الإلكترونية: {Message}", exception.Message);

        // 2. صياغة استجابة JSON أنيقة وموحدة ومحمية للمستخدم النهائي (Problem Details)
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "عذرًا، حدث خطأ داخلي في الخادم. فريق الصيانة البرمجية يعمل على حل المشكلة الآن.",
            Instance = httpContext.Request.Path
        };

        // 3. إرسال الاستجابة بأمان للـ Postman
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // نُرجع true لتأكيد صيد الخطأ بنجاح وحماية السيرفر من الانهيار
        return true;
    }
}
