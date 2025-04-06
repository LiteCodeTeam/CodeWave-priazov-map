using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class ManagerController : Controller
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private readonly PriazovContext _db;

        public ManagerController(HttpContext context, PriazovContext db)
        {
            _request = context.Request;
            _response = context.Response;
            _db = db;
        }

        [HttpGet("{id}")]
        public async Task GetManager(Guid? id)
        {
            await GetManagerAsync(id, _response);
        }

        [NonAction]
        async Task GetManagerAsync(Guid? id, HttpResponse response)
        {
            // получаем пользователя по id
            Manager? user = _db.Managers.FirstOrDefault((u) => u.Id == id);
            // если пользователь найден, отправляем его
            if (user != null)
            {
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(user);
            }
            // если не найден, отправляем статусный код и сообщение об ошибке
            else
            {
                response.StatusCode = 404;
                await response.WriteAsJsonAsync(new { message = "Пользователь не найден" });
            }
        }
    }
}