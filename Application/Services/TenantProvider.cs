using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Application.Services
{

    public class TenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetTenantId()
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?
                .User?
                .FindFirst("tenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim))
                throw new UnauthorizedAccessException("TenantId no encontrado en el token");

            return int.Parse(tenantIdClaim);
        }

        public int GetUserId()
        {
            var value = _httpContextAccessor.HttpContext?
                .User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(value))
                throw new UnauthorizedAccessException("Usuario no autenticado");

            return int.Parse(value);
        }
    }
}
