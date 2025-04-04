using JsonProperty.EFCore;

namespace DataBase.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
    }
    public class Company : User
    {
        public JsonList<string>? Contacts { get; set; }
        public Address Address { get; set; } = null!;
        public Industry Industry { get; set; } = null!;
    }
    public class Manager : User;
}
