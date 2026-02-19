namespace forfreand_api.DTOs
{
	public class StudentRegisterDto
	{
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string FullName { get; set; } = string.Empty; // Adding FullName since it's usually good to have
	}
}
