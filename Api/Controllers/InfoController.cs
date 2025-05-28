using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Infrastructure.Services;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Infrastructure.Data;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/info/")]
    public class InfoController : Controller
    {
        private readonly InfoRepository _repository;
        private readonly InfoService _infoService;

        public InfoController(InfoService infoService, AppDbContext context)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _repository = new InfoRepository(connectionString, context);
            _infoService = infoService;
        }

        [HttpGet("getUserInfo")]
        [Authorize] // 🔒 Requiere autenticación
        public IActionResult GetUserInfo()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity != null)
            {
                var userClaims = identity.Claims;

                var userInfo = new
                {
                    Id = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value,
                    Username = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
                    Email = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                    Role = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value
                };

                return Ok(userInfo);
            }

            return Unauthorized();
        }

        [HttpGet("getUserInfoByDocument")]
        [Authorize] // 🔒 Requiere autenticación
        public IActionResult GetUserInfoByDocument([FromQuery] string document)
        {
            var InfoUser = _repository.GetUserInfoByDocument(document);

            if (InfoUser != null)
            {
                return Ok(InfoUser); // Devuelve los productos en JSON
            }

            return NotFound("Producto no encontrado."); ;
        }

        [HttpPost("getParameter")]
        public IActionResult GetParameter([FromBody] Info info)
        {
            string parameterValue = _repository.GetParameter(info);
            return Ok(parameterValue); // Devuelve los productos en JSON
        }
    }
}
