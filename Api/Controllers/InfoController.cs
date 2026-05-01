using System.Security.Claims;
using Application.Services;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/info/")]
    public class InfoController : Controller
    {
        private readonly InfoRepository _repository;
        private readonly InfoService _infoService;
        private readonly TenantProvider _tenantProvider;

        public InfoController(InfoService infoService, TenantProvider tenantProvider, AppDbContext context, IConfiguration configuration)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = configuration.GetConnectionString("DefaultConnection");
                
            _repository = new InfoRepository(tenantProvider, connectionString, context);
            _infoService = infoService;
            _tenantProvider = tenantProvider;
        }

        [HttpGet("getUserInfo")]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null)
                return Unauthorized();

            var userClaims = identity.Claims;

            var userInfo = new
            {
                Id = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value,
                Username = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
                Email = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                Role = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value,
                TenantId = userClaims.FirstOrDefault(x => x.Type == "tenantId")?.Value
            };

            return Ok(userInfo);
        }

        [HttpGet("getUserInfoByDocument")]
        [Authorize] // 🔒 Requiere autenticación
        public IActionResult GetUserInfoByDocument([FromQuery] string document)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var infoUser = _repository.GetUserInfoByDocument(document, tenantId);

            if (infoUser != null)
                return Ok(infoUser);

            return NotFound("Cliente no encontrado.");
        }

        [HttpPost("getParameter")]
        public IActionResult GetParameter([FromBody] Info info)
        {
            var tenantId = _tenantProvider.GetTenantId();

            string parameterValue = _repository.GetParameter(info.nombreParametro, tenantId);

            return Ok(parameterValue);
        }

        [HttpGet("validate/{tenantId}")]
        public IActionResult ValidateTenant(string tenantId)
        {
            var exists = _infoService.ValidateTenant(tenantId);

            if (!exists)
                return NotFound();

            return Ok();
        }
    }
}
