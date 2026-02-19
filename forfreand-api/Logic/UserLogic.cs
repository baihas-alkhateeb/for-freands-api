using Google.Cloud.Firestore;
using forfreand_api.Models;
using forfreand_api.Services;

namespace forfreand_api.Logic
{
	public class UserLogic(FirestoreService firestoreService, IActivityLogLogic activityLogLogic) : IUserLogic
	{
		private readonly FirestoreDb _db = firestoreService.Db;
		private readonly IActivityLogLogic _activityLogLogic = activityLogLogic;
		private const string CollectionName = "Users";

		public async Task<User?> LoginAsync(string username, string password)
		{
			var query = _db.Collection(CollectionName)
				.WhereEqualTo("username", username)
				.WhereEqualTo("password", password);

			var snapshot = await query.GetSnapshotAsync();
			var doc = snapshot.Documents.Count > 0 ? snapshot.Documents[0] : null;

			if (doc == null) return null;

			var user = doc.ConvertTo<User>();
			user.Id = doc.Id;

			// Update last login timestamp
			await doc.Reference.UpdateAsync("lastLoginAt", DateTime.UtcNow);

			return user;
		}

		public async Task<User> RegisterAsync(User user)
		{
			// Check if username exists
			var existingUser = await _db.Collection(CollectionName)
				.WhereEqualTo("username", user.Username)
				.GetSnapshotAsync();

			if (existingUser.Count > 0)
			{
				throw new InvalidOperationException("اسم المستخدم مستخدم بالفعل.");
			}

			var docRef = _db.Collection(CollectionName).Document();
			user.Id = docRef.Id;
			await docRef.SetAsync(user);
			return user;
		}

		public async Task<User?> GetUserByIdAsync(string id)
		{
			var doc = await _db.Collection(CollectionName).Document(id).GetSnapshotAsync();
			if (!doc.Exists) return null;

			var user = doc.ConvertTo<User>();
			user.Id = doc.Id;
			return user;
		}

		public async Task<bool> IsSuperAdminAsync(string userId)
		{
			var user = await GetUserByIdAsync(userId);
			return user?.Role == "SuperAdmin";
		}

		public async Task<bool> HasPermissionAsync(string userId, string? subjectId = null)
		{
			var user = await GetUserByIdAsync(userId);
			if (user == null) return false;

			if (user.Role == "SuperAdmin") return true;

			if (user.Role == "Assistant")
			{
				if (string.IsNullOrEmpty(subjectId)) return true; // General assistant access
				return user.AssignedSubjectIds.Contains(subjectId);
			}

			return false;
		}

		public async Task<List<User>> GetAllUsersAsync(string? roleFilter = null)
		{
			Query query = _db.Collection(CollectionName);
			if (!string.IsNullOrEmpty(roleFilter))
			{
				query = query.WhereEqualTo("role", roleFilter);
			}

			var snapshot = await query.GetSnapshotAsync();
			return snapshot.Documents.Select(doc =>
			{
				var u = doc.ConvertTo<User>();
				u.Id = doc.Id;
				return u;
			}).ToList();
		}

		public async Task<int> GetStudentCountAsync(string? adminId = null)
		{
			// Assistant sees all students count
			var query = _db.Collection(CollectionName).WhereEqualTo("role", "Student");
			var snapshot = await query.GetSnapshotAsync();
			return snapshot.Count;
		}

		public async Task UpdateUserPermissionsAsync(string id, string role, List<string> assignedSubjectIds)
		{
			var docRef = _db.Collection(CollectionName).Document(id);
			var updates = new Dictionary<string, object>
			{
				{ "role", role },
				{ "assignedSubjectIds", assignedSubjectIds }
			};
			await docRef.UpdateAsync(updates);
		}

		public async Task UpdateProfileAsync(string id, string username, string password, string currentPassword)
		{
			var user = await GetUserByIdAsync(id);
			if (user == null) throw new KeyNotFoundException("User not found.");

			if (user.Password != currentPassword)
				throw new UnauthorizedAccessException("كلمة المرور الحالية غير صحيحة.");

			// Check if new username is taken by another user
			if (user.Username != username)
			{
				var existingUser = await _db.Collection(CollectionName)
					.WhereEqualTo("username", username)
					.GetSnapshotAsync();

				if (existingUser.Count > 0)
				{
					throw new InvalidOperationException("اسم المستخدم مستخدم بالفعل.");
				}
			}

			var docRef = _db.Collection(CollectionName).Document(id);
			var updates = new Dictionary<string, object>
			{
				{ "username", username }
			};

			if (!string.IsNullOrEmpty(password))
			{
				updates.Add("password", password);
			}

			await docRef.UpdateAsync(updates);

			// Log activity
			await _activityLogLogic.LogActivityAsync(id, username, "Updated Profile", "Username or password changed");
		}

		[Obsolete("Use HasPermissionAsync or IsSuperAdminAsync instead")]
		public async Task<bool> IsAdminAsync(string userId)
		{
			var user = await GetUserByIdAsync(userId);
			return user?.Role == "SuperAdmin" || user?.Role == "Admin";
		}
		public async Task DeleteUserAsync(string id)
		{
			await _db.Collection(CollectionName).Document(id).DeleteAsync();
		}
	}
}
