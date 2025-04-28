using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public ActionResult<IEnumerable<MapMark>> GetDataMap()
        {
            return Ok(_db.MapMark.ToListAsync());
        }

        [NonAction]
        public void SetData()
        {
            List<MapMark> stations = new List<MapMark>();
            stations.Add(new MapMark()
            {
                Id = 1,
                PlaceName = "Точка 1",
                GeoLat = 37.610489,
                GeoLong = 55.752308,
            });
            stations.Add(new MapMark()
            {
                Id = 2,
                PlaceName = "Точка 2",
                GeoLat = 37.608644,
                GeoLong = 55.75226,
            });
        }
    }
}