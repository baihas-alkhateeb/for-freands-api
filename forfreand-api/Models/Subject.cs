using Google.Cloud.Firestore;

namespace forfreand_api.Models
{
	[FirestoreData]
	public class Subject
	{
		[FirestoreProperty("id")]
		public string? Id { get; set; }

		[FirestoreProperty("name")]
		public string Name { get; set; } = string.Empty;

		[FirestoreProperty("description")]
		public string Description { get; set; } = string.Empty;

		[FirestoreProperty("level")]
		public string Level { get; set; } = string.Empty; // Kept for backward compatibility or display

		[FirestoreProperty("stageId")]
		public string StageId { get; set; } = string.Empty; // Link to dynamic Stage

		[FirestoreProperty("chapters")]
		public List<Chapter> Chapters { get; set; } = [];

		[FirestoreProperty("examQuestionCount")]
		public int ExamQuestionCount { get; set; } = 10;
	}
}
