using API.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;
using Shared.Security;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
  
    public class AuthController(IAuthService Authservice) : ControllerBase
    {
        public IAuthService Authservice { get; set; } = Authservice;

        [HttpPost]
        [Route("login")]
        
        public async Task<ActionResult<ServiceResponse<string>>> Login([FromBody] UserLogin request) 
        {
            var response = await Authservice.Login(request.Email, request.Password);
            
            if (!response.Success) 
            {
                return BadRequest(response);
            }
            response.Message = "login ok.";
            return Ok(response);
        }

     

        [HttpPost]
        [Route("register")]
        public async Task<ActionResult<ServiceResponse<int>>> RegisterAsync([FromBody] UserRegister request) 
        {

            var response = await Authservice.Register(new User
            {
                Email = request.Email,
                UserName = request.UserName,
                Role = request.Role,
            },
            request.Password);

            if (!response.Success) 
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
