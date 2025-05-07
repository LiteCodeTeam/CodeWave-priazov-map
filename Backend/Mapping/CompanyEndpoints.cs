using Backend.Models.Dto;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Org.BouncyCastle.Asn1.Ocsp;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Backend.Mapping
{
    public static class CompanyEndpoints
    {
        private static readonly HashSet<string> _allowedIndustries = new()
        {
            "Образовательное учреждение",
            "Научно-исследовательский институт",
            "Научно-образовательный проект",
            "Государственное учреждение",
            "Коммерческая деятельность",
            "Стартап",
            "Финансы",
            "Акселератор / инкубатор / технопарк",
            "Ассоциация / объединение",
            "Инициатива",
            "Отраслевое событие / научная конференция",
            "Другое"
        };
        private static readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };


        public static void MapCompanyEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/companies");
            group.MapPost("/create", Create);
            group.MapGet("/review", Review);
            group.MapGet("account", Account);
            group.MapGet("/filter", FilterCompanies);
            group.MapGet("/search", SearchCompanies);
            group.MapPut("/change", Change);
        }

        private static async Task<IResult> Create(CompanyCreateDto companyDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                companyDto,
                new ValidationContext(companyDto),
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

            if (!_allowedIndustries.Any(i => i == companyDto.Industry))
                return Results.BadRequest("Недопустимое значение индустрии");

            if (Zxcvbn.Core.EvaluatePassword(companyDto.Password).Score < 3)
                return Results.BadRequest("Слабый пароль");

            using var db = await factory.CreateDbContextAsync();

            if (db.Users.Any(u => u.Email == companyDto.Email || u.Phone == companyDto.Phone))
                return Results.Conflict("Почта или телефон есть в реесте");

            var company = new Company()
            {
                Name = companyDto.Name,
                Email = companyDto.Email,
                Password = new UserPassword()
                {
                    PasswordHash = PasswordHasher.HashPassword(companyDto.Password),
                    LastUpdated = DateTime.UtcNow
                },
                Phone = companyDto.Phone,
                FullAddress = companyDto.FullAddress,
                Industry = companyDto.Industry,
                LeaderName = companyDto.LeaderName
            };
            await db.Users.AddAsync(company);
            await db.SaveChangesAsync();

            cache.Remove("companies_review");
            return Results.Ok(new CompanyResponseDto(company));
        }

        private static async Task<IResult> Review(
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache)
        {
            var cacheKey = $"companies_review";

            if (cache.TryGetValue(cacheKey, out List<Company>? cachedCompanies))
                return Results.Ok(cachedCompanies);

            using var db = await factory.CreateDbContextAsync();
            
            var query = await db.Users.OfType<Company>()
                .AsQueryable()
                .OrderBy(c => c.Name)
                .Take(5)
                .Select(c => new CompanyResponseDto(c))
                .ToListAsync();

            var count = await db.Users.OfType<Company>().CountAsync() - query.Count;

            cache.Set(cacheKey, query, CacheOptions);

            return Results.Ok(new { query, count });
        }

        public static async Task<IResult> Account([FromQuery] Guid? id,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache)
        {
            if (id == null)
                return Results.BadRequest("Id пуст");

            var cacheKey = $"companies_{id}";
            if (cache.TryGetValue(cacheKey, out CompanyResponseDto? cachedCompany))
                return Results.Ok(cachedCompany);

            using var db = await factory.CreateDbContextAsync();

            var company = await db.Users.OfType<Company>().FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
                return Results.NotFound();

            var companyResponse = new CompanyResponseDto(company);

            cache.Set(cacheKey, companyResponse, CacheOptions);

            return Results.Ok(companyResponse);
        }

        private static async Task<IResult> FilterCompanies(
            [FromQuery] string? industries,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache)
        {
            var cacheKey = $"companies_filter_{industries ?? "all"}";

            if (cache.TryGetValue(cacheKey, out List<Company>? cachedCompanies))
                return Results.Ok(cachedCompanies);

            List<string>? industryList = null;
            if (!string.IsNullOrEmpty(industries))
                industryList = industries.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim())
                                        .ToList();

            if (industryList?.Count > 0 && industryList.Any(i => !_allowedIndustries.Contains(i)))
                return Results.BadRequest("Недопустимые значения индустрий.");

            using var db = await factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().AsQueryable();

            if (industryList?.Count > 0)
                query = query.Where(c => industryList.Contains(c.Industry));

            var companies = await query.OrderBy(c => c.Name).ToListAsync();

            cache.Set(cacheKey, companies, CacheOptions);
            return Results.Ok(companies);
        }

        private static async Task<IResult> SearchCompanies(
            [FromQuery] string? industries,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache,
            [FromQuery] string searchTerm = "")
        {
            var cacheKey = $"companies_search_{industries ?? "all"}_{searchTerm}";

            if (cache.TryGetValue(cacheKey, out List<Company>? cachedCompanies))
                return Results.Ok(cachedCompanies);

            List<string>? industryList = null;
            if (!string.IsNullOrEmpty(industries))
                industryList = industries.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim())
                                        .ToList();

            // Валидация индустрий
            if (industryList?.Count > 0 && industryList.Any(i => !_allowedIndustries.Contains(i)))
                return Results.BadRequest("Недопустимые значения индустрий.");

            using var db = await factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().AsQueryable();

            if (industryList?.Count > 0)
                query = query.Where(c => industryList.Contains(c.Industry));

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%"));

            var companies = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            cache.Set(cacheKey, companies, CacheOptions);

            return Results.Ok(companies);
        }

        public static async Task<IResult> Change([FromQuery] Guid? id,
            [FromBody] CompanyResponseDto companyDto,
            [FromServices] IDbContextFactory<PriazovContext> factory)
        {
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                companyDto,
                new ValidationContext(companyDto),
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

            if (!_allowedIndustries.Any(i => i ==  companyDto.Industry))
                return Results.BadRequest("Недопустимое значение индустрии");

            using var db = await factory.CreateDbContextAsync();

            if (db.Users.Any(u => (u.Email == companyDto.Email || u.Phone == companyDto.Phone)
            && u.Id != id))
                return Results.Conflict("Почта или телефон есть в реестре");

            var company = db.Users.OfType<Company>().FirstOrDefault(c => c.Id == id);

            if (company == null)
                return Results.NotFound();

            company.Name = companyDto.Name;
            company.Email = companyDto.Email;
            company.Phone = companyDto.Phone;
            company.Industry = companyDto.Industry;
            company.PhotoIcon = companyDto.PhotoIcon;
            company.PhotoHeader = companyDto.PhotoHeader;
            company.FullAddress = companyDto.FullAddress;
            company.LeaderName = companyDto.LeaderName;
            company.Description = companyDto.Description;
            company.Contacts = companyDto.Contacts;

            await db.SaveChangesAsync();

            return Results.Ok(companyDto);
        }
    }
}