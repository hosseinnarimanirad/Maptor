# DNS & Mail Setup — Reference + `nagsheyar.ir` Fix Guide

A backend-dev reference for DNS records and email DNS, plus the concrete steps that
fixed (and finish) email for **`nagsheyar.ir`**.

> **The #1 rule that ties this whole doc together**
> Every DNS record has two sides:
> - **Name / Host** — the *left* side = **what you are defining** (a subdomain: `@`, `www`, `mail`, `_dmarc`…).
> - **Value** — the *right* side = **what it points to** (an IP, a hostname, or text).
>
> Mixing these up is the most common DNS mistake. It is exactly what broke the MX
> record below: the mail hostname was typed into the **Name** field instead of the **Value** field.

---

## 1. DNS record types

Mental model: **registrar** (holds your **NS** delegation) → **DNS provider** (hosts the **zone**)
→ the **zone** is just a list of records → each change propagates per its **TTL** (cache seconds).

| Type | Name side (left) | Value side (right) | Example | Common mistake / gotcha |
|------|------------------|--------------------|---------|--------------------------|
| **A** | subdomain (`@`, `www`, `mail`) | **IPv4** | `mail → 87.107.55.182` | Putting a *hostname* in the value. A/AAAA values are IPs only. |
| **AAAA** | subdomain | **IPv6** | `@ → 2a01:...` | Same as A but IPv6; don't reuse an IPv4 here. |
| **CNAME** | subdomain (alias) | **hostname** | `www → nagsheyar.ir.` | Cannot be used on the root `@`; cannot coexist with any other record on the same Name; value is a hostname (often needs a trailing dot). |
| **MX** | `@` (the domain mail is FOR) | **mail-server hostname** + **Priority** | `@ → mail.nagsheyar.ir` (prio 10) | ⚠️ **Name must be `@`, not the mail subdomain.** Value must be a **hostname, never an IP**. (This was the bug.) |
| **TXT** | subdomain or `@` | quoted **text** | `@ → "v=spf1 ..."` | Only **one SPF** TXT per name; keep it quoted; watch length limits. |
| **NS** | `@` or a subzone | nameserver hostname | `@ → ns1.arvancloud.ir.` | Set at the registrar; wrong NS = none of your other records are even consulted. |
| **SOA** | `@` | zone metadata (serial, TTLs) | auto | Usually auto-managed; don't hand-edit unless you know why. |
| **SRV** | `_service._proto` | `prio weight port host` | `_sip._tcp → 0 5 5060 sip...` | Format is 4 values; easy to get field order wrong. |
| **PTR** | *(reverse zone, not your zone)* | hostname | `87.107.55.182 → mail.nagsheyar.ir.` | Lives in the **IP owner's** reverse zone — you request it from the **host**, you can't add it in your domain's DNS. |
| **CAA** | `@` | which CA may issue certs | `0 issue "letsencrypt.org"` | Too strict a CAA can block your own cert renewals. |
| **ALIAS / ANAME** | `@` (root) | hostname | `@ → app.host.com` | Provider-specific workaround for "no CNAME at root"; not every DNS host supports it. |

**Proxy / CDN note (ArvanCloud, Cloudflare):** a record can be *proxied* ("cloud" ON /
orange). The proxy handles **HTTP/HTTPS only** — it hides your real IP behind the CDN.
Great for web records, **fatal for mail**: a proxied `mail` record makes SMTP point at the
CDN, which can't receive mail. Keep mail records **DNS-only** (proxy OFF).

---

## 2. Mail DNS records

Four records make email work and stay out of spam. MX delivers; the other three authenticate.

| Record | Type | Name | Purpose | Example value |
|--------|------|------|---------|---------------|
| **MX** | MX | `@` | Where inbound mail for the domain goes | `10  mail.nagsheyar.ir` |
| **SPF** | TXT | `@` | Which servers may send *as* your domain | `"v=spf1 a:mail.nagsheyar.ir -all"` |
| **DKIM** | TXT | `selector._domainkey` | Public key; receivers verify a signature | `"v=DKIM1; k=rsa; p=MIGf..."` |
| **DMARC** | TXT | `_dmarc` | Policy on SPF/DKIM failure + where to report | `"v=DMARC1; p=none; rua=mailto:postmaster@nagsheyar.ir"` |

Rules & gotchas:
- **MX** value is a **hostname, not an IP**; the priority (lower = preferred) allows backups.
- **SPF**: exactly **one** per name. End with `-all` (hard fail) or `~all` (soft fail).
  Stay under the 10-DNS-lookup limit.
- **DKIM**: the **selector** (`default`, `s1`, `google`…) is chosen by your mail server;
  the key value is machine-generated — **paste it, never hand-edit**.
- **DMARC**: start `p=none` (monitor + collect reports), then tighten to `quarantine` → `reject`.
- **PTR / reverse DNS**: required for self-hosted mail; set by whoever owns the sending IP.
  Forward (A) and reverse (PTR) should agree.

---

## 3. MX record anatomy (the two-field trap — worked example)

An MX record has **three** fields, and two of them are hostnames — which is what causes the mix-up:

| Field (panel labels vary) | Question it answers | Correct value here |
|---|---|---|
| **Name / Host** | *Which domain is this mail FOR?* | `@`  (= `nagsheyar.ir`) |
| **Value / Points to / "Mail server"** | *Which server RECEIVES that mail?* | `mail.nagsheyar.ir` |
| **Priority / Preference** | *Try order among multiple servers* | `10` |

Read as one sentence:
> "Mail for anyone **@nagsheyar.ir** (Name=`@`) is delivered to the server
> **mail.nagsheyar.ir** (Value), priority **10**."

### ❌ Wrong (the actual bug) vs ✅ Right

```
❌  Name = mail.nagsheyar.ir     Value = mail.nagsheyar.ir     Prio = 10
    → creates an MX for the "mail." SUBDOMAIN.
    → nagsheyar.ir itself then has NO MX  → ArvanCloud: "not configured properly."

✅  Name = @                     Value = mail.nagsheyar.ir     Prio = 10
    → creates the MX for the DOMAIN. Correct.
```

Also: the **Value must be a hostname, not an IP** —
`Value = 87.107.55.182` is invalid. Instead point MX → `mail.nagsheyar.ir`, and let a
separate **A** record resolve `mail.nagsheyar.ir → 87.107.55.182`. One clean indirection:
if the mail IP ever changes, you edit one A record and MX still works.

---

## 4. Split-host setup (web on one machine, mail on another)

Totally normal — a domain and its subdomains are independent records that can point anywhere.

Example — website on `11.11.11.11`, mail on `22.22.22.22`:

| Name | Type | Prio | Value | Meaning |
|------|------|------|-------|---------|
| `@`    | A  | — | `11.11.11.11` | root → web server |
| `www`  | A  | — | `11.11.11.11` | www → web server |
| `mail` | A  | — | `22.22.22.22` | **mail host** (proxy OFF) |
| `@`    | MX | 10 | `mail.example.com` | mail delivered to the mail host |
| `@`    | TXT| — | `v=spf1 a:mail.example.com -all` | SPF authorizes the mail host |

The two lines that make the split work:
```
mail  A   22.22.22.22          ← subdomain points at the other machine
@     MX  10  mail.example.com  ← delivery uses that subdomain (hostname, not IP)
```

---

## 5. This deployment — `nagsheyar.ir`

Facts:
- Domains owned: **`nagsheyar.ir`** (primary email), `naghshehyar.ir`, `nagshyar.ir`
  — all point to the same VPS. The two extras can later 301-redirect to the primary
  (and/or forward their mail to it); email is configured on `nagsheyar.ir` only.
- Web: ASP.NET Core on **IIS**, VPS **87.107.146.71**, HTTPS/Swagger already working.
- Mail: **cPanel** host (ParsVDS) at **87.107.55.182**, mailbox `noreply@nagsheyar.ir`.
- DNS at **ArvanCloud**.

Target zone:

| Name | Type | Prio | Value | Proxy |
|------|------|------|-------|-------|
| `@`   | A   | —  | `87.107.146.71`     | web (your choice) |
| `www` | A   | —  | `87.107.146.71`     | web (your choice) |
| `mail`| A   | —  | `87.107.55.182`     | **OFF (DNS-only)** ⚠️ |
| `@`   | MX  | 10 | `mail.nagsheyar.ir` | — |
| `@`   | TXT | —  | *(SPF from cPanel)* | — |
| `default._domainkey` | TXT | — | *(DKIM from cPanel)* | — |
| `_dmarc` | TXT | — | `v=DMARC1; p=none; rua=mailto:postmaster@nagsheyar.ir` | — |

### Fix checklist (in order)

1. **Consistency** — every record and mailbox uses `nagsheyar.ir` (not the other two spellings).
2. **ArvanCloud — MX (root cause fixed):** set **Name = `@`**, **Value = `mail.nagsheyar.ir`**,
   **Priority = 10**. (The old record had the mail hostname in the *Name* field.)
   The "not configured properly" warning clears once this is right.
3. **ArvanCloud — mail A record:** `mail → 87.107.55.182`, **proxy OFF**. (CDN proxy blocks SMTP.)
4. **cPanel → Email Deliverability** (Persian: «قابلیت تحویل ایمیل») for `nagsheyar.ir`:
   click **Repair/Manage** → copy the exact **SPF** and **DKIM** TXT values → paste into
   ArvanCloud. Add the **DMARC** record above.
5. **PTR** — ask the host (ParsVDS) to set reverse DNS for `87.107.55.182 → mail.nagsheyar.ir`.
6. **Test outbound** — from the cPanel server (SSH):
   `telnet alt2.gmail-smtp-in.l.google.com 25`.

---

## 6. Troubleshooting — the two walls

**A. ArvanCloud "MX not configured properly"** → almost always one of:
- Mail hostname in the **Name** field instead of `@` *(the bug here)*, or
- MX **Value** is an **IP** instead of a hostname, or
- The `mail` A record is **proxied** (cloud ON) so MX resolves to CDN IPs.

**B. "Cannot send" / `retry timeout exceeded` to Gmail** → this is **NOT** a DNS-records
problem. `retry timeout exceeded` means the mail server **couldn't connect to Gmail at all**
(if SPF/DKIM were the issue, Gmail would *answer* with a `550-5.7.x` rejection). Root cause is
usually **outbound port 25 blocked** or **Google silently dropping the (Iranian) sending IP**.

Diagnose from the mail server:
```bash
telnet alt2.gmail-smtp-in.l.google.com 25    # hangs = port 25 blocked / IP dropped
                                             # "220 mx.google.com" = connection OK
```
Also check `87.107.55.182` at mxtoolbox.com/blacklists.aspx, and use
**cPanel → Track Delivery** for the exact per-message error.

If port 25 is blocked / the IP is dropped, correct DNS won't rescue **outbound** delivery to
big providers. Options:
1. Configure an **SMTP relay / smarthost** in cPanel/Exim (clean, reputable IPs) so mail is
   handed to the relay instead of connecting to Gmail directly.
2. Ask the host to **open port 25**, fix **PTR**, or move you to a **cleaner IP**.
3. (Inbound mail — receiving — works with the DNS fixes alone; it's outbound to Gmail/Outlook
   that hits the reputation wall.)

---

## 7. Verification commands

```bash
dig +short MX  nagsheyar.ir                        # -> 10 mail.nagsheyar.ir.
dig +short A   mail.nagsheyar.ir                    # -> 87.107.55.182 (NOT an Arvan CDN IP)
dig +short TXT nagsheyar.ir                         # -> "v=spf1 ..."
dig +short TXT default._domainkey.nagsheyar.ir      # -> "v=DKIM1; ..."
dig +short TXT _dmarc.nagsheyar.ir                  # -> "v=DMARC1; ..."
dig +short -x  87.107.55.182                        # -> mail.nagsheyar.ir.  (PTR)
```
Windows equivalent: `nslookup -type=MX nagsheyar.ir`, `nslookup -type=TXT nagsheyar.ir`, etc.

Green when:
- ArvanCloud MX warning gone; `dig MX` shows `10 mail.nagsheyar.ir.`
- cPanel → Email Deliverability shows **SPF / DKIM / PTR all green**.
- `telnet …gmail…:25` connects from the mail server.
- A test mail to Gmail arrives; headers show `spf=pass` and `dkim=pass`; no bounce.

---

### SSL note
HTTPS/Swagger already loads, so your IIS cert is working. Only revisit if the browser shows
an untrusted/self-signed warning — then get a free Let's Encrypt cert on IIS via **win-acme**.
This is independent of email.
