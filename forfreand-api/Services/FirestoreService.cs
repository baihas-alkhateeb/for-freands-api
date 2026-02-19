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

			// 1. Try file path first (Local Development)
			string? authPath = configuration["Firebase:ServiceAccountPath"];
			if (!string.IsNullOrEmpty(authPath) && File.Exists(authPath))
			{
				Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", authPath);
			}
			// 2. Try environment variable content (Production / Render)
			else
			{
				var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");
				if (!string.IsNullOrEmpty(json))
				{
					logger.LogInformation("Found GOOGLE_CREDENTIALS environment variable. Length: {Length}", json.Length);
					try
					{
						var builder = new FirestoreDbBuilder
						{
							ProjectId = projectId,
							JsonCredentials = json
						};
						_db = builder.Build();
						IsConfigured = true;
						logger.LogInformation("Successfully initialized FirestoreDb from GOOGLE_CREDENTIALS.");
						return; // Exit constructor successfully
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Failed to initialize FirestoreDb from GOOGLE_CREDENTIALS environment variable.");
						IsConfigured = false;
						return;
					}
				}
				else
				{
					logger.LogWarning("GOOGLE_CREDENTIALS environment variable not found or empty.");
				}
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
