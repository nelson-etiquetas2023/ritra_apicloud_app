using API.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class UsersController : ControllerBase
    {
        private readonly IUsersService service;

        public UsersController(IUsersService service)
        {
            this.service = service;
        }

        [HttpGet]
        [Route("getusers")]
        public async Task<IActionResult> GetUsersAsync()
        {
            var users = await service.GetUsersAsync();
            return Ok(users);
        }

        [HttpGet]
        [Route("getuserbyid/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await service.GetUsersByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        [Route("createusers")]
        public async Task<IActionResult> CreateUsersAsync([FromBody] User usuario)
        {
            var created = await service.CreateUserAsync(usuario);
            return CreatedAtAction(nameof(GetUserById), new { id = created.UserId }, created);
        }

        [HttpPut]
        [Route("updateusers/{id}")]
        public async Task<IActionResult> UpdateUsersAsync(int id, User usuario)
        {
            var updated = await service.UpdateUserAsync(id, usuario);
            if (updated == null) NotFound();
            return Ok(updated);
        }

        [HttpDelete]
        [Route("deleteusers/{id}")]
        public async Task<IActionResult> DeleteUsersAsync(int id)
        {
            var deleted = await service.DeleteUserAsync(id);
            if (!deleted) NotFound();
            return NoContent();
        }
    }
}
