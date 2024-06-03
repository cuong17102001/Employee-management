using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tc.BaseLibrary.DTOs;
using Tc.ServerLibrary.Repositories.Contracts;

namespace Tc.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController(IUserAccount accountInterface) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> CreateAsync(Register user)
        {
            if (user == null)
            {
                return BadRequest("Model is empty");
            }

            var result = await accountInterface.CreateAsync(user);
            return Ok(result);
        }
    }
}
