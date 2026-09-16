using System.Security.Claims;
using Application.Services;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/info/")]
    public class InfoController : Controller
    {
        private readonly InfoService _infoService;

        public InfoController(InfoService infoService)
        {
            _infoService = infoService;
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
        [Authorize]
        public IActionResult GetUserInfoByDocument([FromQuery] string document)
        {
            var infoUser = _infoService.GetUserInfoByDocument(document);

            if (infoUser != null)
                return Ok(infoUser);

            return NotFound("Cliente no encontrado.");
        }

        [HttpPost("getParameter")]
        public IActionResult GetParameter([FromBody] Info info)
        {
            string parameterValue = _infoService.GetParameter(info.nombreParametro);

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
