using Google.Cloud.Firestore;
using forfreand_api.Models;
using forfreand_api.Services;

namespace forfreand_api.Logic
{
	public class ActivityLogLogic(FirestoreService firestoreService) : IActivityLogLogic
	{
		private readonly FirestoreDb _db = firestoreService.Db;
		private const string CollectionName = "ActivityLogs";

		public async Task LogActivityAsync(string userId, string username, string action, string details)
		{
			var log = new ActivityLog
			{
				UserId = userId,
				Username = username,
				Action = action,
				Details = details,
				Timestamp = DateTime.UtcNow
			};

			var docRef = _db.Collection(CollectionName).Document();
			log.Id = docRef.Id;
			await docRef.SetAsync(log);
		}

		public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10)
		{
			var query = _db.Collection(CollectionName)
				.OrderByDescending("timestamp") // Must match [FirestoreProperty("timestamp")]
				.Limit(count);

			var snapshot = await query.GetSnapshotAsync();
			return snapshot.Documents.Select(doc =>
			{
				var log = doc.ConvertTo<ActivityLog>();
				log.Id = doc.Id;
				return log;
			});
		}
	}
}
