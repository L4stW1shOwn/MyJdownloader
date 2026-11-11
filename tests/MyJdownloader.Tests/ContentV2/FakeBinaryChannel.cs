namespace MyJdownloader.Tests.ContentV2;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Transport;

/// <summary>Hand-written channel double recording binary calls and returning configured bytes.</summary>
internal sealed class FakeBinaryChannel(byte[] bytes) : IDeviceChannel, IBinaryDeviceChannel
{
    public List<(string Path, IReadOnlyList<JsonElement> Args, CancellationToken Token)> Calls { get; } = [];

    public Task<byte[]> CallBinaryAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
    {
        Calls.Add((path, args, cancellationToken));
        return Task.FromResult(bytes);
    }

    public Task<T?> CallAsync<T>(string path, JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task CallAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
