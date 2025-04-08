using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//�������� ������� � ��������� ��
var factory = new DbContextFactory(builder.Configuration, "DefaultConnection");
var db = factory.CreateDbContext();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/users", async () =>
{ 
    return await db.Managers.ToListAsync();
});

app.MapGet("/api/users/{id:Guid}", async (Guid id) =>
{
    // �������� ������������ �� id
    Manager? user = await db.Managers.FirstOrDefaultAsync(u => u.Id == id);

    // ���� �� ������, ���������� ��������� ��� � ��������� �� ������
    if (user == null) return Results.NotFound(new { message = "������������ �� ������" });

    // ���� ������������ ������, ���������� ���
    return Results.Json(user);
});

app.MapPost("/api/users", async (Manager user) =>
{
    // ��������� ������������ � ������
    await db.Managers.AddAsync(user);
    await db.SaveChangesAsync();
    return user;
});

app.MapPut("/api/users", async (Manager userData) =>
{
    // �������� ������������ �� id
    var user = await db.Managers.FirstOrDefaultAsync(u => u.Id == userData.Id);

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