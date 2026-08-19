# M6 — Pilot enablement (doc 06 M6)

**Status:** PREPARED 2026-08-14 — checklist + role setup + operator script (Persian) +
monitoring + feedback sheet. Execution starts when Hossein commits/deploys and schedules
the pilot cohort. Doc 06 M6 acceptance: a real session by **≥2 editors + 1
reviewer/approver** completes the full cycle **on staging first**, then production pilot;
evaluation after ~2–4 weeks before widening layers.

---

## 1. Pre-flight (before anyone touches the app)

| # | Item | State |
|---|---|---|
| 1 | All M1–M5 code committed (M2+ is uncommitted as of 2026-08-14) | ☐ Hossein |
| 2 | API host deployed/restarted from the committed build (staging binds `localhost:5140` from source; the deployed instance has its own config) | ☐ |
| 3 | WPF client build distributed to pilot machines | ☐ |
| 4 | Registry check: `SELECT EntityName, IsVersioningEnabled, SchemaSignature FROM versioning.VersionedLayer` → TrLineSeg **enabled**, Substat **disabled**, both signatures stamped; boot log shows no signature-drift warning (D33) | ☐ |
| 5 | Staging DB is the current working system (D44) — no backup gate needed there; production pilot needs the normal backup window noted | ☐ |

## 2. Role setup (pilot cohort)

Versioning permissions are group «نسخه‌بندی» in the permission picker (enum 400–403;
SuperAdmin already holds all four via seeding):

| Permission (Display Name) | Persian description | Give to |
|---|---|---|
| Versioning.Edit | ایجاد و ارسال جلسه ویرایش | Editors |
| Versioning.Review | بررسی رقابت‌ها و انتخاب پیشنهاد برنده | Senior reviewer |
| Versioning.Approve | تأیید نهایی و ثبت تغییرات در وضعیت جاری | Approver |
| Versioning.HistoryRead | مشاهده تاریخچه عارضه | Everyone in the cohort |

Steps (all in the client, no SQL):
1. مدیریت نقش‌ها (Role Management): create three roles — «ویرایشگر نسخه‌بندی»
   (Edit + HistoryRead), «بررسی‌کننده نسخه‌بندی» (Review + HistoryRead),
   «تصویب‌کننده نسخه‌بندی» (Approve + HistoryRead). Reviewer and approver **should be
   different people** for the pilot (the server does not forbid dual roles).
2. تخصیص نقش به کاربران (User Management): ≥2 editor accounts + 1 reviewer + 1 approver.
   Editors must be real `dbo.User` rows — live-table audit columns FK to them (M4 lesson);
   any account created through the normal user management satisfies this.
3. Each user re-logs in; the ribbon tab «نسخه‌بندی» must show exactly the buttons their
   permissions allow (My-Pending/inbox need Edit; review needs Review; approve needs
   Approve; history needs HistoryRead).

## 3. Runtime smoke of the map-integrated pieces (FIRST)

Already verified without the full app (2026-08-14): the **entire M4/M5 API surface over
real HTTP** (15/15 — login, 401, all reads on the clean baseline, no-op MarkRead, typed
Persian-localizable error envelopes) and the **row context menu in a live WPF
FeatureTable instance** (14/14 binding checks + screenshot `m5-context-menu.png`).

What still needs the running client — interaction and map drawing:

1. **Submit-result dialog**: edit 2 features on TrLineSeg → save → one dialog with counts;
   map reverts to live truth (D34) — the edits "disappearing" from the map is correct and
   the dialog must say so.
2. **Attribute-table context menu** (shared FeatureTable change): identify a TrLineSeg
   feature → right-click the row → menu shows the default commands + «وضعیت پیشنهادها» /
   «تاریخچه عارضه» / «پیشنهادهای من روی نقشه»; the same three appear in the footer strip.
   Right-click must select the row under the cursor; a multi-selection that already
   contains the row must survive. **Also check one NON-versioned layer**: menu shows only
   the default commands and nothing broke.
3. **Pending-status dialog**: on a feature with a pending proposal → count + names
   (+ «شامل پیشنهاد شما»); on a clean feature → the no-pending message.
4. **History window from the context menu**: opens pre-loaded on that feature; bold
   «وضعیت جاری» row; «نمایش روی نقشه» draws hop vs live on the main map behind the dialog.
5. **Own-pending overlay**: with ≥1 pending proposal → amber layer «پیشنهادهای من — …»
   appears in the TOC and draws; invoking again refreshes; after commit/withdraw with
   zero pending → message + overlay removed; layer is deletable from the TOC.
6. **Inbox**: after a submission that joins a competition, both owners see N1 rows;
   unread dot/bold; mark-all-read works; counts update on refresh (manual only, D37).
7. **Localized errors**: force one error (e.g. resubmit a superseded-then-approved
   proposal, or two reviewers resolving the same competition) → the dialog shows a
   **Persian** message, not a raw `message_error_…` key.

## 4. Operator walkthrough — script (Persian)

Doc 02 §9 walkthroughs A, E, F are the UI-level acceptance (deferred from M3); C and D
were service-verified in M4 but their UI touchpoints are re-checked here.

### سناریوی الف — رقابت عمدی (walkthrough A)

1. **ویرایشگر ۱** و **ویرایشگر ۲** (هماهنگی شفاهی): هر دو عارضهٔ مشخصی از لایهٔ «خطوط
   انتقال» را ویرایش کنید (مثلاً تغییر ولتاژ) و ذخیره کنید.
2. هر دو باید پیام «جلسه ارسال شد» را ببینند؛ نفر دوم «۱ مورد وارد رقابت شد». نقشه به
   وضعیت تثبیت‌شده برمی‌گردد — این رفتار درست است، تغییر شما «در انتظار» است.
3. هر دو: «پیشنهادهای در انتظار من» → وضعیت «در رقابت (۲)». محتوای پیشنهاد رقیب نباید
   جایی دیده شود. «صندوق اعلان‌ها» → اعلان ورود به رقابت.
4. **بررسی‌کننده**: «بررسی نسخه‌ها» → رقابت را باز کنید؛ مقایسهٔ ستونی + «نمایش روی
   نقشه». پیشنهاد ویرایشگر ۲ را برنده کنید؛ **دلیل رد** برای بازنده الزامی است.
5. هر دو ویرایشگر: وضعیت هنوز «در حال بررسی» است — بازنده نباید قبل از تثبیت از رد شدن
   باخبر شود (این عمداً است).
6. **تصویب‌کننده**: «تأیید و ثبت» → ردیف را انتخاب و «ثبت موارد انتخابی».
7. بررسی نهایی: مقدار جدید روی نقشه (پس از به‌روزرسانی لایه)؛ «تاریخچه عارضه» یک پرش با
   نام ویرایشگر ۲ نشان می‌دهد؛ ویرایشگر ۲ اعلان «تثبیت شد»، ویرایشگر ۱ اعلان «رد شد» با
   دلیل.

### سناریوی ب — حذف در برابر ویرایش (walkthrough E)

1. ویرایشگر ۱ عارضه‌ای را **حذف** و ذخیره می‌کند؛ ویرایشگر ۲ همان عارضه را **ویرایش**.
2. بررسی‌کننده: در نمای مقایسه، حذف به‌صورت یک گزینه («حذف») دیده می‌شود. حذف را برنده
   کنید (با دلیل برای بازنده).
3. تصویب‌کننده: ردیف نشان «حذف» دارد؛ ثبت کنید.
4. بررسی: عارضه از نقشه رفته؛ «تاریخچه عارضه» آخرین وضعیت را به‌صورت پرش «Delete» نگه
   داشته و «این عارضه حذف شده است» را نشان می‌دهد.

### سناریوی ج — ایجادهای هم‌پوشان (walkthrough F)

1. هر دو ویرایشگر **عارضهٔ جدیدی تقریباً در یک مکان** رسم و ارسال می‌کنند (دو ایجاد
   مستقل — تصادم شناسه‌ای وجود ندارد).
2. بررسی‌کننده: در صف، نشان «پیشنهاد هم‌پوشانی» روی تک‌پیشنهادها؛ هر دو را انتخاب و
   «گروه‌بندی» کنید → یک رقابت. یکی را برنده کنید.
3. تصویب‌کننده ثبت می‌کند؛ فقط یک عارضهٔ جدید روی نقشه ظاهر می‌شود.
4. (اگر بررسی‌کننده گروه‌بندی نکند هر دو ثبت می‌شوند — ریسک پذیرفته‌شدهٔ نسخهٔ اول؛ در
   برگهٔ بازخورد ثبت کنید.)

### نقاط کنترلی سناریوهای ج/د (C/D — در سرویس تأیید شده)

- **کهنگی**: اگر ردیفی در صف تأیید نشان «قدیمی‌شده» دارد، ثبت بدون علامت‌زدن «می‌دانم
  عارضه پس از این پیشنهاد تغییر کرده است» باید با پیام فارسی مسدود شود.
- **بازگرداندن**: تصویب‌کننده یک ردیف را با دلیل بازگرداند → رقابت دوباره در صف بررسی؛
  بررسی‌کننده اعلان «بازگردانده شد» را می‌گیرد و می‌تواند برندهٔ دیگری انتخاب کند.

## 5. Monitoring queries (run on demand during the pilot)

All queries below were executed read-only against staging on **2026-08-14** — every one
runs clean. Baseline: zero competitions/proposals/notifications/history/commits, zero
stale, TrLineSeg enabled + Substat disabled (i.e. no test residue; the pilot starts from
an empty workflow).

```sql
-- Queue depth + oldest open competition age (hours)
SELECT c.State, COUNT(*) AS Cnt, DATEDIFF(HOUR, MIN(c.CreatedAt), SYSUTCDATETIME()) AS OldestHours
FROM versioning.Competition c WHERE c.State <= 1 GROUP BY c.State;   -- 0 Open, 1 Resolved

-- Proposals by state (0 Submitted, 1 SelectedForApproval, 2 ProvisionallyRejected, 3 Committed, 4 Rejected, 5 Withdrawn)
SELECT State, COUNT(*) AS Cnt FROM versioning.Proposal GROUP BY State ORDER BY State;

-- Oldest pending proposal age (hours)
SELECT DATEDIFF(HOUR, MIN(SubmittedAt), SYSUTCDATETIME()) AS OldestPendingHours
FROM versioning.Proposal WHERE State <= 2;

-- Stale pending proposals on TrLineSeg (base RowVersion behind live).
-- Live table schemas vary per entity (tr.Tr_Line_Seg, sub.Substat, …) — the API resolves
-- them from EF metadata; for ad-hoc SQL use the physical names.
SELECT COUNT(*) AS StaleCnt
FROM versioning.Proposal p
JOIN versioning.VersionedLayer l ON l.Id = p.VersionedLayerId AND l.EntityName = 'TrLineSeg'
JOIN tr.Tr_Line_Seg t ON t.Id = p.TargetFeatureId
WHERE p.State <= 2 AND p.BaseRowVersion <> t.RowVersion;

-- Notification backlog (unread per recipient)
SELECT RecipientUserId, COUNT(*) AS Unread FROM versioning.VersionNotification
WHERE ReadAt IS NULL GROUP BY RecipientUserId ORDER BY Unread DESC;

-- Commits per day + history growth
SELECT CAST(CommittedAt AS date) AS D, COUNT(*) AS Batches FROM versioning.CommitBatch GROUP BY CAST(CommittedAt AS date) ORDER BY D DESC;
SELECT COUNT(*) AS HistoryRows FROM versioning.FeatureHistory;

-- Grouping effectiveness (D16 residual-risk counter for the feedback review)
SELECT (SELECT COUNT(*) FROM versioning.DecisionRecord WHERE Action = 6) AS GroupedCompetitions,   -- 6 = GroupProposals (D49)
       (SELECT COUNT(*) FROM versioning.OverlapSuggestion WHERE DismissedAt IS NULL) AS UndismissedSuggestions;
```

## 6. Feedback capture sheet (per session, one row)

| Field | |
|---|---|
| تاریخ / نقش (ویرایشگر، بررسی‌کننده، تصویب‌کننده) | |
| سناریو (کار واقعی / الف / ب / ج) | |
| چند عارضه، چند دقیقه؟ | |
| آیا پیام یا وضعیتی گنگ بود؟ (متن دقیق) | |
| آیا ایجادِ تکراری دیدید که گروه‌بندی نشد؟ (D16 — صریحاً بپرسید) | |
| آیا مجبور شدید خارج از سامانه هماهنگ کنید؟ چرا؟ | |
| پیشنهاد | |

## 7. Substat enablement (second pilot layer, D47 — only after TrLineSeg looks healthy)

```sql
-- pre-check (D43 spirit: never disable/flip with open work in flight — for ENABLE it is
-- safe, listed for symmetry with the rollback below)
UPDATE versioning.VersionedLayer SET IsVersioningEnabled = 1 WHERE EntityName = 'Substat';
```
Clients pick it up on next login (layer list + gate cache ≤60s server-side).

**Rollback for a layer** (doc 06 §5): flip `IsVersioningEnabled = 0` — but D43: blocked
while open proposals exist. The enforcement is procedural; run first:
```sql
SELECT COUNT(*) FROM versioning.Proposal p
JOIN versioning.VersionedLayer l ON l.Id = p.VersionedLayerId
WHERE l.EntityName = 'Substat' AND p.State <= 2;   -- must be 0 before disabling
```

## 8. Acceptance + evaluation

- ☐ Staging dry run: full cycle (≥2 editors, 1 reviewer, 1 approver) through §3 + §4.
- ☐ Production pilot enabled for the cohort; date recorded.
- ☐ Evaluation review scheduled ~2–4 weeks out: monitoring numbers + feedback sheets →
  decide on widening layers (S6 opt-in exists for exactly this), UI adjustments, and
  whether the deferred items (withdraw endpoints, inbox polling) get pulled in.
