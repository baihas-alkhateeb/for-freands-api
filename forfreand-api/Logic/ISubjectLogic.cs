using forfreand_api.Models;

namespace forfreand_api.Logic
{
	public interface ISubjectLogic
	{
		Task<IEnumerable<Subject>> GetAllSubjectsAsync(string? adminId = null);
		Task<Subject?> GetSubjectByIdAsync(string id);
		Task<Subject> CreateSubjectAsync(Subject subject, string adminId);
		Task UpdateSubjectAsync(Subject subject, string adminId);
		Task DeleteSubjectAsync(string id, string adminId);
		Task<int> GetSubjectCountAsync(string? adminId = null);
	}
}
