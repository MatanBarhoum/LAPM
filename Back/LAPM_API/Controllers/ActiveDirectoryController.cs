using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LAPM_API.Services;

namespace LAPM_API.Controllers
{
    [ApiController]
    [Route("api/ad")]
    [Authorize] // Only members of LAPM_Users can query AD based on our fallback policy
    public class ActiveDirectoryController : ControllerBase
    {
        private readonly IActiveDirectoryService _adService;

        public ActiveDirectoryController(IActiveDirectoryService adService)
        {
            _adService = adService;
        }

        [HttpGet("computer/{computerName}")]
        public IActionResult FindComputer(string computerName)
        {
            if (_adService.ComputerExists(computerName))
            {
                return Ok(new { name = computerName, exists = true });
            }
            return NotFound(new { name = computerName, exists = false });
        }

        [HttpGet("user/{userName}")]
        public IActionResult FindUser(string userName)
        {
            if (_adService.UserExists(userName))
            {
                return Ok(new { name = userName, exists = true });
            }
            return NotFound(new { name = userName, exists = false });
        }
    }
 }
