namespace MyJdownloader.LiveTests.Live;

using System.Text.Json;

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
}
