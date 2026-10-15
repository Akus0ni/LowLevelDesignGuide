# Spec 002: Implementation plan

Companion to [`spec.md`](./spec.md). Versions verified against NuGet on 2026-10-15.

## 1. Packages
| Package | Version | Used by |
|---|---|---|
| `Microsoft.Extensions.Hosting.WindowsServices` | 10.0.13 | Api (`AddWindowsService`) |
| `System.Security.Cryptography.ProtectedData` | 10.0.13 | Infrastructure (DPAPI secrets) |

There's no new npm package: WebCrypto (ECDSA P-256) and EventSource are built into the browser.

## 2. Design

### 2.1 `Lld.ServerStatus` (new library, BCL only; used by Api, Admin, App)
```
StatusMessage        record: V, Status (Online|Offline), Reason, At, QuotaResetsAt?
SignedEnvelope       { payload: base64url(UTF-8 JSON), sig: base64url(P1363) }
StatusSigner         ECDsa P-256 private key → SignedEnvelope
StatusVerifier       SPKI public key → Result<StatusMessage> (bad format | bad signature | unsupported version)
ServerStatusTracker  pure state machine + TimeProvider; Snapshot { State: Unknown|Online|Offline, Reason, Since, QuotaResetsAt }
```
- The signer signs the **payload bytes as sent**, so no JSON canonicalisation is needed. The verifier checks the bytes first and parses them only after the signature passes.
- `ServerStatusTracker` rules:
  - `Apply(message)` accepts the message only if `At` is newer than the last accepted one and not more than 5 min in the future.
  - `ReportCallFailed()` → Offline (reason `unreachable`). `ReportCallSucceeded()` → Online: a real HTTPS answer from our domain is direct evidence.
  - `Evaluate()` → Unknown when the newest signed message is more than 7 h old.
  - Raises `Changed` (Observer).
- One **test vector** (fixed test key pair + envelopes: valid, tampered, replayed, far-future) lives in `tests/fixtures/server-status/`. Both the .NET and the Vitest tests read it, so the two verifiers can't drift apart.

### 2.2 Infrastructure (`Lld.Infrastructure`)
- **`ProtectedSecretsConfigurationSource`** works like Docker secrets, one file per key. It reads `*.secret` files from `Hosting:SecretsPath` (`%ProgramData%\LldGuide\secrets`) and decrypts them with DPAPI (LocalMachine scope; the ACL restricts access). The file name maps to a config key, e.g. `Jwt__SigningKey.secret` → `Jwt:SigningKey`. It's only registered on Windows and only when the folder exists.
- `UsageCounter` entity (`Month` key "yyyy-MM", `Requests`, `Bytes`) + migration `AddUsageCounter`.
- `SqliteBackupService`: `VACUUM INTO` a timestamped file, then prune copies older than N days. `Backup:Path` and `Backup:KeepDays` come from configuration.
- `NtfyClient` (`INtfyPublisher`): `POST https://ntfy.sh/<topic>` with the envelope as the body. The base URL is configurable so tests can stub it.

### 2.3 API (`Lld.Api`)
- **Hosting:**
  - `builder.Services.AddWindowsService()`; it does nothing when not running as a service.
  - `appsettings.Production.json` (committed, no secrets): Kestrel `http://localhost:5280`, `Database:Path`, `Hosting:SecretsPath`, budget limits.
- **Forwarded headers:** `XForwardedFor | XForwardedProto`, loopback proxies only (the default), placed first in the pipeline.
- **Global rate limiter:** per client IP, `RateLimiting:GlobalPerMinute` (default 60). Loopback callers (local health checks, the admin) are exempt. The existing `auth` policy stays.
- **`RequestBudgetMiddleware`:**
  - Counts only requests that came through the tunnel (they carry `X-Forwarded-For`). Bytes = request + response bodies + a fixed per-request header allowance.
  - Counters live in `RequestBudget` (singleton, `Interlocked`) and are flushed every minute and on stop by `RequestBudgetFlusher`.
  - Also an OTel counter `lld.tunnel.requests` / `lld.tunnel.bytes`.
- **`RequestBudget` policy:**
  - The month window comes from `Budget:ResetDayOfMonth` (default 1).
  - At ≥ `Budget:ThresholdPercent` (95) of either limit it raises `BudgetExhausted` once per window. Entering a new window raises `BudgetRestored`.
- **`ServerStatusPublisher`** (hosted service; disabled when `ServerStatus:Topic` is empty, as in dev and CI):
  1. Start: `GET {ServerStatus:PublicUrl}/health` with exponential back-off until it answers (skipped when `PublicUrl` is empty). Then publish `online`, or `offline/quota` if the budget is already spent.
  2. Every 6 h it publishes a heartbeat with the current state.
  3. It listens for `BudgetExhausted` → `offline/quota` and `BudgetRestored` → `online`.
  4. `StopAsync` publishes `offline/shutdown` with a 5 s timeout.
- `ServerStatus:PrivateKey` (PKCS#8 base64) and `ServerStatus:Topic` come from the protected secrets. `Jwt:SigningKey` comes from the same place when hosted; Aspire user secrets are still used in dev.

### 2.4 Admin (`Lld.Admin`)
New System.CommandLine sub-commands, using the same Infrastructure services:

| Command | Notes |
|---|---|
| `hosting init` | Creates any secret that's missing: JWT signing key, status key pair, ntfy topic (128-bit, base32). Writes DPAPI files and prints the **public key (SPKI base64)** and the topic. Idempotent: existing secrets are kept. |
| `status keygen --force` | Rotates the status key pair (re-prints the public key; the app must be rebuilt). |
| `status publish online\|offline` | Manual signed publish. |
| `status budget` | Reads `UsageCounter` + limits → requests, bytes, %, reset date. |
| `backup run` / `backup list` | `SqliteBackupService`. |

The spec's `status keygen` becomes `hosting init` for first-time setup (it creates the JWT key too). `status keygen --force` stays for rotating the key.

### 2.5 Deployment (`deploy/`)
- `install.ps1` (requires elevation, idempotent, `-WhatIf` friendly):
  1. Check prerequisites: .NET SDK, and ngrok on PATH (otherwise print the guide link and stop).
  2. `dotnet publish` Api → `C:\LldGuide\api`, Admin → `C:\LldGuide\admin`. The published admin carries `appsettings.Production.json`, so it targets the hosted DB; `dotnet run` keeps using the dev DB.
  3. Create `%ProgramData%\LldGuide\{data,secrets}` and set ACLs: SYSTEM, Administrators, `NT SERVICE\LldGuideApi`. Inheritance is disabled on `secrets`.
  4. `lld-admin hosting init`.
  5. `sc.exe create LldGuideApi` (virtual account, `start= auto`, `ASPNETCORE_ENVIRONMENT=Production`), then `sc.exe failure` set to restart ×3.
  6. Render `ngrok.yml` from `deploy/ngrok.template.yml` (domain + authtoken asked once, stored only in `%ProgramData%\LldGuide\ngrok.yml`), then `ngrok service install --config …` and `ngrok service start`.
  7. `powercfg /change standby-timeout-ac 0` and `hibernate-timeout-ac 0`.
  8. Scheduled task `LldGuide Backup`: daily 13:00, `StartWhenAvailable`, runs `lld-admin backup run` as SYSTEM.
  9. Smoke check: the service is running and `https://<domain>/health` returns 200.
- `uninstall.ps1`: stops and removes both services and the task. Keeps `%ProgramData%\LldGuide` and the backups, and prints where they are.
- `docs/hosting.md`: the setup guide described in spec §3.3.

### 2.6 MAUI (`Lld.App`)
- **Settings:**
  - `appsettings.json` (embedded, committed): `ServerStatus:PublicKey`, an empty `ServerStatus:Topic` and an empty `Api:BaseUrl`.
  - `appsettings.local.json` (embedded **only if present**, git-ignored): topic + public base URL.
  - Loaded with `builder.Configuration.AddJsonStream(...)`.
- **Base URL strategy:** when `Api:BaseUrl` is set, the typed client uses it and service discovery is skipped. When it's empty (Debug under Aspire), the client keeps `http+https://api`.
- **Handler pipeline** (Chain of Responsibility), outermost first:
  1. Standard resilience (already there: retries GET only, back-off + jitter).
  2. `TunnelHeadersHandler` (`ngrok-skip-browser-warning: 1`).
  3. `ServerReachabilityHandler`: network error / 502 / 503 / 504 → `tracker.ReportCallFailed()`; any other response → `ReportCallSucceeded()`.
- **`ServerStatusMonitor`** (singleton): owns a `ServerStatusTracker`.
  - Launch and resume: `GET ntfy.sh/<topic>/json?poll=1&since=12h`, apply every valid message.
  - Foreground: a streaming `GET …/json` loop with reconnect back-off. `App` lifecycle events (`OnSleep`/`OnResume`) stop and start it.
  - A 1-minute timer runs `Evaluate()`. It's local CPU only; no network.
- **`AccessRequestOutbox`:**
  - Stores a JSON file in `FileSystem.AppDataDirectory`.
  - `SessionService.RequestAccess` queues the request when the status isn't Online or the call fails at the network level, and returns `queued: true`.
  - Flushes on `Changed → Online` and on launch, sending items one by one and stopping at the first network failure.
- **Bridge:**
  - `GetServerStatus()` method.
  - The `MainPage` subscription to `Changed` and outbox events calls `hybridWebView.SendRawMessage(json)` with envelopes `{ type: "serverStatus" | "outboxSent", ... }`.
  - `RequestAccess` returns `{ queued }`.

### 2.7 Web (`web/`)
- `types.ts`:
  - `ServerStatus { state: 'unknown'|'online'|'offline'; reason?; since?; quotaResetsAt? }`.
  - `DataSource` adds `getServerStatus()` and `onServerStatusChanged(cb) → unsubscribe`.
  - `requestAccess` returns `{ queued: boolean }`.
  - An `onOutboxSent(cb)` event.
- `BridgeDataSource`: listens for `HybridWebViewMessageReceived` and routes by `type`.
- `HttpDataSource` (browser dev mode):
  - `statusMonitor.ts`: EventSource on `https://ntfy.sh/<topic>/sse` + WebCrypto `verify`, with the same tracker rules ported to TS (`serverStatusTracker.ts`).
  - Topic and key come from `VITE_NTFY_TOPIC` / `VITE_STATUS_PUBLIC_KEY` (`web/.env.local`, git-ignored). With no topic → the status is Online ("assumed"), and failed calls still flip it to Offline.
  - The outbox is in `localStorage`.
- `ServerStatusProvider` + `useServerStatus()` → `{ status, canSync }`.
- `ServerOfflineBanner` in `AppShell`:
  - A fixed slot so content doesn't jump. Offline / unknown / quota copy.
  - Respects `prefers-reduced-motion`, with emil-design-eng polish and a `break-ui` worst-case check (long dates, RTL).
- `RequestAccessForm`: when queued, it shows "Request saved, will be sent when the server is online". A Sonner toast fires on `outboxSent`.

### 2.8 AppHost
- No hosting changes: Aspire stays the dev loop.
- Optional: when `ServerStatus:Topic` is in the AppHost user secrets, it's passed to the API so the status flow can be tried locally against a dev topic.

### 2.9 Tests
| Project | Focus |
|---|---|
| `Lld.ServerStatus.Tests` (new) | signer/verifier round-trip, test-vector cases, tracker rules (replay, future, stale → Unknown, call failure/success) with `FakeTimeProvider` |
| `Lld.Api.Tests` | forwarded headers (client IP + scheme), global limiter per IP + loopback exemption, budget middleware counting only tunnelled requests, threshold → publisher called (fake `INtfyPublisher`), publisher start/stop messages |
| `Lld.Infrastructure` via `Lld.Api.Tests` or `Lld.Admin.Tests` | protected-secrets provider (Windows-only test, skipped elsewhere), backup creates a readable DB + prunes by age |
| `Lld.Admin.Tests` | `hosting init` idempotency (temp secrets dir), `status budget` output, `backup run/list` |
| `web` Vitest | TS verifier against the shared test vector, TS tracker, `ServerStatusProvider`, banner states, outbox (queue → flush on online) |
| `web` Playwright | banner appears/disappears when the mocked status source flips; offline request-access shows the saved message |
| Manual | A1–A3, A6, A8, A10, A11 on the real PC + Windows app + browser + Android phone over mobile data |

`Lld.App` itself has no test project: the outbox and monitor logic sits in testable classes in `Lld.ServerStatus` where possible. MAUI glue is checked manually, as in spec 001.

## 3. Risks
| Risk | Mitigation |
|---|---|
| ngrok service/config syntax differs between agent versions | Pin the agent version in `docs/hosting.md`; validate with `ngrok config check` in `install.ps1`; record deviations in §4 |
| ntfy.sh cache is 12 h and anonymous limits apply | Heartbeat every 6 h (~5 messages/day); stale → Unknown; documented |
| Windows gives services only a few seconds at shutdown → `offline` may not be sent | 5 s timeout; apps also flip on a failed call (A6) |
| Forwarded headers trusted from the wrong source | Loopback-only known proxies; Kestrel bound to localhost; test A8 |
| DPAPI LocalMachine can be decrypted by any local process | The ACL on the secrets folder is the real control; documented honestly in `docs/hosting.md` |
| HybridWebView raw-message API differs from expectations | Verify early (task T10); fall back to `InvokeJavaScriptAsync` |
| Budget count differs from ngrok's own count | Header allowance + 95 % threshold; `status budget` documents that it's an estimate; compare with the ngrok dashboard after a week |

## 4. Outcomes
_Filled in after implementation._
