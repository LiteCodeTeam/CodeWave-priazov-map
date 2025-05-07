using DataBase.Models;
using JsonProperty.EFCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class CompanyDto : UserDto
    {
        [Required]
        public string Industry { get; set; } = null!;
        [Required]
        [StringLength(100, MinimumLength = 4,
            ErrorMessage = "Длина названия 4-100 символов")]
        [RegularExpression(@"^[\p{L}\s]+$",
            ErrorMessage = "Разрешены только буквы и пробелы")]
        public string LeaderName { get; set; } = null!;
    }
    public class CompanyCreateDto : CompanyDto
    {
        [Required]
        [StringLength(30, MinimumLength = 8,
            ErrorMessage = "Длина пароля 8-30 символов")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Пароль слишком слабый")]
        public string Password { get; set; } = null!;
    }
    public class CompanyResponseDto : CompanyDto
    {
        [MaxLength(1024)]
        public string? Description { get; set; }
        public byte[]? PhotoIcon { get; set; }
        public byte[]? PhotoHeader { get; set; }
        [MaxLength(1024)]
        public JsonList<string> Contacts { get; set; } = new JsonList<string>();

        public CompanyResponseDto() { }
        public CompanyResponseDto(Company company)
        {
            Name = company.Name;
            Email = company.Email;
            Phone = company.Phone;
            FullAddress = company.FullAddress;
            Industry = company.Industry;
            LeaderName = company.LeaderName;
        }
    }
}
