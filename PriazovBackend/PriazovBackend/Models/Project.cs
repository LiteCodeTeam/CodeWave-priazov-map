namespace DataBase.Models
{
    internal class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Industry Industry { get; set; } = null!;
        public Region Region { get; set; } = null!;
    }
}
