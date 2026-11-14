using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class PasswordResetToken
    {
        [Key]
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int TenantId { get; set; }
        public required string TokenHash { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
