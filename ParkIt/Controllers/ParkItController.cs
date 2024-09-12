using Microsoft.AspNetCore.Mvc;

namespace ParkIt.Controllers
{
    public class ParkItController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
