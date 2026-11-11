namespace MyJdownloader.Tests.Accounts;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Accounts;
using MyJdownloader.Tests.Facades;
using NSubstitute;

[TestFixture]
public sealed class AccountsApiTests
{
    private const string Base = "/accountsV2/";
    private const string DefaultQueryJson = /*lang=json,strict*/ "{\"enabled\":true,\"error\":true,\"trafficLeft\":true,\"trafficMax\":true,\"userName\":true,\"valid\":true,\"validUntil\":true,\"maxResults\":-1,\"startAt\":0}";

    private static readonly long[] _ids = [1, 2];

    private static (IDeviceChannel Channel, AccountsApi Api) Create()
    {
        var channel = Substitute.For<IDeviceChannel>();
        return (channel, new AccountsApi(channel));
    }

    private static (string Path, IReadOnlyList<JsonElement> Args) Sent(IDeviceChannel channel)
    {
        var call = channel.ReceivedCalls().Single();
        var args = call.GetArguments();
        return ((string)args[0]!, args.OfType<IReadOnlyList<JsonElement>>().Single());
    }

    private static void AssertSent(IDeviceChannel channel, string path, params string[] expectedRawJson)
    {
        var (actualPath, args) = Sent(channel);
        Assert.That(actualPath, Is.EqualTo(Base + path));
        Assert.That(args.Select(a => a.GetRawText()), Is.EqualTo(expectedRawJson));
    }

    private static void ReturnsBool(IDeviceChannel channel, string path, bool? value)
    {
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<bool>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? false));
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    private static void ReturnsLong(IDeviceChannel channel, string path, long? value)
    {
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<long>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value ?? 0L));
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<long?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));
    }

    private static void ReturnsRef<T>(IDeviceChannel channel, string path, T? value)
        where T : class =>
        channel.CallAsync(Base + path, Arg.Any<JsonTypeInfo<T>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(value));

    private static void AssertDeviceUnreadable(MyJdProtocolException? thrown)
    {
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown!.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    private static BasicAuthentication Entry() => new()
    {
        Id = 7,
        Type = BasicAuthType.Http,
        Hostmask = "*.example.com",
        Username = "u",
        Password = "p",
        Enabled = true,
    };

    private static IEnumerable<TestCaseData> IdsCalls()
    {
        yield return new TestCaseData("enableAccounts", (Func<AccountsApi, IReadOnlyList<long>, Task>)((a, i) => a.EnableAccountsAsync(i))).SetName("EnableAccountsAsync");
        yield return new TestCaseData("disableAccounts", (Func<AccountsApi, IReadOnlyList<long>, Task>)((a, i) => a.DisableAccountsAsync(i))).SetName("DisableAccountsAsync");
        yield return new TestCaseData("refreshAccounts", (Func<AccountsApi, IReadOnlyList<long>, Task>)((a, i) => a.RefreshAccountsAsync(i))).SetName("RefreshAccountsAsync");
        yield return new TestCaseData("removeAccounts", (Func<AccountsApi, IReadOnlyList<long>, Task>)((a, i) => a.RemoveAccountsAsync(i))).SetName("RemoveAccountsAsync");
    }

    [Test]
    public async Task AddAccountAsync_Valid_SendsPathAndRawArgs()
    {
        var (channel, api) = Create();

        await api.AddAccountAsync("rapidgator.net", "u", "p");

        AssertSent(channel, "addAccount", "\"rapidgator.net\"", "\"u\"", "\"p\"");
    }

    [Test]
    public async Task AddAccountAsync_PasswordWithSpecialCharacters_SentUnchanged()
    {
        const string password = " a\"b\r\n";
        var (channel, api) = Create();

        await api.AddAccountAsync("rapidgator.net", "u", password);

        Assert.That(Sent(channel).Args[2].GetString(), Is.EqualTo(password));
    }

    [Test]
    public void AddAccountAsync_NullPassword_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.AddAccountAsync("rapidgator.net", "u", null!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("password"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void AddAccountAsync_InvalidUsername_MessageHasNoValue()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.AddAccountAsync("rapidgator.net", "bad\nname", "pw"));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("username"));
            Assert.That(thrown.Message, Does.Not.Contain("bad"));
            Assert.That(thrown.Message, Does.Not.Contain("pw"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    public void AddAccountAsync_BlankHoster_Throws(string hoster)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.AddAccountAsync(hoster, "u", "p"));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("premiumHoster"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    private static readonly string[] _hosters = ["a.com", "b.com"];

    [TestCaseSource(nameof(IdsCalls))]
    public async Task IdsMethods_SendIdsArray(string path, Func<AccountsApi, IReadOnlyList<long>, Task> call)
    {
        ArgumentNullException.ThrowIfNull(call);
        var (channel, api) = Create();

        await call(api, _ids);

        AssertSent(channel, path, "[1,2]");
    }

    [TestCaseSource(nameof(IdsCalls))]
    public async Task IdsMethods_EmptyIds_SendsEmptyArray(string path, Func<AccountsApi, IReadOnlyList<long>, Task> call)
    {
        ArgumentNullException.ThrowIfNull(call);
        var (channel, api) = Create();

        await call(api, []);

        AssertSent(channel, path, "[]");
    }

    [TestCaseSource(nameof(IdsCalls))]
    public void IdsMethods_NullIds_Throws(string path, Func<AccountsApi, IReadOnlyList<long>, Task> call)
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => call(api, null!));

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.Not.Empty);
            Assert.That(thrown!.ParamName, Is.EqualTo("ids"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task ListAccountsAsync_NullQuery_SendsDefaultQuery()
    {
        var (channel, api) = Create();
        ReturnsRef<List<Account>>(channel, "listAccounts", null);

        await api.ListAccountsAsync();

        AssertSent(channel, "listAccounts", DefaultQueryJson);
    }

    [Test]
    public async Task ListAccountsAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<Account>>(channel, "listAccounts", null);

        var accounts = await api.ListAccountsAsync();

        Assert.That(accounts, Is.Empty);
    }

    [Test]
    public async Task ListAccountsAsync_Data_ReturnsAccounts()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listAccounts", new List<Account> { new() { Uuid = 5, Hostname = "h" } });

        var accounts = await api.ListAccountsAsync();

        Assert.That(accounts.Single().Uuid, Is.EqualTo(5));
    }

    [TestCase(-2, 0)]
    [TestCase(-1, -1)]
    public void ListAccountsAsync_InvalidRange_Throws(int maxResults, int startAt)
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.ListAccountsAsync(new AccountQuery { MaxResults = maxResults, StartAt = startAt }));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("query"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task SetUserNameAndPasswordAsync_Valid_SendsArgs()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setUserNameAndPassword", true);

        var result = await api.SetUserNameAndPasswordAsync(42, "u", "p");

        AssertSent(channel, "setUserNameAndPassword", "42", "\"u\"", "\"p\"");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SetUserNameAndPasswordAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "setUserNameAndPassword", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.SetUserNameAndPasswordAsync(42, "u", "p"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task ListPremiumHostersAsync_Valid_ReturnsList()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listPremiumHoster", new List<string> { "a.com", "b.com" });

        var hosters = await api.ListPremiumHostersAsync();

        AssertSent(channel, "listPremiumHoster");
        Assert.That(hosters, Is.EqualTo(_hosters));
    }

    [Test]
    public async Task ListPremiumHostersAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<string>>(channel, "listPremiumHoster", null);

        var hosters = await api.ListPremiumHostersAsync();

        Assert.That(hosters, Is.Empty);
    }

    [Test]
    public async Task ListPremiumHosterUrlsAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<Dictionary<string, string>>(channel, "listPremiumHosterUrls", null);

        var urls = await api.ListPremiumHosterUrlsAsync();

        AssertSent(channel, "listPremiumHosterUrls");
        Assert.That(urls, Is.Empty);
    }

    [Test]
    public async Task ListPremiumHosterUrlsAsync_Data_ReturnedAsIs()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listPremiumHosterUrls", new Dictionary<string, string> { ["a"] = "https://a" });

        var urls = await api.ListPremiumHosterUrlsAsync();

        Assert.That(urls, Is.EqualTo(new Dictionary<string, string> { ["a"] = "https://a" }));
    }

    [Test]
    public async Task ListPremiumHosterUrlsAsync_NullValue_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "listPremiumHosterUrls", new Dictionary<string, string> { ["a"] = null! });

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.ListPremiumHosterUrlsAsync());

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task GetPremiumHosterUrlAsync_NullData_ReturnsNull()
    {
        var (channel, api) = Create();
        ReturnsRef<string>(channel, "getPremiumHosterUrl", null);

        var url = await api.GetPremiumHosterUrlAsync("a");

        AssertSent(channel, "getPremiumHosterUrl", "\"a\"");
        Assert.That(url, Is.Null);
    }

    [Test]
    public async Task GetPremiumHosterUrlAsync_Data_ReturnsUrl()
    {
        var (channel, api) = Create();
        ReturnsRef(channel, "getPremiumHosterUrl", "https://a");

        var url = await api.GetPremiumHosterUrlAsync("a");

        Assert.That(url, Is.EqualTo("https://a"));
    }

    [Test]
    public async Task AddBasicAuthAsync_Valid_SendsArgsAndReturnsId()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "addBasicAuth", 7);

        var id = await api.AddBasicAuthAsync(BasicAuthType.Http, "*.example.com", "u", "p");

        AssertSent(channel, "addBasicAuth", "\"HTTP\"", "\"*.example.com\"", "\"u\"", "\"p\"");
        Assert.That(id, Is.EqualTo(7));
    }

    [Test]
    public async Task AddBasicAuthAsync_FtpType_SendsFtp()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "addBasicAuth", 7);

        await api.AddBasicAuthAsync(BasicAuthType.Ftp, "ftp.example.com", "u", "p");

        Assert.That(Sent(channel).Args[0].GetString(), Is.EqualTo("FTP"));
    }

    [Test]
    public async Task AddBasicAuthAsync_EmptyPassword_Sent()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "addBasicAuth", 7);

        await api.AddBasicAuthAsync(BasicAuthType.Ftp, "ftp.example.com", "u", string.Empty);

        Assert.That(Sent(channel).Args[3].GetString(), Is.Empty);
    }

    [Test]
    public void AddBasicAuthAsync_UnknownType_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.AddBasicAuthAsync(BasicAuthType.Unknown, "h", "u", "p"));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("type"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task AddBasicAuthAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsLong(channel, "addBasicAuth", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.AddBasicAuthAsync(BasicAuthType.Http, "h", "u", "p"));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task ListBasicAuthAsync_NullData_ReturnsEmpty()
    {
        var (channel, api) = Create();
        ReturnsRef<List<BasicAuthentication>>(channel, "listBasicAuth", null);

        var entries = await api.ListBasicAuthAsync();

        AssertSent(channel, "listBasicAuth");
        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task UpdateBasicAuthAsync_Valid_SendsEntryJson()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "updateBasicAuth", true);

        var result = await api.UpdateBasicAuthAsync(Entry());

        var (path, args) = Sent(channel);
        var raw = args.Single().GetRawText();
        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo("/accountsV2/updateBasicAuth"));
            Assert.That(raw, Does.Contain("\"id\":7"));
            Assert.That(raw, Does.Contain("\"password\":\"p\""));
            Assert.That(result, Is.True);
        });
    }

    [Test]
    public void UpdateBasicAuthAsync_NullEntry_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.UpdateBasicAuthAsync(null!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("updatedEntry"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void UpdateBasicAuthAsync_UnknownType_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.UpdateBasicAuthAsync(Entry() with { Type = BasicAuthType.Unknown }));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("updatedEntry"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void UpdateBasicAuthAsync_BlankHostmask_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Catch<ArgumentException>(() => api.UpdateBasicAuthAsync(Entry() with { Hostmask = " " }));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("updatedEntry"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task UpdateBasicAuthAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "updateBasicAuth", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.UpdateBasicAuthAsync(Entry()));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public async Task RemoveBasicAuthsAsync_Valid_SendsIdsAndReturnsResult()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "removeBasicAuths", true);

        var result = await api.RemoveBasicAuthsAsync(_ids);

        AssertSent(channel, "removeBasicAuths", "[1,2]");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RemoveBasicAuthsAsync_NullData_ThrowsProtocol()
    {
        var (channel, api) = Create();
        ReturnsBool(channel, "removeBasicAuths", null);

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => api.RemoveBasicAuthsAsync(_ids));

        AssertDeviceUnreadable(thrown);
    }

    [Test]
    public void RemoveBasicAuthsAsync_NullIds_Throws()
    {
        var (channel, api) = Create();

        var thrown = Assert.Throws<ArgumentNullException>(() => api.RemoveBasicAuthsAsync(null!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("ids"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task EnableAccountsAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();

        await api.EnableAccountsAsync(_ids, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task ListAccountsAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef<List<Account>>(channel, "listAccounts", null);

        await api.ListAccountsAsync(null, cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task SetUserNameAndPasswordAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsBool(channel, "setUserNameAndPassword", true);

        await api.SetUserNameAndPasswordAsync(42, "u", "p", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task AddBasicAuthAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsLong(channel, "addBasicAuth", 7);

        await api.AddBasicAuthAsync(BasicAuthType.Http, "h", "u", "p", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task GetPremiumHosterUrlAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef<string>(channel, "getPremiumHosterUrl", null);

        await api.GetPremiumHosterUrlAsync("a", cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task ListPremiumHosterUrlsAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create();
        ReturnsRef<Dictionary<string, string>>(channel, "listPremiumHosterUrls", null);

        await api.ListPremiumHosterUrlsAsync(cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }

    [Test]
    public void Device_Accounts_ReturnsSameInstance()
    {
        using var http = new HttpClient();
        var client = new MyJDownloaderClient(http);
        var device = client.GetDevice("dev1");

        Assert.That(device.Accounts, Is.SameAs(device.Accounts));
    }

    [Test]
    public void AddAccountAsync_PremiumHosterAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.AddAccountAsync(TextCaps.Identifier, "user", "pw"));
    }

    [Test]
    public void AddAccountAsync_PremiumHosterOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddAccountAsync(TextCaps.IdentifierOver, "user", "pw"), "premiumHoster");
    }

    [Test]
    public void AddAccountAsync_UsernameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.AddAccountAsync("host", TextCaps.Identifier, "pw"));
    }

    [Test]
    public void AddAccountAsync_UsernameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddAccountAsync("host", TextCaps.IdentifierOver, "pw"), "username");
    }

    [Test]
    public void SetUserNameAndPasswordAsync_UsernameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.SetUserNameAndPasswordAsync(1, TextCaps.Identifier, "pw"));
    }

    [Test]
    public void SetUserNameAndPasswordAsync_UsernameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.SetUserNameAndPasswordAsync(1, TextCaps.IdentifierOver, "pw"), "username");
    }

    [Test]
    public void GetPremiumHosterUrlAsync_HosterAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.GetPremiumHosterUrlAsync(TextCaps.Identifier));
    }

    [Test]
    public void GetPremiumHosterUrlAsync_HosterOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.GetPremiumHosterUrlAsync(TextCaps.IdentifierOver), "hoster");
    }

    [Test]
    public void AddBasicAuthAsync_HostmaskAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.AddBasicAuthAsync(BasicAuthType.Ftp, TextCaps.Identifier, "user", "pw"));
    }

    [Test]
    public void AddBasicAuthAsync_HostmaskOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddBasicAuthAsync(BasicAuthType.Ftp, TextCaps.IdentifierOver, "user", "pw"), "hostmask");
    }

    [Test]
    public void AddBasicAuthAsync_UsernameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.AddBasicAuthAsync(BasicAuthType.Ftp, "host", TextCaps.Identifier, "pw"));
    }

    [Test]
    public void AddBasicAuthAsync_UsernameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.AddBasicAuthAsync(BasicAuthType.Ftp, "host", TextCaps.IdentifierOver, "pw"), "username");
    }

    [Test]
    public void UpdateBasicAuthAsync_HostmaskAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.UpdateBasicAuthAsync(new BasicAuthentication { Type = BasicAuthType.Ftp, Hostmask = TextCaps.Identifier }));
    }

    [Test]
    public void UpdateBasicAuthAsync_HostmaskOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.UpdateBasicAuthAsync(new BasicAuthentication { Type = BasicAuthType.Ftp, Hostmask = TextCaps.IdentifierOver }), "updatedEntry");
    }

    [Test]
    public void UpdateBasicAuthAsync_UsernameAtIdentifierMax_IsSent()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertSent(channel, () => api.UpdateBasicAuthAsync(new BasicAuthentication { Type = BasicAuthType.Ftp, Username = TextCaps.Identifier }));
    }

    [Test]
    public void UpdateBasicAuthAsync_UsernameOverIdentifierMax_ThrowsWithoutCallingChannel()
    {
        var channel = Substitute.For<IDeviceChannel>();
        var api = new AccountsApi(channel);

        TextCaps.AssertRejected(channel, () => api.UpdateBasicAuthAsync(new BasicAuthentication { Type = BasicAuthType.Ftp, Username = TextCaps.IdentifierOver }), "updatedEntry");
    }

    [Test]
    public async Task ListAccountsAsync_FilterListChangingAfterCheck_SendsCheckedCopy()
    {
        var (channel, api) = Create();

        await api.ListAccountsAsync(new AccountQuery { Uuids = new ShiftingList() });

        var json = Sent(channel).Args[0].GetRawText();
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"uuidlist\":[1]"));
            Assert.That(json, Does.Not.Contain("-1]"));
        });
    }

    [Test]
    public void ListAccountsAsync_NegativeFilterId_Throws()
    {
        var (channel, api) = Create();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => api.ListAccountsAsync(new AccountQuery { Uuids = [-1] }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("query"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }
}
