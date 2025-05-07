using Backend.Models.Dto;
using DataBase.Models;
using DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;

namespace Backend.Mapping
{
    public static class ManagerEndpoints
    {
        public static void MapManagerEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/managers");
            group.MapPost("/create", Create);
            //group.MapGet("/review", Review);
            //group.MapGet("account", Account);
            //group.MapGet("/filter", FilterCompanies);
            //group.MapGet("/search", SearchCompanies);
            //group.MapPut("/change", Change);
        }

        private static async Task<IResult> Create(
            [FromBody] CompanyCreateDto managerDto,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IMemoryCache cache)
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

            if (db.Users.Any(u => u.Email == managerDto.Email || u.Phone == managerDto.Phone))
                return Results.Conflict("Почта или телефон есть в реесте");

            //var manager = new Manager()
            //{ 
                
            //}
            //await db.AddAsync(manager);
            //await db.SaveChangesAsync();

            cache.Remove("companies_review");
            return Results.Ok();
            //return Results.Ok(new CompanyResponseDto(manager));
        }
    }
}
