using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Backend;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.Data;
using System.ComponentModel.DataAnnotations;

var adminRole = new Role("admin");
var managerRole = new Role("manager");
var companyRole = new Role("company");

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

    var person = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

    if (person == null || !PasswordHasher
        .VerifyPassword(request.Password, person.Password.PasswordHash)) return Results.Unauthorized();

    var newAccessToken = tokenService.GenerateAccessToken(Convert.ToString(person.Id)!,
        person.Email, person.RoleName);
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
    return Results.Ok(new { AccessToken = newAccessToken, Email = person.Email });
});

app.MapPost("/refresh", async (RefreshRequest request, [FromServices] TokenService tokenService) =>
{
    // 1. Âàëèäàöèÿ Refresh Token
    var principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
    if (principal == null)
        return Results.Unauthorized();

    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // 2. Ïðîâåðêà â ÁÄ
    var session = await db.Sessions
        .FirstOrDefaultAsync(s => Convert.ToString(s.UserId) == userId && s.RefreshToken == request.RefreshToken);

    if (session == null || session.ExpiresAt < DateTime.UtcNow)
        return Results.Unauthorized();

    // 3. Ãåíåðàöèÿ íîâûõ òîêåíîâ
    var newAccessToken = tokenService.GenerateAccessToken(userId, session.User.Email, session.User.RoleName);
    var newRefreshToken = tokenService.GenerateRefreshToken(userId);

    // 4. Îáíîâëåíèå â ÁÄ
    session.RefreshToken = newRefreshToken;
    session.ExpiresAt = DateTime.UtcNow.AddDays(7); // Îáíîâëÿåì ñðîê
    await db.SaveChangesAsync();

    return Results.Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
})
.WithName("RefreshToken")
.AllowAnonymous();

// Program.cs
app.MapPost("/logout", async (HttpContext context) =>
{
    // 1. Ïîëó÷àåì refresh token èç çàïðîñà
    var refreshToken = context.Request.Headers["X-Refresh-Token"].ToString();

    if (string.IsNullOrEmpty(refreshToken))
        return Results.BadRequest("Refresh token is required");

    // 2. Íàõîäèì è óäàëÿåì ñåññèþ â ÁÄ
    var session = await db.Sessions
        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    if (session != null)
    {
        db.Sessions.Remove(session);
        await db.SaveChangesAsync();
    }

    return Results.Ok("Logged out successfully");
}).RequireAuthorization();

app.Run();

public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);
public record RefreshRequest(
    [Required] string RefreshToken
);
