using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
            group.MapGet("/filter", FilterCompanies);
            group.MapGet("/search", SearchCompanies);
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
            [FromQuery] string searchTerm = "",
            [FromQuery] int limit = 10) // Лимит результатов
        {
            var cacheKey = $"companies_search_{industries ?? "all"}_{searchTerm}_{limit}";

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

            // Применяем фильтр по индустрии если есть
            if (industryList?.Count > 0)
                query = query.Where(c => industryList.Contains(c.Industry));

            // Поиск по вхождению строки без учета регистра
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%"));

            // Сортировка и ограничение количества результатов
            var companies = await query
                .OrderBy(c => c.Name)
                .Take(limit)
                .ToListAsync();

            cache.Set(cacheKey, companies, CacheOptions);

            return Results.Ok(companies);
        }
    }
}