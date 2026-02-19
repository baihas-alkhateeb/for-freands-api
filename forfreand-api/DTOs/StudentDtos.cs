using System.Text.Json.Serialization;

namespace forfreand_api.DTOs
{
	public class StudentQuestionDto
	{
		public string Id { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public List<StudentOptionDto> Options { get; set; } = new();
		public int Type { get; set; } // 0: Multiple Choice, 1: True/False
		public string? ImageUrl { get; set; }
	}

	public class StudentOptionDto
	{
		public string Id { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
	}

	public class QuizSubmissionDto
	{
		// Can be ChapterId or SubjectId depending on mode
		public string ContextId { get; set; } = string.Empty;
		public bool IsSubjectExam { get; set; } // Track if this was a full subject exam
		public DateTime StartTime { get; set; }
		public List<StudentAnswerDto> Answers { get; set; } = new();
	}

	public class StudentAnswerDto
	{
		public string QuestionId { get; set; } = string.Empty;
		public string SelectedOptionId { get; set; } = string.Empty;
		public int? SelectedOptionIndex { get; set; }
	}

	public class QuizResultDto
	{
		public string Id { get; set; } = string.Empty;
		public int Score { get; set; }
		public int TotalQuestions { get; set; }
		public double Percentage { get; set; }
		public List<QuestionFeedbackDto> Feedback { get; set; } = new();
	}

	public class QuestionFeedbackDto
	{
		public string QuestionId { get; set; } = string.Empty;
		public bool IsCorrect { get; set; }
		public string CorrectOptionId { get; set; } = string.Empty;
		public string Explanation { get; set; } = string.Empty;
	}
}
