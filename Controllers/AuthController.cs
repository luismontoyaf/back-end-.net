using Application.Services;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
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

        [HttpPost("password-reset/request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            try
            {
                await _authService.RequestPasswordResetAsync(request.Email, request.TenantIdentifier);
                return Ok(new
                {
                    message = "Si existe una cuenta asociada a este correo, recibirás instrucciones para restablecer tu contraseña."
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Password reset email delivery failed.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "No fue posible procesar la solicitud. Inténtalo nuevamente más tarde."
                });
            }
        }

        [HttpPost("password-reset/confirm")]
        public async Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetRequest request)
        {
            var reset = await _authService.ResetPasswordAsync(
                request.Token,
                request.NewPassword,
                request.TenantIdentifier);

            if (!reset)
                return BadRequest(new { message = "El enlace de recuperación no es válido o ha expirado." });

            return Ok(new { message = "La contraseña fue actualizada correctamente." });
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
