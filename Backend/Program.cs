using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var adminRole = new Role("admin");
var managerRole = new Role("manager");
var companyRole = new Role("company");

var builder = WebApplication.CreateBuilder();

//Swagger может понадобитьс€ в будущем, но пока что оно нужно было лишь дл€ теста backend,
//Ћично € использовал postman
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

// –егистрируем DbContextFactory
builder.Services.AddDbContextFactory<PriazovContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//—оздание фабрики и контекста бд
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
    // получаем пользовател€ по id
    Manager? user = await db.Managers.FirstOrDefaultAsync(u => u.Id == id);

    // если не найден, отправл€ем статусный код и сообщение об ошибке
    if (user == null) return Results.NotFound(new { message = "ѕользователь не найден" });

    // если пользователь найден, отправл€ем его
    return Results.Json(user);
});

app.MapPost("/api/users", async (Manager user) =>
{
    // добавл€ем пользовател€ в массив
    await db.Managers.AddAsync(user);
    await db.SaveChangesAsync();
    return user;
});

app.MapPut("/api/users", async (Manager userData) =>
{
    // получаем пользовател€ по id
    var user = await db.Managers.FirstOrDefaultAsync(u => u.Id == userData.Id);

    // если не найден, отправл€ем статусный код и сообщение об ошибке
    if (user == null) return Results.NotFound(new { message = "ѕользователь не найден" });

    // если пользователь найден, измен€ем его данные и отправл€ем обратно клиенту
    user.Name = userData.Name;
    user.Email = userData.Email;
    user.Phone = userData.Phone;
    await db.SaveChangesAsync();
    return Results.Json(user);
});

app.Run();