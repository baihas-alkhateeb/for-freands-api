using Google.Cloud.Firestore;
using System.Text.RegularExpressions;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using forfreand_api.Models;
using forfreand_api.Services;

namespace forfreand_api.Logic
{
	public class QuestionLogic(
		FirestoreService firestoreService,
		IUserLogic userLogic,
		PdfService pdfService,
		GeminiService geminiService,
		IActivityLogLogic activityLogLogic) : IQuestionLogic
	{
		private readonly FirestoreDb _db = firestoreService.Db;
		private readonly IUserLogic _userLogic = userLogic;
		private readonly PdfService _pdfService = pdfService;
		private readonly GeminiService _geminiService = geminiService;
		private readonly IActivityLogLogic _activityLogLogic = activityLogLogic;
		private const string CollectionName = "Questions";

		public async Task<IEnumerable<Question>> GetQuestionsAsync(bool includeUnapproved, string? chapterId)
		{
			Query query = _db.Collection(CollectionName);
			if (!includeUnapproved) query = query.WhereEqualTo("isApproved", true);
			if (!string.IsNullOrEmpty(chapterId)) query = query.WhereEqualTo("chapterId", chapterId);

			var snapshot = await query.GetSnapshotAsync();
			return snapshot.Documents.Select(doc =>
			{
				var q = doc.ConvertTo<Question>();
				q.Id = doc.Id;
				return q;
			});
		}

		public async Task<Question?> GetQuestionByIdAsync(string id)
		{
			var doc = await _db.Collection(CollectionName).Document(id).GetSnapshotAsync();
			if (!doc.Exists) return null;
			var q = doc.ConvertTo<Question>();
			q.Id = doc.Id;
			return q;
		}

		public async Task<Question> CreateQuestionAsync(Question question, string? adminId)
		{
			if (adminId != null)
			{
				if (string.IsNullOrEmpty(question.SubjectId)) throw new ArgumentException("SubjectId is required.");
				if (!await _userLogic.HasPermissionAsync(adminId, question.SubjectId))
					throw new UnauthorizedAccessException("Insufficient permissions.");
			}

			var docRef = _db.Collection(CollectionName).Document();
			question.Id = docRef.Id;

			// Ensure options have IDs
			foreach (var option in question.Options)
			{
				if (string.IsNullOrEmpty(option.Id)) option.Id = Guid.NewGuid().ToString();
			}

			await docRef.SetAsync(question);

			// Log Activity
			var user = await _userLogic.GetUserByIdAsync(adminId ?? "Unknown");
			// Fetch subject name for better logging
			var subjectName = "Unknown Subject";
			if (!string.IsNullOrEmpty(question.SubjectId))
			{
				var subjectDoc = await _db.Collection("Subjects").Document(question.SubjectId).GetSnapshotAsync();
				if (subjectDoc.Exists)
				{
					subjectName = subjectDoc.GetValue<string>("name");
				}
			}
			await _activityLogLogic.LogActivityAsync(adminId ?? "Unknown", user?.Username ?? "Unknown", "Created Question", $"Subject: {subjectName}");

			return question;
		}

		public async Task UpdateQuestionAsync(Question question, string adminId)
		{
			if (string.IsNullOrEmpty(question.SubjectId)) throw new ArgumentException("SubjectId is required.");
			if (!await _userLogic.HasPermissionAsync(adminId, question.SubjectId))
				throw new UnauthorizedAccessException("Insufficient permissions.");

			if (string.IsNullOrEmpty(question.Id)) throw new ArgumentException("Id is required.");

			// Ensure options have IDs
			foreach (var option in question.Options)
			{
				if (string.IsNullOrEmpty(option.Id)) option.Id = Guid.NewGuid().ToString();
			}

			await _db.Collection(CollectionName).Document(question.Id).SetAsync(question, SetOptions.Overwrite);
		}

		public async Task ApproveQuestionAsync(string id, string adminId)
		{
			var question = await GetQuestionByIdAsync(id);
			if (question == null) return;

			if (string.IsNullOrEmpty(question.SubjectId) || !await _userLogic.HasPermissionAsync(adminId, question.SubjectId))
				throw new UnauthorizedAccessException("Insufficient permissions or missing SubjectId.");

			await _db.Collection(CollectionName).Document(id).UpdateAsync("isApproved", true);
		}

		public async Task DeleteQuestionAsync(string id, string adminId)
		{
			var question = await GetQuestionByIdAsync(id);
			if (question == null) return;

			if (string.IsNullOrEmpty(question.SubjectId) || !await _userLogic.HasPermissionAsync(adminId, question.SubjectId))
				throw new UnauthorizedAccessException("Insufficient permissions or missing SubjectId.");

			await _db.Collection(CollectionName).Document(id).DeleteAsync();
		}

		public async Task<IEnumerable<Question>> GenerateFromPdfAsync(IFormFile file, string chapterId, int count, int difficulty, int questionType, string language)
		{
			var text = await _pdfService.ExtractTextAsync(file);
			text = _pdfService.RepairArabicText(text);
			var sample = _pdfService.GetRandomContext(text);
			var suggestions = await _geminiService.GenerateQuestionsFromPdfAsync(sample, count, difficulty, questionType, language);
			foreach (var q in suggestions) q.ChapterId = chapterId;
			return suggestions;
		}

		public async Task<IEnumerable<Question>> ExtractFromTemplateAsync(IFormFile file, string chapterId)
		{
			var text = await _pdfService.ExtractTextAsync(file);
			text = _pdfService.RepairArabicText(text);
			var questions = _pdfService.ParseTemplateQuestions(text);
			foreach (var q in questions) q.ChapterId = chapterId;
			return questions;
		}

		public async Task ApproveMultipleAsync(List<Question> questions, string adminId)
		{
			foreach (var q in questions)
			{
				if (string.IsNullOrEmpty(q.SubjectId) || !await _userLogic.HasPermissionAsync(adminId, q.SubjectId))
					throw new UnauthorizedAccessException($"Insufficient permissions for question in chapter: {q.ChapterId}");

				q.IsApproved = true;
				q.IsAiGenerated = true;

				var docRef = _db.Collection(CollectionName).Document();
				q.Id = docRef.Id;
				await docRef.SetAsync(q);
			}

			// Log specific count
			// Fetch subject name for better logging
			string subjectName = "Multiple Subjects";
			if (questions.Count > 0)
			{
				var firstQ = questions[0];
				if (!string.IsNullOrEmpty(firstQ.SubjectId))
				{
					var subjectDoc = await _db.Collection("Subjects").Document(firstQ.SubjectId).GetSnapshotAsync();
					if (subjectDoc.Exists) subjectName = subjectDoc.GetValue<string>("name");
				}
			}

			var user = await _userLogic.GetUserByIdAsync(adminId ?? "Unknown");
			await _activityLogLogic.LogActivityAsync(adminId ?? "Unknown", user?.Username ?? "Unknown", "Approved AI Questions", $"{questions.Count} questions added to {subjectName}");
		}

		public async Task<int> GetQuestionCountAsync(string? adminId = null)
		{
			if (string.IsNullOrEmpty(adminId))
			{
				var snapshot1 = await _db.Collection(CollectionName).GetSnapshotAsync();
				return snapshot1.Count;
			}

			var user = await _userLogic.GetUserByIdAsync(adminId);
			if (user?.Role == "SuperAdmin")
			{
				var snapshot2 = await _db.Collection(CollectionName).GetSnapshotAsync();
				return snapshot2.Count;
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
