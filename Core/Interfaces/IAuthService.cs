using Core.Models;

namespace Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> LogoutAsync(string refreshToken);
        Task<AuthResult> RefreshTokenAsync(string refreshToken);
    }

    public sealed class AuthResult
    {
        public bool Succeeded { get; init; }
        public bool Unauthorized { get; init; }
        public string? Message { get; init; }
        public string? Token { get; init; }
        public string? RefreshToken { get; init; }

        public static AuthResult Success(string? token = null, string? refreshToken = null, string? message = null) => new()
        {
            Succeeded = true,
            Token = token,
            RefreshToken = refreshToken,
            Message = message
        };

        public static AuthResult Failure(string message, bool unauthorized = false) => new()
        {
            Succeeded = false,
            Unauthorized = unauthorized,
            Message = message
        };
    }
}
