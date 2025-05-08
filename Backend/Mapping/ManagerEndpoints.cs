using Backend.Models.Dto;
using DataBase.Models;
using DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Text;
using Backend.Validation;
using System.Globalization;

namespace Backend.Mapping
{
    public static class ManagerEndpoints
    {
        private static readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        public static void MapManagerEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/managers");
            group.MapPost("/create", Create);
            group.MapGet("account", Account);
            group.MapPut("/change", Change);
        }

        private static async Task<IResult> Create(
            [FromBody] ManagerCreateDto managerDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IGeocodingService geocoding)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                managerDto,
                new ValidationContext(managerDto),
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            if (Zxcvbn.Core.EvaluatePassword(managerDto.Password).Score < 3)
                return Results.BadRequest("Слабый пароль");

            using var db = await factory.CreateDbContextAsync();
            var geocoderValidate = new NominatimGeocoder();

            managerDto.FullAddress = managerDto.FullAddress.Trim();
            managerDto.Email = managerDto.Email.Trim();
            managerDto.Phone = managerDto.Phone.Trim();

            if (db.Users.Any(u => u.Email == managerDto.Email || u.Phone == managerDto.Phone))
                return Results.Conflict("Почта или телефон есть в реесте");

            var (isValidAddress, error) = await geocoderValidate.ValidateRussianAddressAsync(managerDto.FullAddress);

            if (!isValidAddress)
                return Results.NotFound(error);

            var coords = await geocoding.GetCoordinatesAsync(managerDto.FullAddress);

            var manager = new Manager()
            {
                Name = managerDto.Name.Trim(),
                Email = managerDto.Email,
                Phone = managerDto.Phone,
                Address = new ShortAddressDto()
                {
                    FullAddress = managerDto.FullAddress,
                    Latitude = decimal.Parse(coords.Latitude, CultureInfo.InvariantCulture),
                    Longitude = decimal.Parse(coords.Longitude, CultureInfo.InvariantCulture),
                }
            };

            await db.Users.AddAsync(manager);
            await db.SaveChangesAsync();

            return Results.Ok(new ManagerResponseDto(manager));
        }

        public static async Task<IResult> Account([FromQuery] Guid? id,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache)
        {
            if (id == null)
                return Results.BadRequest("Id пуст");

            var cacheKey = $"managers_{id}";
            if (cache.TryGetValue(cacheKey, out ManagerResponseDto? cachedManager))
                return Results.Ok(cachedManager);

            using var db = await factory.CreateDbContextAsync();

            var manager = await db.Users.OfType<Manager>().FirstOrDefaultAsync(c => c.Id == id);

            if (manager == null)
                return Results.NotFound();

            var managerResponse = new ManagerResponseDto(manager);

            cache.Set(cacheKey, managerResponse, CacheOptions);

            return Results.Ok(managerResponse);
        }

        public static async Task<IResult> Change([FromQuery] Guid? id,
            [FromBody] ManagerChangeDto managerDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IGeocodingService geocoding)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                managerDto,
                new ValidationContext(managerDto),
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? "Неизвестная ошибка").ToArray()
                    );
                return Results.ValidationProblem(errors);
            }

            using var db = await factory.CreateDbContextAsync();
            var geocoderValidate = new NominatimGeocoder();

            managerDto.FullAddress = managerDto.FullAddress.Trim();
            managerDto.Email = managerDto.Email.Trim();
            managerDto.Phone = managerDto.Phone.Trim();

            if (db.Users.Any(u => (u.Email == managerDto.Email || u.Phone == managerDto.Phone)
            && u.Id != id))
                return Results.Conflict("Почта или телефон есть в реестре");

            var manager = db.Users.OfType<Manager>().FirstOrDefault(c => c.Id == id);

            if (manager == null)
                return Results.NotFound();

            var (isValidAddress, error) = await geocoderValidate.ValidateRussianAddressAsync(managerDto.FullAddress);

            if (!isValidAddress)
                return Results.NotFound(error);

            var coords = await geocoding.GetCoordinatesAsync(managerDto.FullAddress);

            manager.Name = managerDto.Name.Trim();
            manager.Email = managerDto.Email;
            manager.Phone = managerDto.Phone;
            manager.PhotoIcon = managerDto.PhotoIcon;
            manager.Address = new ShortAddressDto()
            {
                FullAddress = managerDto.FullAddress,
                Latitude = decimal.Parse(coords.Latitude, CultureInfo.InvariantCulture),
                Longitude = decimal.Parse(coords.Longitude, CultureInfo.InvariantCulture),
            };

            await db.SaveChangesAsync();

            return Results.Ok(new ManagerResponseDto(manager));
        }
    }
}
