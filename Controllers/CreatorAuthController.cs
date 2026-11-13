using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/platform-auth")]
    public sealed class CreatorAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<CreatorAuthController> _logger;

        public CreatorAuthController(
            IConfiguration configuration,
            IOptions<JwtSettings> jwtOptions,
            ILogger<CreatorAuthController> logger)
        {
            _configuration = configuration;
            _jwtSettings = jwtOptions.Value;
            _logger = logger;
        }

        [AllowAnonymous]
        [EnableRateLimiting("creator-login")]
        [HttpPost("login")]
        public IActionResult Login([FromBody] CreatorLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Unauthorized(new { message = "Correo o contraseña incorrectos." });

            var configuredEmail = _configuration["PlatformCreator:Email"] ?? "canaldeatencionlumocol@gmail.com";
            var passwordHash = _configuration["PlatformCreator:PasswordHash"];
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                _logger.LogError("PlatformCreator:PasswordHash is not configured.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "El acceso de creador no está configurado."
                });
            }

            var emailMatches = string.Equals(
                request.Email.Trim(),
                configuredEmail,
                StringComparison.OrdinalIgnoreCase);

            bool passwordMatches;
            try
            {
                passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, passwordHash);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Platform creator password hash configuration is invalid.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "El acceso de creador no está configurado correctamente."
                });
            }

            if (!emailMatches || !passwordMatches)
                return Unauthorized(new { message = "Correo o contraseña incorrectos." });

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, configuredEmail),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, configuredEmail),
                new Claim(ClaimTypes.Email, configuredEmail),
                new Claim(ClaimTypes.Role, "platform_creator")
            };
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return Ok(new CreatorLoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Email = configuredEmail
            });
        }
    }
}
