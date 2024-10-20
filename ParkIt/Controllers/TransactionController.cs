﻿using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Office.Interop.Excel;
using OfficeOpenXml;
using ParkIt.Models.Data;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using Worksheet = Microsoft.Office.Interop.Excel.Worksheet;
namespace ParkIt.Controllers
{
    //[Authorize]
    public class TransactionController : Controller
    {
        private readonly ILogger<TransactionController> _logger;
        private readonly ParkItDbContext _dbContext;

        private readonly IConfiguration _configuration;

        public TransactionController(ILogger<TransactionController> logger, ParkItDbContext dbContext, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult DownloadExcel()
        {
            string _connectionString = _configuration.GetConnectionString("DefaultConnection");
            string queryString = "SELECT * FROM Transactions";
            string fileName = $"Transactions_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
                                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Transactions");
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
                                        if (columnName == "ArrivalTime" || columnName == "DispatchTime" || columnName == "AddDate")
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
                                        if (columnName == "Runner_Collect_ID")
                                        {
                                            // Get the Zone_ID from the worksheet, checking if the cell value is null or DBNull
                                            var cellValue = worksheet.Cells[row, i].Value;

                                            if (cellValue != null && cellValue != DBNull.Value)
                                            {
                                                int runnerId = Convert.ToInt32(cellValue); // Safely convert the value to an integer

                                                // Await the result of the asynchronous query
                                                var runner = _dbContext.Employee.FirstOrDefault(z => z.Employee_ID == runnerId);

                                                if (runner != null)
                                                {
                                                    // Set the cell to the zone name if found
                                                    worksheet.Cells[row, i].Value = runner.Name;
                                                }
                                                else
                                                {
                                                    // Handle case where zone is not found (optional)
                                                    worksheet.Cells[row, i].Value = "Unknown runner";
                                                }
                                            }
                                        }
                                        if (columnName == "Runner_Dispatch_ID")
                                        {
                                            // Get the Zone_ID from the worksheet, checking if the cell value is null or DBNull
                                            var cellValue = worksheet.Cells[row, i].Value;

                                            if (cellValue != null && cellValue != DBNull.Value)
                                            {
                                                int runnerId = Convert.ToInt32(cellValue); // Safely convert the value to an integer

                                                // Await the result of the asynchronous query
                                                var runner = _dbContext.Employee.FirstOrDefault(z => z.Employee_ID == runnerId);

                                                if (runner != null)
                                                {
                                                    // Set the cell to the zone name if found
                                                    worksheet.Cells[row, i].Value = runner.Name;
                                                }
                                                else
                                                {
                                                    // Handle case where zone is not found (optional)
                                                    worksheet.Cells[row, i].Value = "Unknown runner";
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
            [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

        public async Task<IActionResult> TransactionTable()
        {
            // Step 1: Retrieve all transactions, employees, and zones in advance
            var transactionTable = await _dbContext.Transactions.ToListAsync();
            var runners = await _dbContext.Employee.ToListAsync();
            var zones = await _dbContext.Zone.ToListAsync(); 

            // Step 2: Initialize a list to hold the transaction and runner/zone data
            var transactionsWithRunners = new List<object>();

            if (transactionTable.Any())
            {
                // Step 3: Loop through each transaction and match with runner and zone
                foreach (var t in transactionTable)
                {
                    // Fetch the runner based on Runner_Collect_ID
                    var runner = runners.FirstOrDefault(e => e.Employee_ID == t.Runner_Collect_ID );

                    // Fetch the zone based on Zone_ID
                    var zone = zones.FirstOrDefault(z => z.Zone_ID == t.Zone_ID);

                    // Add each transaction with its corresponding runner's and zone's name to the list
                    transactionsWithRunners.Add(new
                    {

                        Transactioninfo = t,
                        RunnerName = runner?.Name ?? "No Runner",
                        ZoneName = zone?.Zone_Name ?? "No Zones"
                    });
                }
            }

            // Step 4: Check if the request is an AJAX request
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Return the transaction data along with the runner and zone names as a JSON response
                return Json(new
                {
                    success = true,
                    tableinfo = transactionsWithRunners
                });
            }

            // If it's not an AJAX request, return a view (you can modify this based on your needs)
            return View();
        }


        public IActionResult AddTransaction()
        {
            return View("AddTransaction");
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction(Transactions model, IFormFile carPhoto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                               .Select(e => e.ErrorMessage)
                                               .ToList();
                _logger.LogError("Model validation failed: {Errors}", string.Join(", ", errors));
                return Json(new { success = false, message = "Invalid model state", errors = errors });
            }

            try
            {
                if (carPhoto != null && carPhoto.Length > 0)
                {
                    // Specify the directory path
                    string uploadsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ImagesUpload");

                    // Ensure directory exists
                    if (!Directory.Exists(uploadsDirectoryPath))
                    {
                        Directory.CreateDirectory(uploadsDirectoryPath);
                    }

                    var fileName = Path.GetFileName(carPhoto.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    var fullPath = Path.Combine(uploadsDirectoryPath, uniqueFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await carPhoto.CopyToAsync(stream);
                    }

                    // Update the model with the relative path
                    var relativePath = $"/ImagesUpload/{uniqueFileName}";
                    model.FileName = relativePath;
                }

                model.AddDate = DateTime.Now;
                _dbContext.Transactions.Add(model);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Transaction added successfully.");

                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, model = model, redirectTo = Url.Action("TransactionTable", "Transaction") });
                }

                // Return the view for normal (non-AJAX) requests
                return RedirectToAction("TransactionTable");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transaction. Inner exception: {InnerException}", ex.InnerException?.Message);
                return Json(new { success = false, message = "Error adding transaction", exception = ex.InnerException?.Message });
            }
        }


        [HttpGet]
        public IActionResult GetTransactionDetails(int id)
        {
            var transaction = _dbContext.Transactions
                .FirstOrDefault(z => z.Transaction_ID == id);

            if (transaction == null)
            {
                return Json(new { success = false, message = "transaction not found" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    Transaction_ID = transaction.Transaction_ID,
                    CarModel = transaction.CarModel,
                    ArrivalTime = transaction.ArrivalTime,
                    DispatchTime = transaction.DispatchTime,
                    RunnerCollect = _dbContext.Employee.Where(e => e.Employee_ID == transaction.Runner_Collect_ID).Select(e => e.Name).FirstOrDefault(),
                    RunnerDispatch = _dbContext.Employee.Where(e => e.Employee_ID == transaction.Runner_Dispatch_ID).Select(e => e.Name).FirstOrDefault(),
                    Zone_Name = _dbContext.Zone.Where(z => z.Zone_ID == transaction.Zone_ID).Select(z => z.Zone_Name).FirstOrDefault(),
                    Type = transaction.Type,
                    TicketNumber = transaction.TicketNumber,
                    Fee = transaction.Fee,
                    status = transaction.Status,
                    Phone = transaction.PhoneNumber,
                    rating = transaction.Rating,
                    Note = transaction.Note,
                    FileName = transaction.FileName,
                    Parking = transaction.ParkingSpot_ID,
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredTransactions(string type, string status, int? start = null, DateTime? startDate = null, DateTime? endDate = null, int? length = null)
        {

            var runners = await _dbContext.Employee.ToListAsync();
            var zones = await _dbContext.Zone.ToListAsync();


            var query = _dbContext.Transactions.AsQueryable();


            // Apply type filter
            if (!string.IsNullOrEmpty(type) && type != "All")
            {
                query = query.Where(t => t.Type == type);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(t => t.Status == status);
            }

            // Apply date filters
            if (startDate.HasValue)
            {
                query = query.Where(t => t.ArrivalTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Adding end of day to include the entire day in filtering
                var endOfDay = endDate.Value.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.ArrivalTime <= endOfDay);
            }

            var totalRecords = await query.CountAsync();

            // Pagination
            var data = await query
                .Skip(start ?? 0)
                .Take(length ?? int.MaxValue)
                .ToListAsync();

            var FiltererdTransactions = new List<object>();
            foreach (var d in data)
            {
                // Fetch the runner based on Runner_Collect_ID
                var runner = runners.FirstOrDefault(e => e.Employee_ID == d.Runner_Collect_ID);

                // Fetch the zone based on Zone_ID
                var zone = zones.FirstOrDefault(z => z.Zone_ID == d.Zone_ID);

                // Add each transaction with its corresponding runner's and zone's name to the list
                FiltererdTransactions.Add(new
                {

                    Transactioninfo = d,
                    RunnerName = runner?.Name ?? "No Runner",
                    ZoneName = zone?.Zone_Name ?? "No Zones"
                });
            }
            return Ok(new
            {
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords, // Update if applying filters dynamically
                data = FiltererdTransactions
            });
        }
    }
}

