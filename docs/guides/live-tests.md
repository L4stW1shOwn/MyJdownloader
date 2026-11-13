# Live tests

The `tests/MyJdownloader.LiveTests` project runs the library against the real
MyJDownloader service and one of your own devices. It proves the protocol works
end to end (connect, device list, encrypted device calls, session renewal) and
prints the raw answers of a few calls so the models can be frozen from real
data. The suite is opt-in: without credentials, only the live tests are
ignored; the masking helper tests still run.

## Variables

| Variable | Required | Purpose |
|---|---|---|
| `MYJD_EMAIL` | yes | MyJDownloader account email. |
| `MYJD_PASSWORD` | yes | MyJDownloader account password. |
| `MYJD_DEVICE` | no | Device name (`DeviceInfo.Name`). Without it, the single device is used; with several devices and no name, the setup fails with an explicit message. |
| `MYJD_VARIANT_URL` | no | An http(s) link that has variants (a video page, for example). With `MYJD_LIVE_WRITES=1`, the variant probe adds it, dumps its variants and removes it; without it, the probe is ignored. |

Set them in your shell only. Never put them in a file of the repository, in a
`.runsettings` file or in `TestRunParameters`. The password is passed to
`ConnectAsync` once and is not kept.

## Run

```bash
dotnet build tests/MyJdownloader.LiveTests
read -rs MYJD_PASSWORD   # typed without echo, not kept in the shell history; no export
MYJD_EMAIL='<your account email>' MYJD_PASSWORD="$MYJD_PASSWORD" \
  dotnet test tests/MyJdownloader.LiveTests --no-build
unset MYJD_PASSWORD
```

Optionally prefix the `dotnet test` line with `MYJD_LIVE_WRITES=1` (to enable
the write tests), `MYJD_DEVICE='<device name>'` and
`MYJD_VARIANT_URL='<link with variants>'` (for the variant probe).

Run these commands in an interactive terminal: `read -rs` needs a terminal to
read the password from. A shell started by another tool (an IDE task, an
assistant's command runner) cannot prompt you and stops before `dotnet test`.

For `MYJD_VARIANT_URL`, prefer a link without `?` or `=`, such as
`https://www.youtube.com/embed/<video id>` rather than
`https://www.youtube.com/watch?v=<video id>`. When you paste a URL, zsh (with
`url-quote-magic`) inserts a `\` before these characters, and inside single
quotes the `\` stays in the value: the device then receives a broken link and
`getVariants` fails with `INTERNAL_SERVER_ERROR`. Check the value before you
press Enter.

To run a single test, add `--filter "Name=<test name>"` to the `dotnet test`
line, for example `--filter "Name=VariantLink_GetVariants"`.

Do not `export` the password: an exported variable is inherited by every child
process, including long-lived build servers that outlive the run. Passing it on
the `dotnet test` command line only scopes it to that process.

**Warning**: `quality.sh` or a solution-level `dotnet test` (no category filter)
runs the live suite whenever `MYJD_EMAIL` and `MYJD_PASSWORD` are set. The
write tests (stop mark, test link, variant link and container, described below)
additionally need `MYJD_LIVE_WRITES=1`; without it they are ignored. When enabled, they add and
remove a test link in the link grabber and set then remove a stop mark on the
first download.

The tests run serially (to respect the service rate limit), with a 60 second
timeout per test. A device that is offline gives an inconclusive result, and
missing data (for example an empty download list) gives an ignored test with the
reason.

The CI and the publish workflow exclude `TestCategory=Live`, so these tests never
run there.

Never run them with `--diag`, `--blame-crash`, `--blame-hang` or an HTTP handler
that logs requests: the memory dumps and logs would contain the password and the
session keys.

## What the tests read

Session export and restore, device ping, version, uptime and core revision,
link crawler state, update availability, the download list and its packages,
counters, the stop mark, the link grabber lists and jobs, the download folder
history, advanced configuration entries (query and one value, no write), the
event publishers, and the raw system and storage information.

## What the tests change, and how it is restored

Every write is reversible and restored in a `finally` block of the same test.

- **Session renewal**: `ReconnectAsync` is called, then a device call checks the
  new session works.
- **Stop mark**: set on an existing download link, then removed. The test is
  ignored if the download list is empty or if a stop mark already exists. A stop
  mark you set yourself between the check and the set could be removed.
- **Test link**: the fixed URL `https://example.com/myjdownloader-live-test.bin`
  is added to the link grabber with Autostart off. On it, a fake download
  password is set then cleared, and an online check is started. The link is then
  removed. The test acts only on a link whose URL is exactly that one; it never
  empties the list.
- **Variant link** (only with `MYJD_VARIANT_URL`): the link is added with
  Autostart off, its variants are read (typed and raw), and it is removed. The
  test owns every link of its own crawl job, whatever its host: the crawler may
  rewrite the URL (a `youtu.be` link becomes a `youtube.com` link with a plugin
  scheme). It never touches links of other jobs and never empties the list. The
  crawl is awaited up to 90 seconds. The URL and the link names are not written
  to the output.
- **Container** (only with `MYJD_LIVE_WRITES=1`): a crawljob container (with
  confirmation and Autostart off) and a Metalink 4 container, each holding only
  the test link, are added to the link grabber. Container links are collected
  under other jobs, so the probe looks for the test link by its exact URL in the
  whole link grabber; test links left by an interrupted run are removed first, and
  the link is removed at the end. Only links whose URL is exactly the test link
  are touched. A Metalink has no confirmation key: if your device confirms new
  links automatically, the test link can reach the download list, which the probe
  reports but does not clean. The container bytes are not written to the output.
- **Events**: a subscription is created, listened to once briefly, and removed.

## What the tests never call

- `CleanupAsync`, `AbortAsync()` without a job and `ClearListAsync`.
- Restart, update and other system actions.
- The device internet reconnect (`device.Reconnect`; the session `ReconnectAsync`
  is a different call and is used).
- Log upload.
- Account and configuration writes. Accounts are not exercised at all, because
  they hold hoster credentials.

## If a run is interrupted

If a run is stopped, or `AddLinks` or `AddContainer` times out, open the link
grabber on the device and look for the test link
`https://example.com/myjdownloader-live-test.bin` (the container probe adds the
same link); remove it manually. Check the download list for a leftover stop mark too, and remove it
if you did not set it.

## Output and what to share

The probe tests print the raw JSON answers to the test output, never to a file.
Before printing, the values are masked at any depth:

- properties whose name contains `pass`, `token`, `secret`, `key`, `cookie`,
  `auth`, `mail`, `user`, `login`, `pwd`, `credential`, `session` or `signature` (case-insensitive) become `"***"`;
- URL values of every scheme with a host (http, https, ftp, sftp, ...) lose
  their user information, and their query and fragment; free text containing
  `://` is masked entirely;
- the URL keys of the `getDownloadUrls` answer are reduced to
  `scheme://host/***`.

The system and storage probes print local paths, which may contain your OS user
name: review them before sharing. Do not run the suite in a loop: the service
throttles repeated connects. The stop mark test briefly holds a stop mark on the
first download, which may pause the scheduling of new downloads during the test.

Masking is a safety net, not a guarantee. Review the output before sharing it.
The `.trx` files under `TestResults/` contain the same output and your device
data: do not share them and do not attach them to CI artifacts.

## Forward the results

Copy the masked output of the probe tests, with the test names, to update the
models and settle the open assumptions:

- the system information probes feed the system information model (T-044);
- the link grabber probes (`queryLinkCrawlerJobs`, `getVariants`, the link after
  clearing its password) and the download probes (`getStopMark`,
  `getStructureChangeCounter`, `getDownloadUrls`) settle the assumptions marked
  "to be confirmed by live tests";
- the container probe (`addContainer` with a crawljob and a Metalink 4) settles
  that the device starts a job and yields the test link, under another job
  (live-confirmed 2026-11-13).
