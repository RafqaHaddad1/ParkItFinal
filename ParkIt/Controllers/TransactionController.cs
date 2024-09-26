using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIt.Models.Data;

namespace ParkIt.Controllers
{
   
    public class TransactionController : Controller
    {
        private readonly ILogger<TransactionController> _logger;
        private readonly ParkItDbContext _dbContext;

        public TransactionController(ILogger<TransactionController> logger, ParkItDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }
        [HttpGet]
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

