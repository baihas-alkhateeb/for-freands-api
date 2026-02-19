using Microsoft.AspNetCore.Mvc;
using forfreand_api.Models;
using forfreand_api.Logic;

namespace forfreand_api.Controllers.Student
{
	[Route("api/[controller]")]
	[ApiController]
	public class QuizResultsController(IQuizLogic quizLogic) : ControllerBase
	{
		private readonly IQuizLogic _quizLogic = quizLogic;

		[HttpGet("User/{userId}")]
		public async Task<ActionResult<IEnumerable<QuizResult>>> GetUserResults(string userId)
		{
			return Ok(await _quizLogic.GetUserResultsAsync(userId));
		}

		[HttpPost]
		public async Task<ActionResult<QuizResult>> PostQuizResult(QuizResult result)
		{
			var resultSaved = await _quizLogic.SubmitResultAsync(result);
			return Ok(resultSaved);
		}

		[HttpGet("Leaderboard")]
		public async Task<ActionResult<IEnumerable<object>>> GetLeaderboard([FromHeader(Name = "X-User-Id")] string adminId)
		{
			return Ok(await _quizLogic.GetLeaderboardAsync(adminId));
		}
	}
}
