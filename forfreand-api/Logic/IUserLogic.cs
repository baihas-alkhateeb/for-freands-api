using forfreand_api.Models;

namespace forfreand_api.Logic
{
	public interface IUserLogic
	{
		Task<User?> LoginAsync(string username, string password);
		Task<User> RegisterAsync(User user);
		Task<User?> GetUserByIdAsync(string id);
		Task<bool> IsSuperAdminAsync(string userId);
		Task<bool> HasPermissionAsync(string userId, string? subjectId = null);
		Task<List<User>> GetAllUsersAsync(string? roleFilter = null);
		Task<int> GetStudentCountAsync(string? adminId = null);
		Task UpdateUserPermissionsAsync(string id, string role, List<string> assignedSubjectIds);
		Task UpdateProfileAsync(string id, string username, string password, string currentPassword);
		Task DeleteUserAsync(string id);
	}
}
