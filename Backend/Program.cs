using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Backend;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.Data;
using System.ComponentModel.DataAnnotations;

var adminRole = new Role("Admin");
var managerRole = new Role("Manager");
var companyRole = new Role("Company");

var builder = WebApplication.CreateBuilder();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

// Ðåãèñòðèðóåì DbContextFactory
builder.Services.AddDbContextFactory<PriazovContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();


var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var smtp = builder.Configuration.GetSection("SMTP");
var accessTokenSecret = jwtSettings["AccessTokenSecret"]!;
var refreshTokenSecret = jwtSettings["RefreshTokenSecret"]!;

builder.Services.AddSingleton<TokenService>(new TokenService(
    accessTokenSecret,
    refreshTokenSecret,
    jwtSettings
));

// Íàñòðîéêà àóòåíòèôèêàöèè
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(accessTokenSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//Ñîçäàíèå ôàáðèêè è êîíòåêñòà áä
var factory = new DbContextFactory(builder.Configuration, "DefaultConnection");
var db = factory.CreateDbContext();

var managers = db.Users.OfType<Manager>();
var companies = db.Users.OfType<Company>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LoginRequest request, [FromServices] TokenService tokenService) =>
{
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

    //Îáíîâëåíèå â ÁÄ
    await db.Sessions.AddAsync(new UserSession()
    {
        RefreshToken = newRefreshToken,
        UserId = person.Id,
        User = person,
        ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["RefreshTokenExpiryDays"]))
    });
    await db.SaveChangesAsync();

    return Results.Ok(new { AccessToken = newAccessToken, Email = person.Email }); // Статус 200
});

app.MapPost("/refresh", async (RefreshRequest request, [FromServices] TokenService tokenService) =>
{
    if (string.IsNullOrEmpty(request.RefreshToken))
        return Results.BadRequest("Refresh token is required");

    ClaimsPrincipal? principal;
    try
    {
        principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
        if (principal == null)
            return Results.Unauthorized();
    }
    catch (SecurityTokenException ex)
    {
        return Results.BadRequest($"Invalid token: {ex.Message}");
    }


    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // 2. Ïðîâåðêà â ÁÄ
    var session = await db.Sessions
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => Convert.ToString(s.UserId) == userId);

    if (session == null || session.ExpiresAt < DateTime.UtcNow)
        return Results.Unauthorized();

    var newAccessToken = tokenService.GenerateAccessToken(userId, session.User.Email, session.User.Role);
    var newRefreshToken = tokenService.GenerateRefreshToken(userId);

    // 4. Îáíîâëåíèå â ÁÄ
    session.RefreshToken = newRefreshToken;
    session.ExpiresAt = DateTime.UtcNow.AddDays
        (Convert.ToDouble(jwtSettings["RefreshTokenExpiryDays"])); // Дата окончания resfresh-токена
    await db.SaveChangesAsync();

    return Results.Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken }); // Статус 200
})
.WithName("RefreshToken")
.AllowAnonymous();

// Program.cs
app.MapPost("/logout", async (RefreshRequest request, [FromServices] TokenService tokenService) =>
{
    if (string.IsNullOrEmpty(request.RefreshToken))
        return Results.BadRequest("Refresh token is required");

    ClaimsPrincipal? principal;
    try
    {
        principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
        if (principal == null)
            return Results.Unauthorized();
    }
    catch (SecurityTokenException ex)
    {
        return Results.BadRequest($"Invalid token: {ex.Message}");
    }

    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    await db.Sessions.Where(s => Convert.ToString(s.UserId) == userId).ExecuteDeleteAsync();

    await db.RevokedTokens.AddAsync(new RevokedToken
    {
        Token = request.RefreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["RefreshTokenExpiryDays"]))
    });
    await db.SaveChangesAsync();

    return Results.NoContent(); // Статус 204
});

app.MapPost("/forgot-password", async (ForgotPasswordRequest request, PriazovContext db) =>
{
    // 1. Находим пользователя по email
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null)
        return Results.Ok(); // Не сообщаем, что email не найден (безопасность)

    // 2. Генерируем токен и сохраняем его
    var token = new PasswordResetToken
    {
        Token = Guid.NewGuid().ToString() + Guid.NewGuid().ToString(), // 32 символа
        UserId = user.Id,
        ExpiresAt = DateTime.UtcNow.AddHours(1)
    };
    db.PasswordResetTokens.Add(token);
    await db.SaveChangesAsync();

    // 3. Отправляем email со ссылкой (реализуйте свой EmailService)
    var resetLink = $"http://localhost:5145/reset-password?token={token.Token}";
    await EmailService.SendPasswordResetEmail(user.Email, resetLink, smtp);

    return Results.Ok();
});

app.MapGet("/reset-password", (string token) =>
{
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
});

app.MapPost("/reset-password", async (ResetPasswordRequest request, PriazovContext db) =>
{
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
    user.Password.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
    user.Password.LastUpdated = DateTime.UtcNow;

    // 4. Удаляем токен (одноразовый)
    db.PasswordResetTokens.Remove(token);
    await db.SaveChangesAsync();

    return Results.Ok("Пароль успешно изменён.");
});

app.Run();

public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);
public record RefreshRequest(
    [Required] string RefreshToken
);

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
