using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Backend.Validation
{
    public class AddressValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var address = value as string;
            if (string.IsNullOrWhiteSpace(address))
                return new ValidationResult("Адрес не может быть пустым");

            var pattern = @"^\s*[\p{L}0-9\s\.,\-–—()""№#/]{10,255}\s*$";

            if (!Regex.IsMatch(address, pattern))
                return new ValidationResult("Недопустимые символы в адресе");

            if (!HasValidAddressStructure(address))
                return new ValidationResult("Неверная структура адреса");

            return ValidationResult.Success!;
        }

        private bool HasValidAddressStructure(string address)
        {
            var requiredComponents = new[] { "ул", "д", "кв", "г", "стр" };
            return requiredComponents.Any(c => address.Contains(c)) ||
                   address.Contains(" ") ||
                   Regex.IsMatch(address, @"\d");
        }
    }
}
