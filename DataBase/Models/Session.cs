using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.Models
{

    public class RevokedToken
    {
        public Guid Id { get; set; }
        [MaxLength(1024)]
        public string Token { get; set; } = null!; 
        public DateTime ExpiresAt { get; set; }  // Когда токен истечёт естественным путём
    }
    public class UserSession
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        [MaxLength(1024)]
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
