using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LAPM_API.Data;
using LAPM_API.Models;
using LAPM_API.Services;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LAPM_API.Controllers
{
    [ApiController]
    [Route("api/requests")]
    [Authorize] // All users must be in LAPM_Users
    public class RequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveDirectoryService _adService;
        private readonly IConfiguration _configuration;

        public RequestsController(ApplicationDbContext context, IActiveDirectoryService adService, IConfiguration configuration)
        {
            _context = context;
            _adService = adService;
            _configuration = configuration;
        }

        // --- NEW ADMIN ENDPOINT ---
        /// <summary>
        /// Gets a list of all requests in the system. Admins only.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<IEnumerable<AccessRequest>>> GetAllRequests()
        {
            return await _context.AccessRequests
                                 .OrderByDescending(r => r.RequestTime)
                                 .ToListAsync();
        }

        // POST: api/requests
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] AccessRequestDto requestDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!_adService.ComputerExists(requestDto.ComputerName) || !_adService.UserExists(requestDto.DomainUser))
            {
                return BadRequest(new { message = "Invalid computer or user name provided." });
            }

            var requestor = User.Identity?.Name ?? "Unknown";
            var accessRequest = new AccessRequest
            {
                ComputerName = requestDto.ComputerName,
                DomainUser = requestDto.DomainUser,
                ExpirationTime = requestDto.ExpirationTime,
                Requestor = requestor,
                RequestTime = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                Notes = requestDto.Notes
            };

            _context.AccessRequests.Add(accessRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRequestById), new { id = accessRequest.Id }, accessRequest);
        }

        // GET: api/requests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AccessRequest>> GetRequestById(int id)
        {
            var request = await _context.AccessRequests.FindAsync(id);
            if (request == null) return NotFound();

            var adminGroup = _configuration["AccessControl:AdminGroup"];
            var isAdmin = User.IsInRole(adminGroup);
            if (!isAdmin && request.Requestor != User.Identity.Name)
            {
                return Forbid();
            }

            return Ok(request);
        }

        // GET: api/requests/mine
        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<AccessRequest>>> GetMyRequests()
        {
            var requestorName = User.Identity?.Name;
            if (string.IsNullOrEmpty(requestorName))
            {
                return Unauthorized();
            }

            var requests = await _context.AccessRequests
                                         .Where(r => r.Requestor == requestorName)
                                         .OrderByDescending(r => r.RequestTime)
                                         .ToListAsync();

            return Ok(requests);
        }

        // GET: api/requests/pending
        [HttpGet("pending")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<IEnumerable<AccessRequest>>> GetPendingRequests()
        {
            return await _context.AccessRequests
                                 .Where(r => r.Status == RequestStatus.Pending)
                                 .OrderByDescending(r => r.RequestTime)
                                 .ToListAsync();
        }

        // PUT: api/requests/{id}/approve
        [HttpPut("{id}/approve")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.AccessRequests.FindAsync(id);
            if (request == null || request.Status != RequestStatus.Pending)
            {
                return NotFound("Request not found or is not in a pending state.");
            }

            request.Status = RequestStatus.Approved;
            request.ApprovalTime = DateTime.UtcNow;
            request.Approver = User.Identity?.Name ?? "Unknown";

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/requests/{id}/reject
        [HttpPut("{id}/reject")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await _context.AccessRequests.FindAsync(id);
            if (request == null || request.Status != RequestStatus.Pending)
            {
                return NotFound("Request not found or is not in a pending state.");
            }

            request.Status = RequestStatus.Rejected;
            request.Approver = User.Identity?.Name ?? "Unknown";

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- MANAGEMENT ENDPOINTS ---

        [HttpPut("{id}/extend")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> ExtendRequest(int id, [FromBody] ExtendRequestDto extendDto)
        {
            var request = await _context.AccessRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (request.Status != RequestStatus.Approved && request.Status != RequestStatus.Applied)
            {
                return BadRequest(new { message = "This request is not in an active state and cannot be extended." });
            }

            if (extendDto.NewExpirationTime <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "New expiration time must be in the future." });
            }

            request.ExpirationTime = extendDto.NewExpirationTime;
            if (request.Status == RequestStatus.Expired)
            {
                request.Status = RequestStatus.Approved;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}/revoke")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> RevokeRequest(int id)
        {
            var request = await _context.AccessRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (request.Status != RequestStatus.Approved && request.Status != RequestStatus.Applied)
            {
                return BadRequest(new { message = "This request is not in an active state and cannot be revoked." });
            }

            request.Status = RequestStatus.Revoked;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // DTO for creating a request from the frontend
    public class AccessRequestDto
    {
        public string ComputerName { get; set; }
        public string DomainUser { get; set; }
        public DateTime ExpirationTime { get; set; }
        public string? Notes { get; set; }
    }

    // DTO for extending a request
    public class ExtendRequestDto
    {
        public DateTime NewExpirationTime { get; set; }
    }
}
