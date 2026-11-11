namespace MyJdownloader.LiveTests.Live;

using System.Globalization;

/// <summary>
/// Opt-in fixture connecting to the real MyJD service. Credentials come only from the
/// MYJD_EMAIL, MYJD_PASSWORD and (optionally) MYJD_DEVICE environment variables.
/// </summary>
[SetUpFixture]
public sealed class LiveSession
{
    private static HttpClient? _httpClient;

    /// <summary>Gets the connected client.</summary>
    public static MyJDownloaderClient Client { get; private set; } = null!;

    /// <summary>Gets the selected device.</summary>
    public static JDownloaderDevice Device { get; private set; } = null!;

    internal static DeviceInfo SelectDevice(IReadOnlyList<DeviceInfo> devices, string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            return devices.FirstOrDefault(d => d.Name == name)
                ?? throw new InvalidOperationException("No device matches MYJD_DEVICE.");
        }

        return devices.Count switch
        {
            0 => throw new InvalidOperationException("The account has no device."),
            1 => devices[0],
            _ => throw new InvalidOperationException(string.Create(
                CultureInfo.InvariantCulture,
                $"Set MYJD_DEVICE: the account has {devices.Count} devices.")),
        };
    }

    [OneTimeSetUp]
    public async Task ConnectAsync()
    {
        var email = Environment.GetEnvironmentVariable("MYJD_EMAIL");
        var password = Environment.GetEnvironmentVariable("MYJD_PASSWORD");
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Assert.Ignore("Live credentials not set");
        }

        var deviceName = Environment.GetEnvironmentVariable("MYJD_DEVICE");
        _httpClient = new HttpClient();
        Client = new MyJDownloaderClient(_httpClient);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await Client.ConnectAsync(email!, password!, timeout.Token);
        var devices = await Client.GetDevicesAsync(timeout.Token);
        Device = Client.GetDevice(SelectDevice(devices, deviceName).Id);
    }

    [OneTimeTearDown]
    public async Task DisconnectAsync()
    {
        if (_httpClient is null)
        {
            return;
        }

        try
        {
            await Client.DisconnectAsync();
        }
        finally
        {
            _httpClient.Dispose();
            _httpClient = null;
        }
    }
}
