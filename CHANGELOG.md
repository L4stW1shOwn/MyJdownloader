# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Solution skeleton.
- `MyJDownloaderOptions`.
- `AddMyJDownloader` named HTTP client registration.
- Exception hierarchy: MyJdException, MyJdAuthenticationException, MyJdDeviceOfflineException, MyJdRateLimitException, MyJdApiException, MyJdProtocolException, MyJdErrorSource.
- MyJdSession: immutable session with masked ToString() and versioned ToJson()/FromJson() export.
