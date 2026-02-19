using Microsoft.AspNetCore.Mvc;
using forfreand_api.Models;
using forfreand_api.Logic;

namespace forfreand_api.Controllers.Shared
{
	[Route("api/auth")]
	[ApiController]
	public class AuthController(IUserLogic userLogic) : ControllerBase
	{
		private readonly IUserLogic _userLogic = userLogic;

		[HttpPost("login")]
		public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
		{
			var user = await _userLogic.LoginAsync(loginDto.Username, loginDto.Password);
			if (user == null) return Unauthorized();

			// In a real app, generate a JWT token here.
			return Ok(new LoginResponseDto
			{
				Id = user.Id!,
				Username = user.Username,
				Role = user.Role,
				AssignedSubjectIds = user.AssignedSubjectIds,
				Token = user.Id!
			});
		}

		[HttpPost("register")]
		public async Task<ActionResult<User>> Register(DTOs.StudentRegisterDto registerDto)
		{
			if (string.IsNullOrEmpty(registerDto.Username) || string.IsNullOrEmpty(registerDto.Password))
				return BadRequest("Username and Password are required.");

			var user = new User
			{
				Username = registerDto.Username,
				Password = registerDto.Password, // In real app, hash this!
				Role = "Student",
				AssignedSubjectIds = new List<string>()
			};

			try
			{
				var result = await _userLogic.RegisterAsync(user);
				return Ok(new { result.Id, result.Username, result.Role });
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
