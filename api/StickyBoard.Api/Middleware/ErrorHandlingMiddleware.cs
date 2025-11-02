using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs; // DomainException namespace
using StickyBoard.Api.Models; // ErrorCode enum

namespace StickyBoard.Api.Middleware
{
    public sealed class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ErrorHandlingMiddleware(
            RequestDelegate next, 
            ILogger<ErrorHandlingMiddleware> logger, 
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException dex)
            {
                _logger.LogWarning(dex, "Domain exception occurred");

                var error = new ErrorDto
                {
                    Code = dex.Code,
                    Message = dex.Message,
                    Details = _env.IsDevelopment() ? dex.ToString() : null
                };

                context.Response.StatusCode = dex.StatusCode;
                context.Response.ContentType = "application/json";

                var json = JsonSerializer.Serialize(error, JsonOptions());
                await context.Response.WriteAsync(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                var error = new ErrorDto
                {
                    Code = ErrorCode.SERVER_ERROR,
                    Message = "An unexpected error occurred.",
                    Details = _env.IsDevelopment() ? ex.ToString() : null
                };

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var json = JsonSerializer.Serialize(error, JsonOptions());
                await context.Response.WriteAsync(json);
            }
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}
