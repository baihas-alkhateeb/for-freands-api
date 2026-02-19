using UglyToad.PdfPig;
using forfreand_api.Models;
using forfreand_api.Enums;
using System.Text.RegularExpressions;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace forfreand_api.Services
{
	public partial class PdfService
	{
		private static readonly string[] Separator = ["###"];

		public async Task<string> ExtractTextAsync(IFormFile file)
		{
			var extension = Path.GetExtension(file.FileName).ToLower();

			return extension switch
			{
				".pdf" => await ExtractFromPdfAsync(file),
				".txt" => await ExtractFromTxtAsync(file),
				".docx" => await ExtractFromDocxAsync(file),
				_ => throw new NotSupportedException($"File type {extension} is not supported.")
			};
		}

		private static async Task<string> ExtractFromPdfAsync(IFormFile file)
		{
			return await Task.Run(() =>
			{
				using var stream = file.OpenReadStream();
				using var pdf = PdfDocument.Open(stream);
				var text = "";
				foreach (var page in pdf.GetPages())
				{
					text += page.Text;
				}
				return text;
			});
		}

		private static async Task<string> ExtractFromTxtAsync(IFormFile file)
		{
			using var reader = new StreamReader(file.OpenReadStream());
			return await reader.ReadToEndAsync();
		}

		private static async Task<string> ExtractFromDocxAsync(IFormFile file)
		{
			return await Task.Run(() =>
			{
				var sb = new StringBuilder();
				using var stream = file.OpenReadStream();
				using var doc = WordprocessingDocument.Open(stream, false);
				var body = doc.MainDocumentPart?.Document.Body;
				if (body == null) return string.Empty;

				foreach (var paragraph in body.Elements<Paragraph>())
				{
					sb.AppendLine(paragraph.InnerText);
				}
				return sb.ToString();
			});
		}

		public List<Question> ParseTemplateQuestions(string text)
		{
			var questions = new List<Question>();
			// Split by "###" to find each question block clearly
			var blocks = text.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

			foreach (var block in blocks)
			{
				var question = new Question();

				// Get Question Text: starts with Qt:
				var qtMatch = QtRegex().Match(block);
				if (!qtMatch.Success) continue;

				question.Text = qtMatch.Groups[1].Value.Trim();

				if (string.IsNullOrEmpty(question.Text)) continue;

				// Check type: If OP1 exists, it's Multiple Choice
				if (block.Contains("OP1:"))
				{
					question.Type = QuestionType.MultipleChoice;
					for (int i = 1; i <= 4; i++)
					{
						var opMatch = Regex.Match(block, $@"OP{i}:(.*?)(?=OP{i + 1}:|TOP:|Qt:|$)", RegexOptions.Singleline);
						if (opMatch.Success)
						{
							var opText = opMatch.Groups[1].Value.Trim();
							question.Options.Add(new Option { Text = opText });
						}
					}

					// Find Correct Option from TOP:
					var topMatch = TopRegex().Match(block);
					if (topMatch.Success)
					{
						var correctText = topMatch.Groups[1].Value.Trim();
						var correctOption = question.Options.FirstOrDefault(o => o.Text.Equals(correctText, StringComparison.OrdinalIgnoreCase) || correctText.Contains(o.Text));
						if (correctOption != null) correctOption.IsCorrect = true;
					}
				}
				else if (block.Contains("TOP:")) // True/False
				{
					question.Type = QuestionType.TrueFalse;
					var topMatch = TopRegex().Match(block);
					if (topMatch.Success)
					{
						var isTrue = topMatch.Groups[1].Value.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
						question.Options.Add(new Option { Text = "True", IsCorrect = isTrue });
						question.Options.Add(new Option { Text = "False", IsCorrect = !isTrue });
					}
				}

				question.IsApproved = false; // Must be reviewed
				questions.Add(question);
			}

			return questions;
		}

		public string GetRandomContext(string text, int length = 2000)
		{
			if (text.Length <= length) return text;
			var random = new Random();
			int start = random.Next(0, text.Length - length);
			return text.Substring(start, length);
		}

		public string RepairArabicText(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return text;

			var result = new StringBuilder();
			var currentSegment = new StringBuilder();
			bool? isLastArabic = null;

			foreach (var c in text)
			{
				bool isArabic = IsArabic(c);
				bool isWhitespace = char.IsWhiteSpace(c);

				// Only consider real language changes, ignore whitespaces for direction switching
				if (!isWhitespace && isLastArabic != null && isArabic != isLastArabic)
				{
					// Language switch detected
					string segment = currentSegment.ToString();
					if (isLastArabic.Value)
					{
						// Reverse Arabic block
						segment = ReverseString(segment);
					}
					result.AppendLine(segment.Trim());
					currentSegment.Clear();
				}

				currentSegment.Append(c);
				if (!isWhitespace) isLastArabic = isArabic;
			}

			// Final segment
			if (currentSegment.Length > 0)
			{
				string segment = currentSegment.ToString();
				if (isLastArabic == true) segment = ReverseString(segment);
				result.AppendLine(segment.Trim());
			}

			return result.ToString();
		}

		private bool IsArabic(char c)
		{
			return (c >= 0x0600 && c <= 0x06FF) || (c >= 0x0750 && c <= 0x077F) || (c >= 0x08A0 && c <= 0x08FF) || (c >= 0xFB50 && c <= 0xFDFF) || (c >= 0xFE70 && c <= 0xFEFF);
		}

		private string ReverseString(string s)
		{
			char[] charArray = s.ToCharArray();
			Array.Reverse(charArray);
			return new string(charArray);
		}

		[GeneratedRegex(@"Qt:(.*?)(?=OP1:|TOP:|$)", RegexOptions.Singleline)]
		private static partial Regex QtRegex();

		[GeneratedRegex(@"TOP:(.*?)(?=Qt:|$)", RegexOptions.Singleline)]
		private static partial Regex TopRegex();
	}
}
