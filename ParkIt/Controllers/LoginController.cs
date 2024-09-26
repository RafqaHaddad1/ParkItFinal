using Microsoft.AspNetCore.Authorization;
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
                    var token = GenerateJWTToken(employee);

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

            public string GenerateJWTToken(Employee user)
            {
                var claims = new List<Claim>
                {
                     new Claim(ClaimTypes.NameIdentifier, user.Employee_ID.ToString()),
                     new Claim(ClaimTypes.Name, user.Name),
                        
                };

                var jwtToken = new JwtSecurityToken(
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddDays(30),
                    signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]) // Use _configuration
                        ),
                        SecurityAlgorithms.HmacSha256Signature)
                    );

                return new JwtSecurityTokenHandler().WriteToken(jwtToken);
            }
        }
    }
