using Microsoft.AspNetCore.Mvc;
using forfreand_api.Models;
using forfreand_api.Logic;

namespace forfreand_api.Controllers.Admin
{
	[Route("api/[controller]")]
	[ApiController]
	public class SubjectsController(ISubjectLogic subjectLogic) : ControllerBase
	{
		private readonly ISubjectLogic _subjectLogic = subjectLogic;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<Subject>>> GetSubjects([FromHeader(Name = "X-User-Id")] string adminId)
		{
			return Ok(await _subjectLogic.GetAllSubjectsAsync(adminId));
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Subject>> GetSubject(string id)
		{
			var subject = await _subjectLogic.GetSubjectByIdAsync(id);
			if (subject == null) return NotFound();
			return Ok(subject);
		}

		[HttpPost]
		public async Task<ActionResult<Subject>> PostSubject(Subject subject, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				var result = await _subjectLogic.CreateSubjectAsync(subject, adminId);
				return CreatedAtAction("GetSubject", new { id = result.Id }, result);
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> PutSubject(string id, Subject subject, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				subject.Id = id;
				await _subjectLogic.UpdateSubjectAsync(subject, adminId);
				return NoContent();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSubject(string id, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				await _subjectLogic.DeleteSubjectAsync(id, adminId);
				return NoContent();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}
	}
}
