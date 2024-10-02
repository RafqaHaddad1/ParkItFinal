using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

    }
}
