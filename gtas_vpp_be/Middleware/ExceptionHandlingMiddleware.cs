using gtas_vpp_be.Service.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace gtas_vpp_be.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing request.");
                await WriteProblemDetailsAsync(context, ex);
            }
        }

        private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
        {
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var (statusCode, title) = exception switch
            {
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict"),
                BusinessException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
                InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = title,
                Status = statusCode,
                Detail = exception is DbUpdateConcurrencyException
                    ? "The data has been modified by another user. Please refresh the page and try again."
                    : (exception is ConflictException || exception is BusinessException
                        ? exception.Message
                        : (env.IsDevelopment() ? exception.Message : "An unexpected error occurred. Please contact support."))
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
