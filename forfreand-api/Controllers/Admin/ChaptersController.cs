using Microsoft.AspNetCore.Mvc;
using forfreand_api.Models;
using forfreand_api.Logic;

namespace forfreand_api.Controllers.Admin
{
	[Route("api/[controller]")]
	[ApiController]
	public class ChaptersController(IChapterLogic chapterLogic) : ControllerBase
	{
		private readonly IChapterLogic _chapterLogic = chapterLogic;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<Chapter>>> GetChapters()
		{
			return Ok(await _chapterLogic.GetAllChaptersAsync());
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Chapter>> GetChapter(string id)
		{
			var chapter = await _chapterLogic.GetChapterByIdAsync(id);
			if (chapter == null) return NotFound();
			return Ok(chapter);
		}

		[HttpGet("BySubject/{subjectId}")]
		public async Task<ActionResult<IEnumerable<Chapter>>> GetChaptersBySubject(string subjectId)
		{
			return Ok(await _chapterLogic.GetChaptersBySubjectAsync(subjectId));
		}

		[HttpPost]
		public async Task<ActionResult<Chapter>> PostChapter(Chapter chapter, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				var result = await _chapterLogic.CreateChapterAsync(chapter, adminId);
				return CreatedAtAction("GetChapter", new { id = result.Id }, result);
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteChapter(string id, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				await _chapterLogic.DeleteChapterAsync(id, adminId);
				return NoContent();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}
	}
}
