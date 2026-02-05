using System.Net;
using System.Text.Json;
using TestAPI.Models;

namespace TestAPI.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred processing your request."
        };

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = validationException.Errors.ToList()
                };
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new ApiResponse<object>
                {
                    Success = false,
                    Message = exception.Message
                };
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Unauthorized access"
                };
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ApiResponse<object>
                {
                    Success = false,
                    Message = exception.Message
                };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new ApiResponse<object>
                {
                    Success = false,
                    Message = _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An internal server error occurred.",
                    Errors = _environment.IsDevelopment() && exception.StackTrace != null
                        ? new List<string> { exception.StackTrace }
                        : null
                };
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string error) : base(error)
    {
        Errors = new[] { error };
    }
}
