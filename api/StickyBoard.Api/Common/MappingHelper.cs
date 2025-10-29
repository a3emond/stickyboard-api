using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;
using Npgsql;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Repositories.Base
{
    public static class MappingHelper
    {
        public static T MapEntity<T>(NpgsqlDataReader reader) where T : IEntity, new()
        {
            var entity = new T();

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name.ToLower();
                if (!reader.HasColumn(columnName)) continue;

                var value = reader[columnName];
                if (value == DBNull.Value) continue;

                var targetType = prop.PropertyType;

                // Handle JsonDocument explicitly
                if (targetType == typeof(JsonDocument))
                {
                    prop.SetValue(entity, JsonDocument.Parse(value.ToString() ?? "{}"));
                    continue;
                }

                // Handle nullable types safely
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                {
                    // Convert.ChangeType works on the *underlying* type
                    var converted = Convert.ChangeType(value, underlyingType);
                    prop.SetValue(entity, converted);
                    continue;
                }

                // Handle common primitives explicitly if needed
                if (targetType == typeof(Guid))
                    prop.SetValue(entity, (Guid)value);
                else if (targetType == typeof(DateTime))
                    prop.SetValue(entity, (DateTime)value);
                else if (targetType == typeof(string))
                    prop.SetValue(entity, value.ToString());
                else
                    prop.SetValue(entity, Convert.ChangeType(value, targetType));
            }

            return entity;
        }

        private static bool HasColumn(this NpgsqlDataReader reader, string name)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
