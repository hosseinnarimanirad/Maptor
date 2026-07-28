# AFTA 2.x Compliance Assessment & Remediation Plan — MakanNegar Saba

## Context

The MakanNegar Saba product is a two-part system under
`src\IRI.App\Barg`:

- **Backend** — an ASP.NET Core 8 Web API with a clean-architecture layout
  (`Application/`, `Core/`, `Infrastructure/{Ef,Grpc,Kafka,Mongo}`, `Presentation/{Api,Presentation}`).
  Auth is JWT bearer (HS512), authorization is a per-permission policy model with 5 seeded roles,
  and there is a `Docs/` set (`doc_SAD.md`, `doc_SDD.md`, `doc_SRS.md`) plus `README_JWT_AUTHENTICATION.md`.
- **Client** — `IRI.App.MakanNegarSaba` (net8.0-windows WPF, Persian/RTL GIS app) that talks to the
  backend over `HttpClient` + gRPC, with an in-house auto-updater.

They must be certified against the Iranian **AFTA** product-security requirements documented in
`D:\Others\Afta\IRL-AFTA-Documents\3-plan` (classes 2.1–2.9 plus transport sections 3.1–3.6).
This plan (a) records whether each requirement class is met today, ranked by priority, and
(b) lays out the work needed to bring both projects into compliance.

Assessment is **static-analysis only** (read-only exploration of both projects). Anything marked
"verify at runtime" needs a live check before sign-off. This plan file is documentation; no code has
been changed.

> **Two defects override everything else and must be fixed before any external review:**
> 1. **RSA private key committed in source** — `Core\...\Common\Helpers\Helper.cs:12` ships the full
>    private key (`priK`) inside the client binary. The RSA+AES `EncryptedMessage` layer therefore
>    provides zero confidentiality. Also exposed by `DebugController` as a crypto oracle.
> 2. **Cleartext password (+email) written to `saba.log`** on every login —
>    `Services\MakanNegarSabaServices.cs:327`. Plus `ResetPasswordAsync` sets the password hash to the
>    user's own email (`UserCommandRepository.cs:84`), and passwords are hashed with **MD5**
>    (`Core\...\Entities\Users\User.cs:133`).

---

## Compliance summary table (by priority)

Legend: ✅ Compliant · ⚠️ Partial · ❌ Not compliant · ➖ N/A / needs scoping

| Pri | AFTA class / req | Status | Evidence & gap |
|-----|------------------|--------|----------------|
| **P0** | **CRYPTO-2** Hashing (SHA family) | ❌ | Passwords + ShareCode use **MD5** (`User.cs:120,133`). MD5 is not in the AFTA-allowed list (SHA-1/256/384/512). |
| **P0** | **CRYPTO-1** AES symmetric | ⚠️→❌ | AES/RSA `EncryptedMessage` exists, but **private key hardcoded** (`Helper.cs:12`), shipped in client → no real confidentiality. |
| **P0** | **TC-3 / 3.1** Secure channel for initial auth | ❌ | http base URL accepted (`MakanNegarSabaServices.cs:66`); gRPC enables cleartext h2c (`TowerGrpcService.cs:62-65`); HTTPS redirect only when `!Dev && UseSsl` (`Program.cs:255-259`). Password also logged in cleartext. |
| **P0** | **IA-4** Password policy | ❌ | No length/complexity/history validation on SignUp or ChangePassword (`SignUpCommandHandler.cs:33-45`, `UserCommandRepository.cs:54-68`). `ResetPassword` sets hash = email (`:84`). |
| **P1** | **LOG-1** Audit event generation | ❌ | Only successful logins recorded (`UserLogin` table). Failed logins only counted, not logged; no CRUD/admin/logout/permission-change/session audit. No `AuditLog` entity, no EF interceptor. |
| **P1** | **LOG-2** Audit record content | ⚠️ | `UserLogin` has timestamp/user/IP/UA (`UserLogin.cs:5-19`) but no event-type/outcome; most events not captured at all. |
| **P1** | **LOG-3** Protect audit records | ❌ | No access control on log store; client `saba.log` is a plaintext relative-path file with credentials. |
| **P1** | **LOG-5** Filter/sort audit records | ❌ | Only a 3-number `login-statistics` endpoint (`UserController.cs:211`). No log-query API. |
| **P1** | **LOG-6** Detect log tamper | ❌ | No hashing/append-only/read-only enforcement. |
| **P1** | **TSF-2** Protect internal data transfer | ❌ | gRPC has no TLS/auth (`TransmissionLineGrpcService.cs`); service-to-DB relies on connection string only. |
| **P1** | **TSF-6** Authenticity of auto-updates | ❌ | Update ZIP hashed (SHA-256) but **not signed**; hash served by the same (possibly http) endpoint; download is anonymous (`UpdateRepository.cs:184`, `UpdateController.cs:84`). Client applies over bare `new HttpClient()` (`UpdateService.cs:179`). |
| **P1** | **IA-6** Auth mechanisms (2+ for remote) | ❌ | Username/password only. No 2FA/OTP/AD. Remote users require ≥2 mechanisms. |
| **P1** | **PA-2** Inactivity session termination | ❌ | No idle timeout server-side; no auto-lock in client. |
| **P1** | **PA-1** Concurrent-session limit | ❌ | Stateless JWT, no session store, no limit. |
| **P1** | **UDP-10/11** Detect + counter sensitive-data tamper | ❌ | No integrity hashes on stored user data; no countermeasure. |
| **P2** | **IA-8** Actions at session establishment | ❌ | No invalidation/notification of prior sessions on new login. |
| **P2** | **PA-3** User-initiated termination | ⚠️ | Client clears token, but stateless JWT is **not revoked** server-side; token valid until expiry. No logout/revocation endpoint. |
| **P2** | **PA-4/PA-5/PA-6** Show last login / last failure / retain | ❌ | Client never shows the user their own last login or failed-attempt count (only admins see others' `LastLoginAt`). |
| **P2** | **SM-4** Required management capabilities (18 items) | ❌ | Lockout threshold (5), duration (30 min), token TTLs, password rules are **hardcoded** (`User.cs:80-83`), not admin-manageable. No security-settings store. |
| **P2** | **SM-6** User–role association (exactly one role) | ⚠️ | `SyncUserRoles` implies many-to-many; AFTA requires exactly one role per account. Needs constraint/verification. |
| **P2** | **UDP-6** Import access control | ➖/❌ | No server upload/import endpoints; client `.mdb`/tile imports unvalidated. Scope which imports are in cert boundary. |
| **P2** | **UDP-8/9** Export control + rules | ⚠️ | Export permissions defined (`Permission.cs`) but export is client-side; no admin restriction on bulk export. |
| **P2** | **TC-1 / 3.5** Cert validation / pinning | ⚠️ | No dangerous cert-bypass (good), but no pinning and http allowed. |
| **P3** | **IA-1/IA-2** Failed-attempt count + lockout | ✅ | 5 attempts → 30-min lock (`User.cs:77-107`, `LoginQueryHandler.cs:50-78`). Make configurable (SM-4). |
| **P3** | **IA-3** Per-user security attributes | ✅ | `IsActive/IsLocked/FailedLoginAttempts/LastLoginAt/roles/Stamp` (`User.cs:12-46`). |
| **P3** | **UDP-1/2/3** Access-control policy + attribute/rule based | ✅⚠️ | Per-permission policies over roles (`IServiceCollectionExtensions.cs:142-176`). But user-mgmt + Role/Permission controllers use **bare `[Authorize]`** with no policy (`UserController.cs:112-212`) → any authenticated user passes. |
| **P3** | **SM-1/2/3/5** Mgmt of functions, roles | ✅⚠️ | Role CRUD, user status, sync exist; 5 roles seeded (`AddRoleBasedAccessControl.cs:176-390`). Gaps = missing policy gating (above) + no seeded admin user. |
| **P3** | **TSF-1 / RA-1** Secure state / core-function continuity | ⚠️ | Global exception middleware, EF `EnableRetryOnFailure`, UoW transactions, RowVersion concurrency. No health-check probes; verify DB-loss behavior. |
| **P3** | **TSF-4** Reliable timestamps | ⚠️ | Server clock only; no NTP guarantee documented. |
| **P3** | **TSF-5** Update capability | ✅ | Manual+auto update mechanism present (needs TSF-6 signing). |
| **P3** | **IA-5** Limited pre-auth actions | ⚠️ | `TileController`, `LayerSettingController` (`[Authorize]` commented, `:9`), `UpdateController` are anonymous. Confirm each is intended public. |
| **P3** | **CRYPTO-3/4** Key destruction / digital signature | ❌ | Static keys, no destruction; no RSA/ECDSA digital-signature service (JWT HS512 is a MAC, not a signature). Relevant to TSF-6. |
| **P3** | **UDP-4/5/7** Denial rules / residual info / secure transfer | ⚠️/➖ | Session-count denial not implemented; residual-info not assessed; secure-transfer depends on TC fixes. |
| **P3** | **PA-7** Session denial by parameters | ❌ | No location/time/port-based session denial. |
| **P3** | **CORS** (transport hygiene) | ❌ | Permissive policy defined but never registered (`IServiceCollectionExtensions.cs:119-137`); when enabled it is `AllowAnyOrigin`. |
| **P3** | **Kafka / Mongo** infra | ➖ | Empty stub projects (csproj only) — out of scope unless activated. |

**Bottom line:** The identity/authorization *core* is largely compliant (IA-1/2/3, UDP-1/2/3, SM-5,
roles). The system **fails on cryptography, transport security, audit/logging, session management,
secure updates, and configurable security management** — and carries three critical defects
(hardcoded private key, MD5 passwords + cleartext-password logging, reset-to-email).

---

## Remediation plan

### Phase 0 — Critical defects (do first; small, high-impact)

1. **Remove the private key from the client / repo.** Delete `priK` from
   `Core\...\Common\Helpers\Helper.cs:12`; keep only `pubK` client-side. The private key must live
   **only** on the server, injected via user-secrets/env (like the existing JWT key pattern in
   `README_JWT_AUTHENTICATION.md:64-74`). Stop project-referencing `Core` from the WPF client for the
   key; the client should hold the public half only. Rotate the key pair after removal.
2. **Delete the cleartext-credential log line** `MakanNegarSabaServices.cs:327` and audit all
   `Trace.*`/`Debug.WriteLine` calls for token/PII leakage (§4 of client report).
3. **Replace MD5 password hashing** with ASP.NET Core `PasswordHasher<User>` (PBKDF2) or Argon2/bcrypt.
   Change `User.Create`/`ValidatePassword` (`User.cs:119-133`) and widen/keep the `nvarchar(500)` column.
   Add a migration and a one-time rehash-on-next-login path (old MD5 hashes can't be reversed).
   Move `ShareCode` off MD5 to SHA-256 (satisfies **CRYPTO-2**).
4. **Fix `ResetPasswordAsync`** (`UserCommandRepository.cs:70-87`) — never derive a password from the
   email. Use a random token + forced change, and re-enable the commented email verification in
   `SignUpCommandHandler.cs:57`.
5. **Gate `DebugController`** — it is `#if DEBUG` today; confirm Release builds never ship it and that
   CI builds Release. Remove the `Encrypt/Decrypt/Login` oracles or require admin auth.

### Phase 1 — Transport & channel security (TC, 3.x, TSF-2)

- **Enforce HTTPS unconditionally** in the client: reject non-`https` `BaseUrl`
  (`MakanNegarSabaServices.cs:66`), remove the h2c switch and require TLS for gRPC
  (`TowerGrpcService.cs:62-65`), remove hardcoded http IP defaults (`Data\SabaSettings.cs:13-15`).
- **Server side:** make `UseHttpsRedirection` + `UseHsts` unconditional in non-dev
  (`Program.cs:255-259`), configure Kestrel certs (currently commented `Program.cs:15-22`), add TLS +
  `[Authorize]` to the gRPC service, and register a **restrictive** CORS policy (fix the unused
  permissive one at `IServiceCollectionExtensions.cs:119-137`).
- Document the chosen protocol (HTTPS) so the AFTA **3.1** checklist applies; add cert validation per
  **3.5** and consider pinning for the desktop client.

### Phase 2 — Audit & logging (LOG-1..8, LOG-2 fields)

- Introduce an **`AuditLog` entity** + EF `ISaveChangesInterceptor` (backend has none today) capturing
  the LOG-1 event set: logins (success **and** failure with IP/time/outcome), logout, CRUD on
  protected objects, role/permission changes, user status changes, export, admin actions, security-
  function start/stop. Fields per **LOG-2**: date/time, event type, actor identity, outcome, IP.
- Add an **authorized log-query API** (filter/sort by user/type/date/outcome) for **LOG-5**, gated by a
  new `ViewAuditLog` permission (satisfies **LOG-3**).
- Add integrity protection for **LOG-6** (per-record hash chain or DB append-only + restricted grants)
  and threshold/rotation handling for **LOG-7/LOG-8**.
- Switch client logging to a structured sink with rotation and **no credential/PII** content.

### Phase 3 — Identity, sessions & product access (IA-4/6/8, PA-1..7, UDP-10/11)

- **Password policy (IA-4):** enforce configurable min-length (≥8) + character-class rules in SignUp
  and ChangePassword handlers; surface rules to the client UI.
- **Second factor for remote users (IA-6):** add OTP/email verification as a second mechanism.
- **Sessions:** move from purely-stateless JWT to a server-tracked session/refresh model so you can
  implement **PA-1** concurrent-session limit, **PA-2** idle timeout (server + client auto-lock),
  **PA-3** real logout/token revocation, and **IA-8** prior-session invalidation/notification. Add a
  `/refresh` and `/logout` endpoint (refresh generation already exists unused, `JwtService.cs:130`).
- **Show access history (PA-4/5/6):** on login, return + display the user's own last successful login,
  last failed attempt, and failure count; persist failed-login records (currently only counted).
- **Data integrity (UDP-10/11):** store hashes of sensitive user data and alert on mismatch.

### Phase 4 — Security management & authorization gaps (SM-1..6, UDP-1, IA-5)

- **Fix authorization gaps:** replace bare `[Authorize]` on user-management, `RoleController`,
  `PermissionController` with permission policies (e.g. `Roles.ManagePermissions`)
  (`UserController.cs:112-212`, `RoleController.cs:16`, `PermissionController.cs:13`). Re-enable
  `LayerSettingController` `[Authorize]` (`:9`); confirm `TileController`/`UpdateController` anonymity is
  intended (IA-5).
- **Configurable security settings (SM-4):** move lockout threshold/duration, password rules, token
  TTLs, session limits, idle timeout out of `User.cs:80-83`/hardcoded into an admin-managed settings
  store, and expose the SM-4 management capabilities.
- **Seed an initial admin user** (only roles are seeded today) and **enforce one-role-per-user**
  (SM-6) or document the multi-role design decision.

### Phase 5 — Secure update & resilience (TSF-5/6, RA-1, CRYPTO-4)

- **Sign update packages** (RSA-2048+/ECDSA per CRYPTO-4) and verify the signature in the client
  before applying (`UpdateService.cs:175-224`); require auth + HTTPS on `/Download` and `/releases`
  (`UpdateController.cs:84`, `Program.cs:262-273`).
- Add **health-check probes** (DB/dependency) and document/verify secure-state-on-failure (TSF-1/RA-1);
  document the timestamp source (TSF-4, prefer NTP).

### Documentation deliverable

Produce a per-class AFTA response document (mirroring the checklist tables in the 2.x docs) filling the
"Description/Implemented?" columns with the final implementation — this is the artifact the AFTA
reviewer expects. `Docs/doc_SAD.md` already tracks some known issues and is the right home.

---

## Verification

- **Static re-check:** after each phase, re-run the exploration greps used here (private key strings,
  `Trace.*` credential logs, `GetMd5Hash`, bare `[Authorize]`, `UseHttpsRedirection`, cert callbacks).
- **Crypto:** confirm no private key in client binary (`ildasm`/`strings` on the built DLL); confirm
  password column holds PBKDF2/Argon2 hashes after a login rehash.
- **Transport:** packet-capture client↔API and client↔gRPC; confirm TLS only, http refused/redirected,
  and credentials never transit cleartext (TC-3).
- **Auth/session:** drive the login flow — exceed 5 failures (lockout fires), verify password-policy
  rejection, open >N sessions (PA-1), idle past timeout (PA-2), logout invalidates the token (PA-3),
  second login invalidates/notifies the first (IA-8), and last-login/failed-count is shown (PA-4/5).
- **Audit:** trigger each LOG-1 event and confirm an `AuditLog` row with all LOG-2 fields; exercise the
  log-query API as authorized/unauthorized users (LOG-3/5); tamper a row and confirm detection (LOG-6).
- **Update:** feed a tampered/unsigned package to the updater and confirm it is rejected (TSF-6).
- Run the AFTA per-class checklists end-to-end against the running system as the final acceptance gate.
