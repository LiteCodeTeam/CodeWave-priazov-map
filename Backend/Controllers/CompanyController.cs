using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class CompanyController : Controller
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private readonly PriazovContext _db;

        public CompanyController(HttpContext context, PriazovContext db)
        {
            _request = context.Request;
            _response = context.Response;
            _db = db;
        }

        /// <summary>
        /// Выводит список компаний
        /// </summary>
        [HttpGet]
        public async Task GetCompany()
        {
            await _db.Users.OfType<Company>().ToListAsync();
        }

        /// <summary>
        /// Выводит компанию по её id
        /// </summary>
        [HttpGet("{id}")]
        public async Task GetCompany(Guid? id)
        {
            Company? user = _db.Users.OfType<Company>().FirstOrDefault((u) => u.Id == id);
            // если компания найдена, отправляем её
            if (user != null)
            {
                _response.StatusCode = 200;
                await _response.WriteAsJsonAsync(user);
            }
            // если не найдена, отправляем статусный код и сообщение об ошибке
            else
            {
                _response.StatusCode = 404;
                await _response.WriteAsJsonAsync(new { message = "Компания не найдена" });
            }
        }

        /// <summary>
        /// Создаёт новую компанию
        /// </summary>
        [HttpPost]
        public async Task PostCompany()
        {
            try
            {
                // получаем данные компании
                var user = await _request.ReadFromJsonAsync<Company>();
                if (user != null)
                {
                    // устанавливаем id для новой компании
                    user.Id = Guid.NewGuid();
                    // добавляем компанию в список
                    await _db.Users.AddAsync(user);
                    await _db.SaveChangesAsync();
                    await _response.WriteAsJsonAsync(user);
                }
                // если не найдена, отправляем статусный код и сообщение об ошибке
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
        /// Изменяет компанию по id
        /// </summary>
        [HttpPut("{id}")]
        public async Task PutCompany(Guid? id)
        {
            try
            {
                // получаем данные компании
                var userData = await _request.ReadFromJsonAsync<Company>();
                if (userData != null)
                {
                    // получаем данные компании из базы данных
                    Company? user = _db.Users.OfType<Company>().FirstOrDefault((u) => u.Id == id);
                    // если компания найдена, изменяем его данные
                    if (user != null)
                    {
                        _response.StatusCode = 200;
                        user.Name = userData.Name;
                        user.Email = userData.Email;
                        user.Phone = userData.Phone;
                        await _db.SaveChangesAsync();
                        await _response.WriteAsJsonAsync(user);
                    }
                    // если не найдена, отправляем статусный код и сообщение об ошибке
                    else
                    {
                        _response.StatusCode = 404;
                        await _response.WriteAsJsonAsync(new { message = "Компания не найден" });
                    }
                }
                // если не найдена, отправляем статусный код и сообщение об ошибке
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