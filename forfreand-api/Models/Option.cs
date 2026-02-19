using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class Option
	{
		[FirestoreProperty("id")]
		public string? Id { get; set; }

		[FirestoreProperty("text")]
		public string Text { get; set; } = string.Empty;

		[FirestoreProperty("isCorrect")]
		public bool IsCorrect { get; set; }

		[FirestoreProperty("questionId")]
		public string? QuestionId { get; set; }


		public Question? Question { get; set; }
	}
}
