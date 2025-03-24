using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Models;
using Infrastructure.Services;
using Infrastructure.Data;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {

        private readonly UserRepository _userRepository;
        private readonly string _key = "YourSuperLongSecretKeyForJWTAuthentication123!";

        public AuthController(AppDbContext context)
        {
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            Console.WriteLine($"Connection String: {connectionString}");
            _userRepository = new UserRepository(connectionString, context);
        }

        [HttpPost("login")]
         public IActionResult Login([FromBody] LoginRequest request)
        {
            if (_userRepository.ValidateUser(request.Username, request.Password))
            {
                var token = GenerateJwtToken(request.Username);
                return Ok(new { Token = token });
            }

            return Unauthorized(new { Message = "Usuario o contraseña incorrectos" });
        }

        private string GenerateJwtToken(string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username)
            };

            var token = new JwtSecurityToken(
                issuer: "BackendApp",          
                audience: "BackendAppUsuarios",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
