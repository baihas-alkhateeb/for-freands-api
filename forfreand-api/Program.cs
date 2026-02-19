using forfreand_api.Logic;
using forfreand_api.Models;
using forfreand_api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// builder.Services.AddDbContext<AppDbContext>(...); // Removed for Firebase

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
	// 1. Try to get as string (Environment Variable friendly: "https://a.com,https://b.com")
	var allowedOriginsStr = builder.Configuration["AllowedOrigins"];
	var originList = new List<string>();

	if (!string.IsNullOrEmpty(allowedOriginsStr))
	{
		originList.AddRange(allowedOriginsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
										  .Select(o => o.Trim()));
	}
	else
	{
		// 2. Fallback to appsettings.json array
		var appSettingsOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
		if (appSettingsOrigins != null)
		{
			originList.AddRange(appSettingsOrigins);
		}
	}

	// 3. Always allow localhost for React development
	originList.Add("http://localhost:3000");
	originList.Add("http://localhost:5173");

	var allowedOrigins = originList.Distinct().ToArray();

	options.AddPolicy("AllowLocalhost",
		policy => policy.WithOrigins(allowedOrigins)
						  .AllowAnyMethod()
						  .AllowAnyHeader()
						  .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Core Services
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddSingleton<FirestoreService>();

// Business Logic Layer (BLL)
builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<ISubjectLogic, SubjectLogic>();
builder.Services.AddScoped<IChapterLogic, ChapterLogic>();
builder.Services.AddScoped<IQuestionLogic, QuestionLogic>();
builder.Services.AddScoped<IQuizLogic, QuizLogic>();
builder.Services.AddScoped<IActivityLogLogic, ActivityLogLogic>();
builder.Services.AddScoped<IStageLogic, StageLogic>();

// Background Services
builder.Services.AddHostedService<CleanupService>();

var app = builder.Build();

// Seeding logic (Adapted for Firebase)
using (var scope = app.Services.CreateScope())
{
	var firestoreService = scope.ServiceProvider.GetRequiredService<FirestoreService>();
	if (firestoreService.IsConfigured)
	{
		try
		{
			var userLogic = scope.ServiceProvider.GetRequiredService<IUserLogic>();
			// Admin seeding check is handled in Logic or here
			var admin = await userLogic.LoginAsync("superadmin", "superadmin123");
			if (admin == null)
			{
				await userLogic.RegisterAsync(new User { Username = "superadmin", Password = "superadmin123", Role = "SuperAdmin" });
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error during admin seeding: {ex.Message}");
			Console.WriteLine("Please ensure Firestore API is enabled in your Google Cloud Project.");
		}
	}
	else
	{
		Console.WriteLine("Skipping admin seeding: Firestore is not configured.");
	}
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// Configure for Render/Proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");
app.UseAuthorization();
app.MapControllers();

app.Run();
