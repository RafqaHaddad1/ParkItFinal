using Microsoft.AspNetCore.Mvc;
using ParkIt.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace Calendar.Controllers
{
    [Route("Calendar")]
    public class ScheduleController : Controller
    {
        private readonly ParkItDbContext _context;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(ParkItDbContext context, ILogger<ScheduleController> logger)
        {
            _context = context;
            _logger = logger;
        }

      
        public IActionResult Calendar()
        {
            return View();
        }
    
        [HttpGet("GetEvents")]
        public async Task<IActionResult> GetEvents()
        {
            try
            {
                var events = await (from e in _context.Event
                                    join emp in _context.Employee
                                    on e.Employee_ID equals emp.Employee_ID
                                    select new
                                    {
                                        id = e.EventID,
                                        title = emp.Name,
                                        start = e.Start,
                                        end = e.End,
                                        description = e.Description,
                                        color = e.ThemeColor,
                                    })
                           .ToListAsync();


                return Json(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events");
                return StatusCode(500, "Internal server error");
            }
        }
                [HttpPost("AddEvent")]
               public async Task<IActionResult> AddEvent([FromBody]Event newEvent)
                {
                    try
                    {
                        _context.Event.Add(newEvent);
                        await _context.SaveChangesAsync();

                        // Return a success response
                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        Console.WriteLine(ex.Message);

                        // Return an error response
                        return Json(new { success = false, errorMessage = "An error occurred while adding the event." });
                    }
                }


        [HttpGet("EditEvent/{id}")]
        public IActionResult EditEvent(int id)
        {
            try
            {
                var model = _context.Event.Find(id);
                if (model == null)
                {
                    _logger.LogWarning("Event with ID {Id} not found.", id);
                    return NotFound();
                }

                _logger.LogInformation("Event with ID {Id} retrieved successfully.", id);
                
                
                return View(model); // Return the view with the event model
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the event with ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }


        [HttpPost("UpdateEvent")]
        public async Task<IActionResult> UpdateEvent([FromBody]Event updatedEvent)
        {
            Console.WriteLine($"Updated event {updatedEvent.EventID},{updatedEvent.Description},{updatedEvent.Start},{updatedEvent.End},{updatedEvent.ThemeColor},{updatedEvent.Zone_ID},{updatedEvent.Employee_ID}");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingEvent = await _context.Event.FindAsync(updatedEvent.EventID);
                if (existingEvent == null)
                {
                    _logger.LogWarning("Event with ID {EventID} not found.", updatedEvent.EventID);
                    return Json(new { success = false, message = "Event not found" });
                }

                _context.Entry(existingEvent).CurrentValues.SetValues(updatedEvent);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event with ID {EventID} updated successfully.", updatedEvent.EventID);
                return Json(new { success = true, message = "Event updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the event with ID {EventID}", updatedEvent.EventID);
                return Json(new { success = false, message = "Error updating event", exception = ex.Message });
            }
        }


        [HttpDelete("DeleteEvent/{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var previousevent = await _context.Event.FindAsync(id);
                if (previousevent == null)
                {
                    _logger.LogWarning("Event with ID {Id} not found.", id);
                    return NotFound();
                }

                _context.Event.Remove(previousevent);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event with ID {Id} deleted successfully.", id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the event with ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetEventsFromZone")]
        public async Task<IActionResult> GetEventsFromZone(int zoneID)
        {
            try
            {
                var events = await (from e in _context.Event
                                    join emp in _context.Employee
                                    on e.Employee_ID equals emp.Employee_ID
                                    join z in _context.Zone 
                                    on e.Zone_ID equals z.Zone_ID
                                    where e.Zone_ID == zoneID
                                    select new
                                    {
                                        id = e.EventID,
                                        title = emp.Name,
                                        empid = e.Employee_ID,
                                        zone_name = z.Zone_Name,
                                        zone_id = zoneID,
                                        start = e.Start,
                                        end = e.End,
                                        description = e.Description,
                                        color = e.ThemeColor,
                                    })
                           .ToListAsync();

                
                return Json(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
