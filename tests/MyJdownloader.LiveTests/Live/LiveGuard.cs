namespace MyJdownloader.LiveTests.Live;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.Errors;

/// <summary>Helpers shared by the live tests.</summary>
internal static class LiveGuard
{
    /// <summary>Runs a device call, turning an offline device into an inconclusive result.</summary>
    public static async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (MyJdDeviceOfflineException)
        {
            Assert.Inconclusive("Device offline");
        }
    }

    /// <summary>Runs a device call returning a value, turning an offline device into an inconclusive result.</summary>
    public static async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (MyJdDeviceOfflineException)
        {
            Assert.Inconclusive("Device offline");
            throw;
        }
    }

    /// <summary>Parses a JSON literal into a standalone element used as a raw call argument.</summary>
    public static JsonElement Json(string literal)
    {
        using var document = JsonDocument.Parse(literal);
        return document.RootElement.Clone();
    }

    /// <summary>Performs a raw channel call and returns the answer as JSON.</summary>
    public static async Task<JsonElement> RawAsync(string path, JsonElement[] args, CancellationToken cancellationToken)
    {
        var answer = await LiveSession.Device.Channel.CallAsync(path, LiveJsonContext.Default.JsonElement, args, cancellationToken);
        return answer;
    }

    /// <summary>Replaces object property names that may be URLs by a host-only form.</summary>
    public static JsonElement RedactUrlKeys(JsonElement answer)
    {
        if (answer.ValueKind != JsonValueKind.Object)
        {
            return answer;
        }

        var redacted = new Dictionary<string, JsonElement>();
        var index = 0;
        foreach (var property in answer.EnumerateObject())
        {
            var key = Uri.TryCreate(property.Name, UriKind.Absolute, out var uri)
                ? uri.Scheme + "://" + uri.Host + "/***"
                : "***";
            index++;
            redacted[key + "#" + index.ToString(CultureInfo.InvariantCulture)] = property.Value;
        }

        return JsonSerializer.SerializeToElement(redacted, LiveJsonContext.Default.DictionaryStringJsonElement);
    }
}
