using DataBase;
using DataBase.Models;
using Backend.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.ComponentModel.DataAnnotations;
using Backend.Models.Dto;

namespace Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class ManagerController(PriazovContext db) : ControllerBase
    {
        private readonly PriazovContext _db = db;

        /// <summary>
        /// Выводит менеджера по его id
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetManager(Guid? id)
        {
            Manager? user = _db.Users.OfType<Manager>().FirstOrDefault((u) => u.Id == id);
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
        public IActionResult PostManager(ManagerCreateDto managerDto)
        {
            try
            {
                // получаем данные пользователя
                if (managerDto != null)
                {

                    //if (string.IsNullOrEmpty(managerDto.Name) ||
                    //!RegexUtilities.IsValidEmail(managerDto.Email) ||
                    //Regex.Match(managerDto.Phone, @"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$", RegexOptions.IgnoreCase).Success)
                    //{
                    //    throw new Exception("Некорректные данные");
                    //}
                    var manager = new Manager()
                    {
                        Name = managerDto.Name,
                        Email = managerDto.Email,
                        Password = new UserPassword()
                        {
                            PasswordHash = PasswordHasher.HashPassword(managerDto.Password),
                            LastUpdated = DateTime.UtcNow
                        },
                        Phone = managerDto.Phone,
                    };
                    // добавляем пользователя в список
                    _db.Users.AddAsync(manager);
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
                if (manager != null && RegexUtilities.IsValidEmail(manager.Email))
                {
                    // получаем данные пользователя из базы данных
                    Manager? user = _db.Users.OfType<Manager>().FirstOrDefault((u) => u.Id == id);
                    // если пользователь найден, изменяем его данные
                    if (user != null)
                    {
                        if (manager.Name == "" ||
                        !RegexUtilities.IsValidEmail(manager.Email) ||
                        Regex.Match(manager.Phone, @"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$", RegexOptions.IgnoreCase).Success)
                        {
                            throw new Exception("Некорректные данные");
                        }

                        user.Name = manager.Name;
                        user.Email = manager.Email;
                        user.Phone = manager.Phone;
                        user.Password = manager.Password;
                        _db.SaveChangesAsync();
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