using gtas_vpp_shared.DTOs.Res.VPP;
using Radzen;
using System.Collections;
using System.Globalization;

namespace gtas_vpp_fe.Helpers
{
    public static class VppOrderGridFilterHelper
    {
        public static List<ColumnFilterScope> BuildColumnFilterScopes(IEnumerable<FilterDescriptor> filters, string excludedProperty)
        {
            return filters
                .Where(filter => !TargetsColumn(filter, excludedProperty))
                .Select(BuildColumnFilterScope)
                .Where(filter => filter != null)
                .Cast<ColumnFilterScope>()
                .ToList();
        }

        public static VppRequestResDTO BuildFilterValueDto(Dictionary<string, object?> dict)
        {
            var dto = new VppRequestResDTO();

            SetIntProperty(dict, dto, "Year", nameof(VppRequestResDTO.Year));
            SetIntProperty(dict, dto, "Month", nameof(VppRequestResDTO.Month));
            SetIntProperty(dict, dto, "Status", nameof(VppRequestResDTO.Status));
            SetIntProperty(dict, dto, nameof(VppRequestResDTO.TotalLines), nameof(VppRequestResDTO.TotalLines));
            SetIntProperty(dict, dto, nameof(VppRequestResDTO.TotalQty), nameof(VppRequestResDTO.TotalQty));
            SetDateTimeProperty(dict, dto, nameof(VppRequestResDTO.SubmittedDate), nameof(VppRequestResDTO.SubmittedDate));
            SetBoolProperty(dict, dto, "IsAdditionalOrder", nameof(VppRequestResDTO.IsAdditionalOrder));
            SetBoolProperty(dict, dto, "IsDeadlinePassed", nameof(VppRequestResDTO.IsDeadlinePassed));

            SetStringProperty(dict, dto, nameof(VppRequestResDTO.VppCode));
            SetStringProperty(dict, dto, nameof(VppRequestResDTO.DepartmentCode));
            SetStringProperty(dict, dto, nameof(VppRequestResDTO.MemberCompanyCode));
            SetStringProperty(dict, dto, nameof(VppRequestResDTO.RequesterName));
            SetStringProperty(dict, dto, nameof(VppRequestResDTO.Description));
            SetStringProperty(dict, dto, nameof(VppRequestResDTO.StatusText));
            SetStringProperty(dict, dto, nameof(VppRequestResDTO.SubmittedDateText));

            return dto;
        }

        private static bool TargetsColumn(FilterDescriptor filter, string property)
        {
            return string.Equals(filter.FilterProperty, property, StringComparison.OrdinalIgnoreCase)
                || string.Equals(filter.Property, property, StringComparison.OrdinalIgnoreCase);
        }

        private static ColumnFilterScope? BuildColumnFilterScope(FilterDescriptor filter)
        {
            var property = !string.IsNullOrWhiteSpace(filter.FilterProperty)
                ? filter.FilterProperty
                : filter.Property;

            if (string.IsNullOrWhiteSpace(property))
            {
                return null;
            }

            var values = GetFilterValues(filter.FilterValue)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return values.Count == 0
                ? null
                : new ColumnFilterScope
                {
                    Property = property,
                    Values = values
                };
        }

        private static IEnumerable<string> GetFilterValues(object? filterValue)
        {
            if (filterValue is IEnumerable values && filterValue is not string)
            {
                foreach (var value in values.Cast<object?>())
                {
                    var formatted = FormatFilterValue(value);
                    if (!string.IsNullOrWhiteSpace(formatted))
                    {
                        yield return formatted;
                    }
                }

                yield break;
            }

            var singleValue = FormatFilterValue(filterValue);
            if (!string.IsNullOrWhiteSpace(singleValue))
            {
                yield return singleValue;
            }
        }

        private static string? FormatFilterValue(object? value)
        {
            return value switch
            {
                null => null,
                DateTime dateTime => dateTime.ToString(DateFormatter.LongDate, CultureInfo.GetCultureInfo("vi-VN")),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(DateFormatter.LongDate, CultureInfo.GetCultureInfo("vi-VN")),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        private static void SetIntProperty(Dictionary<string, object?> dict, VppRequestResDTO dto, string dictKey, string propertyName)
        {
            if (dict.TryGetValue(dictKey, out var value) && int.TryParse(value?.ToString(), out var parsed))
            {
                typeof(VppRequestResDTO).GetProperty(propertyName)?.SetValue(dto, parsed);
            }
        }

        private static void SetDateTimeProperty(Dictionary<string, object?> dict, VppRequestResDTO dto, string dictKey, string propertyName)
        {
            if (dict.TryGetValue(dictKey, out var value) && DateTime.TryParse(value?.ToString(), out var parsed))
            {
                typeof(VppRequestResDTO).GetProperty(propertyName)?.SetValue(dto, parsed);
            }
        }

        private static void SetBoolProperty(Dictionary<string, object?> dict, VppRequestResDTO dto, string dictKey, string propertyName)
        {
            if (dict.TryGetValue(dictKey, out var value) && bool.TryParse(value?.ToString(), out var parsed))
            {
                typeof(VppRequestResDTO).GetProperty(propertyName)?.SetValue(dto, parsed);
            }
        }

        private static void SetStringProperty(Dictionary<string, object?> dict, VppRequestResDTO dto, string propertyName)
        {
            if (dict.TryGetValue(propertyName, out var value) && value != null)
            {
                typeof(VppRequestResDTO).GetProperty(propertyName)?.SetValue(dto, value.ToString());
            }
        }

        public sealed class ColumnFilterScope
        {
            public string Property { get; set; } = string.Empty;
            public List<string> Values { get; set; } = new();
        }
    }
}
