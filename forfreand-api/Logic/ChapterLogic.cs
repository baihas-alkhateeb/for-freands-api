using Google.Cloud.Firestore;
using forfreand_api.Models;
using forfreand_api.Services;

namespace forfreand_api.Logic
{
	public class ChapterLogic(FirestoreService firestoreService, IUserLogic userLogic) : IChapterLogic
	{
		private readonly FirestoreDb _db = firestoreService.Db;
		private readonly IUserLogic _userLogic = userLogic;
		private const string CollectionName = "Chapters";

		public async Task<IEnumerable<Chapter>> GetAllChaptersAsync()
		{
			var snapshot = await _db.Collection(CollectionName).GetSnapshotAsync();
			return snapshot.Documents.Select(doc =>
			{
				var c = doc.ConvertTo<Chapter>();
				c.Id = doc.Id;
				return c;
			});
		}

		public async Task<IEnumerable<Chapter>> GetChaptersBySubjectAsync(string subjectId)
		{
			var snapshot = await _db.Collection(CollectionName).WhereEqualTo("subjectId", subjectId).GetSnapshotAsync();
			return snapshot.Documents.Select(doc =>
			{
				var c = doc.ConvertTo<Chapter>();
				c.Id = doc.Id;
				return c;
			});
		}

		public async Task<Chapter?> GetChapterByIdAsync(string id)
		{
			var doc = await _db.Collection(CollectionName).Document(id).GetSnapshotAsync();
			if (!doc.Exists) return null;
			var c = doc.ConvertTo<Chapter>();
			c.Id = doc.Id;
			return c;
		}

		public async Task<Chapter> CreateChapterAsync(Chapter chapter, string adminId)
		{
			if (!await _userLogic.HasPermissionAsync(adminId, chapter.SubjectId))
				throw new UnauthorizedAccessException("Insufficient permissions.");

			var docRef = _db.Collection(CollectionName).Document();
			chapter.Id = docRef.Id;
			await docRef.SetAsync(chapter);
			return chapter;
		}

		public async Task DeleteChapterAsync(string id, string adminId)
		{
			var chapter = await GetChapterByIdAsync(id);
			if (chapter == null) return;

			if (!await _userLogic.HasPermissionAsync(adminId, chapter.SubjectId))
				throw new UnauthorizedAccessException("Insufficient permissions.");

			await _db.Collection(CollectionName).Document(id).DeleteAsync();
		}

		public async Task<IEnumerable<Question>> GetQuestionsByChapterIdAsync(string chapterId)
		{
			var snapshot = await _db.Collection("Questions")
				.WhereEqualTo("chapterId", chapterId)
				.WhereEqualTo("isApproved", true)
				.GetSnapshotAsync();

			return snapshot.Documents.Select(doc =>
			{
				var q = doc.ConvertTo<Question>();
				q.Id = doc.Id;
				return q;
			});
		}
	}
}
