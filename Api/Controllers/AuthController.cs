using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Models;
using Infrastructure.Services;
using Infrastructure.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity.Data;
using Core.Interfaces;
using Application.Services;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {

        private readonly UserRepository _userRepository;
        private readonly IUserRepository _userIRepository;
        private readonly UserService _userService;
        private readonly string _key = "YourSuperLongSecretKeyForJWTAuthentication123!";

        public AuthController(AppDbContext context, IUserRepository iUserRepository, UserService userService)
        {
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _userRepository = new UserRepository(connectionString, context);
            _userIRepository = iUserRepository;
            _userService = userService;
        }

        [HttpPost("login")]
         public async Task<IActionResult> Login([FromBody] Core.Models.LoginRequest request)
        {
            var user = _userRepository.GetUserByEmail(request.Username);
            if (_userRepository.ValidateUser(request.Username, request.Password))
            {
                await _userRepository.DeleteRefreshTokenByUserAsync(user.Id ?? throw new("Id no valido"));

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Guardá el refresh token en base de datos o en memoria para asociarlo al usuario
                await _userRepository.SaveRefreshTokenAsync(user.Id ?? 0, refreshToken, DateTime.UtcNow.AddDays(7));

                return Ok(new { 
                    Token = token,
                    RefreshToken = refreshToken
                });
            }

            return Unauthorized(new { Message = "Usuario o contraseña incorrectos" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            var storedToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (storedToken != null)
            {
                await _userRepository.DeleteRefreshTokenByUserAsync(storedToken.UserId);
            }
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

            var user = await _userIRepository.GetUserByIdAsync(storedToken.UserId);
            if (user == null)
            {
                return Unauthorized(new { message = "Usuario no encontrado" });
            }

            var userDto = new EmployeDto
            {
                Id = user.Id,
                nombre = user.nombre,
                apellidos = user.apellidos,
                correo = user.correo,
                rol = _userService.ConvertTypeUser(user.rol)
            };

            // Generar nuevos tokens
            var newJwt = GenerateJwtToken(userDto);
            var newRefreshToken = GenerateRefreshToken();

            // Opcional: eliminar el anterior
            await _userRepository.DeleteRefreshTokenByUserAsync(user.Id);

            // Guardar el nuevo
            await _userRepository.SaveRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(1));

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
                new Claim(ClaimTypes.Role, user.rol.ToString()) // Agrega el rol del usuario
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
