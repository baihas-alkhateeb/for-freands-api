using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class User
	{
		[FirestoreProperty("id")]
		public string? Id { get; set; }

		[FirestoreProperty("username")]
		public string Username { get; set; } = string.Empty;

		[FirestoreProperty("password")]
		public string Password { get; set; } = string.Empty;

		[FirestoreProperty("role")]
		public string Role { get; set; } = "Student";

		[FirestoreProperty("assignedSubjectIds")]
		public List<string> AssignedSubjectIds { get; set; } = [];

		[FirestoreProperty("quizResults")]
		public List<QuizResult> QuizResults { get; set; } = [];

		[FirestoreProperty("lastLoginAt")]
		public DateTime? LastLoginAt { get; set; }
	}
}
