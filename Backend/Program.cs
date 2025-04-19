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

var adminRole = new Role("admin");
var managerRole = new Role("manager");
var companyRole = new Role("company");

var builder = WebApplication.CreateBuilder();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

// ������������ DbContextFactory
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

// ��������� ��������������
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

//�������� ������� � ��������� ��
var factory = new DbContextFactory(builder.Configuration, "DefaultConnection");
var db = factory.CreateDbContext();

var managers = db.Users.OfType<Manager>();
var companies = db.Users.OfType<Company>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/managers/{id:Guid}", async (Guid id) =>
{
    // �������� ������������ �� id
    Manager? manager = await managers.FirstOrDefaultAsync(u => u.Id == id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (manager == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, ���������� ���
    return Results.Json(manager);
}).WithTags("Managers");

app.MapPost("/api/managers", async (Manager manager) =>
{
    manager.RoleName = managerRole.Name;
    manager.Password = PasswordHasher.HashPassword(manager.Password);
    // ��������� ������������ � ������
    await db.Users.AddAsync(manager);
    await db.SaveChangesAsync();
    return manager;
}).WithTags("Managers");

app.MapPut("/api/managers", async (Manager managerData) =>
{
    // �������� ������������ �� id
    var manager = await managers.FirstOrDefaultAsync(u => u.Id == managerData.Id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (manager == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, �������� ��� ������ � ���������� ������� �������
    manager.Name = managerData.Name;
    manager.Email = managerData.Email;
    manager.Phone = managerData.Phone;
    manager.Password = PasswordHasher.HashPassword(managerData.Password);
    manager.PhotoIcon = managerData.PhotoIcon;
    await db.SaveChangesAsync();
    return Results.Json(manager);
}).WithTags("Managers");

app.MapGet("/api/companies", async () =>
{
    return await companies.ToListAsync();
}).WithTags("Companies");

app.MapGet("/api/companies/{id:Guid}", async (Guid id) =>
{
    // �������� ������������ �� id
    Company? company = await companies.FirstOrDefaultAsync(c => c.Id == id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (company == null) return Results.NotFound(new { message = "�������� �� �������" });

    // ���� ������������ ������, ���������� ���
    return Results.Json(company);
}).WithTags("Companies");

app.MapPost("/api/companies", async (Company company) =>
{
    company.RoleName = companyRole.Name;
    company.Password = PasswordHasher.HashPassword(company.Password);
    // ��������� ������������ � ������
    await db.Users.AddAsync(company);
    await db.SaveChangesAsync();
    return company;
}).WithTags("Companies");


app.MapPut("/api/companies", async (Company companyData) =>
{
    // �������� ������������ �� id
    var company = await companies.FirstOrDefaultAsync(u => u.Id == companyData.Id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (company == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, �������� ��� ������ � ���������� ������� �������
    company.Name = companyData.Name;
    company.Email = companyData.Email;
    company.Phone = companyData.Phone;
    company.Password = PasswordHasher.HashPassword(companyData.Password);
    company.PhotoIcon = companyData.PhotoIcon;
    company.Projects = companyData.Projects;
    company.Address = companyData.Address;
    company.Description = companyData.Description;
    await db.SaveChangesAsync();
    return Results.Json(company);
}).WithTags("Companies");

app.MapPost("/login", async (User user, TokenService tokenService) =>
{
    var person = await db.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
    if (person == null || !PasswordHasher
        .VerifyPassword(user.Password, person.Password)) return Results.Unauthorized();
    var newAccessToken = tokenService.GenerateAccessToken(Convert.ToString(person.Id)!,
        person.Email, person.RoleName);
    var newRefreshToken = tokenService.GenerateRefreshToken(Convert.ToString(person.Id)!);

    //���������� � ��
    await db.Sessions.AddAsync(new UserSession()
    {
        RefreshToken = newRefreshToken,
        UserId = person.Id,
        User = person,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    });
    await db.SaveChangesAsync();
    return Results.Ok(new { AccessToken = newAccessToken, Email = person.Email });
});

app.MapPost("/refresh", async (RefreshRequest request, TokenService tokenService) =>
{
    // 1. ��������� Refresh Token
    var principal = tokenService.ValidateToken(request.RefreshToken, isAccessToken: false);
    if (principal == null)
        return Results.Unauthorized();

    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // 2. �������� � ��
    var session = await db.Sessions
        .FirstOrDefaultAsync(s => Convert.ToString(s.UserId) == userId && s.RefreshToken == request.RefreshToken);

    if (session == null || session.ExpiresAt < DateTime.UtcNow)
        return Results.Unauthorized();

    // 3. ��������� ����� �������
    var newAccessToken = tokenService.GenerateAccessToken(userId, session.User.Email, session.User.RoleName);
    var newRefreshToken = tokenService.GenerateRefreshToken(userId);

    // 4. ���������� � ��
    session.RefreshToken = newRefreshToken;
    session.ExpiresAt = DateTime.UtcNow.AddDays(7); // ��������� ����
    await db.SaveChangesAsync();

    return Results.Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
})
.WithName("RefreshToken")
.AllowAnonymous();

app.Run();