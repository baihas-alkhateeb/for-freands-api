using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace forfreand_api.Services
{
	public class FirestoreService
	{
		private readonly FirestoreDb? _db;
		public bool IsConfigured { get; }

		public FirestoreService(IConfiguration configuration, ILogger<FirestoreService> logger)
		{
			string? projectId = configuration["Firebase:ProjectId"];
			if (string.IsNullOrEmpty(projectId) || projectId == "YOUR_PROJECT_ID")
			{
				logger.LogWarning("Firebase ProjectId is missing or has default value. Firestore features will be disabled.");
				IsConfigured = false;
				return;
			}

			// سيتم استخدام ملف serviceAccountKey.json المحمل في الجذر تلقائياً إذا تم ضبط البيئة
			// أو يمكننا تحديده برمجياً هنا
			string? authPath = configuration["Firebase:ServiceAccountPath"];
			if (!string.IsNullOrEmpty(authPath) && File.Exists(authPath))
			{
				Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", authPath);
			}

			try
			{
				_db = FirestoreDb.Create(projectId);
				IsConfigured = true;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to initialize FirestoreDb with ProjectId: {ProjectId}", projectId);
				IsConfigured = false;
			}
		}

		public FirestoreDb Db => _db ?? throw new InvalidOperationException("Firestore is not configured. Please check your appsettings.json.");
	}
}
