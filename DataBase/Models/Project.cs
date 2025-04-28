using JsonProperty.EFCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBase.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        [MaxLength(150)]
        public string Name { get; set; } = null!;
        public Guid CompanyId { get; set; }
        [NotMapped]
        public Company Company { get; set; } = null!;
        [MaxLength(1024)]
        public string? Description { get; set; }
        public byte[]? PhotoIcon { get; set; }
        [MaxLength(1024)]
        public JsonList<byte[]> Photos { get; set; } = new JsonList<byte[]>();
    }
}
