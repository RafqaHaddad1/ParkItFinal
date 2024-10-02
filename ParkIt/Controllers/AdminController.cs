using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using ParkIt.Models.Data;
using ParkIt.Models.Helper;

namespace ParkIt.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ParkItDbContext _dbContext;
        private readonly Password _password;
        private readonly IConfiguration _configuration;
        public AdminController(ILogger<AdminController> logger, ParkItDbContext dbContext, Password password, IConfiguration configuration)
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
            string queryString = "SELECT * FROM Admin";
            string fileName = $"Admins_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Admin");
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
                                        if (columnName == "UpdateDate" || columnName == "DeleteDate" || columnName == "AddDate")
                                        {
                                            worksheet.Cells[row, i].Style.Numberformat.Format = "MM/dd/yyyy HH:mm"; // Set the date format
                                        }
                                    }
                                }

                                // AutoFit the columns after processing the data
                                worksheet.Cells.AutoFitColumns();

                                // Save the file once all data is loaded and formatting is done
                                package.SaveAs(new FileInfo(filePath));
                            }
                        }
                    }
                }

                // Check if the file exists and return it as a downloadable response
                if (System.IO.File.Exists(filePath))
                {
                    byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    return NotFound("File not found.");
                }
            }
            catch (Exception exception)
            {
                // Log error with additional context
                string errorFileName = $"ErrorLog_{DateTime.Now:yyyyMMddHHmmss}.log";
                string errorFilePath = Path.Combine(logFolder, errorFileName);
                using (StreamWriter sw = new StreamWriter(errorFilePath))
                {
                    sw.WriteLine($"Error occurred while generating the Excel file: {exception.Message}");
                    sw.WriteLine($"Stack Trace: {exception.StackTrace}");
                    sw.WriteLine($"Query: {queryString}");
                    sw.WriteLine($"File Path: {filePath}");
                }

                // Return an error response with more details
                return StatusCode(500, "Internal server error. Please check the logs for more details.");
            }
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Admins()
        {
            var model =
                 _dbContext.Admin
                 .Where(e => e.IsDeleted == false || e.IsDeleted == null)
                .ToList();


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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AddAdmin()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddNewAdmin(Admin model, IFormFileCollection Files)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid model state", errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            try
            {

                var paths = new List<string>();
                if (Files == null || Files.Count == 0)
                {
                    return Json(new { success = false, message = "No files uploaded" });
                }

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
                
                var pass = _password.HashPassword(model.Password);
                model.Password = pass;
                model.AddDate = DateTime.Now;
                model.IsDeleted = false;
                _dbContext.Admin.Add(model);
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
                return View("Admins");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding admin");
                return Json(new { success = false, message = "Error adding admin", exception = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var admin = await _dbContext.Admin.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            admin.IsDeleted = true;
            admin.DeleteDate = DateTime.Now;
            _dbContext.Update(admin);
            await _dbContext.SaveChangesAsync();
            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult GetAdminDetails(int id)
        {
            var user = _dbContext.Admin
                .FirstOrDefault(z => z.Admin_ID == id);

            if (user == null)
            {
                return Json(new { success = false, message = "user not found" });
            }
            var result = new
            {
                success = true,
                data = user
            };

            return Json(result);
        }


        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> EditAdmin(int id)
        {
            try
            {
                var model = _dbContext.Admin.Find(id);
                // Store the employee data in the session
                HttpContext.Session.SetString("AdminData", JsonConvert.SerializeObject(model));

                _logger.LogInformation("Info sent Successfully");
                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        Admin = model,
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
       
        public async Task<IActionResult> SavePostEdit(Admin model, IFormFileCollection Files)
        {
            Console.WriteLine("model add date" + model.AddDate);
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                _logger.LogInformation($"Trying to find admin with ID: {model.Admin_ID}");

                // Retrieve the existing employee from the database
                var existingAdmin = await _dbContext.Admin.FindAsync(model.Admin_ID);

                if (existingAdmin == null)
                {
                    return Json(new { success = false, message = "Admin not found" });
                }
               
                // Retrieve existing file paths and initialize paths list
                var existingPaths = existingAdmin.Files?.Split(';').ToList() ?? new List<string>();
                model.AddDate = existingAdmin.AddDate;
                // Update the existing employee with new values (excluding files)
                _dbContext.Entry(existingAdmin).CurrentValues.SetValues(model);
                existingAdmin.AddDate = model.AddDate ?? existingAdmin.AddDate;
                existingAdmin.UpdateDate = DateTime.Now;
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
                existingAdmin.Files = string.Join(";", existingPaths);
                Console.WriteLine(existingAdmin.Files);
             
                // Save changes to the database
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Admin updated successfully");

                return Json(new { success = true, message = "Admin updated successfully", redirectTo = "/Admin/Admins" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin");
                return Json(new { success = false, message = "Error updating admin", exception = ex.Message });
            }
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
                var admin =  _dbContext.Admin
                    .FirstOrDefault(e => e.Files.Contains(filePath));

                if (admin != null)
                {
                    var existingPaths = admin.Files.Split(';').Select(p => p.Trim()).ToList();
                    existingPaths.Remove(filePath);
                    admin.Files = string.Join(";", existingPaths);
                    _dbContext.Entry(admin).State = EntityState.Modified;
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

    }
}
