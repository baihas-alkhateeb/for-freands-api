using Google.Cloud.Firestore;
using forfreand_api.Models;
using forfreand_api.Services;
using forfreand_api.DTOs;
using System.Linq;

namespace forfreand_api.Logic
{
	public class QuizLogic(FirestoreService firestoreService, IUserLogic userLogic, ISubjectLogic subjectLogic, IChapterLogic chapterLogic) : IQuizLogic
	{
		private readonly FirestoreDb _db = firestoreService.Db;
		private readonly IUserLogic _userLogic = userLogic;
		private readonly ISubjectLogic _subjectLogic = subjectLogic;
		private readonly IChapterLogic _chapterLogic = chapterLogic;
		private const string CollectionName = "QuizResults";

		public async Task<QuizResult> SubmitResultAsync(QuizResult result)
		{
			result.TakenAt = DateTime.UtcNow;
			var docRef = _db.Collection(CollectionName).Document();
			result.Id = docRef.Id;
			await docRef.SetAsync(result);
			return result;
		}

		public async Task<IEnumerable<QuizResult>> GetUserResultsAsync(string userId)
		{
			var snapshot = await _db.Collection(CollectionName).WhereEqualTo("userId", userId).GetSnapshotAsync();
			return snapshot.Documents.Select(doc =>
			{
				var r = doc.ConvertTo<QuizResult>();
				r.Id = doc.Id;
				return r;
			});
		}

		public async Task<IEnumerable<object>> GetLeaderboardAsync(string? adminId = null)
		{
			var snapshot = await _db.Collection(CollectionName).OrderByDescending("takenAt").Limit(100).GetSnapshotAsync();
			var rawResults = snapshot.Documents.Select(doc =>
			{
				var r = doc.ConvertTo<QuizResult>();
				r.Id = doc.Id;
				return r;
			}).ToList();

			// Filtering for Assistants
			var user = string.IsNullOrEmpty(adminId) ? null : await _userLogic.GetUserByIdAsync(adminId);
			if (user != null && user.Role == "Assistant")
			{
				rawResults = rawResults.Where(r => r.SubjectId != null && user.AssignedSubjectIds.Contains(r.SubjectId)).ToList();
			}
			// Students can see ALL results (Global Leaderboard)

			// Enrichment
			var studentIds = rawResults.Select(r => r.UserId).Where(id => id != null).Distinct().ToList();
			var subjectIds = rawResults.Select(r => r.SubjectId).Where(id => id != null).Distinct().ToList();
			var chapterIds = rawResults.Where(r => !string.IsNullOrEmpty(r.ChapterId)).Select(r => r.ChapterId).Where(id => id != null).Distinct().ToList();

			var studentsList = await _userLogic.GetAllUsersAsync();
			var students = studentsList.Where(u => u.Id != null).ToDictionary(u => u.Id!, u => u.Username);

			var subjectsList = await _subjectLogic.GetAllSubjectsAsync();
			var subjects = subjectsList.Where(s => s.Id != null).ToDictionary(s => s.Id!, s => s.Name);

			var chaptersList = await _chapterLogic.GetAllChaptersAsync();
			var chapters = chaptersList.Where(c => c.Id != null).ToDictionary(c => c.Id!, c => c.Title);

			var leaderboard = rawResults.Select(r => new
			{
				r.Id,
				r.Score,
				r.TotalQuestions,
				r.TakenAt,
				r.UserId,
				Username = r.UserId != null && students.TryGetValue(r.UserId, out var uname) ? uname : "طالب غير معروف",
				r.SubjectId,
				SubjectName = r.SubjectId != null && subjects.TryGetValue(r.SubjectId, out var sname) ? sname : "مادة غير معروفة",
				r.ChapterId,
				ChapterName = r.ChapterId != null && chapters.TryGetValue(r.ChapterId, out var cname) ? cname : null,
				TestType = string.IsNullOrEmpty(r.ChapterId) ? "مادة كاملة" : "محاضرة"
			});


			return leaderboard;
		}

		public async Task<IEnumerable<StudentQuestionDto>> StartQuizAsync(string? subjectId, string? chapterId, int count)
		{
			// 1. Fetch Questions
			IEnumerable<Question> allQuestions = [];
			if (!string.IsNullOrEmpty(chapterId))
			{
				allQuestions = await _chapterLogic.GetQuestionsByChapterIdAsync(chapterId);
			}
			else if (!string.IsNullOrEmpty(subjectId))
			{
				// Fetch metadata to find all chapters? Or direct query if Question has SubjectId (it does!)
				var query = _db.Collection("Questions").WhereEqualTo("subjectId", subjectId).WhereEqualTo("isApproved", true);
				var snapshot = await query.GetSnapshotAsync();
				allQuestions = snapshot.Documents.Select(doc =>
				{
					var q = doc.ConvertTo<Question>();
					q.Id = doc.Id;
					return q;
				});
			}

			// 2. Randomize & Limit
			var randomQuestions = allQuestions.OrderBy(x => Guid.NewGuid()).Take(count).ToList();

			// 2.5. Auto-fix missing Option IDs (Self-healing)
			foreach (var q in randomQuestions)
			{
				bool needsUpdate = false;
				foreach (var o in q.Options)
				{
					if (string.IsNullOrEmpty(o.Id))
					{
						o.Id = Guid.NewGuid().ToString();
						needsUpdate = true;
					}
				}

				if (needsUpdate)
				{
					// Update the question in the database to persist the new IDs
					// We use a fire-and-forget approach or await? Await is safer to ensure consistency.
					await _db.Collection("Questions").Document(q.Id).SetAsync(q, SetOptions.Overwrite);
				}
			}

			// 3. Map to DTO (Hide Answers)
			return randomQuestions.Select(q => new StudentQuestionDto
			{
				Id = q.Id!,
				Text = q.Text!,
				Type = (int)q.Type,
				ImageUrl = q.ImageUrl,
				Options = q.Options?.Select(o => new StudentOptionDto
				{
					Id = o.Id ?? "",
					Text = o.Text ?? ""
				}).ToList() ?? []
			});
		}

		public async Task<QuizResultDto> SubmitStudentQuizAsync(string studentId, QuizSubmissionDto submission)
		{
			int score = 0;
			int total = submission.Answers.Count;
			var feedbackList = new List<QuestionFeedbackDto>();

			// 1. Fetch original questions to verify answers
			foreach (var answer in submission.Answers)
			{
				var qDoc = await _db.Collection("Questions").Document(answer.QuestionId).GetSnapshotAsync();
				if (!qDoc.Exists) continue;

				var question = qDoc.ConvertTo<Question>();
				question.Id = qDoc.Id;

				bool isCorrect = false;
				string correctOptionId = "";
				string explanation = question.Explanation ?? ""; // Assuming we might store explanation later

				// Determine correctness based on Type
				// Determine correctness
				// We support MultipleChoice (1) and TrueFalse (2)
				// Both use the Options list with IsCorrect flag.
				if (question.Type == Enums.QuestionType.MultipleChoice || question.Type == Enums.QuestionType.TrueFalse || (int)question.Type == 0)
				{
					var correctOpt = question.Options.FirstOrDefault(o => o.IsCorrect);
					correctOptionId = correctOpt?.Id ?? "";
					if (correctOpt != null && correctOpt.Id == answer.SelectedOptionId)
					{
						isCorrect = true;
						score++;
					}
				}

				feedbackList.Add(new QuestionFeedbackDto
				{
					QuestionId = question.Id!,
					IsCorrect = isCorrect,
					CorrectOptionId = correctOptionId,
					Explanation = question.Explanation ?? ""
				});
			}

			// 2. Save Result
			var result = new QuizResult
			{
				UserId = studentId,
				Score = score,
				TotalQuestions = total,
				TakenAt = DateTime.UtcNow,
				SubjectId = submission.IsSubjectExam ? submission.ContextId : null, // If subject exam, ContextId is SubjectId
				ChapterId = !submission.IsSubjectExam ? submission.ContextId : null  // If chapter exam, ContextId is ChapterId
			};

			// If it was a subject exam, we need to ensure SubjectId is set.
			// If it was a chapter exam, we should probably ALSO fetch the subjectId from the chapter/question to store it?
			// For simplicity/speed:
			if (!submission.IsSubjectExam)
			{
				// Fetch SubjectId from Chapter...
				// Or leave it null? Leaderboard uses SubjectId. 
				// Better to fetch one question's subject ID or assume caller sends valid stuff?
				// Let's try to look it up if we have time, or rely on frontend sending it? 
				// Frontend sends ContextId. 
				// Let's leave as is for now, maybe enhance later.
				// Wait, if ChapterId is set, Leaderboard tries to show SubjectName.
				// Leaderboard logic: r.SubjectId != null.
				// We should try to set SubjectId even for Chapter exams.
				if (!string.IsNullOrEmpty(result.ChapterId))
				{
					var chDoc = await _db.Collection("Chapters").Document(result.ChapterId).GetSnapshotAsync();
					if (chDoc.Exists)
					{
						result.SubjectId = chDoc.GetValue<string>("subjectId");
					}
				}
			}

			await SubmitResultAsync(result);

			return new QuizResultDto
			{
				Id = result.Id!,
				Score = score,
				TotalQuestions = total,
				Percentage = total > 0 ? (double)score / total * 100 : 0,
				Feedback = feedbackList
			};
		}
	}
}
