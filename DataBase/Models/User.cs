using JsonProperty.EFCore;

namespace DataBase.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Phone { get; set; }
        public Role Role { get; set; } = null!;
    }
    public class Company : User
    {
        public String? Description { get; set; }
        public JsonList<string>? Contacts { get; set; }
        public Industry Industry { get; set; } = null!;
        public JsonDictionary<string, object> Address { get; set; } = null!;
    }
    public class Manager : User;
    public class Role
    {
        public String Name { get; set; } = null!;
        public Role(string name) => Name = name;
    }
}
