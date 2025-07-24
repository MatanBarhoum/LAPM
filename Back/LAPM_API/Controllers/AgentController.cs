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

        [HttpGet("state/{computerName}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRequiredAdminState(string computerName)
        {
            var now = DateTime.UtcNow;
            var computerNameUpper = computerName.ToUpper(); 

            var validUsers = await _context.AccessRequests
                .Where(r => r.ComputerName.ToUpper() == computerNameUpper
                           && (r.Status == RequestStatus.Approved || r.Status == RequestStatus.Applied)
                           && r.ExpirationTime > now)
                .Select(r => r.DomainUser)
                .Distinct()
                .ToListAsync();

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
