using Microsoft.AspNetCore.Mvc;
using ParkIt.Models;
using ParkIt.Models.Data;
using ParkIt.ViewModel;
using System.Diagnostics;

namespace ParkIt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ParkItDbContext _dbContext;
        public HomeController(ILogger<HomeController> logger, ParkItDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            int inactiveZoneCount = _dbContext.Zone.Count(z => !z.Active); 
            int activeRunners = _dbContext.Employee.Count(e => e.Active);
            int numberOfCars = _dbContext.Transactions.Count();
            var model = new DashboardModel
            {
                InactiveZoneCount = inactiveZoneCount,
                ActiveRunners = activeRunners,
                NumberOfCars = numberOfCars 
            };

            var username = HttpContext.Session.GetString("UserName");
            ViewBag.Username = username;
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
