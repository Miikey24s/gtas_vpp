using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace gtas_vpp_be.Service.Helpers
{
    public static class ObjectHelpers
    {
        public static string SetPropertyJson<T>(this T property, T value, out string json)
        {
            property = value;
            json = ((property == null) ? string.Empty : JsonSerializer.Serialize(property));
            return json;
        }

        public static T? GetPropertyJson<T>(this string propertyJson, ref T property)
        {
            T val = property;
            if (val == null)
            {
                property = (T?)(string.IsNullOrWhiteSpace(propertyJson) ? default! : JsonSerializer.Deserialize<T>(propertyJson)) ?? default!;
            }

            return property;
        }

        public static T DeepClone<T>(this T value)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value)) ?? default!;
        }

        public static string SetJsonProperty<T>(this T value, ref T property)
        {
            property = value;
            if (property != null)
            {
                return JsonSerializer.Serialize(property);
            }

            return string.Empty;
        }

        public static T? GetJsonProperty<T>(this string propertyJson, ref T property)
        {
            T val = property;
            if (val == null)
            {
                property = (T?)((!string.IsNullOrWhiteSpace(propertyJson)) ? ((T?)JsonSerializer.Deserialize<T?>(propertyJson!)) : default!) ?? default!;
            }

            return property;
        }

        public static IEnumerable<string> EnumToListString<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).ToList();
        }
    }
}
