# Hosting the API on this PC

How the LLD Guide API is exposed to real devices (spec 002). The API runs as a Windows service on this PC.
**ngrok** gives it a public HTTPS address, and **ntfy.sh** tells apps whether it's online.

> Status: work in progress (spec 002). Section 1 can be done now. Sections 2+ come with `deploy/install.ps1`.

## 1. ngrok account and static domain (one-time, ~5 minutes)
1. Sign up (free) at <https://dashboard.ngrok.com/signup>.
2. Get your free static domain: in the dashboard open **Universal Gateway → Domains**. Free accounts get one
   `*.ngrok-free.dev` domain; create or claim it if one isn't listed. Write it down; it becomes the app's
   API base URL (`https://<your-domain>`).
3. Copy your **authtoken** from **Getting Started → Your Authtoken**. Treat it like a password: it never goes into the
   repo. `install.ps1` asks for it once and stores it only in `%ProgramData%\LldGuide\ngrok.yml`.
4. Install the agent (PowerShell):
   ```powershell
   winget install --id ngrok.ngrok -e
   ngrok version
   ```
5. Optional smoke test (until the installer exists): start the API locally, then
   ```powershell
   ngrok config add-authtoken <your-authtoken>
   ngrok http 5281 --url https://<your-domain>
   ```
   and open `https://<your-domain>/health` from your phone on mobile data. It should answer `Healthy` (a browser
   shows ngrok's warning page first; the app skips it with the `ngrok-skip-browser-warning` header).
   Stop it with `Ctrl+C`.

Free-plan limits the app is designed around: **20,000 HTTP requests and 1 GB data per month**. Check when your
monthly usage resets in the dashboard (**Billing / Usage**) and note the day; it becomes `Budget:ResetDayOfMonth`.

## 2. Install the services
_Coming in spec 002 (T8): `deploy/install.ps1`._

## 3. Dev Tunnels fallback
_Coming in spec 002 (T8)._

## 4. Troubleshooting
_Coming in spec 002 (T8)._
