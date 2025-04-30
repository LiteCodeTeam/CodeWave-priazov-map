using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        [JsonIgnore]
        public Guid UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; } = null!;
        [MaxLength(1024)]
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
