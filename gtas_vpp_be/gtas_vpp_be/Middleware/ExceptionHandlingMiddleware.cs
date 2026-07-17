using gtas_vpp_be.Service.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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

            var exposesSafeDetail = exception is ConflictException
                or DbUpdateConcurrencyException
                or BusinessException;
            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            var errorCode = exception switch
            {
                ConflictException => "Conflict",
                DbUpdateConcurrencyException => "ConcurrencyConflict",
                BusinessException => "BusinessRuleViolated",
                UnauthorizedAccessException => "Forbidden",
                KeyNotFoundException => "NotFound",
                ArgumentException => "RequestInvalid",
                InvalidOperationException => "OperationInvalid",
                _ => "ServerError"
            };
            var detail = exception switch
            {
                DbUpdateConcurrencyException => "The data has been modified by another user. Please refresh the page and try again.",
                ConflictException or BusinessException => exception.Message,
                ArgumentException => "The request is invalid.",
                InvalidOperationException => "The operation is not valid in its current state.",
                _ when env.IsDevelopment() => exception.Message,
                _ => "An unexpected error occurred. Please contact support."
            };

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = title,
                Status = statusCode,
                Detail = detail
            };

            problemDetails.Extensions["traceId"] = traceId;
            problemDetails.Extensions["errorCode"] = errorCode;
            problemDetails.Extensions["safeDetail"] = exposesSafeDetail;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
