using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Construction;
using Microsoft.EntityFrameworkCore;
using ParkIt.Data;
using ParkIt.Models.Data;
using ParkIt.ViewModel; 

namespace ParkIt.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly ParkItDbContext _dbContext;
        private readonly Password _password;


        public UserController(ILogger<UserController> logger, ParkItDbContext dbContext, Password password)
        {
            _logger = logger;
            _dbContext = dbContext;
            _password = password;
        }
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var model = await
                 _dbContext.Employee
                 .Where(e => e.IsDeleted.HasValue && e.IsDeleted.Value == false)
                .ToListAsync();
            
        
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    modell = model,
                });
            }
            // Return the view for normal (non-AJAX) requests
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> AddUser()
        {

            var Zones = await _dbContext.Zone.ToListAsync();
            var Subzones = await _dbContext.Subzone.ToListAsync();
            var employee = new Employee();

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    zones = Zones,
                    subzones = Subzones,
                    Employee = employee,
              
                });
            }
            // Return the view for normal (non-AJAX) requests
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> AddEmployee(Employee model, IFormFileCollection Files)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid model state", errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            try
            {
             
                var paths = new List<string>();
                foreach (var file in Files)
                {
                    if (file != null && file.Length > 0)
                    {
                        Console.Write(file);
                        // Specify the directory path
                        string uploadsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FilesUpload");

                        // Ensure directory exists
                        if (!Directory.Exists(uploadsDirectoryPath))
                        {
                            Directory.CreateDirectory(uploadsDirectoryPath);
                        }

                        var fileName = Path.GetFileName(file.FileName);
                        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                        var fullPath = Path.Combine(uploadsDirectoryPath, uniqueFileName);
                       
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Add the relative path to the list
                        var relativePath = $"/FilesUpload/{uniqueFileName}";
                        Console.Write(relativePath);
                        paths.Add(relativePath);
                    }
                }

                // Set the model's Files property after the loop
                var pathsString = string.Join(";", paths);
                model.Files = pathsString;
                if (model.Title == "Supervisor")
                {
                    model.Supervisor_ID =null;
                }
                var pass = _password.HashPassword(model.Password);
                model.Password = pass;
                model.AddDate = DateTime.Now;
                model.IsDeleted = false;
                _dbContext.Employee.Add(model);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Added successfully");
                
                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        info = model,

                    });
                }
                // Return the view for normal (non-AJAX) requests
                return View("Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee");
                return Json(new { success = false, message = "Error adding employee", exception = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var employee = await _dbContext.Employee.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            employee.IsDeleted = true;
            employee.DeleteDate = DateTime.Now;  
            _dbContext.Update(employee);
            await _dbContext.SaveChangesAsync();
            return Json(new { success = true });
        }
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            try
            {
                var model = _dbContext.Employee.Find(id);
                //var unhashedPassword = _password.UnHashPassword(model.Password);
                var Zones = await _dbContext.Zone.ToListAsync();
                var Subzones = await _dbContext.Subzone.ToListAsync();
             

                _logger.LogInformation("Info sent Successfully");
                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        zones = Zones,
                        subzones = Subzones,
                        Employee = model,
                        //unhashedPassword = unhashedPassword,
                       
                    });
                }
                // Return the view for normal (non-AJAX) requests
                return View();
            
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the employee with ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePostEdit(Employee model, IFormFileCollection Files)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                _logger.LogInformation($"Trying to find employee with ID: {model.Employee_ID}");

                // Retrieve the existing employee from the database
                var existingEmployee = await _dbContext.Employee.FindAsync(model.Employee_ID);

                if (existingEmployee == null)
                {
                    return Json(new { success = false, message = "Employee not found" });
                }
                // Retrieve existing file paths and initialize paths list
                var existingPaths = existingEmployee.Files?.Split(';').ToList() ?? new List<string>();
                // Update the existing employee with new values (excluding files)
                _dbContext.Entry(existingEmployee).CurrentValues.SetValues(model);   // Process new files and collect their paths
                var newPaths = new List<string>();

                foreach (var file in Files)
                {
                    if (file != null && file.Length > 0)
                    {
                        _logger.LogInformation($"Processing file: {file.FileName}");


                        // Specify the directory path
                        string uploadsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FilesUpload");

                        // Ensure directory exists
                        if (!Directory.Exists(uploadsDirectoryPath))
                        {
                            Directory.CreateDirectory(uploadsDirectoryPath);
                        }

                        var fileName = Path.GetFileName(file.FileName);
                        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                        var fullPath = Path.Combine(uploadsDirectoryPath, uniqueFileName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Add the relative path to the new paths list
                        var relativePath = $"/FilesUpload/{uniqueFileName}";
                        _logger.LogInformation($"File saved at path: {relativePath}");
                        newPaths.Add(relativePath);
                    }
                }

                // Combine existing paths with new ones
                existingPaths.AddRange(newPaths);
                foreach (var file in existingPaths)
                {
                    Console.WriteLine(file);
                }

                // Update the employee's Files property with combined paths
                existingEmployee.Files = string.Join(";", existingPaths);
                Console.WriteLine(existingEmployee.Files);
                if(existingEmployee.Title == "Supervisor")
                {
                    existingEmployee.Supervisor_ID = null;
                }
                // Save changes to the database
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Employee updated successfully");
             
                return Json(new { success = true, message = "Employee updated successfully", redirectTo ="/User/Users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                return Json(new { success = false, message = "Error updating employee", exception = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFiles()
        {
            // Fetch all employees
            var employees = await _dbContext.Employee.ToListAsync();

            // Extract file names/paths from the Files column (assuming Files is a comma-separated string)
            var files = employees
                .SelectMany(employee => employee.Files.Split(','))
                .Select(file => file.Trim()) // Trim whitespace around file names
                .Distinct() // Ensure uniqueness if necessary
                .ToList();

            return Ok(files);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteFiles(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Json(new { success = false, message = "File path is required" });
            }

            try
            {
                // Specify the directory path
                string uploadsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                // Normalize and construct the full path
                string relativePath = filePath.TrimStart('/');
                string fullPath = Path.GetFullPath(Path.Combine(uploadsDirectoryPath, relativePath));

                // Log paths for debugging
                _logger.LogInformation($"Uploads Directory Path: {uploadsDirectoryPath}");
                _logger.LogInformation($"Relative Path: {relativePath}");
                _logger.LogInformation($"Full Path: {fullPath}");

                // Check if file exists
                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogWarning($"File does not exist at path: {fullPath}");
                    return Json(new { success = false, message = "File not found" });
                }

                // Delete the file
                System.IO.File.Delete(fullPath);

                // Optionally, update the database to remove file reference
                var employee = await _dbContext.Employee
                    .FirstOrDefaultAsync(e => e.Files.Contains(filePath));

                if (employee != null)
                {
                    var existingPaths = employee.Files.Split(';').Select(p => p.Trim()).ToList();
                    existingPaths.Remove(filePath);
                    employee.Files = string.Join(";", existingPaths);
                    _dbContext.Entry(employee).State = EntityState.Modified;
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation($"File {filePath} deleted successfully");
                return Json(new { success = true, message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return Json(new { success = false, message = "Error deleting file", exception = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetSupervisor()
        {
            try
            {
                var supervisors = await _dbContext.Employee
                 .Where(e => e.Title == "Supervisor")
                 .Select(e => new
                 {
                     Supervisor_Name = e.Name,
                     Supervisor_ID = e.Employee_ID
                 })
                 .ToListAsync();

                foreach (var s in supervisors)
                {
                    Console.WriteLine($"Supervisor : {s.Supervisor_Name}, {s.Supervisor_ID}");
                }
                return Json(supervisors);

            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }
        [HttpGet] 
        public async Task<IActionResult> GetSupervisorByZoneID([FromQuery] int zoneid)
        {
            try
            {
                var supervisors = await _dbContext.Employee
                     .Where(e => e.Title == "Supervisor" && e.Zone_ID == zoneid)
                     .Select(e => new
                     {
                         Supervisor_Name = e.Name,
                         Supervisor_ID = e.Employee_ID
                     })
                     .ToListAsync();

                if (!supervisors.Any())
                {
                    Console.WriteLine($"No supervisors found for Zone ID: {zoneid}");
                }

                foreach (var s in supervisors)
                {
                    Console.WriteLine($"Supervisor : {s.Supervisor_Name}, {s.Supervisor_ID}");
                }
                return Json(supervisors);

            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRunnerByZone(int id)
        {
            try
            {
                var runner = await _dbContext.Employee
                 .Where(e => e.Title == "Runner" && e.Zone_ID == id) 
                 .Select(e => new
                 {
                     Runner_Name = e.Name,
                     Runner_ID = e.Employee_ID
                 })
                 .ToListAsync();

                foreach (var s in runner)
                {
                    Console.WriteLine($"Supervisor : {s.Runner_Name}, {s.Runner_ID}");
                }
                return Json(runner);

            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }
        [HttpGet]
        public async Task<IActionResult> GetEmployeeByZone(int id)
        {
            try
            {
                var runner = await _dbContext.Employee
                 .Where(e => e.Zone_ID == id)
                 .Select(e => new
                 {
                     Employee_Name = e.Name,
                     Employee_ID = e.Employee_ID
                 })
                 .ToListAsync();

                foreach (var s in runner)
                {
                    Console.WriteLine($"Supervisor : {s.Employee_Name}, {s.Employee_ID}");
                }
                return Json(runner);

            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }
        [HttpGet]
        public async Task<IActionResult> GetRunnerById(int id)
        {
            try
            {
                var runner = await _dbContext.Employee
                    .Where(e => e.Employee_ID == id)
                    .Select(e => new
                    {
                        Runner_Name = e.Name
                    })
                    .FirstOrDefaultAsync();

                if (runner == null)
                {
                    return Json(new { success = false, message = "Runner not found" });
                }

                return Json(runner);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetRunnerCollectByTransactionId(int id)
        {
            try
            {
                var transaction = await _dbContext.Transactions
                    .Where(t => t.Transaction_ID == id)
                    .Select(t => new
                    {
                        Runner_Name = _dbContext.Employee
                                        .Where(e => e.Employee_ID == t.Runner_Collect_ID)
                                        .Select(e => e.Name)
                                        .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (transaction == null || string.IsNullOrEmpty(transaction.Runner_Name))
                {
                    return Json(new { success = false, message = "Runner not found" });
                }

                return Json(new { success = true, Runner_Name = transaction.Runner_Name });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
        [HttpGet]
        public IActionResult GetUserDetails(int id)
        {
            var user = _dbContext.Employee
                .FirstOrDefault(z => z.Employee_ID == id);

            if (user == null)
            {
                return Json(new { success = false, message = "user not found" });
            }
            var result = new
            {
                success = true,
                data = new
                {
                    Employee_ID = user.Employee_ID,
                    Name = user.Name,
                    Title = user.Title,
                    Active = user.Active,
                    Phone = user.PhoneNumber,
                    Address = user.Address,
                    NFCCode = user.NFCCode,
                    Zone_Name = _dbContext.Zone.Where(z => z.Zone_ID == user.Zone_ID).Select(z => z.Zone_Name).FirstOrDefault(),
                    Subzone_Name = _dbContext.Subzone.Where(z => z.Subzone_ID == user.Subzone_ID).Select(z => z.Subzone_Name).FirstOrDefault(),
                    Supervisor = _dbContext.Employee.Where(e => e.Employee_ID == user.Supervisor_ID).Select(e => e.Name).FirstOrDefault(),
                    Kafeel = user.Kafeel,
                    type = user.EmploymentType,
                    AdditionalNotes = user.AdditionalNotes,
                }
            };

            return Json(result);
        }

    }
}
