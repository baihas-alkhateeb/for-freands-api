using Microsoft.AspNetCore.Mvc;
using forfreand_api.Logic;
using forfreand_api.Models;

namespace forfreand_api.Controllers.SuperAdmin
{
	[Route("api/[controller]")]
	[ApiController]
	public class ActivityLogsController(IActivityLogLogic activityLogLogic, IUserLogic userLogic) : ControllerBase
	{
		private readonly IActivityLogLogic _activityLogLogic = activityLogLogic;
		private readonly IUserLogic _userLogic = userLogic;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<ActivityLog>>> GetRecentActivities([FromHeader(Name = "X-User-Id")] string adminId, [FromQuery] int count = 10)
		{
			var user = await _userLogic.GetUserByIdAsync(adminId);
			if (user == null || (user.Role != "SuperAdmin" && user.Role != "Assistant")) return Unauthorized();

			var logs = await _activityLogLogic.GetRecentActivitiesAsync(count);
			return Ok(logs);
		}
	}
}
