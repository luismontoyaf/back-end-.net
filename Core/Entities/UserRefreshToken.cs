using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class UserRefreshToken
    {
        public int IdRefreshToken { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
    }
}
