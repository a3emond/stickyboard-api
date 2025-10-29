using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StickyBoard.Api.Common.Filters
{
    /// <summary>
    /// Ensures all response media types in Swagger/OpenAPI are shown as application/json,
    /// without changing actual runtime behavior.
    /// </summary>
    public sealed class ForceJsonContentTypeFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var response in operation.Responses)
            {
                // Copy any text/plain or untyped response as JSON for docs
                if (response.Value.Content.ContainsKey("text/plain"))
                {
                    var schema = response.Value.Content["text/plain"];
                    response.Value.Content.Remove("text/plain");
                    response.Value.Content["application/json"] = schema;
                }

                // If no explicit type at all, create one
                if (response.Value.Content.Count == 0)
                {
                    response.Value.Content["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema { Type = "object" }
                    };
                }
            }
        }
    }
}