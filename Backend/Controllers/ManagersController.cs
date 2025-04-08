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

        /// <summary>
        /// Выводит менеджера по его id
        /// </summary>
        [HttpGet("{id}")]
        public async Task GetManager(Guid? id)
        {
            Manager? user = _db.Managers.FirstOrDefault((u) => u.Id == id);
            // если пользователь найден, отправляем его
            if (user != null)
            {
                _response.StatusCode = 200;
                await _response.WriteAsJsonAsync(user);
            }
            // если не найден, отправляем статусный код и сообщение об ошибке
            else
            {
                _response.StatusCode = 404;
                await _response.WriteAsJsonAsync(new { message = "Пользователь не найден" });
            }
        }

        /// <summary>
        /// Создаёт нового менеджера
        /// </summary>
        [HttpPost]
        public async Task PostManager()
        {
            try
            {
                // получаем данные пользователя
                var user = await _request.ReadFromJsonAsync<Manager>();
                if (user != null)
                {
                    // устанавливаем id для нового пользователя
                    user.Id = Guid.NewGuid();
                    // добавляем пользователя в список
                    await _db.Managers.AddAsync(user);
                    await _db.SaveChangesAsync();
                    await _response.WriteAsJsonAsync(user);
                }
                // если не найден, отправляем статусный код и сообщение об ошибке
                else
                {
                    throw new Exception("Некорректные данные");
                }
            }
            catch (Exception)
            {
                _response.StatusCode = 400;
                await _response.WriteAsJsonAsync(new { message = "Некорректные данные" });
            }
        }

        /// <summary>
        /// Изменяет менеджера по id
        /// </summary>
        [HttpPut("{id}")]
        public async Task PutManager(Guid? id)
        {
            try
            {
                // получаем данные пользователя
                var userData = await _request.ReadFromJsonAsync<Manager>();
                if (userData != null)
                {
                    // получаем данные пользователя из базы данных
                    Manager? user = _db.Managers.FirstOrDefault((u) => u.Id == id);
                    // если пользователь найден, изменяем его данные
                    if (user != null)
                    {
                        _response.StatusCode = 200;
                        user.Name = userData.Name;
                        user.Email = userData.Email;
                        user.Phone = userData.Phone;
                        await _db.SaveChangesAsync();
                        await _response.WriteAsJsonAsync(user);
                    }
                    // если не найден, отправляем статусный код и сообщение об ошибке
                    else
                    {
                        _response.StatusCode = 404;
                        await _response.WriteAsJsonAsync(new { message = "Пользователь не найден" });
                    }
                }
                // если не найден, отправляем статусный код и сообщение об ошибке
                else
                {
                    throw new Exception("Некорректные данные");
                }
            }
            catch (Exception)
            {
                _response.StatusCode = 400;
                await _response.WriteAsJsonAsync(new { message = "Некорректные данные" });
            }
        }
    }
}