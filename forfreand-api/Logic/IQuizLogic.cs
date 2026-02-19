using forfreand_api.DTOs;
using forfreand_api.Models;

namespace forfreand_api.Logic
{
	public interface IQuizLogic
	{
		Task<QuizResult> SubmitResultAsync(QuizResult result);
		Task<IEnumerable<QuizResult>> GetUserResultsAsync(string userId);
		Task<IEnumerable<object>> GetLeaderboardAsync(string? adminId = null);

		// Student Quiz Methods
		// If chapterId is null, it's a subject-wide exam (using subjectId)
		Task<IEnumerable<StudentQuestionDto>> StartQuizAsync(string? subjectId, string? chapterId, int count);
		Task<QuizResultDto> SubmitStudentQuizAsync(string studentId, QuizSubmissionDto submission);
	}
}
