using forfreand_api.Logic;
using forfreand_api.Models;
using Microsoft.AspNetCore.Mvc;

namespace forfreand_api.Controllers.Admin
{
	[ApiController]
	[Route("api/[controller]")]
	public class StagesController(IStageLogic stageLogic) : ControllerBase
	{
		private readonly IStageLogic _stageLogic = stageLogic;

		[HttpGet]
		public async Task<IActionResult> GetStages()
		{
			var stages = await _stageLogic.GetAllStagesAsync();
			return Ok(stages);
		}

		[HttpPost]
		public async Task<IActionResult> CreateStage([FromBody] Stage stage, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			try
			{
				var createdStage = await _stageLogic.CreateStageAsync(stage, adminId);
				return Ok(createdStage);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateStage(string id, [FromBody] Stage stage, [FromHeader(Name = "X-User-Id")] string adminId)
		{
			if (id != stage.Id) return BadRequest();
			try
			{
				await _stageLogic.UpdateStageAsync(stage, adminId);
				return NoContent();
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteStage(string id, [FromHeader(Name = "X-User-Id")] string adminId, [FromQuery] string confirmation = "")
		{
			try
			{
				await _stageLogic.DeleteStageAsync(id, adminId, confirmation);
				return Ok();
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized();
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message, error = "CONFIRMATION_REQUIRED" });
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
