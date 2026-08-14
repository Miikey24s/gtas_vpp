using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using gtas_vpp_shared.DTOs.Req;
using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class SharedWireContractManifestTests
{
    private const string ExpectedManifestSha256 =
        "77B0B1A064CFD49DCA3A3C144A2B1BCF4C6706A76422B35005070FC16D25C384";

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> NonWireTypesMovedByArch001 =
    [
        "gtas_vpp_shared.DTOs.Res.v_Users",
        "gtas_vpp_shared.DTOs.Res.v_WFXCompany",
        "gtas_vpp_shared.DTOs.Share.DropdownModel",
        "gtas_vpp_shared.DTOs.Share.GlobalClass",
        "gtas_vpp_shared.DTOs.Share.GlobalStorageFieldModel",
        "gtas_vpp_shared.DTOs.Share.GlobalStorageHeaderModel",
        "gtas_vpp_shared.DTOs.Share.GlobalStorageModel",
        "gtas_vpp_shared.DTOs.Share.GridColumnPropertyAttribute"
    ];

    [Fact]
    public void AllSharedWireDtoShapes_MatchTheApprovedBaseCommitManifest()
    {
        var manifest = CreateManifest(typeof(AuthenticationLoginRequest).Assembly);
        var actualHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(manifest)));

        Assert.True(
            actualHash.Equals(ExpectedManifestSha256, StringComparison.Ordinal),
            $"Wire-contract manifest hash was {actualHash}.\n{manifest}");
    }

    private static string CreateManifest(Assembly sharedAssembly)
    {
        var manifest = new StringBuilder();
        var wireTypes = sharedAssembly
            .GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith("gtas_vpp_shared.DTOs", StringComparison.Ordinal) == true)
            .Where(type => !NonWireTypesMovedByArch001.Contains(type.FullName ?? type.Name))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var type in wireTypes)
        {
            manifest
                .Append("TYPE|")
                .Append(FormatType(type))
                .Append('|')
                .Append(GetTypeKind(type))
                .Append("|BASE=")
                .Append(FormatType(type.BaseType))
                .Append('\n');

            if (type.IsEnum)
            {
                foreach (var name in Enum.GetNames(type))
                {
                    var value = Enum.Parse(type, name);
                    manifest
                        .Append("ENUM|")
                        .Append(FormatType(type))
                        .Append('|')
                        .Append(name)
                        .Append('=')
                        .Append(Enum.Format(type, value, "D"))
                        .Append('\n');
                }

                continue;
            }

            foreach (var property in type
                         .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(property => property.GetIndexParameters().Length == 0)
                         .OrderBy(property => GetJsonName(property), StringComparer.Ordinal)
                         .ThenBy(property => property.Name, StringComparer.Ordinal))
            {
                var ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
                var converter = property.GetCustomAttribute<JsonConverterAttribute>();
                var numberHandling = property.GetCustomAttribute<JsonNumberHandlingAttribute>();
                var order = property.GetCustomAttribute<JsonPropertyOrderAttribute>();

                manifest
                    .Append("PROP|")
                    .Append(FormatType(type))
                    .Append('|')
                    .Append(GetJsonName(property))
                    .Append("|CLR=")
                    .Append(property.Name)
                    .Append("|TYPE=")
                    .Append(FormatType(property.PropertyType))
                    .Append("|READ=")
                    .Append(property.CanRead)
                    .Append("|WRITE=")
                    .Append(property.CanWrite)
                    .Append("|IGNORE=")
                    .Append(ignore?.Condition.ToString() ?? "None")
                    .Append("|REQUIRED=")
                    .Append(property.IsDefined(typeof(JsonRequiredAttribute)))
                    .Append("|EXTENSION=")
                    .Append(property.IsDefined(typeof(JsonExtensionDataAttribute)))
                    .Append("|INCLUDE=")
                    .Append(property.IsDefined(typeof(JsonIncludeAttribute)))
                    .Append("|ORDER=")
                    .Append(order?.Order.ToString() ?? "0")
                    .Append("|NUMBER=")
                    .Append(numberHandling?.Handling.ToString() ?? "Default")
                    .Append("|CONVERTER=")
                    .Append(converter?.ConverterType?.FullName ?? "Default")
                    .Append('\n');
            }
        }

        return manifest.ToString();
    }

    private static string GetJsonName(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        ?? WebJson.PropertyNamingPolicy!.ConvertName(property.Name);

    private static string GetTypeKind(Type type) =>
        type.IsEnum ? "enum"
        : type.IsInterface ? "interface"
        : type.IsValueType ? "struct"
        : "class";

    private static string FormatType(Type? type)
    {
        if (type is null)
        {
            return "none";
        }

        if (type.IsGenericParameter)
        {
            return $"`{type.Name}";
        }

        if (type.IsArray)
        {
            return $"{FormatType(type.GetElementType())}[]";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var genericName = type.GetGenericTypeDefinition().FullName
            ?? type.GetGenericTypeDefinition().Name;
        genericName = genericName[..genericName.IndexOf('`')];

        return $"{genericName}<{string.Join(',', type.GetGenericArguments().Select(FormatType))}>";
    }
}
