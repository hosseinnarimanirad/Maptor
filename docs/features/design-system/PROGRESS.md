# Design-system remediation — progress log

**One of three documents in this folder.** [`AUTHORING.md`](AUTHORING.md) is what a view author
needs — tokens, screen grammar, colour rules, traps. [`README.md`](README.md) is the audit and the
registers. **This file is the itinerary**: what has been done, what was decided and why, what is
left, and how to pick the work up cold.

If you are resuming this work, read this file first, then §8 (verification) and §6
(must-not-change) of the README. If you are just writing a view, you only need `AUTHORING.md`.

Last updated: **2026-08-17**.

---

## 1. Status at a glance

| Step | Title | State |
|---|---|---|
| 1 | Build the missing vocabulary | **Done**, committed |
| 2 | Fix the theme-breaking bugs | **Done**, committed |
| 3 | Rewrite the SLD editor views | **Done**, committed |
| — | Dark-theme remediation (unplanned, user-driven) | **Done**, partly uncommitted |
| 4 | Security / auth surface | **Done**, committed — scope narrowed by the user |
| 5a | Library dictionaries + colours | **Done**, uncommitted |
| 5b | Re-point Saba's duplicated styles | **Done**, uncommitted |
| 5c | Delete README section 7 dead code | **Done**, uncommitted |
| 6 | Remaining sweep | **Done**, uncommitted |
| 7 | Documentation and guard | **Done**, uncommitted |

**All seven steps are complete.** What remains is not remediation work: the visual checks listed in
§6 below, and the two standing hazards (the probes live in a scratchpad; four apps cannot be built).

Build baseline after every step: `IRI.Maptor.Jab.Wpf` and `IRI.App.MakanNegarSaba` both at
**0 errors**, no new warning categories.

---

## 2. Commit map

| Commit | Contents |
|---|---|
| `97dc453c6` | `style: maptor design system step 1` |
| `c61b9e232` | `style: maptor design system step 2` |
| `75312c409` | `style: maptor design system step 3` |
| `ec3cabb41` | `style: maptor design system step fix theme` — legend views, `ApplicationPresenter` theme-mode fix, README |
| `3347c2b9d` | `fix: dark mode styles` — the `Background` fix on `MapLegendItemView` + `Scalebar`, `CoordinatePanelView` rebuild, plus the user's own fixes to `MainWindow`, `Controls.Button`, `LanguageSelectorView`, `SketchBarView` |

| `ea8c93f77` | `fix: styles` — status-palette scoping fix (README §4.4c) |
| `9594c725d` | `style: maptor design system step 4` (README §9d) |

### Uncommitted right now — Step 5a only (README §9e)

`IRI.Maptor.Colors.xaml`, `MapOptionStyles.xaml`, `MenuIconStyles.xaml`,
`FeatureTableFilters.xaml`, `DataGridDictionaryBehavior.cs`, 4 `MapOptions` views, 3 `MapMarkers`
views, `MapLegendView.xaml`, Saba `AboutMe`/`LoginView`/`BufferZoneView`, NaghsheYar `AboutMe`,
plus these docs.

---

## 3. What each step actually delivered

### Step 1 — Vocabulary
54 new keys across 6 new dictionaries (`Controls.Section`, `Controls.PasswordBox`,
`Controls.Inputs.Extra`, `Controls.Misc`, `Common.Effects`, `Brushes.OnMap`) plus metric tokens in
`Common.Metrics.xaml`. **Purely additive** — nothing was re-pointed yet, so this step could not
regress anything.

Root cause it addressed: the composition vocabulary (Card, FieldRow, FieldLabel, SectionExpander)
existed only in Saba's app-level `Saba.Styles.xaml`, so library views had nothing to build a form
from; and `Slider`, `ColorPicker`, `PasswordBox`, `GroupBox`, `ListBox`, `Separator`,
`GridSplitter` had no style anywhere.

### Step 2 — Theme bugs
All 13 theme-breaking bugs. Both `FeatureStatus` converters rewired onto the theme palette via a
new `Helpers/StatusBrushes.cs`; a new `EditorFacingStatus` → pill-style converter so a *rejected*
proposal no longer renders identically to a *committed* one.

### Step 3 — SLD editor
All 9 SLD views rebuilt on the shared vocabulary — the area the user identified as "really bad".
Uncovered a whole latent bug class: `Controls.ComboBox.xaml` and `Controls.RadioButton.xaml` bound
`{StaticResource Localization}` without declaring the provider, so those style entries silently
failed to load and the keys were absent at runtime. `RadioButton.Form` was actually referenced by
`LayerSettings_ExportView`, which had been rendering unstyled radio buttons.

### Dark-theme remediation (unplanned)
Triggered by the user reporting the legend did not follow the dark theme. Delivered: legend popup
surfaces; the `SystemColors.HighlightTextBrushKey` selection trigger that made a selected layer
name invisible; `CoordinatePanelView` rebuilt from an accent-filled bar into a themed surface
(labels went from **1.83:1** to **21:1** contrast); the `ApplyTheme` mode-drop bug; and the
status-palette scoping fix.

**The actual fix for the invisible layer names came from the user**, not from me: giving
`MapLegendItemView` an opaque `Background="{DynamicResource MahApps.Brushes.ThemeBackground}"`
instead of `Transparent`. See §5 below — my four wrong hypotheses are recorded there because the
reasoning error is reusable.

### Step 4 — Security / auth surface
**Scope deliberately narrowed by the user**: they are happy with how those screens look, so this
became a pure structural refactor. Two parts of the original plan were **dropped by decision, not
by oversight**:

- ~~Move the 7 Security views and 2 auth dialogs onto the majority dialog shell~~
- ~~Add a canonical `Validation.ErrorTemplate` and a single error mechanism~~

Either is a fresh decision if ever wanted, not leftover Step 4 work.

`SecurityInputStyles.xaml` is deleted. It had been merged by **13** views — the 7 Security ones
plus 4 Dialogs and 2 LayerSettings views that merged the entire file to reach two icon styles. The
security-specific remainder now lives in `Controls.SecurityInputs.xaml` under
`IRI.Maptor.Styles.Security.*`, and `myErrorTemplate` (zero references) was deleted.

Only one rendering change, explicitly approved: adornment icons moved from a fixed `Gray`
(`#808080`) to themed `Muted`, gaining an explicit `Height`.

---

## 4. What is left

### ~~Step 5 — Consolidate the parallel dictionaries~~ — **DONE** (5a §9e, 5b §9g, 5c §9h)

Original description kept below for context.

### Step 5 — Consolidate the parallel dictionaries — **highest ripple risk**
Fold `MapOptionStyles.xaml` and `MenuIconStyles.xaml` into the main convention; rename the
`FeatureTableFilters.xaml` keys; namespace `IRI.Maptor.Colors.xaml`'s bare names. Re-point Saba's
`SectionExpander`, `Card`, `Pill*`, `FieldRow`, `FieldLabel`, `FieldValue` at the promoted library
keys so Saba keeps only genuinely app-specific styles (`Avatar`, `UserRow`, `RoleCard`,
`RoleCheck`, `RoleBadge*`). Delete everything in README §7.

**Before starting, read §6 of this file.** Key renames propagate silently to consuming apps, and
four of those apps cannot currently be built, so a rename there is *unverifiable by compiler*.

### Step 6 — Remaining sweep
Map chrome onto the `OnMap` tokens; the Versioning queue views (`ReviewQueueView` has a good
`BadgesTemplate` that is file-private, so `ApprovalQueueView` re-inlines a diverged copy);
remaining dialog polish; `MultiSelectItem`'s two private ~120-line design systems; remove the six
app-shell `Slider` declarations once views reference the keyed style.

### Step 7 — Documentation and guard
Turn README §5 into a reference for view authors. Optionally add a build check that fails on new
literal hex under `Views/`.

---

## 5. Hard-won rules (do not relearn these)

**Building proves nothing.** XAML `StaticResource` resolves at *runtime*. A missing or misspelled
key compiles cleanly and crashes when the view is first realised. Every step here was verified by
constructing the affected views in a real `Application` context, not by `dotnet build`.

**Grep XAML *and* C# before calling a resource key dead, or renaming one.** `TryFindResource("literal")`
is invisible to an XAML search, and renaming past it fails **silently** — no build error, no
exception, the resource is just never applied. `FeatureTableColumnHeaderTemplate` looked dead by
XAML grep in Step 5a and was in fact fetched from `DataGridDictionaryBehavior.cs`. Known
string-referenced keys are listed in README §9e.

**A rename is only half-verified by checking the new key exists.** Also assert the *old* key no
longer resolves, or a half-finished rename passes silently. The probe now carries that negative
check.

**A `StaticResource` resolves only within its own dictionary and that dictionary's merges** — never
across sibling dictionaries in one `MergedDictionaries` list. This silently disabled
`ComboBox.Normal` and `RadioButton.Form` (Step 3). When adding any style with an `IsPersian`
trigger, declare the `Localization` provider *in that same file*.

**Element-tree resources beat `Application.Resources`.** A dictionary merged into a view's own
`Resources` is found first, so an app-level runtime swap can never win against it. This is what
made the status palette permanently light (README §4.4c).

**Score contrast against the surface actually painted behind the element**, not the theme token you
expect to be there. A `Transparent` control composites onto whatever its *container's* template
paints. Getting this wrong made a probe report "readable" for text that was invisible on screen.

**Composite translucent brushes before scoring them.** `MahApps.Brushes.Accent2/3/4` are one amber
at `0x99`/`0x66`/`0x33` alpha; treating them as opaque produces meaningless ratios.

**The accent is identical in light and dark.** Amber chrome says nothing about the active mode —
identify it by `MahApps.Brushes.ThemeBackground` (`#FFFFFFFF` light / `#FF252525` dark).
`IdealForeground` is white in *both*, so **nothing** can sit legibly on an amber accent fill; use
accent as a border with a themed surface behind text.

**When told "don't change how X looks", verify supposed duplicates setter-by-setter.** The audit
claimed three Security styles duplicated existing ones; each differed in ways that would have
changed rendering, so all three were kept verbatim and only renamed.

**A binary asset that renders blank is a bytes problem until proven otherwise.** Check the file
signature before investigating build actions, pack URIs or bindings. WPF distinguishes the two
cases for you: `NotSupportedException: No imaging component suitable` means *found but undecodable*;
`IOException` means *not found*. All 15 flag PNGs had been silently destroyed by
`* text=auto eol=crlf` in `.gitattributes` (README §9f) — and that damage is **not reversible**,
because collapsing `0d 0a` to `0a` loses which `0a` bytes had a `0d`.

**XML comments cannot contain `--`.** Cost two build breaks in this work.

---

## 6. Landmines and open items

**There are now TWO probes, both in the session scratchpad.** `StyleProbe/` covers the library;
`SabaProbe/` mirrors Saba's `App.xaml` and constructs Saba's own views, which the library probe
cannot see because it does not reference the app. Step 5b needed the second one. Both are lost
when the session ends.

**The verification probe lives in a session scratchpad and will not survive.** It is ~200 lines
(`Probe.cs` + `StyleProbe.csproj`), references `IRI.Maptor.Jab.Wpf.csproj` directly, and prints
`RESULT: PASS` / `FAIL (n)`. README §8 describes how to rebuild it. **Consider promoting it into
the repository** (e.g. `tools/StyleProbe/`) before Step 5 — that step needs it most, and
re-authoring it each session is waste. Not done yet because it adds files nobody asked for.

**Four apps cannot be built, for reasons unrelated to this work:**

| App | Failure |
|---|---|
| `AlborzNegar`, `RaviyaneNoor`, `Shahab` | `MC3074` — XAML binds `assembly=IRI.Maptor.Jab.Controls`, an assembly name that no longer exists |
| `NaghsheYar` | `CS2001` — source files referenced by a path outside the repository |
| `NiocExpSpatialEditor` | `NU1010` — no `PackageVersion` for `MahApps.Metro.IconPacks` / `Telerik.UI.for.Wpf.60.Xaml` |

They *do* reference `IRI.Maptor.Jab.Wpf` and *are* affected by shared-dictionary changes. Their
`App.xaml` already carries the Group A status merge. Treat them as verification blind spots.

**`ServerSettingsView`'s border does not paint.** `IRI.Maptor.Styles.Security.FieldGroup` binds
`BorderBrush`/`BorderThickness` to an ancestor named `root`; that view has none, so both setters
are inert. This predates the refactor (its private copy had the same dead bindings) and was left
as-is. Fixing it means giving the view a named root or a variant style.

**`FlashErrorIcon`** in `IRI.Maptor.Animations.xaml` is now unreferenced in the library but was
deliberately not deleted — that dictionary is merged by apps that cannot be built.

### Needs the user's eyes (nobody has seen these rendered)

- **The map navigation pad after Step 6** (README §9i). It was a dark widget with white glyphs and
  now follows the theme, so in the light theme it becomes a **light pad with dark glyphs** sitting
  on the map. That is the biggest single appearance change in Step 6 — check it against both a
  bright satellite basemap and a dark one.
- **The sketch bar's title and WGS84/UTM labels** — white → near-black on the amber. Contrast goes
  from 2.11:1 to about 9:1, but it is a visible change of character.
- **MultiSelectItem** — its selection/hover colour is no longer dark blue; it follows the accent.
- **Saba's role/user management + settings screens after 5b** (README §9g). Specifically: the
  panel header is now a **strong amber band** instead of a pale tint; pill spacing shifted; and the
  three collapsible sections have a new 1px rule above their body and 2px shorter headers. If the
  headers now look uneven where one section holds a toggle switch, that is the `MinHeight` 40→38
  change and the fix is a `MinHeight` on the library style.
- **Language selector flags** — replaced from flagcdn (README §9f). True flag aspect ratios now
  differ per country inside the 24×18 box, top-aligned; check whether that reads as ragged.
- **Map markers vs the MapOptions dot.** The user moved the MapOptions centre dot from `Accent`
  (80% alpha) to `Highlight` (opaque). The Step 5a markers are still on `Accent` and may want the
  same change for the same reason.
- **Map markers after Step 5a** — the biggest visible change so far. They went from opaque
  municipal red to `MahApps.Brushes.Accent`, which is amber **at 80% alpha**, and two of the three
  markers have no outline. Check them against satellite/bright imagery, not just street tiles. The
  fallback if it reads badly is a non-theme-swapped `IRI.Maptor.Brushes.OnMap.Marker` token.
- Security/auth screens and the 4 dialogs after Step 4 — the icon colour is the one visible delta.
- The `Accent4` value chip in `CoordinatePanelView` against real basemap imagery; the panel sits at
  `Opacity="0.8"` at rest.
- **A selected layer in the legend.** The selection `Foreground` override was removed because it
  painted white on a transparent background; selection is now indicated only by the item's own
  chrome. If that reads as insufficient, the fix is a real selected *background*, not restoring
  that setter.
