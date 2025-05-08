using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Collections.Specialized.BitVector32;

namespace GoogleMaps.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class MapController : Controller
    {
        private readonly PriazovContext _db;

        public MapController(PriazovContext db)
        {
            _db = db;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ShortAddressDto>> GetDataMap()
        {
            return Ok(_db.Addresses.Select(m => new ShortAddressDto()
                { 
                    
                }).ToList());
        }

        [NonAction]
        public void SetData()
        {
            List<ShortAddressDto> stations = new List<ShortAddressDto>();
            //stations.Add(new MapMark()
            //{
            //    PlaceName = "Точка 1",
            //    GeoLat = (decimal)37.610489,
            //    GeoLong = (decimal)55.752308,
            //});
            //stations.Add(new MapMark()
            //{
            //    PlaceName = "Точка 2",
            //    GeoLat = (decimal)37.608644,
            //    GeoLong = (decimal)55.75226,
            //});
        }
    }
}