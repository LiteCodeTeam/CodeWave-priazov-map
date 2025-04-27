using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Mapping
{
    public static class PasswordEndpoints
    {
        public static void MapPasswordEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/password");

            group.MapPost("/forgot-password", ForgotPassword);
            group.MapPost("/reset-password", PostResetPassword);
            group.MapGet("/reset-password", GetResetPassword);
        }
        private static async Task<IResult> ForgotPassword(ForgotPasswordRequest request,
            [FromServices] EmailService emailService,
            [FromServices] IDbContextFactory<PriazovContext> factory)
        {
            await using var db = await factory.CreateDbContextAsync();

            // 1. Находим пользователя по email
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Results.Ok(); // Не сообщаем, что email не найден (безопасность)

            // 2. Генерируем токен и сохраняем его
            var token = new PasswordResetToken
            {
                Token = Guid.NewGuid().ToString()[..6],
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            var update = await db.PasswordResetTokens
                .Where(t => t.UserId == user.Id)
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Token, token.Token)
                .SetProperty(t => t.ExpiresAt, token.ExpiresAt));

            if (update == 0)
            {
                db.PasswordResetTokens.Add(token);
                await db.SaveChangesAsync();
            }

            // 3. Отправляем email со ссылкой (реализуйте свой EmailService)
            await emailService.SendPasswordResetEmail(user.Email, token.Token);

            return Results.Ok();
        }
        private static async Task<IResult> PostResetPassword(ResetPasswordRequest request,
            [FromServices] EmailService emailService,
            [FromServices] IDbContextFactory<PriazovContext> factory)
        {
            await using var db = await factory.CreateDbContextAsync();

            var token = await db.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == request.Token && t.ExpiresAt > DateTime.UtcNow);
            if (token == null)
                return Results.BadRequest("Недействительный или просроченный токен.");

            var user = await db.Users
                .Include(u => u.Password)
                .FirstOrDefaultAsync(u => u.Id == token.UserId);
            if (user == null)
                return Results.NotFound("Пользователь не найден.");

            // 3. Хешируем новый пароль и обновляем
            user.Password.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.Password.LastUpdated = DateTime.UtcNow;

            db.PasswordResetTokens.Remove(token);
            await db.SaveChangesAsync();
            await emailService.SendPasswordOkayEmail(user.Email);

            return Results.Ok("Пароль успешно изменён.");
        }
        private static async Task<IResult> GetResetPassword(string token,
            [FromServices] IDbContextFactory<PriazovContext> factory)
        {
            await using var db = await factory.CreateDbContextAsync();

            // Проверяем валидность токена
            var isValid = db.PasswordResetTokens.Any(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);

            if (!isValid)
                return Results.BadRequest("Недействительная или просроченная ссылка");

            return Results.Content(
                $"""
               <form method="post" action="/reset-password">
                   <input type="hidden" name="token" value="{token}">
                   <input type="password" name="newPassword" placeholder="Password" required>
                   <input type="password" name="confirmPassword" placeholder="Repeat Password" required>
                   <button type="submit">Save</button>
               </form>
               """, "text/html");
        }
    }
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Token, string NewPassword);
}
