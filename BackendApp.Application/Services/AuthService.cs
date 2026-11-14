using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly TenantService _tenantService;
        private readonly UserService _userService;
        private readonly JwtSettings _jwtSettings;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            TenantService tenantService,
            UserService userService,
            IOptions<JwtSettings> jwtOptions,
            EmailService emailService,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tenantService = tenantService;
            _userService = userService;
            _jwtSettings = jwtOptions.Value;
            _emailService = emailService;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task RequestPasswordResetAsync(string email, string tenantIdentifier)
        {
            var tenant = _tenantService.GetByIdentifier(tenantIdentifier);
            if (tenant is null)
                return;

            var user = await _userRepository.GetActiveUserByEmailAsync(email.Trim(), tenant.Id);
            if (user is null)
                return;

            var token = GeneratePasswordResetToken();
            var tokenHash = HashPasswordResetToken(token);
            var now = DateTime.UtcNow;

            await _userRepository.InvalidatePasswordResetTokensAsync(user.Id, tenant.Id);
            await _userRepository.SavePasswordResetTokenAsync(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TenantId = tenant.Id,
                TokenHash = tokenHash,
                ExpiryDate = now.AddMinutes(30),
                CreatedAt = now
            });

            try
            {
                var frontendUrl = _configuration["PasswordReset:FrontendUrl"];
                if (string.IsNullOrWhiteSpace(frontendUrl) && _environment.IsDevelopment())
                    frontendUrl = "http://localhost:4200";

                if (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri) ||
                    string.IsNullOrWhiteSpace(frontendUri.Host) ||
                    !string.IsNullOrEmpty(frontendUri.UserInfo) ||
                    !string.IsNullOrEmpty(frontendUri.Query) ||
                    !string.IsNullOrEmpty(frontendUri.Fragment) ||
                    (frontendUri.Scheme != Uri.UriSchemeHttps && !_environment.IsDevelopment()))
                {
                    throw new InvalidOperationException("PasswordReset:FrontendUrl debe configurarse con una URL HTTPS válida.");
                }

                var senderEmail = _configuration["MAILJET_SENDER_EMAIL"] ?? tenant.correo;
                if (string.IsNullOrWhiteSpace(senderEmail))
                    throw new InvalidOperationException("El tenant no tiene un correo remitente configurado.");

                var resetUrl = $"{frontendUrl.TrimEnd('/')}/{Uri.EscapeDataString(tenant.identificador)}/reset-password?token={Uri.EscapeDataString(token)}";
                await _emailService.SendPasswordResetEmailAsync(
                    user.correo,
                    user.nombre,
                    tenant.nombre,
                    senderEmail,
                    resetUrl);
            }
            catch (Exception exception)
            {
                await _userRepository.InvalidatePasswordResetTokensAsync(user.Id, tenant.Id);
                _logger.LogError(exception, "Failed to send a password reset email.");
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword, string tenantIdentifier)
        {
            var tenant = _tenantService.GetByIdentifier(tenantIdentifier);
            if (tenant is null || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
                return false;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
            return await _userRepository.CompletePasswordResetAsync(
                HashPasswordResetToken(token),
                tenant.Id,
                passwordHash,
                DateTime.UtcNow);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var tenant = _tenantService.GetByIdentifier(request.TenantIdentifier);

            if (tenant is null)
                return AuthResult.Failure("Tenant no encontrado");

            var user = _userRepository.GetUserByEmail(request.Username, tenant.Id);

            if (user is null || !_userRepository.ValidateUser(request.Username, request.Password, tenant.Id))
                return AuthResult.Failure("Usuario o contraseña incorrectos", unauthorized: true);

            if (user.estado != 1)
            {
                return AuthResult.Failure(
                    "Tu usuario está inactivo. Contacta al administrador de tu empresa para solicitar acceso.",
                    unauthorized: true);
            }

            if (!tenant.estado)
            {
                return AuthResult.Failure(
                    "La empresa se encuentra inactiva. Contacta a soporte para recibir ayuda.",
                    unauthorized: true);
            }

            await _userRepository.DeleteRefreshTokenByUserAsync(user.Id, tenant.Id);

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            await _userRepository.SaveRefreshTokenAsync(
                user.Id,
                user.TenantId,
                refreshToken,
                DateTime.UtcNow.AddDays(7));

            return AuthResult.Success(accessToken, refreshToken);
        }

        public async Task<AuthResult> LogoutAsync(string refreshToken)
        {
            var storedToken = await _userRepository.GetRefreshTokenAsync(refreshToken);

            if (storedToken is null)
                return AuthResult.Success(message: "Sesión cerrada correctamente");

            var user = await _userRepository.GetUserByIdAsync(storedToken.UserId, storedToken.TenantId);

            if (user is null)
                return AuthResult.Success(message: "Sesión cerrada correctamente");

            if (storedToken.TenantId != user.TenantId)
                return AuthResult.Failure("Token inválido para este tenant", unauthorized: true);

            await _userRepository.DeleteRefreshTokenByUserAsync(user.Id, user.TenantId);

            return AuthResult.Success(message: "Sesión cerrada correctamente");
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _userRepository.GetRefreshTokenAsync(refreshToken);

            if (storedToken is null || storedToken.ExpiryDate < DateTime.UtcNow)
                return AuthResult.Failure("Refresh token inválido o expirado", unauthorized: true);

            var user = await _userRepository.GetUserByIdAsync(storedToken.UserId, storedToken.TenantId);

            if (user is null)
                return AuthResult.Failure("Usuario no encontrado", unauthorized: true);

            if (storedToken.TenantId != user.TenantId)
                return AuthResult.Failure("Token inválido para este tenant", unauthorized: true);

            var userDto = new EmployeDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                nombre = user.nombre,
                apellidos = user.apellidos,
                correo = user.correo,
                rol = _userService.ConvertTypeUser(user.rol)
            };

            var newAccessToken = GenerateJwtToken(userDto);
            var newRefreshToken = GenerateRefreshToken();

            await _userRepository.DeleteRefreshTokenByUserAsync(user.Id, user.TenantId);
            await _userRepository.SaveRefreshTokenAsync(
                user.Id,
                user.TenantId,
                newRefreshToken,
                DateTime.UtcNow.AddDays(1));

            return AuthResult.Success(newAccessToken, newRefreshToken);
        }

        private string GenerateJwtToken(EmployeDto user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.nombre ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.nombre ?? string.Empty),
                new Claim(ClaimTypes.Email, user.correo ?? string.Empty),
                new Claim(ClaimTypes.Role, user.rol ?? string.Empty),
                new Claim("tenantId", user.TenantId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(29),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private static string GeneratePasswordResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashPasswordResetToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
