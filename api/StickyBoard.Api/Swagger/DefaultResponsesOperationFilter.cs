using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using StickyBoard.Api.DTOs.Common;

namespace StickyBoard.Api.Swagger
{
    public sealed class DefaultResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var errorSchema = context.SchemaGenerator.GenerateSchema(typeof(ErrorDto), context.SchemaRepository);
            var errorContent = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Schema = errorSchema }
            };

            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request", Content = errorContent });
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized", Content = errorContent });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden", Content = errorContent });
            operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found", Content = errorContent });
            operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Internal Server Error", Content = errorContent });
        }
    }
}