using System.Text.Json;

namespace StickyBoard.Api.Utils
{
    public static class JsonExtensions
    {
        /// <summary>
        /// Serializes any object or dictionary into a compact JSON string.
        /// Returns "{}" for null objects.
        /// </summary>
        public static string ToJson(this object? obj)
        {
            if (obj is null)
                return "{}";

            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        /// <summary>
        /// Parses a JSON string into a JsonDocument, returning an empty object on error.
        /// </summary>
        public static JsonDocument ToJsonDocument(this object? obj)
        {
            try
            {
                var json = obj?.ToJson() ?? "{}";
                return JsonDocument.Parse(json);
            }
            catch
            {
                return JsonDocument.Parse("{}");
            }
        }
    }
}