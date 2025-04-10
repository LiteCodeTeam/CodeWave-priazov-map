using JsonProperty.EFCore;

namespace DataBase.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        public string? Description { get; set; }
        public string? PhotoIcon { get; set; }
        public JsonList<string>? Photos { get; set; }
    }
}
