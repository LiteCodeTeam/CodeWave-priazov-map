using DataBase.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Dto
{
    public class ManagerCreateDto : UserDto
    {
        [Required]
        [StringLength(30, MinimumLength = 8,
            ErrorMessage = "Длина пароля 8-30 символов")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Пароль слишком слабый")]
        public string Password { get; set; } = null!;
    }
}