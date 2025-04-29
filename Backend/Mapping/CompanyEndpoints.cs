using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public static void MapCompanyEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/companies");
            group.MapGet("/filter", FilterCompanies);
            group.MapGet("/search", SearchCompanies);
        }

        private static async Task<IResult> FilterCompanies(
            [FromQuery] List<string>? industries,
            [FromServices] IDbContextFactory<PriazovContext> factory)
        {
            if (industries?.Count > 0 && industries.Any(i => !_allowedIndustries.Contains(i)))
                return Results.BadRequest("Недопустимые значения индустрий.");

            using var db = await factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().AsQueryable();

            if (industries?.Count > 0)
                query = query.Where(c => industries.Contains(c.Industry)).OrderBy(c => c.Name);

            var companies = await query.ToListAsync();
            return Results.Ok(companies);
        }

        private static async Task<IResult> SearchCompanies(
            [FromQuery] List<string>? industries,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromQuery] string searchTerm = "",
            [FromQuery] int limit = 10) // Лимит результатов
        {
            // Валидация индустрий
            if (industries?.Count > 0 && industries.Any(i => !_allowedIndustries.Contains(i)))
                return Results.BadRequest("Недопустимые значения индустрий.");

            using var db = await factory.CreateDbContextAsync();

            var query = db.Users.OfType<Company>().AsQueryable();

            // Применяем фильтр по индустрии если есть
            if (industries?.Count > 0)
                query = query.Where(c => industries.Contains(c.Industry));

            // Поиск по вхождению строки без учета регистра
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%"));

            // Сортировка и ограничение количества результатов
            var companies = await query
                .OrderBy(c => c.Name)
                .Take(limit)
                .ToListAsync();

            return Results.Ok(companies);
        }
    }
}