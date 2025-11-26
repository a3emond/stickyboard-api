using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source?.Id == "Form")
            .ToList();

        if (!formParams.Any())
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = formParams.ToDictionary(
                            p => p.Name,
                            p => p.Type == typeof(IFormFile)
                                ? new OpenApiSchema { Type = "string", Format = "binary" }
                                : new OpenApiSchema { Type = "string", Format = "uuid" }
                        ),
                        Required = new HashSet<string>(
                            formParams.Where(p => p.IsRequired).Select(p => p.Name)
                        )
                    }
                }
            }
        };

        operation.Parameters.Clear();
    }
}