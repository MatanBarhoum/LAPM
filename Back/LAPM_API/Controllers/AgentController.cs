using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LAPM_API.Data;
using LAPM_API.Models;


namespace LAPM_API.Controllers
{
    [ApiController]
    [Route("api/agent")]
    public class AgentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AgentController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Provides the complete list of users that should have local admin rights RIGHT NOW.
        /// This is a more robust, self-healing approach for the agent.
        /// It handles new grants, expirations, and revocations automatically.
        /// </summary>
        [HttpGet("state/{computerName}")]
        [AllowAnonymous] // NOTE: For production, this should be secured with a client cert or pre-shared key.
        public async Task<IActionResult> GetRequiredAdminState(string computerName)
        {
            var now = DateTime.UtcNow;
            var computerNameUpper = computerName.ToUpper(); // Prepare for case-insensitive comparison

            // --- FIX: Replaced .Equals() with .ToUpper() for EF Core compatibility ---
            var validUsers = await _context.AccessRequests
                .Where(r => r.ComputerName.ToUpper() == computerNameUpper
                           && (r.Status == RequestStatus.Approved || r.Status == RequestStatus.Applied)
                           && r.ExpirationTime > now)
                .Select(r => r.DomainUser)
                .Distinct()
                .ToListAsync();

            // After getting the state, mark any newly 'Approved' requests as 'Applied'.
            var requestsToUpdate = await _context.AccessRequests
                .Where(r => r.ComputerName.ToUpper() == computerNameUpper
                           && r.Status == RequestStatus.Approved
                           && r.ExpirationTime > now)
                .ToListAsync();

            if (requestsToUpdate.Any())
            {
                foreach (var req in requestsToUpdate)
                {
                    req.Status = RequestStatus.Applied;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { ActiveAdmins = validUsers });
        }
    }
}
