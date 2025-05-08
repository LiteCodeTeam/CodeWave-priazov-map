using JsonProperty.EFCore;
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

            address = NormalizeAddress(address);

            if (address.Length < 10 || address.Length > 255)
                return new ValidationResult("Адрес должен содержать от 10 до 255 символов");

            if (!Regex.IsMatch(address, @"^[\p{L}0-9\s\.,\-–—()""№#/]+$"))
                return new ValidationResult("Адрес содержит недопустимые символы");

            var structureError = ValidateAddressStructure(address);
            if (structureError != null)
                return new ValidationResult(structureError);

            return ValidationResult.Success!;
        }

        private string NormalizeAddress(string address)
        {
            address = Regex.Replace(address, @"\s+", " ");
            address = Regex.Replace(address, @"\s*([\.,\-–—])\s*", "$1");
            return address.Trim();
        }

        private string? ValidateAddressStructure(string address)
        {
            var components = new (string[] patterns, string error)[]
            {
                (new[] { @"\b(ул|улица|проезд|пер|переулок)\b\s+[^0-9\s]" }, "Укажите улицу"),
                (new[] { @"\b(д|дом)\b", @"\s*\d+" }, "Укажите номер дома"),
                (new[] { @"\b(г|город|с|село|дер|деревня)\b\s+[^0-9\s]" }, "Укажите населённый пункт")
            };

            foreach (var component in components)
            {
                if (!component.patterns.Any(p => Regex.IsMatch(address, p, RegexOptions.IgnoreCase)))
                    return component.error;
            }

            if (Regex.IsMatch(address, @"\b(кв|квартира|офис)\b", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(address, @"(кв|квартира|офис)\s*\d+", RegexOptions.IgnoreCase))
            {
                return "Укажите номер квартиры/офиса";
            }

            return null;
        }
    }
    public class JsonListValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext context)
        {       
            var Jsonlist = value as JsonList<string>;
            if (Jsonlist == null)
                return new ValidationResult("Объект не является json объектом");

            var list = Jsonlist.ToString()?.Split(",").ToList();

            foreach (var item in list ?? new List<string> { })
            {
                
            }
            return ValidationResult.Success!;
        }
    }
}