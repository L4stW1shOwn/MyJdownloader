# MyJdownloader

A .NET client library for the [MyJDownloader](https://my.jdownloader.org) API.

> **Status: work in progress.** This library is under development and does
> not contain any protocol features yet. The current packages only provide
> the solution skeleton: an options type and a dependency injection
> registration. Do not use it in production.

## Packages

- `MyJdownloader`: core library.
- `MyJdownloader.Extensions.DependencyInjection`: integration with
  `Microsoft.Extensions.DependencyInjection`.

```shell
dotnet add package MyJdownloader
dotnet add package MyJdownloader.Extensions.DependencyInjection
```

## Usage

Register the named HTTP client used by the library:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyJdownloader;

services.AddMyJDownloader(o => o.AppKey = "MyApp");

// AddMyJDownloader returns an IHttpClientBuilder, so the client can be
// enriched, for example with resilience (package
// Microsoft.Extensions.Http.Resilience):
// services.AddMyJDownloader(o => o.AppKey = "MyApp")
//     .AddStandardResilienceHandler();
```

## Security warning

A later version is planned to let you export the session so it can be
restored without logging in again. The exported session will contain keys
derived from your password. Treat it as a secret: store it in a protected
location (for example DPAPI or a secret vault), never in plain text, in
source control or in logs.

## Design decisions

Architecture decisions are documented in docs/adr in the source
repository.

## License

MIT
