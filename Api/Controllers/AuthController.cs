using Application.Services;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Core.Models.LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return ToActionResult(result, includeLowercaseTokenNames: false);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            var result = await _authService.LogoutAsync(request.RefreshToken);
            return ToActionResult(result, includeLowercaseTokenNames: false);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return ToActionResult(result, includeLowercaseTokenNames: true);
        }

        private IActionResult ToActionResult(AuthResult result, bool includeLowercaseTokenNames)
        {
            if (!result.Succeeded)
            {
                return result.Unauthorized
                    ? Unauthorized(new { message = result.Message })
                    : BadRequest(new { message = result.Message });
            }

            if (result.Token is null)
                return Ok(new { message = result.Message });

            return includeLowercaseTokenNames
                ? Ok(new { token = result.Token, refreshToken = result.RefreshToken })
                : Ok(new { Token = result.Token, RefreshToken = result.RefreshToken });
        }
    }
}
