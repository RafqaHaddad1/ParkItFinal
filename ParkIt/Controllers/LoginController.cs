using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIt.Models.Data;
using ParkIt.Models.Helper;
namespace ParkIt.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly ParkItDbContext _dbContext;
        private readonly Password _password;

        public LoginController(ILogger<LoginController> logger, ParkItDbContext dbContext, Password password)
        {
            _logger = logger;
            _dbContext = dbContext;
            _password = password;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CheckUsernameAndPassword(string username, string password)
        {
            try
            {
                // Check if the employee exists with the given username
                var employee = await _dbContext.Employee
                    .FirstOrDefaultAsync(e => e.Name == username);

                if (employee == null)
                {
                    return Json(new { success = false, message = "Username not found." });
                }

                // Verify the password (assuming passwords are hashed)
                var decryptedPassword = _password.UnHashPassword(employee.Password); // Ensure this is done securely
                if (password != decryptedPassword)
                {
                    return Json(new { success = false, message = "Incorrect password." });
                }
                AuthenticateUserSession(employee);
              
                // Return success and message if login is successful
                return Json(new { success = true, redirectTo = "/Home/Index" });
            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        private void AuthenticateUserSession(Employee staff)
        {
            HttpContext.Session.SetString("UserId", staff.Employee_ID.ToString());
            HttpContext.Session.SetString("UserName", staff.Name);
            HttpContext.Session.SetString("UserTitle", staff.Title); // Updated key to "UserTitle"
            // Add any additional session data as needed
        }
    }
}
