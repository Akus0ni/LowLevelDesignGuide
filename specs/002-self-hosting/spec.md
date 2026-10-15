# Spec 002: Self-hosting & server status

| | |
|---|---|
| **Status** | Draft, awaiting owner review |
| **Branch** | `spec/002-self-hosting` |
| **Covers** | R35–R38, R44 (status part), R45 (status part), R46 (PRD §13); PRD F8 |
| **Depends on** | Spec 001 (merged, PR #3) |
| **Case study** | None (feature spec). Sync itself is spec 010; this spec provides the status signal it will use. |

## 1. Goal
Run the API on the owner's PC so real devices can use it over the internet, within ngrok's free
limits, and let every app know whether the server is up **without spending ngrok quota**. The PC
announces `online` / `offline` through a **signed** ntfy.sh message; apps show "Server offline",
queue access requests, and send them by themselves when the server is back online.

## 2. User stories
1. *As the owner,* I run one install script and the API and ngrok start with Windows (no login needed), the PC doesn't sleep on AC power, and the database is backed up every day.
2. *As a user on mobile data,* I can request access and sign in through the public ngrok address.
3. *As a user,* when the PC is off I see "Server offline" right away. My access request is saved on the device and sent by itself when the server is back.
4. *As a user,* when the PC starts, the open app switches back to online within seconds with no action from me.
5. *As the owner,* I can see this month's request and data usage against ngrok's limits. Before the limits are hit, the apps stop calling the server by themselves.
6. *As the owner,* a forged or replayed ntfy message has no effect, because apps only trust messages signed by the PC.

## 3. Scope

### 3.1 Server status message (shared contract)
- Payload: `{ v: 1, status: "online" | "offline", reason: "startup" | "heartbeat" | "shutdown" | "quota" | "manual", at: <UTC ISO-8601>, quotaResetsAt?: <date> }`.
- Signed with **ECDSA P-256 / SHA-256** (built into .NET and WebCrypto; IEEE P1363 signature format). The ntfy message body is `{ payload: <base64url canonical JSON>, sig: <base64url> }`.
- The signing and verifying code is shared by the API, `Lld.Admin` and `Lld.App`, and uses only the BCL.
- **Anti-replay:** an app accepts a message only if `at` is newer than the last one it accepted and not more than 5 minutes in the future. Old genuine messages that are replayed are ignored.
- **Freshness:** while online, the API publishes a heartbeat every **6 hours**. If the newest valid message is older than 7 hours, the status is **Unknown**. Unknown behaves like Offline, except that the user's own actions (sign in, request access) are still attempted.

### 3.2 API hosting (`Lld.Api`)
- `UseWindowsService()`. In the hosted profile Kestrel listens on **`http://localhost:<port>` only**: ngrok terminates TLS, and the API is never reachable from the LAN.
- **Forwarded headers** are trusted from loopback only, so the client IP (used for rate limiting) and `https` scheme (for Secure cookies) come from ngrok.
- `ServerStatusPublisher` (hosted service):
  - On start it confirms the public URL answers `/health` (one ngrok request) and then publishes `online`. If the tunnel isn't up yet, it retries with back-off.
  - It publishes a heartbeat every 6 h and publishes `offline` on graceful stop (5 s timeout).
  - The private key is read from a **DPAPI-protected file** under `%ProgramData%\LldGuide\` (ACL: service account + Administrators).
- **Global rate limiting:** one per-client-IP limit across all endpoints, on top of the existing `auth` policy. Values come from configuration.
- **Request budget meter:** middleware counts requests and request+response bytes per month. The counter lives in memory, is flushed to SQLite (`UsageCounter`) every minute and on stop, and is also exposed as an OTel metric. At **95 %** of either limit it publishes `offline` (reason `quota`, with `quotaResetsAt`) and keeps serving requests already in flight. A new month resets the counter and publishes `online`. The reset day is configurable because ngrok's cycle may not start on the 1st.

### 3.3 Operations (`deploy/`, new folder)
- `install.ps1` (run as Administrator, idempotent):
  1. `dotnet publish` the API to `C:\LldGuide\api`.
  2. Register the API as a Windows service under a virtual account (`NT SERVICE\LldGuideApi`), set to automatic start with restart-on-failure.
  3. Create `%ProgramData%\LldGuide\` (database, key file, ngrok config) with tight ACLs.
  4. Install ngrok as a service from `%ProgramData%\LldGuide\ngrok.yml`. The repo has only a template; the authtoken never enters the repo.
  5. Run `powercfg` so the PC doesn't sleep on AC power.
  6. Register the daily backup scheduled task.
- `uninstall.ps1` reverses every step except data and backups.
- **Backup:** `lld-admin backup run` runs `VACUUM INTO` to `D:\Applications\LldGuide\backups\lld-<yyyyMMdd-HHmm>.db` (configurable) and keeps **14 days**. The scheduled task runs daily at **13:00** with *run as soon as possible after a missed start*, because the PC is off at night.
- **Setup guide** (`docs/hosting.md`) covers:
  - ngrok sign-up, claiming the free static domain, and the authtoken.
  - Running the install script.
  - First-time `lld-admin status keygen`.
  - Building the Android app against the public URL.
  - **Dev Tunnels fallback** (persistent tunnel, `devtunnel host`, its 30-day idle expiry, and that switching means rebuilding the app with a new base URL).
  - Troubleshooting.

### 3.4 Admin console (`Lld.Admin`)
| Command | Purpose |
|---|---|
| `status keygen` | Creates the P-256 key pair and a random ntfy topic (128-bit). Stores the private key DPAPI-protected and prints the public key + topic for the app build settings. Refuses to overwrite without `--force`. |
| `status publish online\|offline` | Publishes a signed message manually (testing, planned maintenance). |
| `status budget` | Shows this month's requests and bytes against the limits, the % used and the reset date. |
| `backup run` / `backup list` | Runs a backup now / lists the backups that are kept. |

### 3.5 MAUI app (`Lld.App`)
- **API base URL is a build setting**: Debug keeps Aspire service discovery, and Release / hosted builds use the configured public URL. Switching tunnels changes configuration, not code.
- A `DelegatingHandler` adds `ngrok-skip-browser-warning` to every API call. HTTP calls use client **retry with exponential back-off + jitter** (no retry on 4xx), and a network failure or 5xx marks the server Offline.
- `ServerStatusMonitor`:
  - On launch and on resume it reads the topic's cached messages (`poll=1`) and takes the newest valid one.
  - While the app is in the foreground it holds an ntfy JSON stream subscription. It closes the stream in the background and re-polls on resume.
  - Raises `StatusChanged` (Observer) and pushes the status to React through the bridge.
- `AccessRequestOutbox`: if the status isn't Online or a submit fails at the network level, the request is stored in a JSON file in app data. The outbox flushes when the status becomes Online and on launch. The server already merges duplicate pending requests, so a resend is safe.

### 3.6 React (`web/`)
- `DataSource` gains `getServerStatus()` + `onServerStatusChanged()`. `BridgeDataSource` delegates to C#. `HttpDataSource` (browser dev mode) has a small TS monitor that uses EventSource + WebCrypto verification and a `localStorage` outbox.
- A **"Server offline" banner** shows in all layouts, with a quota variant: "Server is resting until <date>". It doesn't shift layout and follows the installed UI skills.
- Request access while offline shows "Request saved, will be sent when the server is online". When the outbox sends it, a toast says so.
- A `useServerStatus()` hook exposes `canSync`. The Sync button (spec 010) will use it; this spec only provides the hook.

## 4. Out of scope
- The sync endpoint, device SQLite and the Sync button (spec 010). This spec delivers only the status signal and the `canSync` flag.
- App distribution (stores, auto-update). The phone build is sideloaded from a local Release build.
- iOS / macOS (Phase 2), cloud hosting, a self-hosted ntfy server, and owner alerting / uptime monitoring.
- Running ngrok and Dev Tunnels together; Dev Tunnels is documented, not automated.

## 5. Acceptance criteria
| # | Criterion | How verified |
|---|---|---|
| A1 | After a reboot with nobody logged in, the API and ngrok are running and `https://<domain>/health` answers | Manual (reboot) |
| A2 | A real Android phone on **mobile data** requests access and signs in through ngrok | Manual + screenshots |
| A3 | Stopping the API service flips open apps (Windows, Android, browser) to "Server offline" within **10 s**; starting it flips them back | Manual + screenshots |
| A4 | An access request made while offline is sent automatically once the server is online and appears in `lld-admin requests list` | Integration test (outbox) + manual |
| A5 | Unsigned, wrongly signed, replayed-older and far-future messages are ignored | Unit tests (shared verifier, .NET + TS) |
| A6 | Killing the API process (no `offline` message) still marks the app Offline on its next failed call | Unit test + manual |
| A7 | Budget meter counts requests + bytes, survives restart, publishes `offline/quota` at 95 % and `online` after the reset | Unit tests (fake clock) + `status budget` |
| A8 | The API isn't reachable on the LAN address; rate limits apply per real client IP behind ngrok | Integration test (forwarded headers) + manual |
| A9 | Backup creates a consistent DB copy, prunes copies older than 14 days, and the task catches up after a missed run | Unit/integration test + manual |
| A10 | `install.ps1` is idempotent, `uninstall.ps1` removes services and tasks but keeps data | Manual (run twice, then uninstall) |
| A11 | No secrets in the repo (authtoken, private key); the key file is unreadable by non-admin users | Review + manual ACL check |
| A12 | All CI checks green; zero warnings; guardrail lint clean | CI |

## 6. LLD hooks (worth noting in later lessons)
| Pattern / principle | Where |
|---|---|
| Observer | `ServerStatusMonitor.StatusChanged` → banner, outbox flush, future sync trigger |
| Outbox | `AccessRequestOutbox` (store-and-forward, idempotent resend) |
| Chain of Responsibility / Decorator | `DelegatingHandler` pipeline (header, retry/back-off, offline detection, auth from spec 001) |
| Strategy | status source (ntfy stream vs poll), base URL per build |
| Digital signature vs secrecy | security comes from the signature, not from keeping the topic name secret |

## 7. Decisions (owner, 2026-10-15)
| Topic | Decision |
|---|---|
| ngrok | No account yet; the spec includes a step-by-step setup guide |
| Status monitor | C# in MAUI (bridge to React); small TS version only for browser dev mode |
| Signing key | ECDSA P-256; private key in a DPAPI-protected, ACL-restricted file; public key bundled in the app |
| Backups | Local only, `D:\Applications\LldGuide\backups`, keep 14 days |
| Install | PowerShell `install.ps1` / `uninstall.ps1` |
| Budget visibility | `lld-admin status budget` + OTel metric |
| Near the limit | API publishes signed `offline` (quota) at 95 %; `online` after the monthly reset |

## 8. Open questions
1. **Real Android phone for A2:** do you have one you can sideload a Release APK onto (USB debugging or file transfer)?
2. **ntfy topic in a public repo:** the topic and public key ship inside the app anyway. Should they be committed in app settings (simple; security relies on the signature) or injected at build time from a git-ignored file (keeps casual spam away)? *Recommendation: commit the public key, inject the topic.*
3. **Backup time:** daily at 13:00 with catch-up after a missed run. Is that time OK, or do you prefer another one, or "on every service stop"?
