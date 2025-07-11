using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;

namespace LAPM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Returns the username and all AD group memberships for the authenticated user.
        /// The frontend can use this to dynamically show/hide UI elements.
        /// </summary>
        [HttpGet("session")]
        public IActionResult GetUserSession()
        {
            if (User.Identity is not ClaimsIdentity claimsIdentity)
            {
                return Unauthorized();
            }

            // Extract the user's name, removing the domain if present for cleaner display.
            var username = claimsIdentity.Name?.Split('\\').Last() ?? "Unknown";

            // Extract all role claims (which correspond to AD groups)
            var roles = claimsIdentity.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == ClaimTypes.GroupSid)
                .Select(c => new SecurityIdentifier(c.Value).Translate(typeof(NTAccount)).Value)
                .Select(r => r.Split('\\').Last()) // Return just the group name
                .Distinct()
                .ToList();

            var adminGroup = _configuration["AccessControl:AdminGroup"]?.Split('\\').Last();

            return Ok(new
            {
                Username = username,
                Roles = roles,
                IsAdmin = roles.Contains(adminGroup) // Convenience flag for the frontend
            });
        }
    }
}
