using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;
using Npgsql;
using StickyBoard.Api.Models.Base;

namespace StickyBoard.Api.Common
{
    public static class MappingHelper
    {
        public static T MapEntity<T>(NpgsqlDataReader reader) where T : IEntity, new()
        {
            var entity = new T();

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var col = prop.GetCustomAttribute<ColumnAttribute>()?.Name 
                          ?? prop.Name.ToLower();

                if (!reader.HasColumn(col))
                    continue;

                var dbValue = reader[col];
                if (dbValue is DBNull)
                    continue;

                var targetType = prop.PropertyType;
                
                // -----------------------------------------
                // Enum
                // -----------------------------------------
                if (targetType.IsEnum)
                {
                    if (dbValue is int i)
                        prop.SetValue(entity, Enum.ToObject(targetType, i));
                    else if (int.TryParse(dbValue.ToString(), out var j))
                        prop.SetValue(entity, Enum.ToObject(targetType, j));
                    else
                        prop.SetValue(entity, Enum.Parse(targetType, dbValue.ToString()!, ignoreCase: true));

                    continue;
                }

                // -----------------------------------------
                // Binary: bytea → byte[]
                // -----------------------------------------
                if (targetType == typeof(byte[]))
                {
                    prop.SetValue(entity, (byte[])dbValue);
                    continue;
                }


                // -----------------------------------------
                // JSON: jsonb → JsonDocument
                // -----------------------------------------
                if (targetType == typeof(JsonDocument))
                {
                    if (dbValue is string s)
                        prop.SetValue(entity, JsonDocument.Parse(s));
                    else if (dbValue is JsonElement el)
                        prop.SetValue(entity, JsonDocument.Parse(el.GetRawText()));
                    else
                        prop.SetValue(entity, JsonDocument.Parse("{}"));
                    continue;
                }

                // -----------------------------------------
                // Arrays: PostgreSQL text[] → string[]
                // -----------------------------------------
                if (targetType == typeof(string[]))
                {
                    prop.SetValue(entity, (string[])dbValue);
                    continue;
                }

                // -----------------------------------------
                // Nullable<T>
                // -----------------------------------------
                var underlying = Nullable.GetUnderlyingType(targetType);
                if (underlying != null)
                {
                    prop.SetValue(entity, Convert.ChangeType(dbValue, underlying));
                    continue;
                }

                // -----------------------------------------
                // Guid
                // -----------------------------------------
                if (targetType == typeof(Guid))
                {
                    prop.SetValue(entity, (Guid)dbValue);
                    continue;
                }

                // -----------------------------------------
                // bool
                // -----------------------------------------
                if (targetType == typeof(bool))
                {
                    prop.SetValue(entity, (bool)dbValue);
                    continue;
                }

                // -----------------------------------------
                // int
                // -----------------------------------------
                if (targetType == typeof(int))
                {
                    prop.SetValue(entity, (int)dbValue);
                    continue;
                }

                // -----------------------------------------
                // DateTime
                // -----------------------------------------
                if (targetType == typeof(DateTime))
                {
                    prop.SetValue(entity, (DateTime)dbValue);
                    continue;
                }

                // -----------------------------------------
                // string
                // -----------------------------------------
                if (targetType == typeof(string))
                {
                    prop.SetValue(entity, dbValue.ToString());
                    continue;
                }

                // -----------------------------------------
                // Fallback: Convert.ChangeType
                // -----------------------------------------
                prop.SetValue(entity, Convert.ChangeType(dbValue, targetType));
            }

            return entity;
        }

        // Helper for safe column lookup
        private static bool HasColumn(this NpgsqlDataReader r, string column)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), column, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
