using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog.Config;

var builder = WebApplication.CreateBuilder();
var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();
builder.Services.AddDbContextFactory<PriazovContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/users", async (PriazovContext db) => await db.Users.ToListAsync());

app.MapGet("/api/users/{id:int}", async (Guid id, IDbContextFactory<PriazovContext> factory) =>
{
    using var db = factory.CreateDbContext();
    // �������� ������������ �� id
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (user == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, ���������� ���
    return Results.Json(user);
});

app.MapPost("/api/users", async (User user, IDbContextFactory<PriazovContext> factory) =>
{
    using var db = factory.CreateDbContext();
    // ��������� ������������ � ������
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return user;
});

app.MapPut("/api/users", async (User userData, IDbContextFactory<PriazovContext> factory) =>
{
    using var db = factory.CreateDbContext();
    // �������� ������������ �� id
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userData.Id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (user == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, �������� ��� ������ � ���������� ������� �������
    user.Name = userData.Name;
    user.Email = userData.Email;
    user.Phone = userData.Phone;
    await db.SaveChangesAsync();
    return Results.Json(user);
});

app.Run();