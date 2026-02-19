using System.Text.Json;
using forfreand_api.Models;
using forfreand_api.Enums;
using System.Text;
using Google.GenAI;
using Google.GenAI.Types;

namespace forfreand_api.Services
{
	public class GeminiService
	{
		private readonly Client? _client;
		private readonly ILogger<GeminiService> _logger;
		public bool IsConfigured { get; }

		public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
		{
			_logger = logger;
			var apiKey = configuration["Gemini:ApiKey"];

			if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
			{
				_logger.LogWarning("Gemini API Key is missing or has default value. AI features will be disabled.");
				IsConfigured = false;
			}
			else
			{
				_client = new Client(apiKey: apiKey);
				IsConfigured = true;
			}
		}

		public async Task<string> RepairDistortedTextAsync(string garbledText)
		{
			if (!IsConfigured || _client == null || string.IsNullOrWhiteSpace(garbledText))
			{
				return garbledText;
			}

			var prompt = $@"
                The following text was extracted from a PDF and contains distorted Arabic text (possibly reversed characters, words, or extraction artifacts).
                
                YOUR TASK:
                1. Repair the Arabic text so it is readable and follows logical character/word order.
                2. Fix common PDF extraction noise (e.g., isolated letters, missing spaces).
                3. IMPORTANT: You MUST preserve all structural markers exactly as they are: '###', 'Qt:', 'OP1:', 'OP2:', 'OP3:', 'OP4:', 'TOP:'.
                4. Do NOT translate the text. Keep it in its original language (Arabic).
                5. Return ONLY the repaired text.

                DISTORTED TEXT:
                {garbledText}
            ";

			try
			{
				var response = await _client.Models.GenerateContentAsync(
					model: "gemini-1.5-flash",
					contents: prompt
				);

				var repairedText = response.Candidates?[0]?.Content?.Parts?[0]?.Text;
				return repairedText?.Trim() ?? garbledText;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error repairing text via Gemini");
				return garbledText; // Fallback to original
			}
		}

		public async Task<List<Question>> GenerateQuestionsFromPdfAsync(string contextText, int count, int difficulty, int questionType, string language)
		{
			if (!IsConfigured || _client == null)
			{
				_logger.LogWarning("GenerateQuestionsFromPdfAsync called but Gemini is not configured.");
				return [];
			}

			if (string.IsNullOrWhiteSpace(contextText))
			{
				_logger.LogWarning("GenerateQuestionsFromPdfAsync called with empty context.");
				return [];
			}

			string typeString = questionType switch
			{
				1 => "Multiple Choice",
				2 => "True/False",
				_ => "Mixed (Multiple Choice and True/False)"
			};

			string difficultyString = difficulty switch
			{
				1 => "Easy",
				2 => "Medium",
				3 => "Hard",
				_ => "Medium"
			};

			string langString = "English";
			if (language.Equals("ar", StringComparison.OrdinalIgnoreCase)) langString = "Arabic";

			var prompt = $@"
                Base every question and its options EXCLUSIVELY on the information provided in the 'TEXT' section below.
                
                CRITICAL CONSTRAINTS:
                1. Do NOT refer to 'code', 'functions', 'widgets', or specific programming libraries UNLESS they are explicitly written in the provided TEXT.
                2. If the TEXT does not contain programming source code, do NOT generate technical coding questions. Use conceptual or theoretical questions instead.
                3. If a question is about a specific piece of code or function from the TEXT, you MUST include that code snippet inside the question text itself so the student can see it.
                4. Do NOT hallucinate or use external knowledge not present in the provided text.
                5. Match the language of the questions and options to the requested language ({langString}).

                Generate {count} educational questions based on the following lecture text.

                Parameters:
                - Type: {typeString}
                - Difficulty: {difficultyString}
                - Language: {langString}

                TEXT:
                {contextText}

                Return ONLY a JSON array in the following format:
                [
                    {{
                        ""text"": ""Question text here"",
                        ""type"": 1, 
                        ""difficulty"": {difficulty},
                        ""options"": [
                            {{ ""text"": ""Option 1"", ""isCorrect"": true }},
                            {{ ""text"": ""Option 2"", ""isCorrect"": false }},
                            ...
                        ]
                    }}
                ]
                Type: 1 for MultipleChoice, 2 for TrueFalse.
            ";

			try
			{
				var response = await _client.Models.GenerateContentAsync(
					model: "gemini-3-flash-preview",
					contents: prompt
				);

				var aiText = response.Candidates?[0]?.Content?.Parts?[0]?.Text;

				if (string.IsNullOrEmpty(aiText)) return [];

				if (aiText.Contains("```json"))
				{
					aiText = aiText.Split("```json")[1].Split("```")[0].Trim();
				}

				var results = JsonSerializer.Deserialize<List<QuestionSuggestion>>(aiText, _jsonOptions);

				if (results == null) return [];

				return results.Select(q => new Question
				{
					Text = q.Text,
					Difficulty = q.Difficulty,
					Type = (QuestionType)(q.Type == 0 ? 1 : q.Type),
					IsAiGenerated = true,
					IsApproved = false,
					Options = [.. q.Options.Select(o => new Option { Text = o.Text, IsCorrect = o.IsCorrect })]
				}).ToList();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating questions via Gemini SDK");
				throw;
			}
		}

		private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

		private class QuestionSuggestion
		{
			public string Text { get; set; } = string.Empty;
			public int Type { get; set; }
			public int Difficulty { get; set; }
			public List<OptionSuggestion> Options { get; set; } = [];
		}

		private class OptionSuggestion
		{
			public string Text { get; set; } = string.Empty;
			public bool IsCorrect { get; set; }
		}
	}
}
