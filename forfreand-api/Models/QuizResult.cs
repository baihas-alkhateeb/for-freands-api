using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class QuizResult
	{
		[FirestoreProperty("id")]
		public string? Id { get; set; }

		[FirestoreProperty("score")]
		public int Score { get; set; }

		[FirestoreProperty("totalQuestions")]
		public int TotalQuestions { get; set; }

		[FirestoreProperty("takenAt")]
		public DateTime TakenAt { get; set; } = DateTime.UtcNow;

		[FirestoreProperty("userId")]
		public string? UserId { get; set; }


		public User? User { get; set; }

		[FirestoreProperty("subjectId")]
		public string? SubjectId { get; set; }

		public Subject? Subject { get; set; }

		[FirestoreProperty("chapterId")]
		public string? ChapterId { get; set; }

		public Chapter? Chapter { get; set; }
	}
}
