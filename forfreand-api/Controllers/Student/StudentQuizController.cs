using Microsoft.AspNetCore.Mvc;
using forfreand_api.Logic;
using forfreand_api.DTOs;
using forfreand_api.Models; // For User model if needed

namespace forfreand_api.Controllers.Student
{
	[Route("api/student/quiz")]
	[ApiController]
	public class StudentQuizController(IQuizLogic quizLogic, ISubjectLogic subjectLogic) : ControllerBase
	{
		private readonly IQuizLogic _quizLogic = quizLogic;
		private readonly ISubjectLogic _subjectLogic = subjectLogic;

		[HttpGet("start")]
		public async Task<ActionResult<IEnumerable<StudentQuestionDto>>> StartQuiz(
			[FromQuery] string? subjectId,
			[FromQuery] string? chapterId,
			[FromQuery] int count = 0) // Changed default to 0 to detect if user provided it or not
		{
			if (string.IsNullOrEmpty(subjectId) && string.IsNullOrEmpty(chapterId))
			{
				return BadRequest("Either subjectId or chapterId must be provided.");
			}

			int finalCount = count;
			if (finalCount <= 0)
			{
				finalCount = 10; // Default fallback
				if (!string.IsNullOrEmpty(subjectId))
				{
					var subject = await _subjectLogic.GetSubjectByIdAsync(subjectId);
					if (subject != null && subject.ExamQuestionCount > 0)
					{
						finalCount = subject.ExamQuestionCount;
					}
				}
			}

			var questions = await _quizLogic.StartQuizAsync(subjectId, chapterId, finalCount);
			return Ok(questions);
		}

		[HttpPost("submit")]
		public async Task<ActionResult<QuizResultDto>> SubmitQuiz(
			[FromHeader(Name = "X-User-Id")] string studentId,
			[FromBody] QuizSubmissionDto submission)
		{
			if (string.IsNullOrEmpty(studentId)) return Unauthorized();

			var result = await _quizLogic.SubmitStudentQuizAsync(studentId, submission);
			return Ok(result);
		}
	}
}
