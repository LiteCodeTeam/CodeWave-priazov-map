using Backend.Models;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Backend.Mapping
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/auth");

            group.MapPost("/login", Login);
            group.MapPost("/refresh", RefreshToken);
            group.MapPost("/logout", Logout);
        }

        private static async Task<IResult> Login(
            LoginRequest request,
            [FromServices] TokenService tokenService,
            [FromServices] IDbContextFactory<PriazovContext> factory,
            [FromServices] IOptions<JwtSettings> jwtSettings)
        {
            await using var db = await factory.CreateDbContextAsync();

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest("Email and password are required");

            var person = await db.Users
                .Include(u => u.Password)
                .FirstOrDefaultAsync(u => u.Email == request.Email);
            if (person == null)
                return Results.Unauthorized();

            if (!PasswordHasher.VerifyPassword(request.Password, person.Password.PasswordHash))
                return Results.Unauthorized();

            var newAccessToken = tokenService.GenerateAccessToken(Convert.ToString(person.Id)!,
                person.Email, person.Role);
            var newRefreshToken = tokenService.GenerateRefreshToken(Convert.ToString(person.Id)!);

            await db.Sessions.Where(s => s.UserId == person.Id).ExecuteDeleteAsync();

            await db.Sessions.AddAsync(new UserSession()
            {
                RefreshToken = newRefreshToken,
                UserId = person.Id,
                User = person,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings.Value.RefreshTokenExpiryDays))
            });
            await db.SaveChangesAsync();

            return Results.Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken,
                person.Email }); // Статус 200
        }

        private static async Task<IResult> RefreshToken(
     RefreshRequest request,
     [FromServices] TokenService tokenService,
     [FromServices] IDbContextFactory<PriazovContext> factory,
     [FromServices] IOptions<JwtSettings> jwtSettings)
        {
            await using var db = await factory.CreateDbContextAsync();

            if (string.IsNullOrEmpty(request.RefreshToken))
                return Results.BadRequest("Refresh token is required");

            var principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
            if (principal == null)
                return Results.Unauthorized();

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var session = await db.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == Guid.Parse(userId));

            if (session == null || session.ExpiresAt < DateTime.UtcNow || session.RefreshToken != request.RefreshToken)
                return Results.Unauthorized();

            db.Sessions.Remove(session);
            await db.SaveChangesAsync();

            var newAccessToken = tokenService.GenerateAccessToken(userId, session.User.Email, session.User.Role);
            var newRefreshToken = tokenService.GenerateRefreshToken(userId);

            await db.Sessions.AddAsync(new UserSession
            {
                RefreshToken = newRefreshToken,
                UserId = Guid.Parse(userId),
                ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays)
            });

            await db.SaveChangesAsync();

            return Results.Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
        }

        private static async Task<IResult> Logout(
            RefreshRequest request,
            [FromServices] TokenService tokenService,
            [FromServices] IDbContextFactory<PriazovContext> factory)
        {
            await using var db = await factory.CreateDbContextAsync();

        if (string.IsNullOrEmpty(request.RefreshToken))
            return Results.BadRequest("Refresh token is required");

        var principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
        if (principal == null)
            return Results.Unauthorized();

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

        await db.Sessions.Where(s => Convert.ToString(s.UserId) == userId).ExecuteDeleteAsync();

        return Results.NoContent(); // Статус 204
        }
    }
    public record LoginRequest(
    [Required] string Email,
    [Required] string Password
    );
    public record RefreshRequest(
        [Required] string RefreshToken
    );
}
