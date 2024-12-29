using Microsoft.AspNetCore.Mvc;
using webMVC.Services;

namespace webMVC.Controllers
{
    public class PlanetController : Controller
    {
       private readonly PlanetService _planetService;
        private readonly ILogger<PlanetController > _logger;

        public PlanetController(PlanetService planetService, ILogger<PlanetController> logger)
        {
            _planetService = planetService;
            _logger = logger;               
        }



        [Route("Trang-Chu")] // Chịu ảnh hưởng tuyệt đối
        [Route("Trang-Chu/[action]", Name = "route1", Order = 1)] // Chịu ảnh hưởng tuyệt đối
        [Route("Trang-Chu/[controller]", Name = "route2", Order = 3)] // Chịu ảnh hưởng tuyệt đối
        public ActionResult Index()
        {
            return View();
        }
                
        [HttpGet]
        public IActionResult PlanetInfo(int id) {
            // if (string.IsNullOrEmpty(id)) return View();

            var planet = _planetService.Where(p => p.Id == id).FirstOrDefault();

            return View("Detail", planet);
        }
    }
}
