namespace forfreand_api.Models
{
	public class LoginResponseDto
	{
		public string Id { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public List<string> AssignedSubjectIds { get; set; } = [];
		public string Token { get; set; } = string.Empty; // Placeholder for future JWT
	}
}
