using forfreand_api.Models;

namespace forfreand_api.Logic
{
	public interface IQuestionLogic
	{
		Task<IEnumerable<Question>> GetQuestionsAsync(bool includeUnapproved, string? chapterId);
		Task<Question?> GetQuestionByIdAsync(string id);
		Task<Question> CreateQuestionAsync(Question question, string? adminId);
		Task UpdateQuestionAsync(Question question, string adminId);
		Task ApproveQuestionAsync(string id, string adminId);
		Task DeleteQuestionAsync(string id, string adminId);

		// PDF/AI Integration
		Task<IEnumerable<Question>> GenerateFromPdfAsync(IFormFile file, string chapterId, int count, int difficulty, int questionType, string language);
		Task<IEnumerable<Question>> ExtractFromTemplateAsync(IFormFile file, string chapterId);
		Task ApproveMultipleAsync(List<Question> questions, string adminId);
		Task<int> GetQuestionCountAsync(string? adminId = null);
	}
}
