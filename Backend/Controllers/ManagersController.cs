using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private readonly PriazovContext _db;

        public ManagerController(PriazovContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Выводит менеджера по его id
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetManager(Guid? id)
        {
            Manager? user = _db.Managers.FirstOrDefault((u) => u.Id == id);
            // если пользователь найден, отправляем его
            if (user != null)
            {
                return Ok(user);
            }
            // если не найден, отправляем статусный код и сообщение об ошибке
            else
            {
                return NotFound(new { message = "Пользователь не найден" });
            }
        }

        /// <summary>
        /// Создаёт нового менеджера
        /// </summary>
        [HttpPost]
        public IActionResult PostManager(Manager manager)
        {
            try
            {
                // получаем данные пользователя
                if (manager != null)
                {
                    // добавляем пользователя в список
                    _db.Managers.AddAsync(manager);
                    _db.SaveChangesAsync();
                    return Ok(manager);
                }
                // если не найден, отправляем статусный код и сообщение об ошибке
                else
                {
                    throw new Exception("Некорректные данные");
                }
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Некорректные данные" });
            }
        }

        /// <summary>
        /// Изменяет менеджера по id
        /// </summary>
        [HttpPut("{id}")]
        public IActionResult PutManager(Guid? id, Manager manager)
        {
            try
            {
                if (manager != null)
                {
                    // получаем данные пользователя из базы данных
                    Manager? user = _db.Managers.FirstOrDefault((u) => u.Id == id);
                    // если пользователь найден, изменяем его данные
                    if (user != null)
                    {
                        user.Name = manager.Name;
                        user.Email = manager.Email;
                        user.Phone = manager.Phone;
                        _db.SaveChanges();
                        return Ok(user);
                    }
                    // если не найден, отправляем статусный код и сообщение об ошибке
                    else
                    {
                        return NotFound(new { message = "Пользователь не найден" });
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
                return BadRequest(new { message = "Некорректные данные" });
            }
        }
    }
}