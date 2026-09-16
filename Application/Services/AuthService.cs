using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Interfaces;
using Core.Models;
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

        public AuthService(
            IUserRepository userRepository,
            TenantService tenantService,
            UserService userService,
            IOptions<JwtSettings> jwtOptions)
        {
            _userRepository = userRepository;
            _tenantService = tenantService;
            _userService = userService;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var tenant = _tenantService.GetByIdentifier(request.TenantIdentifier);

            if (tenant is null)
                return AuthResult.Failure("Tenant no encontrado");

            var user = _userRepository.GetUserByEmail(request.Username, tenant.Id);

            if (user is null || !_userRepository.ValidateUser(request.Username, request.Password, tenant.Id))
                return AuthResult.Failure("Usuario o contraseña incorrectos", unauthorized: true);

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
    }
}
