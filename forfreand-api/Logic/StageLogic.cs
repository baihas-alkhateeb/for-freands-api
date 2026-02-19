using Google.Cloud.Firestore;
using forfreand_api.Models;
using forfreand_api.Services;

namespace forfreand_api.Logic
{
	public interface IStageLogic
	{
		Task<IEnumerable<Stage>> GetAllStagesAsync();
		Task<Stage> CreateStageAsync(Stage stage, string adminId);
		Task UpdateStageAsync(Stage stage, string adminId);
		Task DeleteStageAsync(string id, string adminId, string confirmation);
	}

	public class StageLogic(FirestoreService firestoreService, IUserLogic userLogic, IActivityLogLogic activityLogLogic) : IStageLogic
	{
		private readonly FirestoreDb _db = firestoreService.Db;
		private readonly IUserLogic _userLogic = userLogic;
		private readonly IActivityLogLogic _activityLogLogic = activityLogLogic;
		private const string CollectionName = "Stages";

		public async Task<IEnumerable<Stage>> GetAllStagesAsync()
		{
			var snapshot = await _db.Collection(CollectionName).OrderBy("order").GetSnapshotAsync();
			return snapshot.Documents.Select(doc =>
			{
				var s = doc.ConvertTo<Stage>();
				s.Id = doc.Id;
				return s;
			});
		}

		public async Task<Stage> CreateStageAsync(Stage stage, string adminId)
		{
			if (!await _userLogic.IsSuperAdminAsync(adminId)) throw new UnauthorizedAccessException("Only SuperAdmin can create stages.");

			stage.CreatedAt = DateTime.UtcNow;
			stage.AdminId = adminId;

			var docRef = _db.Collection(CollectionName).Document();
			stage.Id = docRef.Id;
			await docRef.SetAsync(stage);

			var user = await _userLogic.GetUserByIdAsync(adminId);
			await _activityLogLogic.LogActivityAsync(adminId, user?.Username ?? "Unknown", "Created Stage", stage.Name);

			return stage;
		}

		public async Task UpdateStageAsync(Stage stage, string adminId)
		{
			if (!await _userLogic.IsSuperAdminAsync(adminId)) throw new UnauthorizedAccessException("Only SuperAdmin can update stages.");
			if (string.IsNullOrEmpty(stage.Id)) throw new ArgumentException("Id is required.");

			await _db.Collection(CollectionName).Document(stage.Id).SetAsync(stage, SetOptions.Overwrite);

			var user = await _userLogic.GetUserByIdAsync(adminId);
			await _activityLogLogic.LogActivityAsync(adminId, user?.Username ?? "Unknown", "Updated Stage", stage.Name);
		}

		public async Task DeleteStageAsync(string id, string adminId, string confirmation)
		{
			if (!await _userLogic.IsSuperAdminAsync(adminId)) throw new UnauthorizedAccessException("Only SuperAdmin can delete stages.");

			// 1. Get all subjects for this stage
			var subjectsQuery = _db.Collection("Subjects").WhereEqualTo("stageId", id);
			var subjectsSnapshot = await subjectsQuery.GetSnapshotAsync();

			// 2. Safety Check
			if (subjectsSnapshot.Count > 0 && confirmation?.ToLower() != "delete")
			{
				throw new InvalidOperationException("This stage contains subjects. To delete it and all its data, please type 'delete'.");
			}

			// 3. Cascade Delete
			foreach (var subjectDoc in subjectsSnapshot.Documents)
			{
				// Delete all questions linked to this subject
				var questionsQuery = _db.Collection("Questions").WhereEqualTo("subjectId", subjectDoc.Id);
				var questionsSnapshot = await questionsQuery.GetSnapshotAsync();

				foreach (var questionDoc in questionsSnapshot.Documents)
				{
					await questionDoc.Reference.DeleteAsync();
				}

				// Delete the subject itself
				await subjectDoc.Reference.DeleteAsync();
			}

			// 4. Delete the Stage
			await _db.Collection(CollectionName).Document(id).DeleteAsync();

			var user = await _userLogic.GetUserByIdAsync(adminId);
			await _activityLogLogic.LogActivityAsync(adminId, user?.Username ?? "Unknown", "Deleted Stage", $"{id} (Cascading)");
		}
	}
}
