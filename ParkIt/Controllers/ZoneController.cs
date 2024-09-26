using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ParkIt.Models.Data;

using System.Security.Policy;
using Zone = ParkIt.Models.Data.Zone;

namespace ParkIt.Controllers
{
   
    public class ZoneController : Controller
    {
        private readonly ILogger<ZoneController> _logger;
        private readonly ParkItDbContext _dbContext;

        public ZoneController(ILogger<ZoneController> logger, ParkItDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }
        [HttpGet]
        public async Task<IActionResult> CoveredZones()
        {
            var model = await _dbContext.Zone.Where(z => z.IsDeleted == null || z.IsDeleted == false).ToListAsync();

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    zonemodel = model,
                });
            }
            // Return the view for normal (non-AJAX) requests
            return View();
        }
        public IActionResult AddZone()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetZones()
        {
            try
            {
                var zones = await _dbContext.Zone
                .Where(e => e.IsDeleted == false || e.IsDeleted == null)
                 .Select(z => new
                 {
                     Zone_ID = z.Zone_ID,
                     Zone_Name = z.Zone_Name
                 })
                 .ToListAsync();

                foreach (var s in zones)
                {
                    Console.WriteLine($"Zone : {s.Zone_Name}, {s.Zone_ID}");
                }
                return Json(zones);

            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }

        }

        [HttpGet]
        public IActionResult EditZone(int id)
        {

            var zone = _dbContext.Zone.FirstOrDefault(z => (z.Zone_ID == id && z.IsDeleted == false) || (z.Zone_ID == id && z.IsDeleted == null));
            var subzones = _dbContext.Subzone.Where(s => (s.Zone_ID == id && s.IsDeleted == false) || (s.Zone_ID == id && s.IsDeleted == null)).ToList();

            if (zone == null)
            {
                return NotFound();
            }

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    zoneinfo = zone,
                    subzoneinfo = subzones
                });
            }
            // Return the view for normal (non-AJAX) requests
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddZoneWithSubzones(Zone zoneModel, string subzonesData)
        {
            zoneModel.AddDate = DateTime.Now;
            Console.WriteLine(zoneModel);
            Console.WriteLine(subzonesData);
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid model state", errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            try
            {
                if (_dbContext == null)
                {
                    _logger.LogError("DbContext is null");
                    return Json(new { success = false, message = "Database context is not initialized" });
                }

                // Add the Zone
                _dbContext.Zone.Add(zoneModel);
                await _dbContext.SaveChangesAsync();

                // Deserialize subzones data 
                var subzones = JsonConvert.DeserializeObject<List<Subzone>>(subzonesData);

                // Set the Zone_ID for each Subzone
                foreach (var subzone in subzones)
                {
                    subzone.Zone_ID = zoneModel.Zone_ID;
                    subzone.AddDate = DateTime.Now;
                    subzone.IsDeleted = false;
                    _dbContext.Subzone.Add(subzone);

                }
                zoneModel.IsDeleted = false;
                
                await _dbContext.SaveChangesAsync();
                zoneModel.NumberOfSubzone = await GetSubzoneCountByZoneIdAsync(zoneModel.Zone_ID);
                zoneModel.NumberOfRunner = await GetNumberOfRunners(zoneModel.Zone_ID);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Added successfully");
                return RedirectToAction("CoveredZones");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Zone and Subzones");
                return Json(new { success = false, message = "Error adding Zone and Subzones", exception = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveZone(int zoneid)
        {
            var zone = await _dbContext.Zone.FindAsync(zoneid);

            if (zone == null)
            {
                return NotFound();
            }
            //Fetch related subzones
            var subzones = await _dbContext.Subzone
                .Where(s => s.Zone_ID == zoneid)
                .ToListAsync();

            // Remove related subzones
            foreach (var subzone in subzones)
            {
                subzone.IsDeleted = true;
                subzone.DeleteDate = DateTime.Now;
                _dbContext.Subzone.Update(subzone);
            }
            var employees = await _dbContext.Employee
             .Where(s => s.Zone_ID == zoneid)
             .ToListAsync();
            foreach (var employee in employees)
            {
                employee.IsDeleted = true;
                employee.DeleteDate = DateTime.Now;
                employee.Zone_ID = null;
                _dbContext.Employee.Update(employee);
            }
            zone.IsDeleted = true;
            zone.DeleteDate = DateTime.Now;
            _dbContext.Zone.Update(zone);
            await _dbContext.SaveChangesAsync();
            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> AddSubzone(int id, [FromForm] string model)
        {
            if (string.IsNullOrEmpty(model))
            {
                return Json(new
                {
                    success = false,
                    message = "Model data is missing"
                });
            }

            var subzoneModel = JsonConvert.DeserializeObject<Subzone>(model);

            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid model state",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            try
            {
                subzoneModel.IsDeleted = false;
                subzoneModel.Zone_ID = id;
                subzoneModel.AddDate = DateTime.Now;
                _dbContext.Subzone.Add(subzoneModel);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Subzone added successfully with ID {subzoneModel.Subzone_ID}");

                return Json(new { success = true, message = "Subzone added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding subzone");
                return Json(new
                {
                    success = false,
                    message = "Error adding subzone",
                    exception = ex.Message
                });
            }
        }

        public async Task<IActionResult> DeleteSubzone(int id)
        {
            try
            {
                var subzone = _dbContext.Subzone.Find(id);
                if (subzone == null) return Json(new { success = false });

                subzone.IsDeleted = true;
                subzone.DeleteDate = DateTime.Now;
                _dbContext.Subzone.Update(subzone);
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePostEdit(Zone model, string? subzonesData)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogError(error.ErrorMessage);
                }

                return Json(new { success = false, message = "Invalid data" });
            }


            try
            {
                _logger.LogInformation($"Trying to find zone with ID: {model.Zone_ID}");

                // Retrieve the existing zone from the database
                var existingZone = await _dbContext.Zone.FindAsync(model.Zone_ID);

                if (existingZone == null)
                {
                    return Json(new { success = false, message = "Zone not found" });
                }
                model.AddDate = existingZone.AddDate;
                // Update the existing zone with new values
                _dbContext.Entry(existingZone).CurrentValues.SetValues(model);
                model.UpdateDate = DateTime.Now;
                existingZone.UpdateDate = DateTime.Now;
                // Update the NumberOfSubzone property
                existingZone.NumberOfSubzone = await GetSubzoneCountByZoneIdAsync(existingZone.Zone_ID);
                existingZone.NumberOfRunner = await GetNumberOfRunners(existingZone.Zone_ID);
                // Save changes to the database
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Zone updated successfully");

                return RedirectToAction("CoveredZones");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating zone");
                return Json(new { success = false, message = "Error updating zone", exception = ex.Message });
            }
        }

        public async Task<int> GetSubzoneCountByZoneIdAsync(int zoneId)
        {
            return await _dbContext.Subzone
                                 .Where(s => (s.Zone_ID == zoneId && s.IsDeleted == false) || (s.Zone_ID == zoneId && s.IsDeleted == null))
                                 .CountAsync();
        }
        [HttpGet]
        public async Task<int> GetNumberOfRunners(int zoneId)
        {
            return await _dbContext.Employee
                                 .Where(s => (s.Zone_ID == zoneId && s.IsDeleted == false) || (s.Zone_ID == zoneId && s.IsDeleted == null))
                                 .CountAsync();
        }
        [HttpGet]
        public IActionResult GetAllCoordinates()
        {
            var zones = _dbContext.Zone
                .Where(z => z.IsDeleted == false || z.IsDeleted == null)
                .Select(z => new
                {
                    z.Zone_ID,
                    AllCoordinates = z.AllCoordinates // Assuming coordinates are stored as a string "lat,lng"
                })
                .ToList();

            // Log the zones for debugging
            _logger.LogInformation("Zones with coordinates: " + string.Join(", ", zones.Select(z => $"ID: {z.Zone_ID}, Coordinates: {z.AllCoordinates}")));

            return Ok(zones);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSubzone(Subzone subzone)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                _logger.LogInformation($"Trying to find subzone with ID: {subzone.Subzone_ID}");

                // Retrieve the existing subzone from the database
                var existingSubzone = await _dbContext.Subzone.FindAsync(subzone.Subzone_ID);

                if (existingSubzone == null)
                {
                    return Json(new { success = false, message = "Subzone not found" });
                }
                subzone.IsDeleted = false;
                existingSubzone.IsDeleted = false;
                subzone.AddDate = existingSubzone.AddDate;
                // Update the existing subzone with new values
                _dbContext.Entry(existingSubzone).CurrentValues.SetValues(subzone);
                subzone.UpdateDate = DateTime.Now;
                existingSubzone.UpdateDate = DateTime.Now;
                // Save changes to the database
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Subzone updated successfully");

                return Json(new { success = true, message = "Subzone updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subzone");
                return Json(new { success = false, message = "Error updating subzone", exception = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSubzonesByZoneId(int zoneId)
        {
            try
            {
                var subzones = _dbContext.Subzone
             .Where(s => (s.Zone_ID == zoneId && s.IsDeleted == null )||( s.Zone_ID == zoneId && s.IsDeleted == false))
             .Select(s => new
             {
                 Subzone_ID = s.Subzone_ID,
                 Subzone_Name = s.Subzone_Name,
                 capacity = s.Capacity,
                 zone_id = s.Zone_ID,
             })
             .ToList();

                foreach (var subzone in subzones)
                {
                    Console.WriteLine($"Subzone ID: {subzone.Subzone_ID}, Zone ID: {subzone.Subzone_Name}");
                }
                if (subzones.Any())
                {
                    return Json(new { success = true, subzones = subzones });
                }
                else
                {
                    return Json(new { success = false, message = "No subzones found for this zone." });
                }
            }
            catch (Exception ex)
            {
                // Log the exception here
                return Json(new { success = false, message = "An error occurred while fetching subzones." });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetZoneName(int id)
        {
            try
            {
                var zone = _dbContext.Zone
                 .Where(e => (e.Zone_ID == id && e.IsDeleted==false) || (e.Zone_ID == id && e.IsDeleted == null))
                 .Select(e => new
                 {
                     Zone_ID = e.Zone_ID,
                     Zone_Name = e.Zone_Name,
                 });
                return Json(zone);

            }
            catch (Exception ex)
            {
                // Log the exception details and return an error response
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetZoneByTransactionId(int id)
        {
            try
            {
                var transaction = await _dbContext.Transactions
                    .Where(t => t.Transaction_ID == id)
                    .Select(t => new
                    {
                        Zone_Name = _dbContext.Zone
                                    .Where(e => (e.Zone_ID == t.Zone_ID && e.IsDeleted == false) || (e.Zone_ID == t.Zone_ID && e.IsDeleted == null))
                                    .Select(e => e.Zone_Name)
                                    .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (transaction == null || string.IsNullOrEmpty(transaction.Zone_Name))
                {
                    return Json(new { success = false, message = "Zone not found" });
                }

                return Json(new { success = true, Zone_Name = transaction.Zone_Name });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
        [HttpGet]
        public IActionResult GetZoneDetails(int id)
        {
            var zone = _dbContext.Zone
                .FirstOrDefault(z => z.Zone_ID == id);

            if (zone == null)
            {
                return Json(new { success = false, message = "Zone not found" });
            }
            var result = new
            {
                success = true,
                data = new
                {
                    Zone_ID = zone.Zone_ID,
                    Zone_Name = zone.Zone_Name,
                    Area = zone.Area,
                    Street = zone.Street,
                    Supervisor = _dbContext.Employee
                            .Where(e => e.Employee_ID == zone.Supervisor_ID)
                            .Select(e => e.Name)
                .FirstOrDefault(),
                    Coordinate = zone.AllCoordinates,
                    Active = zone.Active,
                    Subzones = _dbContext.Subzone.Where(s=> s.Zone_ID == id).Select(sz => new
                    {
                        Name = sz.Subzone_Name,
                        Capacity = sz.Capacity
                    }).ToList()
                }
            };

            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllZonesCoordinates()
        {
            try
            {
                // Asynchronously query the database
                var coordinates = await _dbContext.Zone
                  .Where(z => z.IsDeleted == false || z.IsDeleted == null)
                  .Select(zone => zone.AllCoordinates)
                  .ToListAsync();


                // Check if the list is empty
                if (!coordinates.Any())
                {
                    return Json(new { success = false, message = "No coordinates found" });
                }

                return Json(new { success = true, coordinates });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

    }
}