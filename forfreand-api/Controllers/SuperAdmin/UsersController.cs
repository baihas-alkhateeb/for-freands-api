using Microsoft.AspNetCore.Mvc;
using forfreand_api.Models;
using forfreand_api.Logic;

namespace forfreand_api.Controllers.SuperAdmin
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController(IUserLogic userLogic) : ControllerBase
	{
		private readonly IUserLogic _userLogic = userLogic;

		[HttpGet]
		public async Task<ActionResult<List<User>>> GetUsers([FromQuery] string? role, [FromHeader] string adminId)
		{
			if (!await _userLogic.HasPermissionAsync(adminId)) return Unauthorized();
			var users = await _userLogic.GetAllUsersAsync(role);
			return Ok(users.Select(u => new { u.Id, u.Username, u.Role, u.AssignedSubjectIds }));
		}

		[HttpPost("assistant")]
		public async Task<ActionResult<User>> AddAssistant(User assistant, [FromHeader] string superAdminId)
		{
			if (!await _userLogic.IsSuperAdminAsync(superAdminId)) return Unauthorized("Only SuperAdmin can add assistants.");
			assistant.Role = "Assistant";
			var result = await _userLogic.RegisterAsync(assistant);
			return Ok(new { result.Id, result.Username, result.Role, result.AssignedSubjectIds });
		}

		[HttpPut("{id}/permissions")]
		public async Task<ActionResult> UpdatePermissions(string id, [FromBody] User permissionDto, [FromHeader] string superAdminId)
		{
			if (!await _userLogic.IsSuperAdminAsync(superAdminId)) return Unauthorized("Only SuperAdmin can update permissions.");
			await _userLogic.UpdateUserPermissionsAsync(id, permissionDto.Role, permissionDto.AssignedSubjectIds);
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteUser(string id, [FromHeader] string superAdminId)
		{
			if (!await _userLogic.IsSuperAdminAsync(superAdminId)) return Unauthorized("Only SuperAdmin can delete users.");
			await _userLogic.DeleteUserAsync(id);
			return NoContent();
		}

		[HttpPut("{id}/profile")]
		public async Task<ActionResult> UpdateProfile(string id, [FromBody] ProfileUpdateDto profileDto)
		{
			// Basic validation
			if (string.IsNullOrEmpty(profileDto.Username) || (profileDto.Password != null && string.IsNullOrEmpty(profileDto.CurrentPassword)))
				return BadRequest("Username and Current Password are required.");

			try
			{
				await _userLogic.UpdateProfileAsync(id, profileDto.Username, profileDto.Password ?? "", profileDto.CurrentPassword);
				return NoContent();
			}
			catch (UnauthorizedAccessException ex)
			{
				return Unauthorized(ex.Message);
			}
		}
	}

	public class ProfileUpdateDto
	{
		public string Username { get; set; } = string.Empty;
		public string? Password { get; set; }
		public string CurrentPassword { get; set; } = string.Empty;
	}
}
