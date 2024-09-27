using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ParkIt.Models.Data;
using ParkIt.Models.Helper;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ParkIt.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly ParkItDbContext _dbContext;
        private readonly Password _password;
        private readonly IConfiguration _configuration;

        public LoginController(ILogger<LoginController> logger, ParkItDbContext dbContext, Password password, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _password = password;
            _configuration = configuration;
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
                var decryptedPassword = _password.UnHashPassword(employee.Password);
                if (password != decryptedPassword)
                {
                    return Json(new { success = false, message = "Incorrect password." });
                }

                // Generate the JWT Token
                var token = GenerateJwtToken(employee);

                // Set the token in an HTTP-only cookie (secure and cannot be accessed via JavaScript)
                SetJwtCookie(token);

                // Return success message along with a redirect URL to the dashboard
                return Json(new { success = true, redirectTo = Url.Action("Index", "Home") });
            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                _logger.LogError($"An error occurred: {ex.Message}");
                return Json(new { success = false, message = "Internal server error" });
            }
        }


        private string GenerateJwtToken(Employee employee)
        {
            var jwtSettings = _configuration.GetSection("JWT");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, employee.Employee_ID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, employee.Name),
                new Claim("role", employee.Title), // Add additional claims as needed
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["ValidIssuer"],
                audience: jwtSettings["ValidAudience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetJwtCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Ensures the cookie is not accessible via JavaScript (prevents XSS)
                Secure = true,   // Only send cookie over HTTPS (make sure this is true in production)
                Expires = DateTime.Now.AddMinutes(60) // Set expiration time (same as token expiration)
            };

            Response.Cookies.Append("JwtToken", token, cookieOptions);
        }
    }
}
