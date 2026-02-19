using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class ActivityLog
	{
		[FirestoreDocumentId]
		public string Id { get; set; } = string.Empty;

		[FirestoreProperty("username")]
		public string Username { get; set; } = string.Empty; // Who performed the action

		[FirestoreProperty("userId")]
		public string UserId { get; set; } = string.Empty; // ID of user

		[FirestoreProperty("action")]
		public string Action { get; set; } = string.Empty; // e.g., "Created Subject"

		[FirestoreProperty("details")]
		public string Details { get; set; } = string.Empty; // e.g., "Physics"

		[FirestoreProperty("timestamp")]
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}
