namespace DataBase.Models
{
    internal class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Phone { get; set; }
        public bool IsManager { get; set; }
        public Region? UserRegion { get; set; }
        public Industry? UserIndustry { get; set; }
    }
}
