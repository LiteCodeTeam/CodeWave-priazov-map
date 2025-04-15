using DataBase;
using DataBase.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Backend.Models;

var adminRole = new Role("admin");
var managerRole = new Role("manager");
var companyRole = new Role("company");

var builder = WebApplication.CreateBuilder();

//Swagger ����� ������������ � �������, �� ���� ��� ��� ����� ���� ���� ��� ����� backend,
//����� � ����������� postman
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

// ������������ DbContextFactory
builder.Services.AddDbContextFactory<PriazovContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// ��� �������������� ������� � ��������� � ����������
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

//Console.WriteLine(jwtSettings.Issuer);
//Console.WriteLine(jwtSettings.Audience);
//Console.WriteLine(jwtSettings.SecretKey);

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuerSigningKey = true
        };
    });

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

//�������� ������� � ��������� ��
var factory = new DbContextFactory(builder.Configuration, "DefaultConnection");
var db = factory.CreateDbContext();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/api/managers/{id:Guid}", async (Guid id) =>
{
    // �������� ������������ �� id
    Manager? manager = await db.Managers.FirstOrDefaultAsync(u => u.Id == id);

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
    await db.Managers.AddAsync(manager);
    await db.SaveChangesAsync();
    return manager;
}).WithTags("Managers");

app.MapPut("/api/managers", async (Manager managerData) =>
{
    // �������� ������������ �� id
    var manager = await db.Managers.FirstOrDefaultAsync(u => u.Id == managerData.Id);

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

app.MapPost("/login/managers", (Manager manager) =>
{
    // ������� ������������ 
    Manager? person = db.Managers.FirstOrDefault(p => p.Email == manager.Email);
    // ���� ������������ �� ������, ���������� ��������� ��� 401
    if (person is null) return Results.Unauthorized();
    if (!PasswordHasher.VerifyPassword(manager.Password, person.Password)) return Results.Unauthorized();

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, person.Email) };
    // ������� JWT-�����
    var jwt = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(60)),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            SecurityAlgorithms.HmacSha256));
    var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

    // ��������� �����
    var response = new
    {
        access_token = encodedJwt,
        email = person.Email
    };

    return Results.Json(response);
}).WithTags("Managers");

app.MapGet("/api/companies", async () =>
{
    return await db.Companies.ToListAsync();
}).WithTags("Companies");

app.MapGet("/api/companies/{id:Guid}", async (Guid id) =>
{
    // �������� ������������ �� id
    Company? company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id);

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
    await db.Companies.AddAsync(company);
    await db.SaveChangesAsync();
    return company;
}).WithTags("Companies");

app.MapPut("/api/companies", async (Company companyData) =>
{
    // �������� ������������ �� id
    var company = await db.Companies.FirstOrDefaultAsync(u => u.Id == companyData.Id);

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

app.MapPost("/login/companies", (Company company) =>
{
    // ������� ������������ 
    Company? person = db.Companies.FirstOrDefault(p => p.Email == company.Email);
    // ���� ������������ �� ������, ���������� ��������� ��� 401
    if (person is null) return Results.Unauthorized();
    if (!PasswordHasher.VerifyPassword(company.Password, person.Password)) return Results.Unauthorized();

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, person.Email) };
    // ������� JWT-�����
    var jwt = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(60)),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            SecurityAlgorithms.HmacSha256));
    var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

    // ��������� �����
    var response = new
    {
        access_token = encodedJwt,
        email = person.Email
    };

    return Results.Json(response);
}).WithTags("Companies");

app.Run();