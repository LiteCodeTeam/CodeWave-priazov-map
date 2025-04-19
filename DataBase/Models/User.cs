using JsonProperty.EFCore;

namespace DataBase.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string? PhotoIcon { get; set; }
    }
    public class Company : User
    {
        public string IndustryName { get; set; } = null!;
        public JsonDictionary<string, string> Address { get; set; } = null!;
        public string? Description { get; set; }
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
