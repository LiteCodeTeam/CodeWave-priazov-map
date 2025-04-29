using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Backend.Validation;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class CompanyController(PriazovContext db) : ControllerBase
    {
        private readonly PriazovContext _db = db;

        /// <summary>
        /// Выводит список компаний
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<Company>> GetCompany()
        {
            return Ok(_db.Users.OfType<Company>().ToListAsync());
        }

        /// <summary>
        /// Выводит компанию по её id
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetCompany(Guid? id)
        {
            Company? user = _db.Users.OfType<Company>().FirstOrDefault((u) => u.Id == id);
            // если компания найдена, отправляем её
            if (user != null)
            {
                return Ok(user);
            }
            // если не найдена, отправляем статусный код и сообщение об ошибке
            else
            {
                return NotFound(new { message = "Компания не найдена" });
            }
        }

        /// <summary>
        /// Создаёт новую компанию
        /// </summary>
        [HttpPost]
        public IActionResult PostCompany(Company company)
        {
            try
            {
                if (company != null)
                {

                    if (company.Name == "" ||
                    !RegexUtilities.IsValidEmail(company.Email) || 
                    Regex.Match(company.Phone, @"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$", RegexOptions.IgnoreCase).Success)
                    {
                        throw new Exception("Некорректные данные");
                    }

                    // добавляем компанию в список
                    _db.Users.AddAsync(company);
                    _db.SaveChangesAsync();
                    return Ok(company);
                }
                // если не найдена, отправляем статусный код и сообщение об ошибке
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
        /// Изменяет компанию по id
        /// </summary>
        [HttpPut("{id}")]
        public IActionResult PutCompany(Guid? id, Company company)
        {
            try
            {
                if (company != null && RegexUtilities.IsValidEmail(company.Email))
                {
                    // получаем данные компании из базы данных
                    Company? user = _db.Users.OfType<Company>().FirstOrDefault((u) => u.Id == id);
                    // если компания найдена, изменяем его данные
                    if (user != null)
                    {

                        if (company.Name == "" ||
                        !RegexUtilities.IsValidEmail(company.Email) ||
                        Regex.Match(company.Phone, @"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$", RegexOptions.IgnoreCase).Success)
                        {
                            throw new Exception("Некорректные данные");
                        }

                        user.Name = company.Name;
                        user.Email = company.Email;
                        user.Phone = company.Phone;
                        user.Password = company.Password;
                        _db.SaveChangesAsync();
                        return Ok(user);
                    }
                    // если не найдена, отправляем статусный код и сообщение об ошибке
                    else
                    {
                        return NotFound(new { message = "Компания не найден" });
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
                return BadRequest(new { message = "Некорректные данные" });
            }
        }
    }
}