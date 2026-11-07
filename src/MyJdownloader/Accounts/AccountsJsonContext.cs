namespace MyJdownloader.Accounts;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Source-generated, tolerant JSON metadata for the <c>/accountsV2</c> namespace.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(TolerantEnumConverter<BasicAuthType>),
        typeof(UnixMillisecondsConverter),
    ])]
[JsonSerializable(typeof(AccountQuery))]
[JsonSerializable(typeof(List<Account>))]
[JsonSerializable(typeof(List<BasicAuthentication>))]
[JsonSerializable(typeof(BasicAuthentication))]
[JsonSerializable(typeof(BasicAuthType))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class AccountsJsonContext : JsonSerializerContext
{
}
