using JsonProperty.EFCore;
using System.ComponentModel.DataAnnotations;

namespace DataBase.Models
{
    public class UserPassword
    {
        public Guid Id { get; set; }

        // Внешний ключ для связи с пользователем (1:1)
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Хэш пароля (не сам пароль!)
        [MaxLength(256)]
        public string PasswordHash { get; set; } = null!;

        // Дата последнего изменения пароля
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
    public class User
    {
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(150)]
        public string Email { get; set; } = null!;
        public UserPassword Password { get; set; } = null!;
        [MaxLength(12)]
        public string Phone { get; set; } = null!;
        [MaxLength(18)]
        public string RoleName { get; set; } = null!;
        public byte[]? PhotoIcon { get; set; }
        public UserSession? Session { get; set; }
    }
    public class Company : User
    {
        [MaxLength(100)]
        public string IndustryName { get; set; } = null!;
        [MaxLength(1024)]
        public JsonDictionary<string, string> Address { get; set; } = null!;
        [MaxLength(1024)]
        public string? Description { get; set; }
        [MaxLength(1024)]
        public JsonList<string>? Contacts { get; set; }
        public List<Project>? Projects { get; set; }
    }
    public class Manager : User;
    public class Role
    {
        public String Name { get; set; } = String.Empty;
        public Role(string name) => Name = name;
    }
}
