using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class Stage
	{
		[FirestoreProperty("id")]
		public string Id { get; set; } = string.Empty;

		[FirestoreProperty("name")]
		public string Name { get; set; } = string.Empty;

		[FirestoreProperty("order")]
		public int Order { get; set; } = 0; // For sorting stages in the UI

		[FirestoreProperty("adminId")]
		public string AdminId { get; set; } = string.Empty; // To track who created it

		[FirestoreProperty("createdAt")]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
