using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using ParkIt.Models.Data;
using ParkIt.Models.Helper;


namespace ParkIt.Controllers
{
    //[Authorize]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly ParkItDbContext _dbContext;
        private readonly Password _password;
        private readonly IConfiguration _configuration;

        public UserController(ILogger<UserController> logger, ParkItDbContext dbContext, Password password, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _password = password;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult DownloadExcel()
        {
            string _connectionString = _configuration.GetConnectionString("DefaultConnection");
            string queryString = "SELECT * FROM Employee";
            string fileName = $"Employee{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            string filePath = Path.Combine(Path.GetTempPath(), fileName);
            string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Logs");

            try
            {
                // Ensure the log folder exists
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(queryString, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            using (ExcelPackage package = new ExcelPackage())
                            {
                                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Employee");
                                worksheet.Cells[1, 1].LoadFromDataReader(reader, true);

                                // Format headers
                                for (int i = 1; i <= reader.FieldCount; i++)
                                {
                                    worksheet.Cells[1, i].Style.Font.Bold = true;
                                    worksheet.Cells[1, i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    worksheet.Cells[1, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                                    worksheet.Cells[1, i].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                }
                                // Set date format for specific columns
                                for (int row = 2; row <= worksheet.Dimension.End.Row; row++) // Start from the second row
                                {
                                    for (int i = 1; i <= reader.FieldCount; i++)
                                    {
                                        string columnName = reader.GetName(i - 1); // Get the column name

                                        // Apply date format if the column is one of the specified date columns
                                        if (columnName == "DeleteDate" || columnName == "UpdateDate" || columnName == "AddDate")
                                        {
                                            worksheet.Cells[row, i].Style.Numberformat.Format = "MM/dd/yyyy HH:mm"; // Set the date format
                                        }
                                        if (columnName == "Zone_ID")
                                        {
                                            // Get the Zone_ID from the worksheet, checking if the cell value is null or DBNull
                                            var cellValue = worksheet.Cells[row, i].Value;

                                            if (cellValue != null && cellValue != DBNull.Value)
                                            {
                                                int zoneId = Convert.ToInt32(cellValue); // Safely convert the value to an integer

                                                // Await the result of the asynchronous query
                                                var zone = _dbContext.Zone.FirstOrDefault(z => z.Zone_ID == zoneId);

                                                if (zone != null)
                                                {
                                                    // Set the cell to the zone name if found
                                                    worksheet.Cells[row, i].Value = zone.Zone_Name;
                                                }
                                                else
                                                {
                                                    // Handle case where zone is not found (optional)
                                                    worksheet.Cells[row, i].Value = "Unknown Zone";
                                                }
                                            }
                                            else
                                            {
                                                // Handle the case where the cell is null or contains DBNull
                                                worksheet.Cells[row, i].Value = "Zone ID is missing";
                                            }
                                        }
                                        if (columnName == "Subzone_ID")
                                        {
                                            // Get the Zone_ID from the worksheet, checking if the cell value is null or DBNull
                                            var cellValue = worksheet.Cells[row, i].Value;

                                            if (cellValue != null && cellValue != DBNull.Value)
                                            {
                                                int subzoneId = Convert.ToInt32(cellValue); // Safely convert the value to an integer

                                                // Await the result of the asynchronous query
                                                var subzone = _dbContext.Subzone.FirstOrDefault(z => z.Subzone_ID == subzoneId);

                                                if (subzone != null)
                                                {
                                                    // Set the cell to the zone name if found
                                                    worksheet.Cells[row, i].Value = subzone.Subzone_Name;
                                                }
                                                else
                                                {
                                                    // Handle case where zone is not found (optional)
                                                    worksheet.Cells[row, i].Value = "Unknown Subzone";
                                                }
                                            }
                                            else
                                            {
                                                // Handle the case where the cell is null or contains DBNull
                                                worksheet.Cells[row, i].Value = "Subzone ID is missing";
                                            }
                                        }
                                        if (columnName == "Supervisor_ID")
                                        {
                                            // Get the Zone_ID from the worksheet, checking if the cell value is null or DBNull
                                            var cellValue = worksheet.Cells[row, i].Value;

                                            if (cellValue != null && cellValue != DBNull.Value)
                                            {
                                                int supervisorId = Convert.ToInt32(cellValue); // Safely convert the value to an integer

                                                // Await the result of the asynchronous query
                                                var supervisor = _dbContext.Employee.FirstOrDefault(z => z.Employee_ID == supervisorId);

                                                if (supervisor != null)
                                                {
                                                    // Set the cell to the zone name if found
                                                    worksheet.Cells[row, i].Value = supervisor.Name;
                                                }
                                                else
                                                {
                                                    // Handle case where zone is not found (optional)
                                                    worksheet.Cells[row, i].Value = "Unknown Supervisor";
                                                }
                                            }
                                            else
                                            {
                                                // Handle the case where the cell is null or contains DBNull
                                                worksheet.Cells[row, i].Value = "";
                                            }
                                        }
                                    }
                                }
                                worksheet.Cells.AutoFitColumns();
                                // Save to file
                                package.SaveAs(new FileInfo(filePath));
                            }
                        }
                    }
                }

                // Return the file as a downloadable response
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception exception)
            {
                // Log error to file
                string errorFileName = $"ErrorLog_{DateTime.Now:yyyyMMddHHmmss}.log";
                using (StreamWriter sw = new StreamWriter(Path.Combine(logFolder, errorFileName)))
                {
                    sw.WriteLine(exception.ToString());
                }

                // Return an error response (can customize this)
                return StatusCode(500, "Internal server error");
            }
        }
        //[HttpGet]
        //[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

        //public async Task<IActionResult> Users()
        //{
        //    var model = await
        //         _dbContext.Employee
        //         .Where(e => e.IsDeleted == false || e.IsDeleted == null) 
        //        .ToListAsync();


        //    if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //    {
        //        return Json(new
        //        {
        //            success = true,
        //            modell = model,
        //        });
        //    }
        //    // Return the view for normal (non-AJAX) requests
        //    return View();
        //}
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

        public async Task<IActionResult> AddUser()
        {

            var Zones = await _dbContext.Zone.Where(z => z.IsDeleted == false || z.IsDeleted == null).ToListAsync();
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
                    model.Supervisor_ID = null;
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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> EditUser(int id)
        {
            try
            {
                var model = _dbContext.Employee.Find(id);
                //var unhashedPassword = _password.UnHashPassword(model.Password);
                var Zones = await _dbContext.Zone.ToListAsync();
                var Subzones = await _dbContext.Subzone.ToListAsync();

                // Store the employee data in the session
                HttpContext.Session.SetString("EmployeeData", JsonConvert.SerializeObject(model));

                _logger.LogInformation("Info sent Successfully");
                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        zones = Zones,
                        subzones = Subzones,
                        Employee = model,
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
                model.AddDate = existingEmployee.AddDate;
                // Update the existing employee with new values (excluding files)
                _dbContext.Entry(existingEmployee).CurrentValues.SetValues(model);
                // Process new files and collect their paths

                existingEmployee.UpdateDate = DateTime.Now;
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
                if (existingEmployee.Title == "Supervisor")
                {
                    existingEmployee.Supervisor_ID = null;
                }
                // Save changes to the database
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Employee updated successfully");

                return Json(new { success = true, message = "Employee updated successfully", redirectTo = "/User/Users" });
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
                 .Where(e => (e.Title == "Supervisor" && e.IsDeleted == null) || (e.Title == "Supervisor" && e.IsDeleted == false))
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
                     .Where(e => (e.Title == "Supervisor" && e.Zone_ID == zoneid && e.IsDeleted == false) || (e.Title == "Supervisor" && e.Zone_ID == zoneid && e.IsDeleted == null))
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
                 .Where(e => (e.Title == "Runner" && e.Zone_ID == id && e.IsDeleted == false) || (e.Title == "Runner" && e.Zone_ID == id && e.IsDeleted == null))
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
                 .Where(e => (e.Zone_ID == id && e.IsDeleted == false) || (e.Zone_ID == id && e.IsDeleted == null))
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
                    .Where(e => (e.Employee_ID == id && e.IsDeleted == false) || (e.Employee_ID == id && e.IsDeleted == null))
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

        [HttpGet]
        public JsonResult GetFilteredUsers(string employeeType, string title, bool? activeStatus)
        {
            var employees = _dbContext.Employee.AsQueryable();

            if (!string.IsNullOrEmpty(employeeType))
            {
                employees = employees.Where(e => e.EmploymentType == employeeType);
            }

            if (!string.IsNullOrEmpty(title))
            {
                employees = employees.Where(e => e.Title == title);
            }

            if (activeStatus.HasValue)
            {
                employees = employees.Where(e => e.Active == activeStatus.Value);
            }

            var result = employees.Select(e => new
            {
                employee_ID = e.Employee_ID,
                name = e.Name,
                title = e.Title,
                employmentType = e.EmploymentType,
                active = e.Active
            }).ToList();

            return Json(new { success = true, data = result });
        }
    }
}
