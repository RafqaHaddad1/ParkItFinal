using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using ParkIt.Models.Data;
    using ParkIt.Models.Helper;
    using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
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
        [AllowAnonymous]
        [HttpPost]
            public async Task<IActionResult> CheckUsernameAndPassword(string username, string password)
            {
                try
                {
                    var employee = await _dbContext.Employee
                        .FirstOrDefaultAsync(e => e.Name == username);

                    if (employee == null)
                    {
                        return Json(new { success = false, message = "Username not found." });
                    }

                    var decryptedPassword = _password.UnHashPassword(employee.Password);
                    if (password != decryptedPassword)
                    {
                        return Json(new { success = false, message = "Incorrect password." });
                    }
                    AuthenticateUserSession(employee);
                    var token = GenerateJSONWebToken(employee);

                    return Json(new { success = true, redirectTo = "/Home/Index", token });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return StatusCode(500, "Internal server error");
                }
            }

            private void AuthenticateUserSession(Employee staff)
            {
                HttpContext.Session.SetString("UserId", staff.Employee_ID.ToString());
                HttpContext.Session.SetString("UserName", staff.Name);
                HttpContext.Session.SetString("UserTitle", staff.Title);
            }

            private string GenerateJSONWebToken(Employee userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Name, userInfo.Name),
            };

            var token = new JwtSecurityToken(_configuration["Jwt:ValidIssuer"],
              _configuration["Jwt:ValidIssuer"],
               claims,
              expires: DateTime.Now.AddMinutes(120),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
       

    }
}
