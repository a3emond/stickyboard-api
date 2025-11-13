using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using StickyBoard.Api.DTOs.Common;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StickyBoard.Api.Common.Filters
{
    /// <summary>
    /// Ensures standard error responses are documented for all endpoints.
    /// Adds consistent schema + example error payloads for API consumers.
    /// </summary>
    public sealed class DefaultResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Generate shared schema for ErrorDto
            var errorSchema = context.SchemaGenerator.GenerateSchema(typeof(ErrorDto), context.SchemaRepository);

            // Common error response example structure
            OpenApiMediaType ExampleError(string code, string message)
            {
                return new OpenApiMediaType
                {
                    Schema = errorSchema,
                    Example = new OpenApiObject
                    {
                        ["code"] = new OpenApiString(code),
                        ["message"] = new OpenApiString(message),
                        ["details"] = new OpenApiString("Additional debug info in development mode only")
                    }
                };
            }

            // Standard HTTP Error responses to attach
            var standardErrors = new Dictionary<string, (string description, OpenApiMediaType example)>
            {
                ["400"] = ("Bad Request", ExampleError("VALIDATION_ERROR", "Invalid input")),
                ["401"] = ("Unauthorized", ExampleError("AUTH_INVALID", "Authentication required or invalid token")),
                ["403"] = ("Forbidden", ExampleError("FORBIDDEN", "User does not have access to this resource")),
                ["404"] = ("Not Found", ExampleError("NOT_FOUND", "Requested resource does not exist")),
                ["500"] = ("Internal Server Error", ExampleError("SERVER_ERROR", "Unexpected server error"))
            };

            foreach (var entry in standardErrors)
            {
                if (!operation.Responses.ContainsKey(entry.Key))
                {
                    operation.Responses.Add(
                        entry.Key,
                        new OpenApiResponse
                        {
                            Description = entry.Value.description,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = entry.Value.example
                            }
                        }
                    );
                }
            }
        }
    }
}
