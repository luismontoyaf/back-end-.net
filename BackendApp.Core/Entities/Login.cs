using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string TenantIdentifier { get; set; }
    }

    public class PasswordResetRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string TenantIdentifier { get; set; }
    }

    public class ConfirmPasswordResetRequest
    {
        [Required]
        public required string Token { get; set; }

        [Required]
        [MinLength(8)]
        public required string NewPassword { get; set; }

        [Required]
        public required string TenantIdentifier { get; set; }
    }
}