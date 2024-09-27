using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ParkIt.Models;
using ParkIt.Models.Data;
using System.Diagnostics;

namespace ParkIt.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ParkItDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, ParkItDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {

            int activeZoneCount = _dbContext.Zone.Count(z => z.Active);
            int activeRunners = _dbContext.Employee.Count(e => e.Active);
            int numberOfCars = _dbContext.Transactions.Count();
            var totalIncome = _dbContext.Transactions.Sum(o => o.Fee);

            var username = HttpContext.Session.GetString("UserName");
            var zoneRankings = _dbContext.Zone
             .Select(zone => new
             {
                 ZoneName = zone.Zone_Name,
                 TransactionCount = _dbContext.Transactions.Count(t => t.Zone_ID == zone.Zone_ID)
             })
             .OrderByDescending(zr => zr.TransactionCount)
            .ToList();

            var allTransactions = _dbContext.Transactions
                  .Where(t => t.Status != "Completed") // Exclude transactions with status "Completed"
                  .Join(_dbContext.Zone,
                        t => t.Zone_ID,
                        z => z.Zone_ID,
                   (t, z) => new { t, z })
                  .Join(_dbContext.Employee,
                        tz => tz.t.Runner_Collect_ID,
                        v => v.Employee_ID,
                        (tz, v) => new { tz.t, tz.z, vCollect = v })
                  .Join(_dbContext.Employee,
                        tznv => tznv.t.Runner_Dispatch_ID,
                        v => v.Employee_ID,
                        (tznv, vDispatch) => new
                        {
                            ZoneName = tznv.z.Zone_Name,
                            Name = tznv.vCollect.Name,
                            Active = vDispatch.Active,
                            Status = tznv.t.Status,
                            CarArrivedAt = tznv.t.ArrivalTime
                        })
                  .ToList();

            // Check if the request is an AJAX request
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    activeZoneCount = activeZoneCount,
                    activeRunners = activeRunners,
                    numberOfCars = numberOfCars,
                    totalIncome = totalIncome,
                    username = username,
                    zoneRanking = zoneRankings,
                    alltransactions = allTransactions
                });
            }

            // Return the view for normal (non-AJAX) requests
            return View();
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
