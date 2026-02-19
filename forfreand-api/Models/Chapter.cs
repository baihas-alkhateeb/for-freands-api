using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class Chapter
	{
		[FirestoreProperty("id")]
		public string? Id { get; set; }

		[FirestoreProperty("title")]
		public string Title { get; set; } = string.Empty;

		[FirestoreProperty("name")] // Fallback for legacy data
		public string Name { get => Title; set => Title = string.IsNullOrEmpty(value) ? Title : value; }

		[FirestoreProperty("topics")]
		public List<string> Topics { get; set; } = [];

		[FirestoreProperty("subjectId")]
		public string? SubjectId { get; set; }


		public Subject? Subject { get; set; }

		public List<Question> Questions { get; set; } = [];
	}
}
