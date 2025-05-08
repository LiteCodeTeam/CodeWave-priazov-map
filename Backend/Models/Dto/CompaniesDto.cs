﻿using Backend.Validation;
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
        [RegularExpression(@"^\s*[\p{L}\s]+\s*$",
            ErrorMessage = "Разрешены только буквы и пробелы")]
        public string LeaderName { get; set; } = null!;
    }
    public class CompanyCreateDto : CompanyDto
    {
        [Required]
        [StringLength(30, MinimumLength = 8,
            ErrorMessage = "Длина пароля 8-30 символов")]
        [RegularExpression(@"^\s*(?=.*[a-zа-яё])(?=.*[A-ZА-ЯЁ])(?=.*\d)(?=.*[^\da-zA-Zа-яА-ЯЁё]).{8,30}\s*$",
        ErrorMessage = "Пароль слишком слабый")]
        public string Password { get; set; } = null!;
    }
    public class CompanyResponseDto : CompanyDto
    {
        public Guid Id { get; set; }
        [MaxLength(1024)]
        public string? Description { get; set; }
        public byte[]? PhotoIcon { get; set; }
        public byte[]? PhotoHeader { get; set; }
        public CompanyResponseDto() { }
        public CompanyResponseDto(Company company)
        {
            Id = company.Id;
            Name = company.Name;
            Email = company.Email;
            Phone = company.Phone;
            FullAddress = company.Address.FullAddress;
            Industry = company.Industry;
            LeaderName = company.LeaderName;
        }
    }

    public class CompanyChangeDto : CompanyDto
    {
        public string? Description { get; set; }
        public byte[]? PhotoIcon { get; set; }
        public byte[]? PhotoHeader { get; set; }
    }
}
