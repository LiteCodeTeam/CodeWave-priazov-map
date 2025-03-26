using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace Test.Controllers;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    public PriazovContext Db { get; }

    public HomeController(PriazovContext context)
    {
        Db = context;
        Db.Database.Migrate();
    }
    [HttpPost]
    [Route("Post")]
    public async Task<IActionResult> CreateUser(User user)
    {
        await Db.Users.AddAsync(user);
        await Db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
    [HttpGet]
    [Route("Get")]
    public async Task<ICollection> Index()
    {
        return await Db.Users.ToListAsync<User>();
    }
}
