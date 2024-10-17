using Microsoft.AspNetCore.Mvc;

namespace ParkIt.Controllers
{
    public class ViewsController : Controller
    {
        public IActionResult Home()
        {
            return View("~/Views/Home/Index.cshtml");
        }
        public IActionResult Login()
        {
            return View("~/Views/Login/Login.cshtml");
        }
        public IActionResult Zones()
        {
            return View("~/Views/Zone/CoveredZones.cshtml");
        }
        public IActionResult AddZone()
        {
            return View("~/Views/Zone/AddZone.cshtml");
        }
        public IActionResult EditZone()
        {
            return View("~/Views/Zone/EditZone.cshtml");
        }
        public IActionResult Employees()
        {
            return View("~/Views/User/Users.cshtml");
        }
        public IActionResult AddEmployee()
        {
            return View("~/Views/User/AddUser.cshtml");
        }
        public IActionResult Transactions()
        {
            return View("~/Views/Transaction/TransactionTable.cshtml");
        }
        public IActionResult AddTransaction()
        {
            return View("~/Views/Transaction/AddTransaction.cshtml");
        }
        public IActionResult Schedule()
        {
            return View("~/Views/Schedule/Calendar.cshtml");
        }
        public IActionResult Admins()
        {
            return View("~/Views/Admin/Admins.cshtml");
        }
        public IActionResult AddAdmin()
        {
            return View("~/Views/Admin/AddAdmin.cshtml");
        }
        public IActionResult Settings()
        {
            return View("~/Views/Settings/Index.cshtml");
        }
    }
}
