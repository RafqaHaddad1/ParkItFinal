using Microsoft.AspNetCore.Http; // Add this for IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ParkIt.Models.Data;
using ParkIt.ViewModel;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;

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

        public async Task<IActionResult> TransactionTable()
        {
            
            var model = new TransactionsListViewModel
            {
                Transaction = await _dbContext.Transactions.ToListAsync(),
            };

            // Debugging line to ensure data is present
            Console.WriteLine($"Number of transactions: {model.Transaction.Count()}");

            return View(model);
        }
        //public IActionResult Transaction()
        //{
        //    return View("TransactionTable");
        //}

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
                    string uploadsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",  "ImagesUpload");

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

                _dbContext.Transactions.Add(model);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Transaction added successfully.");
                return RedirectToAction("TransactionTable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transaction. Inner exception: {InnerException}", ex.InnerException?.Message);
                return Json(new { success = false, message = "Error adding transaction", exception = ex.InnerException?.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _dbContext.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            _dbContext.Transactions.Remove(transaction);
            await _dbContext.SaveChangesAsync();
            return Json(new { success = true });
        }

        public IActionResult EditTransaction(int id)
        {
            try
            {
                var model = _dbContext.Transactions.Find(id);
                _logger.LogInformation("Info sent Successfully");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the transaction with ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePostEdit(Transactions model, IFormFile carPhoto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                _logger.LogInformation($"Trying to find transaction with ID: {model.Transaction_ID}");

                var existingTransaction = await _dbContext.Transactions.FindAsync(model.Transaction_ID);

                if (existingTransaction == null)
                {
                    return Json(new { success = false, message = "Transaction not found" });
                }

                // Handle the image upload if a new image is provided
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

                    // Update the model with the new relative path
                    var relativePath = $"/ImagesUpload/{uniqueFileName}";
                    model.FileName = relativePath;
                }

                // Update existing transaction with new values (including the image path if updated)
                _dbContext.Entry(existingTransaction).CurrentValues.SetValues(model);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Transaction updated successfully");
                return RedirectToAction("TransactionTable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction");
                return Json(new { success = false, message = "Error updating transaction", exception = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTransactionDetails(int id)
        {
            var transaction = _dbContext.Transactions
                .FirstOrDefault(z => z.Transaction_ID == id);

            if (transaction == null)
            {
                return Json(new { success = false, message = "user not found" });
            }
            var result = new
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
                    TicketNumber  = transaction.TicketNumber,
                    Fee = transaction.Fee,
                    status = transaction.Status,
                    Phone = transaction.PhoneNumber,
                    rating = transaction.Rating,
                    Note = transaction.Note,
                    FileName = transaction.FileName,
                    Parking = transaction.ParkingSpot_ID,
                }
            };

            return Json(result);
        }

    }
}
