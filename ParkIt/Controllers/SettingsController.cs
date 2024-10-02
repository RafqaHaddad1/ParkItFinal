using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIt.Models.Data; // Update to your actual namespace
using ParkIt.Models.Helper;
using System.IdentityModel.Tokens.Jwt;

namespace ParkIt.Controllers
{

    public class SettingsController : Controller
    {
        private readonly ParkItDbContext _context;
        private readonly Password _password;

        public SettingsController(ParkItDbContext context, Password password)
        {
            _context = context;
            _password = password;

        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAdminData()
        {
            // Extract the JWT token from the cookie
            var jwtToken = Request.Cookies["jwtToken"]; // Replace with your actual cookie name

            if (string.IsNullOrEmpty(jwtToken))
            {
                return Unauthorized(); // Return unauthorized if the token is not found
            }

            // Decode the JWT token to retrieve the admin ID
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);

            // Retrieve the admin ID from the 'sub' claim
            var adminIdString = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (adminIdString == null || !int.TryParse(adminIdString, out int adminId))
            {
                return Unauthorized(); // Return unauthorized if no admin ID is found or if conversion fails
            }

            // Fetch admin data from the database using the admin ID
            var admin = await _context.Admin
                .Where(a => a.Admin_ID == adminId) // Here adminId is now of type int
                .Select(a => new
                {
                    admin_Name = a.Admin_Name,
                    email = a.Email,
                    phoneNumber = a.PhoneNumber,
                    address = a.Address,
                    access = a.Access,
                    notes = a.Notes,
                    addDate = a.AddDate,
                    updateDate = a.UpdateDate
                })
                .FirstOrDefaultAsync();

            if (admin == null)
            {
                return NotFound(); // If no admin found with the given ID
            }

            return Json(admin); // Return the admin data as JSON
        }


        [HttpPost]
       
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // Extract the JWT token from the cookie
            var jwtToken = Request.Cookies["jwtToken"]; // Replace with your actual cookie name

            if (string.IsNullOrEmpty(jwtToken))
            {
                return Unauthorized(); // Return unauthorized if the token is not found
            }

            // Decode the JWT token to retrieve the admin ID
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);

            // Retrieve the admin ID from the 'sub' claim
            var adminIdString = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (adminIdString == null)
            {
                return Unauthorized("Admin ID not found.");
            }

            // Fetch the admin from the database
            var admin = _context.Admin.FirstOrDefault(a => a.Admin_ID == int.Parse(adminIdString));

            if (admin == null)
            {
                return Content("Admin not found.");
            }

            // Unhash the stored password and compare it with the provided old password
            var unhashedPassword = _password.UnHashPassword(admin.Password);
            if (unhashedPassword != oldPassword)
            {
                return Content("Old password is incorrect.");
            }

            // Check if the new password and confirm password match
            if (newPassword != confirmPassword)
            {
                return Content("New password and confirm password do not match.");

            }

            // Hash the new password and save it to the database
            admin.Password = _password.HashPassword(newPassword);
            _context.SaveChanges();

            return Content("Password changed successfully.");
        }

        //[HttpPost]
        //public async Task<IActionResult> DeleteAdmin(string deletePassword)
        //{
        //    // Add your logic to delete the admin account here

        //    return RedirectToAction("Index");
        //}
    }
}
