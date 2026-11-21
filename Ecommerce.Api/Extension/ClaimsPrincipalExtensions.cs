using System.Security.Claims;


namespace Ecommerce.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetCustomerId(this ClaimsPrincipal user)
    {
        
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             

        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int customerId))
        {
            throw new UnauthorizedAccessException("تعذر التحقق من هوية العميل من التوكن المرفق.");
        }
        return customerId;
    }
}
