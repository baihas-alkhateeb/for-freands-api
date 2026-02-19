using forfreand_api.Models;

namespace forfreand_api.Logic
{
	public interface IChapterLogic
	{
		Task<IEnumerable<Chapter>> GetAllChaptersAsync();
		Task<IEnumerable<Chapter>> GetChaptersBySubjectAsync(string subjectId);
		Task<Chapter?> GetChapterByIdAsync(string id);
		Task<Chapter> CreateChapterAsync(Chapter chapter, string adminId);
		Task DeleteChapterAsync(string id, string adminId);
		Task<IEnumerable<Question>> GetQuestionsByChapterIdAsync(string chapterId);
	}
}
