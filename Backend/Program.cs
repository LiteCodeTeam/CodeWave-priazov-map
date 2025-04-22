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
    if (person == null)
        return Results.Unauthorized();

    var password = await db.Password.FirstOrDefaultAsync(p => p.UserId == person.Id);
    if (password == null)
        return Results.Unauthorized();

    //if (!PasswordHasher.VerifyPassword(request.Password, password.PasswordHash))
    //    return Results.Unauthorized();

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
        .FirstOrDefaultAsync(s => Convert.ToString(s.UserId) == userId);
    var user = await db.Users
        .FirstOrDefaultAsync(s => Convert.ToString(s.Id) == userId);

    if (session == null || session.ExpiresAt < DateTime.UtcNow)
        return Results.Unauthorized();

    var newAccessToken = tokenService.GenerateAccessToken(userId, user.Email, user.Role);
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

app.Run();

public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);
public record RefreshRequest(
    [Required] string RefreshToken
);
