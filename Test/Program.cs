using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();

// ïîëó÷àåì ñòðîêó ïîäêëþ÷åíèÿ èç ôàéëà êîíôèãóðàöèè
string? connection = builder.Configuration.GetConnectionString("DefaultConnection");

// äîáàâëÿåì êîíòåêñò ApplicationContext â êà÷åñòâå ñåðâèñà â ïðèëîæåíèå
builder.Services.AddDbContext<PriazovContext>(options => options.UseNpgsql(connection));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/users", async (PriazovContext db) => await db.Users.ToListAsync());

app.MapGet("/api/users/{id:int}", async (Guid id, PriazovContext db) =>
{
    // ïîëó÷àåì ïîëüçîâàòåëÿ ïî id
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // åñëè íå íàéäåí, îòïðàâëÿåì ñòàòóñíûé êîä è ñîîáùåíèå îá îøèáêå
    if (user == null) return Results.NotFound(new { message = "Ïîëüçîâàòåëü íå íàéäåí" });

    // åñëè ïîëüçîâàòåëü íàéäåí, îòïðàâëÿåì åãî
    return Results.Json(user);
});

app.MapDelete("/api/users/{id:int}", async (Guid id, PriazovContext db) =>
{
    // ïîëó÷àåì ïîëüçîâàòåëÿ ïî id
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // åñëè íå íàéäåí, îòïðàâëÿåì ñòàòóñíûé êîä è ñîîáùåíèå îá îøèáêå
    if (user == null) return Results.NotFound(new { message = "Ïîëüçîâàòåëü íå íàéäåí" });

    // åñëè ïîëüçîâàòåëü íàéäåí, óäàëÿåì åãî
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Json(user);
});

app.MapPost("/api/users", async (User user, PriazovContext db) =>
{
    // äîáàâëÿåì ïîëüçîâàòåëÿ â ìàññèâ
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return user;
});

app.MapPut("/api/users", async (User userData, PriazovContext db) =>
{
    // ïîëó÷àåì ïîëüçîâàòåëÿ ïî id
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userData.Id);

    // åñëè íå íàéäåí, îòïðàâëÿåì ñòàòóñíûé êîä è ñîîáùåíèå îá îøèáêå
    if (user == null) return Results.NotFound(new { message = "Ïîëüçîâàòåëü íå íàéäåí" });

    // åñëè ïîëüçîâàòåëü íàéäåí, èçìåíÿåì åãî äàííûå è îòïðàâëÿåì îáðàòíî êëèåíòó
    user.Name = userData.Name;
    user.Email = userData.Email;
    user.Phone = userData.Phone;
    await db.SaveChangesAsync();
    return Results.Json(user);
});

app.Run();