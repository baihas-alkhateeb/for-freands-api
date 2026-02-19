using Microsoft.AspNetCore.Mvc;
using forfreand_api.Models;
using forfreand_api.Logic;

namespace forfreand_api.Controllers.Admin
{
	[Route("api/[controller]")]
	[ApiController]
	public class QuestionsController(IQuestionLogic questionLogic) : ControllerBase
	{
		private readonly IQuestionLogic _questionLogic = questionLogic;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<Question>>> GetQuestions([FromQuery] bool includeUnapproved = false, [FromQuery] string? chapterId = null)
		{
			return Ok(await _questionLogic.GetQuestionsAsync(includeUnapproved, chapterId));
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Question>> GetQuestion(string id)
		{
			var question = await _questionLogic.GetQuestionByIdAsync(id);
			if (question == null) return NotFound();
			return Ok(question);
		}

		[HttpPost("GenerateFromPdf")]
		public async Task<ActionResult<IEnumerable<Question>>> GenerateFromPdf(
			IFormFile file,
			[FromQuery] string chapterId,
			[FromQuery] int count = 3,
			[FromQuery] int difficulty = 1,
			[FromQuery] int questionType = 0,
			[FromQuery] string language = "ar")
		{
			return Ok(await _questionLogic.GenerateFromPdfAsync(file, chapterId, count, difficulty, questionType, language));
		}

		[HttpPost("ExtractFromTemplate")]
		public async Task<ActionResult<IEnumerable<Question>>> ExtractFromTemplate(IFormFile file, [FromQuery] string chapterId)
		{
			return Ok(await _questionLogic.ExtractFromTemplateAsync(file, chapterId));
		}

		[HttpPost("ApproveMultiple")]
		public async Task<ActionResult> ApproveMultiple([FromBody] List<Question> questions, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				await _questionLogic.ApproveMultipleAsync(questions, adminId);
				return Ok("Questions approved and saved.");
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpPost]
		public async Task<ActionResult<Question>> PostQuestion(Question question, [FromHeader(Name = "X-User-Id")] string? adminId)
		{
			try
			{
				if (!Request.Headers.ContainsKey("X-Draft")) question.IsApproved = true;
				var result = await _questionLogic.CreateQuestionAsync(question, adminId);
				return CreatedAtAction("GetQuestion", new { id = result.Id }, result);
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> PutQuestion(string id, [FromBody] Question question, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			if (id != question.Id) return BadRequest();
			try
			{
				await _questionLogic.UpdateQuestionAsync(question, adminId);
				return NoContent();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpPatch("{id}/Approve")]
		public async Task<IActionResult> ApproveQuestion(string id, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				await _questionLogic.ApproveQuestionAsync(id, adminId);
				return NoContent();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteQuestion(string id, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				await _questionLogic.DeleteQuestionAsync(id, adminId);
				return NoContent();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}
	}
}
