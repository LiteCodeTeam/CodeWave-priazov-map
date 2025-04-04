using DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        [HttpGet]
        public HttpResponse GetUsers()
        {
            ;

            return Results.Json();
        }
    }
}
