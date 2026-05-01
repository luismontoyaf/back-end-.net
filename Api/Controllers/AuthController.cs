using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {

        private readonly UserRepository _userRepository;
        private readonly IUserRepository _userIRepository;
        private readonly UserService _userService;
        private readonly TenantService _tenantService;
        private readonly string _key;

        public AuthController(AppDbContext context, 
            IUserRepository iUserRepository, 
            UserService userService, 
            IConfiguration configuration,
            TenantService tenantService)
        {
            _key = configuration["JwtSettings:SecretKey"];

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            _userRepository = new UserRepository(connectionString, context);
            _userIRepository = iUserRepository;
            _userService = userService;
            _tenantService = tenantService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Core.Models.LoginRequest request)
        {
            var tenantId = _tenantService.GetByIdentifier(request.TenantIdentifier.ToString());

            if (tenantId == null)
            {
                throw new Exception("Tenant no encontrado");
            }

            // 1. Buscar usuario por correo + tenant
            var user = _userRepository.GetUserByEmail(request.Username, tenantId.Id);

            if (user == null)
                return Unauthorized(new { Message = "Usuario o contraseña incorrectos" });

            // 2. Validar contraseña
            var isValid = _userRepository.ValidateUser(request.Username, request.Password, tenantId.Id);

            if (!isValid)
                return Unauthorized(new { Message = "Usuario o contraseña incorrectos" });

            // 3. Eliminar refresh tokens anteriores
            await _userRepository.DeleteRefreshTokenByUserAsync(user.Id, tenantId.Id);

            // 4. Generar tokens
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // 5. Guardar refresh token con tenant
            await _userRepository.SaveRefreshTokenAsync(
                user.Id,
                user.TenantId,
                refreshToken,
                DateTime.UtcNow.AddDays(7)
            );

            // 6. Respuesta
            return Ok(new
            {
                Token = token,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            var storedToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (storedToken == null)
                return Ok(new { message = "Sesión cerrada correctamente" });

            var user = await _userIRepository.GetUserByIdAsync(storedToken.UserId, storedToken.TenantId);

            if (user == null)
                return Ok(new { message = "Sesión cerrada correctamente" });

            if (storedToken.TenantId != user.TenantId)
                return Unauthorized();

            await _userRepository.DeleteRefreshTokenByUserAsync(user.Id, user.TenantId);

            return Ok(new { message = "Sesión cerrada correctamente" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
        {
            var storedToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (storedToken == null || storedToken.ExpiryDate < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token inválido o expirado" });
            }

            var user = await _userIRepository.GetUserByIdAsync(storedToken.UserId, storedToken.TenantId);

            if (user == null)
            {
                return Unauthorized(new { message = "Usuario no encontrado" });
            }

            if (storedToken.TenantId != user.TenantId)
            {
                return Unauthorized(new { message = "Token inválido para este tenant" });
            }

            var userDto = new EmployeDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                nombre = user.nombre,
                apellidos = user.apellidos,
                correo = user.correo,
                rol = _userService.ConvertTypeUser(user.rol)
            };

            var newJwt = GenerateJwtToken(userDto);
            var newRefreshToken = GenerateRefreshToken();

            await _userRepository.DeleteRefreshTokenByUserAsync(user.Id, user.TenantId);

            await _userRepository.SaveRefreshTokenAsync(
                user.Id,
                user.TenantId,
                newRefreshToken,
                DateTime.UtcNow.AddDays(1)
            );

            return Ok(new
            {
                token = newJwt,
                refreshToken = newRefreshToken
            });
        }

        private string GenerateJwtToken(EmployeDto user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.nombre),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Agrega el ID del usuario
                new Claim(ClaimTypes.Name, user.nombre),
                new Claim(ClaimTypes.Email, user.correo), // Agrega el email
                new Claim(ClaimTypes.Role, user.rol.ToString()), // Agrega el rol del usuario
                new Claim("tenantId", user.TenantId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "BackendApp",          
                audience: "BackendAppUsuarios",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(29),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }
    }
}
