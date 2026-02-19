using System.Net;
using System.Text.Json;

namespace forfreand_api.Middleware
{
	public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
	{
		private readonly RequestDelegate _next = next;
		private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
				await HandleExceptionAsync(context, ex);
			}
		}

		private static Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			var code = HttpStatusCode.InternalServerError; // 500 if unexpected

			if (exception is UnauthorizedAccessException) code = HttpStatusCode.Unauthorized;
			else if (exception is ArgumentException) code = HttpStatusCode.BadRequest;

			var result = JsonSerializer.Serialize(new
			{
				error = exception.Message,
				type = exception.GetType().Name,
				status = (int)code
			});

			context.Response.ContentType = "application/json";
			context.Response.StatusCode = (int)code;

			return context.Response.WriteAsync(result);
		}
	}
}
