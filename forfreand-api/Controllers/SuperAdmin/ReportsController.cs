using Microsoft.AspNetCore.Mvc;
using forfreand_api.Logic;

namespace forfreand_api.Controllers.SuperAdmin
{
	[Route("api/[controller]")]
	[ApiController]
	public class ReportsController(IUserLogic userLogic, IQuestionLogic questionLogic, ISubjectLogic subjectLogic) : ControllerBase
	{
		private readonly IUserLogic _userLogic = userLogic;
		private readonly IQuestionLogic _questionLogic = questionLogic;
		private readonly ISubjectLogic _subjectLogic = subjectLogic;

		[HttpGet("student-count")]
		public async Task<ActionResult<int>> GetStudentCount([FromHeader(Name = "X-User-Id")] string adminId)
		{
			var user = await _userLogic.GetUserByIdAsync(adminId);
			if (user == null || (user.Role != "SuperAdmin" && user.Role != "Assistant")) return Unauthorized();

			var count = await _userLogic.GetStudentCountAsync(adminId);
			return Ok(count);
		}

		[HttpGet("question-count")]
		public async Task<ActionResult<int>> GetQuestionCount([FromHeader(Name = "X-User-Id")] string adminId)
		{
			var user = await _userLogic.GetUserByIdAsync(adminId);
			if (user == null || (user.Role != "SuperAdmin" && user.Role != "Assistant")) return Unauthorized();

			var count = await _questionLogic.GetQuestionCountAsync(adminId);
			return Ok(count);
		}

		[HttpGet("subject-count")]
		public async Task<ActionResult<int>> GetSubjectCount([FromHeader(Name = "X-User-Id")] string adminId)
		{
			var user = await _userLogic.GetUserByIdAsync(adminId);
			if (user == null || (user.Role != "SuperAdmin" && user.Role != "Assistant")) return Unauthorized();

			var count = await _subjectLogic.GetSubjectCountAsync(adminId);
			return Ok(count);
		}
	}
}
