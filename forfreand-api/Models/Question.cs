using forfreand_api.Enums;
using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class Question
	{
		[FirestoreProperty("id")]
		public string? Id { get; set; }

		[FirestoreProperty("text")]
		public string Text { get; set; } = string.Empty;

		[FirestoreProperty("subjectId")]
		public string? SubjectId { get; set; }

		[FirestoreProperty("type")]
		public QuestionType Type { get; set; }

		[FirestoreProperty("difficulty")]
		public int Difficulty { get; set; } = 1;

		[FirestoreProperty("isAiGenerated")]
		public bool IsAiGenerated { get; set; } = false;

		[FirestoreProperty("isApproved")]
		public bool IsApproved { get; set; } = false;

		[FirestoreProperty("chapterId")]
		public string? ChapterId { get; set; }


		public Chapter? Chapter { get; set; }

		[FirestoreProperty("options")]
		public List<Option> Options { get; set; } = [];

		[FirestoreProperty("imageUrl")]
		public string? ImageUrl { get; set; }

		[FirestoreProperty("explanation")]
		public string? Explanation { get; set; }
	}
}
