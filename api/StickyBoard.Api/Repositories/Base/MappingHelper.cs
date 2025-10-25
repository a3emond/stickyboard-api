using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
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

                // Handle special types
                if (prop.PropertyType == typeof(Guid))
                    prop.SetValue(entity, (Guid)value);
                else if (prop.PropertyType == typeof(DateTime))
                    prop.SetValue(entity, (DateTime)value);
                else if (prop.PropertyType == typeof(string))
                    prop.SetValue(entity, value.ToString());
                else
                    prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
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