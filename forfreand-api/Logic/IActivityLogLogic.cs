using forfreand_api.Models;

namespace forfreand_api.Logic
{
	public interface IActivityLogLogic
	{
		Task LogActivityAsync(string userId, string username, string action, string details);
		Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10);
	}
}
