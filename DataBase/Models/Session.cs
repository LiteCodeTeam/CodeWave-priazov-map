using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.Models
{
    public class RefreshRequest
    {
        public required string RefreshToken { get; set; }
    }
    public class RevokedToken
    {
        public Guid Id { get; set; }
        public string TokenId { get; set; } = null!; // jti из JWT
        public DateTime ExpiresAt { get; set; }  // Когда токен истечёт естественным путём
    }
    public class UserSession
    {
        public Guid Id { get; set; }
        [Column("UserId")]
        public Guid UserId { get; set; }
        [NotMapped]
        public User User { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
