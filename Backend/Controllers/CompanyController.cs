using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly PriazovContext _db;

        public CompanyController(PriazovContext db)
        {
            _db = db;
        }

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
                if (company != null)
                {
                    // получаем данные компании из базы данных
                    Company? user = _db.Users.OfType<Company>().FirstOrDefault((u) => u.Id == id);
                    // если компания найдена, изменяем его данные
                    if (user != null)
                    {
                        user.Name = company.Name;
                        user.Email = company.Email;
                        user.Phone = company.Phone;
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