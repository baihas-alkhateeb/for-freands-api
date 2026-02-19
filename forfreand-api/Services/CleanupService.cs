using Google.Cloud.Firestore;
using forfreand_api.Models;

namespace forfreand_api.Services
{
	public class CleanupService(IServiceScopeFactory scopeFactory, ILogger<CleanupService> logger) : BackgroundService
	{
		private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
		private readonly ILogger<CleanupService> _logger = logger;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Cleanup Service is starting.");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _scopeFactory.CreateScope();
					var firestoreService = scope.ServiceProvider.GetRequiredService<FirestoreService>();
					var db = firestoreService.Db;

					await CleanInactiveStudentsAsync(db);
					await HandleLeaderboardResetAsync(db);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred executing cleanup tasks.");
				}

				// Wait for 12 hours before next check
				await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
			}
		}

		private async Task CleanInactiveStudentsAsync(FirestoreDb db)
		{
			_logger.LogInformation("Checking for inactive students...");

			var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
			var studentsQuery = db.Collection("Users")
				.WhereEqualTo("role", "Student");

			var snapshot = await studentsQuery.GetSnapshotAsync();
			int deletedCount = 0;

			foreach (var doc in snapshot.Documents)
			{
				var user = doc.ConvertTo<User>();
				// Delete if never logged in or last login > 30 days
				if (user.LastLoginAt == null || user.LastLoginAt < thirtyDaysAgo)
				{
					await doc.Reference.DeleteAsync();
					deletedCount++;
				}
			}

			if (deletedCount > 0)
			{
				_logger.LogInformation($"Deleted {deletedCount} inactive student accounts.");
			}
		}

		private async Task HandleLeaderboardResetAsync(FirestoreDb db)
		{
			_logger.LogInformation("Checking leaderboard reset status...");

			var sysDataRef = db.Collection("SystemData").Document("LeaderboardStatus");
			var snapshot = await sysDataRef.GetSnapshotAsync();

			DateTime lastReset = DateTime.MinValue;
			if (snapshot.Exists)
			{
				lastReset = snapshot.GetValue<DateTime>("lastResetAt");
			}

			if (DateTime.UtcNow - lastReset > TimeSpan.FromDays(15))
			{
				_logger.LogInformation("Resetting leaderboard (15 days elapsed)...");

				var resultsRef = db.Collection("QuizResults");
				var resultsSnapshot = await resultsRef.GetSnapshotAsync();

				// Firestore batching for deletion
				var batch = db.StartBatch();
				int count = 0;
				foreach (var doc in resultsSnapshot.Documents)
				{
					batch.Delete(doc.Reference);
					count++;
					if (count >= 500) // Firestore batch limit
					{
						await batch.CommitAsync();
						batch = db.StartBatch();
						count = 0;
					}
				}

				if (count > 0) await batch.CommitAsync();

				// Update last reset date
				await sysDataRef.SetAsync(new Dictionary<string, object>
				{
					{ "lastResetAt", DateTime.UtcNow }
				});

				_logger.LogInformation("Leaderboard has been cleared.");
			}
		}
	}
}
