using Google.Cloud.Firestore;
using forfreand_api.Models;
using forfreand_api.Services;

namespace forfreand_api.Logic
{
	public class SubjectLogic(FirestoreService firestoreService, IUserLogic userLogic, IActivityLogLogic activityLogLogic) : ISubjectLogic
	{
		private readonly FirestoreDb _db = firestoreService.Db;
		private readonly IUserLogic _userLogic = userLogic;
		private readonly IActivityLogLogic _activityLogLogic = activityLogLogic;
		private const string CollectionName = "Subjects";

		public async Task<IEnumerable<Subject>> GetAllSubjectsAsync(string? adminId = null)
		{
			var snapshot = await _db.Collection(CollectionName).GetSnapshotAsync();
			var subjects = snapshot.Documents.Select(doc =>
			{
				var s = doc.ConvertTo<Subject>();
				s.Id = doc.Id;
				return s;
			});

			if (string.IsNullOrEmpty(adminId)) return subjects;

			var user = await _userLogic.GetUserByIdAsync(adminId);
			if (user?.Role == "SuperAdmin") return subjects;

			if (user?.Role == "Assistant" || user?.Role == "Student")
			{
				// Return ALL subjects for read-only view
				return subjects;
			}

			return [];
		}

		public async Task<Subject?> GetSubjectByIdAsync(string id)
		{
			var doc = await _db.Collection(CollectionName).Document(id).GetSnapshotAsync();
			if (!doc.Exists) return null;
			var s = doc.ConvertTo<Subject>();
			s.Id = doc.Id;
			return s;
		}

		public async Task<Subject> CreateSubjectAsync(Subject subject, string adminId)
		{
			if (!await _userLogic.IsSuperAdminAsync(adminId)) throw new UnauthorizedAccessException("Only SuperAdmin can create subjects.");

			var docRef = _db.Collection(CollectionName).Document();
			subject.Id = docRef.Id;

			// Ensure chapters have subjectId
			foreach (var chapter in subject.Chapters)
			{
				chapter.SubjectId = subject.Id;
				if (string.IsNullOrEmpty(chapter.Id)) chapter.Id = Guid.NewGuid().ToString();
			}

			await docRef.SetAsync(subject);

			var user = await _userLogic.GetUserByIdAsync(adminId);
			await _activityLogLogic.LogActivityAsync(adminId, user?.Username ?? "Unknown", "Created Subject", subject.Name);

			return subject;
		}

		public async Task UpdateSubjectAsync(Subject subject, string adminId)
		{
			if (!await _userLogic.HasPermissionAsync(adminId, subject.Id)) throw new UnauthorizedAccessException("Insufficient permissions.");
			if (string.IsNullOrEmpty(subject.Id)) throw new ArgumentException("Id is required.");

			// Ensure chapters have subjectId and Ids
			foreach (var chapter in subject.Chapters)
			{
				chapter.SubjectId = subject.Id;
				if (string.IsNullOrEmpty(chapter.Id)) chapter.Id = Guid.NewGuid().ToString();
			}

			await _db.Collection(CollectionName).Document(subject.Id).SetAsync(subject, SetOptions.Overwrite);
		}

		public async Task DeleteSubjectAsync(string id, string adminId)
		{
			if (!await _userLogic.IsSuperAdminAsync(adminId)) throw new UnauthorizedAccessException("Only SuperAdmin can delete subjects.");
			await _db.Collection(CollectionName).Document(id).DeleteAsync();
		}

		public async Task<int> GetSubjectCountAsync(string? adminId = null)
		{
			if (string.IsNullOrEmpty(adminId))
			{
				var snapshot = await _db.Collection(CollectionName).GetSnapshotAsync();
				return snapshot.Count;
			}

			var user = await _userLogic.GetUserByIdAsync(adminId);
			if (user?.Role == "SuperAdmin")
			{
				var snapshot = await _db.Collection(CollectionName).GetSnapshotAsync();
				return snapshot.Count;
			}

			if (user?.Role == "Assistant")
			{
				// User requested: Assistant sees ALL stats (read-only)
				var snapshot = await _db.Collection(CollectionName).GetSnapshotAsync();
				return snapshot.Count;
			}

			return 0;
		}
	}
}
