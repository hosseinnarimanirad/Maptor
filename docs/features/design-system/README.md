# IRI.Maptor.Jab.Wpf design system — audit, token reference, and remediation plan

Session date: **2026-08-16**
Scope: `src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf` (~100 XAML views), its `Assets/Styles`
dictionaries, the two `FeatureStatus` C# converters, and the consuming apps' style layers
where they are implicated.

Status: **Steps 1, 2 and 3 complete and verified. Steps 4-7 not started.** All work uncommitted.

---

> **Writing or editing a view?** You want [`AUTHORING.md`](AUTHORING.md) — the token reference,
> the screen grammar, the colour rules, and the traps. Nothing else in this folder is needed for
> that.
>
> **Resuming the remediation work?** Start with [`PROGRESS.md`](PROGRESS.md) — the itinerary (what
> is done, what was decided and why, what is left, known blind spots).
>
> **This file** is the audit and the registers: why each change was made, what must not be
> touched, and the per-step records.

## 0. How to use this document

Read §1 and §2 first — they explain *why* the UI is inconsistent, and that reasoning is what
makes the rest actionable. §5 is the token reference you need before writing any view. §6 is
the safety register: read it before running any bulk edit, because several things that look
like violations are deliberate and a careless sweep will break them.

Confidence markers used throughout:

- **[verified]** — I opened the file and confirmed it personally in this session.
- **[audited]** — reported by a delegated audit agent with file and line number, spot-checked
  but not exhaustively re-read. Treat as reliable but re-confirm before editing.

---

## 1. TL;DR

The project is **not** missing a design system. It has a good one: ~117 keyed resources under
`Assets/Styles`, merged through `Controls.All.xaml`, which every host app merges at App level.
Theme switching is live and real.

The inconsistency has **three independent causes**, and they need different fixes. Conflating
them is why this looks like a vague "everything is messy" problem when it is actually three
tractable ones.

| # | Cause | Fix shape |
|---|---|---|
| A | The *composition* vocabulary lived only in the Saba app, not the library | Promote it down (**done, Step 1**) |
| B | Several common controls had no style anywhere in the repo | Write them (**done, Step 1**) |
| C | Styles that did exist were simply never applied to ~37 views | Mechanical application pass (Steps 3-6) |

---

## 2. The three root causes, with evidence

### Cause A — composition vocabulary in the wrong project

The library owned *control* styles (Button, TextBox, ComboBox, TextBlock…). The *composition*
vocabulary — the part that makes a screen look designed — existed only in
`src/IRI.App/Barg/IRI.App.MakanNegarSaba/Assets/Saba.Styles.xaml`, 20 keys: **[verified]**

`Panel`, `PanelHeader`, `PanelTitle`, `Card`, `CardTitle`, `FieldRow`, `FieldLabel`,
`FieldValue`, `FieldHint`, `Pill(.Small)`, `PillText(.Small)`, `SectionExpander`, `Avatar`,
`UserRow`, `RoleCard`, `RoleCheck`, `RoleBadge(.Glyph)`.

`Saba.Styles.*` was the **only** app-level style prefix in the entire repository — no other app
(NaghsheYar, SanadNegar, AlborzNegar, GonbadNegar, RaviyaneNoor…) had one. **[verified]**

The reference view the user named as good —
`src/IRI.App/Barg/IRI.App.MakanNegarSaba/View/RoleManagement/UserManagementView.xaml` — is
built almost entirely from these keys. Views living *inside the library* had no equivalent, so
they could not look like it even in principle.

### Cause B — controls the design system never covered

Usage counted across `Views/**/*.xaml`: **[verified]**

| Control | Uses | Style before Step 1 |
|---|---:|---|
| `Slider` | 11 | none — every app re-declared `BasedOn MahApps.Styles.Slider` in its own shell (6 copies) |
| `mahApp:ColorPicker` | 10 | none anywhere in the repo |
| `Separator` | 13 | none |
| `ListBox` (container) | 13 | only `ListBoxItem` |
| `PasswordBox` | 12 | none in the library; Security grew a local one |
| `GroupBox` | 7 | none |
| `Popup` | 9 | none |
| `Label` | 5 | none |
| `TreeView` | 4 | none |
| `GridSplitter` | 2 | none |
| `DatePicker` | 2 | none |
| `ProgressRing` | 5 | none |

The SLD editor is built almost entirely from `Slider`, `ColorPicker` and `NumericUpDown` — two
of the three had zero coverage. That is the mechanical reason it looks unfinished.

### Cause C — existing styles simply not applied

**37 view files reference zero `IRI.Maptor.Styles.*` resources.** **[verified]** Thirteen are
MapMarkers (legitimately exempt, see §6). The rest are not.

The clearest single piece of evidence is `Views/Symbology/Sld/SldEditorView.xaml:14-15`
**[verified]**:

```xml
<!--this view had no shared styles in scope at all, so its tabs (and everything else)
    rendered with stock MahApps chrome instead of the Maptor design system-->
```

That premise is **false**. `Controls.All.xaml` is merged at App level in
`MakanNegarSaba/App.xaml:26`, `NaghsheYar/App.xaml:22` and `SanadNegar/App.xaml:22`, so every
`IRI.Maptor.Styles.*` key was already resolvable from those views. The author then merged the
dictionary locally at line 17 and applied the result to exactly one line — the `TabItem` style
at line 21 — and it never reached the seven child views.

`Views/Symbology/Sld/PointSymbolizerView.xaml` is the representative worst case **[verified]**:
78 lines, not one style reference, raw `Margin="4"` labels, a hard-coded
`<ColumnDefinition Width="120"/>` label column, and bare `ComboBox` / `NumericUpDown` /
`ColorPicker` / `Slider` / `TextBox` at WPF defaults.

Important context: the **older** `LayerSettings_ExportView.xaml` correctly applies eleven
distinct keyed styles. **[audited]** So this is a recent regression in the SLD work, not
legacy debt.

---

## 3. How the design system is wired

Worth writing down because none of it was documented anywhere before.

- **Entry point**: `Assets/Styles/Controls.All.xaml` merges every other dictionary. Consuming
  apps merge just this one, at App level.
- **Merge order matters.** Anything `BasedOn IRI.Maptor.Styles.TextBlock` must be merged
  *after* `Controls.TextBlock.xaml` — this is why `Controls.Pill.xaml` and (now)
  `Controls.Section.xaml` sit near the end of the list.
- **`StaticResource` inside a dictionary resolves against that dictionary and its own merged
  dictionaries.** So any file using `{StaticResource IRI.Maptor.Styles.CornerRadius.*}` must
  merge `Common.Metrics.xaml` itself. Several files already do; the new ones follow suit.
- **Theme swapping is live.** `Helpers/ThemeHelper.cs:155-163` **[verified]** swaps
  `Assets/Styles/Status.{Light|Dark}.xaml` into `Application.Resources` at runtime;
  `Controls.All.xaml` merges `Status.Light.xaml` as the default. Consequently **any literal
  hex or named colour on a Foreground/Background is a genuine dark-theme bug**, not a style
  preference.
- **Critical**: XAML `StaticResource` resolves at **runtime**, not build time. A missing key
  compiles cleanly and crashes the app at startup. See §8 for the verification recipe.

---

## 4. Findings by area

Four audits: three delegated (Security, Symbology, Versioning), one done directly
(Dialogs, Map, MapOptions, MapMarkers).

| Area | Files | Findings | Worst file |
|---|---:|---:|---|
| Security / Localization / MultiSelect / Controls / Imaging | 16 | 212 | `SecurityInputStyles.xaml` (25) |
| Symbology / SLD / LayerSettings | 12 | 267 | `SldEditorView.xaml` (57) |
| Versioning / GoTo / General (FeatureTable) | 13 | 157 | `FeatureTableFilters.xaml` (24) |
| Dialogs / Map / MapOptions / MapMarkers | 43 | see below | `EmailSignUpDialogView.xaml` |

### 4.1 Bugs — **all fixed in Step 2**, see §9b

These were defects users could see, not merely inconsistencies. Kept here as the record of what
was wrong and why, since the reasoning explains several of the Step 2 diffs.

1. **`Assets/Converters/ColorConverters/FeatureStatusToBrushConverter.cs:18-24`** **[verified]**
   returns `Brushes.Black` for `Unchanged` and `Brushes.DarkGray` as fallback; the rest come
   from `MapAppColors`. `FeatureStatusToBackgroundBrushConverter.cs:18-20` **[verified]** uses a
   *third* palette (`ModernUIColors`, hand-tuned 0.2/0.3 alpha). None participate in the theme
   swap, so the entire FeatureTable status column is frozen to light theme and an unchanged
   row's glyph renders black-on-dark.
   **This is a C# fix and the highest-leverage single change in the audit.**
2. **`Views/Versioning/MyPendingView.xaml:60-61`** **[audited]** renders six distinct statuses in
   one identical `Pill.Accent`. `StatusLabel` (`MyPendingViewModel.cs:99-107`) resolves
   `PendingReview`, `InCompetition`, `UnderReview`, `Committed`, `Rejected`, `Withdrawn` — and
   **a rejected proposal is pixel-identical to a committed one**, differing only in Persian text.
3. **`Assets/Styles/SecurityInputStyles.xaml:51`** **[verified]** — the password reveal button is
   literally `<Button Background="Red" Height="30" Width="30"/>`: a bare red square, no icon,
   no hover, no style.
4. **`Assets/Styles/SecurityInputStyles.xaml:73`** **[verified]** — `borderGroup` sets
   `Background="White"` and all seven Security views use it. The foreground *does* follow the
   theme, so dark mode gives near-white text on a white slab.
5. **`Views/GoTo/GoToView.xaml:20`** **[audited]** — a `TextBlock` with no `Foreground` on a
   `MahApps.Brushes.Highlight` pane while its sibling at line 22 is explicitly `White`. Dark
   text on a coloured pane today. Line 19 `Fill="White"` and line 23 `FontFamily="Segoe UI"`
   compound it (the latter un-does the RTL font handling every other view respects).
6. **`Views/LayerSettings/LayerSettings_GeneralView.xaml:129/137/146`** **[audited]** —
   `Foreground="White"` glyphs sitting on `SystemControlDisabledBaseLow`; effectively invisible
   in light theme.
7. **`Views/Symbology/Sld/SldEditorView.xaml:162` and `:262`** **[audited]** —
   `Background="LightGray"` GridSplitters; bright bars bisecting the panel in dark theme. Note
   line 148 in the same file uses `MahApps.Brushes.Gray8` correctly, 14 lines earlier.
8. **`Views/MultiSelectItem/SelectedItem.xaml:111`** **[audited]** — references
   `{DynamicResource LightRedColor}` with a capital L, which does not resolve. The mouse-leave
   animation silently no-ops, so the button stays dark blue after hover.
9. **`Views/Security/ForgetPasswordView.xaml:22-25`** **[audited]** — declares one row and places
   its only child at `Grid.Row="1"`. WPF clamps so it renders today, but it breaks the moment a
   second row is added.
10. **`Views/Symbology/Sld/TextSymbolizerView.xaml:11`** **[audited]** — adds its own
    `ScrollViewer` inside the one `SldEditorView.xaml:266` already provides. Nested scrollers.
11. **`Views/Dialogs/EmailSignUpDialogView.xaml:67,130`** and
    **`Views/Dialogs/ChangePasswordDialogView.xaml:70`** **[verified]** — `Background="White"` /
    `Foreground="Black"` on dialog text; dark-theme breaks.
12. **`Views/Imaging/ImageViewer.xaml:8`** **[audited]** — `Background="#FFFAFFFF"`, an off-white
    with a 1/255 blue tint, hard-locking the image canvas to light theme.
13. **`Views/Dialogs/EmailSignUpDialogView.xaml:56,58`** **[verified, found during Step 2]** —
    `Foreground="White"` set inline on both dialog buttons, overriding the styles' own. Harmless
    on `Button.Primary.Large` (already `IdealForeground`) but wrong on `Button.Secondary.Large`,
    whose background is `MahApps.Brushes.Gray10` — **white-on-near-white made the Cancel caption
    invisible in light theme.** This was worse than several items on the original list and was
    only found by reading the surrounding markup while fixing #11.

### 4.2 Fragmented vocabularies

- **Status colours** — three greens for three meanings (`Valid` for pills; `Valid.Fill`
  repurposed as "live" in `FeatureTimelineView:93-94`; `EmeraldBrush`/`#FF008A00` for "New" in
  FeatureTable). Four ambers, including `Warning` repurposed to mean "unread" in
  `InboxView:85-87`. The "delete" badge appears under **three** localization keys bound to
  **two** different properties across four files. **[audited]**
- **Error display** — four mutually incompatible mechanisms across seven sibling Security
  forms: an inline `Validation.HasError`→`ToolTip` trigger (one view), `ValidationRules` with
  no visual feedback at all (two views), and nothing whatsoever (four views, including all
  three password-confirmation pairs). Meanwhile `myErrorTemplate` is defined, styled and
  animated in `SecurityInputStyles.xaml:89-130` — and referenced by **nothing**. **[audited]**
- **Drop shadows** — 32 hand-rolled `DropShadowEffect` instances, no token. **[verified]**
  `BlurRadius="8" Color="Gray" Opacity="0.5" ShadowDepth="2"` appears 4× byte-identical
  (`GeometryEditorView:274`, `MapLegendView:225`, `MapLegendView:319`, `FeatureTableFilters:64`);
  `BlurRadius="2" ShadowDepth="1" Opacity="0.25" Color="Black"` appears 6×.
- **Inline `FontSize`** — 44 instances spanning 7, 8, 10, 11, 12, 13, 14, 15, 16, 18, 20
  **[verified]**, while `Controls.TextBlock.xaml` already defines a scale (11/12/13/14/20).
- **Label-column widths inside one dialog** — 90 (`SimpleFilterEditorView:31`), 100
  (`ScaleRangeEditorView:15`, `SldEditorView:77` and `:183`), 120 (all five symbolizers).
  Labels visibly fail to align as the user moves between tabs. **[audited]**
- **Naming conventions** — `IRI.Maptor.Styles.*` coexists with camelCase dictionaries
  (`mapOptionsButton`, `menuIcon`, `securityIconPhosphor`) and bare colour names (`red`,
  `green`, `steel`). `FeatureTableFilters.xaml` mixes three conventions in one file.
  **[verified]**

### 4.3 `SecurityInputStyles.xaml` is a second, ungoverned design system

> **RESOLVED in Step 4 (2026-08-17) — see §9d.** The file is deleted; what was genuinely
> security-specific lives in `Controls.SecurityInputs.xaml` under the normal key convention, and
> the six unrelated views no longer merge anything security-named. The audit below is kept as the
> record of why. One correction to it: `borderGroup`, `securityPathText` and
> `securityPasswordBoxBase` were **not** folded into `Border.Card` / `TextBlock.Caption` /
> `TextBox.Large` as this section anticipated. Each differs from its supposed twin in ways that
> would have changed how the auth screens render, and the user had signed those screens off, so
> all three were kept verbatim and only renamed.

Named as if feature-local, but **six unrelated dialog and layer-settings views already reach
into it** for generic form iconography (`CsvTsvOpenDialogView`, `DxfOpenDialogView`,
`GeoJsonTopoJsonOpenDialogView`, `PrintToPdfDialogView`, `LayerSettings_ExportView`,
`LayerSettings_GeneralView`). **[audited]**

Of its 7 keys, only 2 are genuinely security-specific. `borderGroup` duplicates `Border.Card`,
`securityPathText` duplicates `TextBlock.Caption`, and `securityPasswordBoxBase` is a
near-clone of `TextBox.Large` — identical in 7 of 8 setters, with the eighth being a latent
bug: it pins `FontFamily` to `iranSans` unconditionally, so a `PasswordBox` and the `TextBox`
directly above it render in different typefaces under any non-Persian culture. **[audited]**

### 4.4 Dialogs — the pattern is established, and two files opt out

11 of 14 dialogs follow one clean shell **[verified]**: `MetroWindow` +
`IRI.Maptor.Styles.MetroWindow.Dialog` + `Border.Card` sections + `DialogFooterView`.

The deviants are exactly the two auth dialogs — `ChangePasswordDialogView.xaml` and
`EmailSignUpDialogView.xaml` — which use neither the window style nor the footer, set
`FontFamily` on the window directly, and depend on the `SecurityInputStyles` parallel system.
`MessageBoxView.xaml` uses the window style but no footer.

That is a coherent story: **the auth/security surface is the one area that never adopted the
dialog pattern.**

### 4.4a Legend views and dark theme (user-reported, 2026-08-16)

The three legend views are mostly theme-aware already — 76 `DynamicResource` theme-brush uses
between them and **no** hardcoded `Foreground` on any `TextBlock`. The dark-theme failures are
concentrated in three popup surfaces, plus some untokenized shadows. Full inventory of the 15
literal colours:

| Site | Verdict |
|---|---|
| `MapLegendView.xaml:221` `Background="White"` (data-source filter popup) | **real bug** → `Border.Popup` |
| `MapLegendView.xaml:314` `Background="White"` (import-layer popup) | **real bug** → `Border.Popup` |
| `MapLegendItemView.xaml:759/760` `Background="White"` + `BorderBrush="#E0E0E0"` (pending-changes popup) | **real bug** → `Border.Popup` |
| `MapLegendView.xaml:225/319`, `MapLegendItemView.xaml:44/742/889`, `MapDrawingLegendView.xaml:71` — `DropShadowEffect Color="Gray"/"Black"` | cosmetic → `Effects.Elevation1/2/3` |
| `MapLegendItemView.xaml:978` `BorderBrush="Black"` | inert — `BorderThickness="0,0,0,0"` |
| `MapLegendView.xaml:270`, `MapLegendItemView.xaml:718` | dead — inside comment blocks |
| `MapLegendItemView.xaml:779/786` `Foreground="White"` on glyphs | dead — parent `StackPanel` is `Visibility="Collapsed"` |

So the visible defect was **four attributes across three popups**. **Fixed 2026-08-16** — all
three now use `IRI.Maptor.Styles.Border.Popup`. Verified by constructing all three legend views
in the probe.

The five `DropShadowEffect` colours were **deliberately left alone**. A black shadow is correct
in both themes, so they are not a theme bug; swapping them for `Effects.Elevation*` changes the
tuned blur/depth/opacity for no functional gain. Tokenize them in Step 6 if wanted.

#### Saba's Expander headers — checked, and NOT the problem

`IRI.App.MakanNegarSaba/MainWindow.xaml:1156/1166/1174/1198` hardcode `Foreground="White"` on the
legend Expander headers. I assumed this was a second contributor and **measured it instead of
guessing. It is not.** Under Saba's shipped `Light.Amber` accent:

| | accent | `IdealForeground` | contrast |
|---|---|---|---|
| Light.Amber | `#CCF0A30A` | `#FFFFFFFF` | 2.11:1 |
| Dark.Amber | `#CCF0A30A` | `#FFFFFFFF` | 2.11:1 |

MahApps derives **White** for text-on-accent in both themes, and the accent itself does not
change between them. So the hardcoded `White` already matches what the token would produce;
replacing it would be a visual no-op today. The header *is* low-contrast at 2.11:1 (WCAG AA
wants 4.5:1 for body text), but that is inherent to white-on-amber — a property of the chosen
MahApps accent, not a bug a token swap can fix. Changing it is a design decision about the
accent, not remediation.

Empirically confirmed with a stock MahApps `Expander` realised in the probe: its header really is
accent-painted (`Border bg=#CCF0A30A`), so text-on-accent is the right mental model — the
conclusion is just that White is already the right answer for this accent.

#### Second user report: drawing-legend item text unreadable — NOT reproduced

A follow-up report said the drawing legend's layer-name text cannot be seen in dark theme. I
built a contrast scanner into the probe (realise the view under the dark palette, walk the visual
tree, score every foreground against the nearest opaque ancestor background) and ran it over all
three legend views. **Result under a correctly themed host: 0 text elements below 3:1.** The
defect does not reproduce from the library code alone.

Two false leads are recorded here because both are easy to fall into again:

1. **"Black text on #252525, 1.37:1"** — my first scan reported exactly this and it looked like a
   smoking gun. It was a harness artifact. `TextBlock.Foreground` is an *inherited* property, and
   I had hosted the views in a bare `Border`; a real `MetroWindow` sets a themed
   `TextElement.Foreground` that cascades down. With that inheritance in place the readings are
   clean. The scanner now runs both ways and labels them.
2. **"Accent2/3/4 are unreadable"** — the scan flagged them at 2.11:1, but `Accent`/`Accent2`/
   `Accent3`/`Accent4` are the *same* colour at decreasing alpha (`#CC/#99/#66/#33 F0A30A`).
   Scoring them as opaque is wrong; composited over `#252525` they resolve dark and themed text
   on them is fine. Any contrast tooling here must composite alpha before judging.

What *is* real and worth knowing: because these `TextBlock`s set no `Foreground` of their own,
they depend entirely on inheritance. Any ancestor that sets a non-themed `Foreground` breaks
them. There is already one such ancestor in this very file — `drawingItemSelector` in
`MapDrawingLegendView.xaml` sets `Foreground="LightGray"` on `IsEnabled=False`. Note also that
`IRI.Maptor.Styles.TextBlock` (the base) deliberately sets **no** `Foreground`, and must not be
given one: `TextBlock.ButtonContent` derives from it and relies on inheriting the host button's
`IdealForeground`, so adding a themed foreground to the base would turn every button caption dark
on its accent background.

Three of the five affected `TextBlock`s were given `Style="{StaticResource IRI.Maptor.Styles.TextBlock}"`
anyway — they already declared `VerticalAlignment="Center"`, so this is a zero-layout-change
consistency win that also picks up the Persian font trigger. **It does not change any colour**
and is not a fix for the reported symptom.

**Was still open** at that point. Resolved below once a screenshot arrived.

#### 4.4b Resolved with a screenshot — "a theme that was applied only partially" [verified]

The screenshot showed the legend list painted **white** while the surrounding chrome was amber,
with no layer names visible except on the one drawing item whose selected row paints a dark
accent behind it.

The decisive fact: `#FFFFFFFF` is `Light.Amber`'s `ThemeBackground`. **The app was running the
light theme**, not a dark one. The amber accent is byte-identical in both modes, so dark-looking
chrome says nothing about which mode is active — that misled both of us.

Four hypotheses were measured and **all four disproved**. Recording them so nobody re-walks them:

| Hypothesis | Measurement | Verdict |
|---|---|---|
| Items host is a stock control defaulting to `SystemColors.WindowBrush` | `TreeView`/`ListBox` `Background` = `#FF252525` under dark | wrong — MahApps implicit styles do theme them |
| MahApps `Expander` leaks `IdealForeground` (white) into its content | `Expander.Foreground` = `#FF000000` light / `#FFFFFFFF` dark | wrong — it tracks the theme correctly |
| The row `RadioButton` (implicit `ToggleButton` style) hands down white | row `Foreground` = `#FF000000` light / `#FFFFFFFF` dark | wrong — also correct |
| Library dictionaries merge their own MahApps theme, so brushes resolve in the wrong scope | no MahApps theme anywhere under `Assets/` | wrong for MahApps brushes — **but true for the status palette, see §4.4c** |

Under a *consistently applied* theme every legend text/surface pair measures readable in both
modes. The bug is therefore not a colour choice in the legend at all — it is that the app can end
up in light mode when the user asked for dark.

> **CORRECTION (same day).** The paragraphs below identify a real bug, but it was **not** the
> cause of the invisible layer names. The user found the actual fix: give `MapLegendItemView` an
> **opaque themed background**.
>
> ```xml
> Background="{DynamicResource MahApps.Brushes.ThemeBackground}"   <!-- was Transparent -->
> ```
>
> Why that works and why the measurements missed it: each row is a `RadioButton` wearing the
> implicit MahApps `ToggleButton` style, and that template paints its **own** background. With the
> item view `Transparent`, the layer name composited onto the *button's* surface, not the items
> host's. Every probe above scored text against `ThemeBackground` — the wrong surface, so the pairs
> came back readable while the real pairing was unreadable. **Lesson: score text against the
> surface actually painted behind it in the visual tree, not against the theme token you expect
> to be there.** The same one-line fix was applied to `Scalebar`.
>
> The theme-mode bug below is still genuine and worth keeping fixed — it just was not this
> symptom.

**A separate real bug found while investigating — `ApplicationPresenter.cs:435-442`.** The handler
re-applied the theme on accent change but passed only the colour:

```csharp
ThemeHelper.ApplyTheme(theme.Value);          // mode omitted
```

`ThemeHelper.ApplyTheme` declares `mode ??= ThemeMode.Light`, so every accent change silently
reset the whole app to light. Fixed to pass `GeneralSettings?.MahAppsThemeMode`, and to also fire
on `MahAppsThemeMode` changing (it was ignored entirely before).

Note the one path where this was masked: `ThemeSelectionViewModel.SelectThemeCommand` assigns
`_generalSettings.MahAppsTheme` (firing the buggy handler) and *then* calls
`ApplyTheme(theme.Color, SelectedMode)` itself, so the correct mode landed last. Any other writer
of `MahAppsTheme` got light. `ThemeHelper.AvailableThemes` hardcoding `ThemeMode.Light` at
`ThemeHelper.cs:57` is **not** a bug — `ThemeSelectionViewModel.LoadThemes` overwrites `.Mode` on
every item at line 86.

**Two genuine legend defects fixed alongside**, both independent of the theme mode:

- `MapLegendView.xaml` selected-node trigger set
  `TextElement.Foreground` to `SystemColors.HighlightTextBrushKey` — the colour for text sitting
  *on* the selection highlight — while the line above forces `Bd.Background` to `Transparent`
  (the highlight fill is commented out). White text on the list's own surface: the selected
  layer name was invisible in the light theme. Setter removed.
- The `IsEnabled=False` trigger used `SystemColors.GrayTextBrushKey`, a fixed mid-grey that does
  not move with the theme. Now `MahApps.Brushes.Gray3`.

**Defensive change:** all three legend view roots now set
`Foreground="{DynamicResource MahApps.Brushes.ThemeForeground}"`. Their text elements previously
carried no foreground at all and depended entirely on inheriting a correct one from whatever host
they were dropped into. This does not change any colour under a correct theme — it stops a
mis-themed host from making the names invisible. Safe with respect to the
`TextBlock.ButtonContent` trap in §4.4a: a `Button`/`ToggleButton` sets its own `Foreground`,
which still wins for its own content.

#### 4.4c The status palette never goes dark inside any view [verified]

Found while chasing the above. **FIXED 2026-08-17** — see the resolution at the end of this
section.

`Controls.All.xaml:9-10` merges `Status.Light.xaml` and comments that "ThemeHelper appends
Status.Light/Dark at app level, which **wins**". That is backwards. WPF resolves a resource by
walking the element tree's own dictionaries first and only then `Application.Resources` — so any
view that merges `Controls.All.xaml` into its local `Resources` (which is nearly all of them)
finds the light palette first and never sees the app-level dark one.

Measured under `Status.Dark.xaml` applied at app level:

```
IRI.Maptor.Brushes.Valid     app=#FF4CC38A  view=#FF1B7F4B   <== view does not see the dark palette
IRI.Maptor.Brushes.Invalid   app=#FFF2777A  view=#FFB3261E
IRI.Maptor.Brushes.Warning   app=#FFE5B84B  view=#FF8A6100
IRI.Maptor.Brushes.Muted     app=#FF9AA0A6  view=#FF5F6368
```

This also means `Helpers/StatusBrushes.cs` (Step 2) and XAML disagree: the helper calls
`Application.Current.TryFindResource`, which searches app scope and *does* get the dark value,
while a `{DynamicResource}` in the same view gets the light one. Same key, two colours.

All 93 usages across the repo are `DynamicResource`, **zero** `StaticResource` — so removing the
merge cannot throw; a missing key just leaves the property unset.

**Resolution (2026-08-17).** The `Status.Light.xaml` merge was removed from `Controls.All.xaml`
(replaced by a comment recording why it must not come back), and added at **application** level to
the 9 App.xaml files that reference this library:

```
IRI.App.AlborzNegar          IRI.App.NiocExpSpatialEditor   IRI.Maptor.Bag.Geospatial
IRI.App.MakanNegarSaba       IRI.App.SanadNegar             IRI.Maptor.Bag.SpatialDataManagement
IRI.App.RaviyaneNoor         IRI.App.Shahab
IRI.App.NaghsheYar
```

The other 6 WPF apps (`PezeshkYarHaj`, `GonbadNegar`, `EsiDb`, `Tahlilgar3D`, `ZaminNegar`,
`RahyabTehran`) were checked and reference `IRI.Maptor.Jab.Wpf` **nowhere** — no csproj reference,
no pack URI. They were deliberately left alone; adding a pack URI for an unreferenced assembly
would throw at startup.

Placement matters: the entry goes in `Application.Resources.MergedDictionaries` at top level,
because `ThemeHelper.ApplyTheme` only scans that list for `/Assets/Styles/Status.` to remove, and
appends the new variant last so it wins. Nested inside another dictionary it would be invisible to
the swap and the bug would return in a new form.

Verified: the same probe that exposed the bug now reports `app` and `view` lookups **agreeing** on
all four keys under `Status.Dark.xaml`, with all 88 style keys still resolving.

```
IRI.Maptor.Brushes.Valid     app=#FF4CC38A  view=#FF4CC38A   (agree)
IRI.Maptor.Brushes.Invalid   app=#FFF2777A  view=#FFF2777A   (agree)
IRI.Maptor.Brushes.Warning   app=#FFE5B84B  view=#FFE5B84B   (agree)
IRI.Maptor.Brushes.Muted     app=#FF9AA0A6  view=#FF9AA0A6   (agree)
```

Build status of the 9: `MakanNegarSaba`, `SanadNegar`, `Bag.Geospatial`,
`Bag.SpatialDataManagement` and the library build clean. `AlborzNegar`, `RaviyaneNoor`, `Shahab`,
`NaghsheYar` and `NiocExpSpatialEditor` fail — but on **pre-existing** breakage unrelated to this
change, and in every case `App.xaml` itself compiled:

- `MC3074` — their XAML still binds `assembly=IRI.Maptor.Jab.Controls`, an assembly name that no
  longer exists (the library is `IRI.Maptor.Jab.Wpf`). These apps have been unbuildable for a while.
- `NaghsheYar` — `CS2001`, source files referenced by a path outside the repository.
- `NiocExpSpatialEditor` — `NU1010`, `MahApps.Metro.IconPacks` / `Telerik.UI.for.Wpf.60.Xaml` have
  no `PackageVersion` under central package management.

**Note for whoever revives those four apps:** the `App.xaml` merge is already in place, so the
status palette will theme correctly once the namespace/package problems are sorted.

#### 4.4d Over-map chrome: Scalebar and CoordinatePanelView [verified]

The §6 register said over-map surfaces were deliberately **not** theme-swapped, on the reasoning
that they float over unpredictable map imagery. The user overrode that for `Scalebar` (themed
`Background`/`Foreground`, dropped the literal `Background="White"`) and was happy with the
result, so themed over-map chrome is now the intended direction. `Brushes.OnMap.*` remains for
surfaces that genuinely sit on imagery with no panel behind them.

`CoordinatePanelView` was then brought to the same language. It had measured badly in **both**
modes, because it filled the panel with `MahApps.Brushes.Accent` and put hardcoded white text on
it:

| pair | Light | Dark |
|---|---|---|
| `staticText` White on Accent (as written) | **1.83:1** | **2.97:1** |
| `IdealForeground` on Accent — the usual rescue token | **1.83:1** | **2.97:1** |
| `dynamicText` Highlight on ThemeBackground | 3.78:1 | 4.06:1 |

`IdealForeground` is white in both Amber themes, so **no available foreground token can sit
legibly on the amber accent** — the accent had to stop being the fill. It is now the *border*,
with a themed surface behind the content:

| pair | Light | Dark |
|---|---|---|
| labels — ThemeForeground on ThemeBackground | 21.00:1 | 15.33:1 |
| values — ThemeForeground on Accent4 chip | 18.14:1 | 10.33:1 |

Chip choice was measured, not guessed. `Gray10` (the standard subtle-surface token) composited to
only **1.07:1** against a white panel and effectively vanished; `Accent3` went too heavy in dark
(2.30:1 separation). `Accent4` sits at 1.16:1 light / 1.48:1 dark — visible, subtle, and it keeps
a hint of the panel's original amber identity. **Composite translucent tints over their real
backdrop before scoring them**; `Accent2/3/4` are the same amber at `0x99/0x66/0x33` alpha and
scoring them as opaque is meaningless (an error made twice in this project).

Also fixed there: three hardcoded `Foreground="White"` values, and `Width="100"` on the value
chips became `MinWidth` (a fixed 100px clipped long UTM eastings).

### 4.5 MapOptions — near-consistent, one real divergence

All four sibling views are `Width="150" Height="130"` and correctly use `mapOptionsButton` /
`mapOptionsPath` throughout. **[verified]** The only divergence is the centre anchor dot:

| View | Dot |
|---|---|
| `MapTwoOptions`, `MapThreeOptions` | `Width="10" Height="10"`, opaque, `x:Name="pointEllipse"` |
| `MapFourOptions`, `MapFiveOptions` | `Width="8" Height="8" Opacity=".6"`, unnamed |

So the same anchor point renders at two sizes depending on how many options the menu has.
All four use `Stroke="White"`, which is over-map contrast — see §6.

---

## 5. Token reference (as it stood after Step 1 — superseded)

> **Use [`AUTHORING.md`](AUTHORING.md) instead.** This section is the Step 1 snapshot and is now
> incomplete: Steps 4, 5 and 6 added `IRI.Maptor.Styles.Security.*`, `IRI.Maptor.Templates.*`,
> `IRI.Maptor.Brushes.Brand.*`, `IRI.Maptor.Brushes.OnAccent.Text`, the renamed `Button.MapOption`
> / `Path.MapOption` / `PackIconMaterial.Menu` / `Path.Menu` / `Border.Menu`, and removed the
> 23 bare colour names. The current total is **190 keys across 32 dictionaries**.
>
> Kept as the record of what Step 1 delivered.

Everything below resolves from `Controls.All.xaml`. Verified present by the probe in §8.

### Metrics — `Common.Metrics.xaml`
`Thickness.DialogContent`(8), `.RowGap`(0,4), `.DialogFooter`(0,8,0,0), `.TabTrackPadding`(3)
`CornerRadius.Control`(6), `.Surface`(5), `.TabTrack`(9)
**New:** `Size.FieldLabelColumn`(120), `Thickness.FieldGap`(4), `.FieldRowGap`(0,0,0,6),
`.ViewContent`(8), `.SectionGap`(0,8,0,0), `Size.Icon.Field`(24), `.Icon.Small`(16),
`.Icon.Indicator`(10), `Size.Button.IconSquare`(32), `Opacity.Disabled`(0.5)

### Surfaces — `Controls.Border.xaml` + **new** `Controls.Section.xaml`
`Border.Card` — **field-row container** (MinHeight 39, padding 4,0). *Not* a content card.
`Border.Panel`, `.PanelHeader`, `.Toolbar`, `.DialogFooter`
**New:** `Border.Section` (content card, padding 12,9), `.Divider`, `.Banner`
(+`.Warning`/`.Invalid`/`.Accent`), `.Popup`

### Text — `Controls.TextBlock.xaml` + **new** `Controls.Section.xaml`
`TextBlock`, `.WindowTitle`(14/Bold), `.HeaderTitle`, `.SectionHeader`(12/SemiBold/Accent),
`.Normal`(12), `.Normal.Bold`, `.ButtonContent`(13), `.ButtonContent.Small`(11), `.Title`(20),
`.EmptyState`(13), `.Caption`(11/Muted), `.Hint`(13)
**New:** `.PanelTitle`(14/Bold), `.CardTitle`(13/Bold), `.FieldLabel`(12/Gray3),
`.FieldValue`(13), `.FieldHint`(11/Gray3), `.Error`(11/Invalid), `.Note`(11/Muted/Italic)

### Form rows — **new**
`Grid.FieldRow` — pair the label column with `Size.FieldLabelColumn`

### Controls
Existing: `Button.Primary/.Secondary` (+`.Large`/`.Small`), `.Dialogs.Primary/.Secondary`
(+`.Circle`), `.TabClose`, `.CircleLight/.CircleDark`; `TextBox.*`; `ComboBox.*`;
`CheckBox.Normal/.Small`; `RadioButton.Form(.Latin)`; `NumericUpDown.Normal/.Small`;
`ToggleSwitch.Normal/.Small`; `ToggleButton.CircleLight/.CircleDark`; `TabControl.*`;
`TabItem(.Form)`; `DataGrid(+.ReadOnly/.ColumnHeader/.Cell/.Row)`; `Expander.Section`;
`ScrollBar(.Slim)`; `Pill` (+`.Valid`/`.Invalid`/`.Warning`/`.Accent`) and `Pill.Text.*`
**New:** `Button.IconSquare`(32²); `PasswordBox(.Normal/.Large)`; `Slider.Normal/.Small`;
`ColorPicker.Normal/.Small`; `DatePicker.Normal/.Small`; `ProgressRing.Inline`; `Label.Form`;
`ListBox.Plain`; `ListBoxItem.Row`; `Separator.Horizontal`;
`GridSplitter.Vertical/.Horizontal`; `Pill.Small` + `Pill.Text.Small`

### Icons — `Controls.Path.xaml` + **new** `Controls.Misc.xaml`
`PackIconMaterial.ButtonContent`(+`.Small`), `.ChevronPrevious/.ChevronNext/.ChevronDouble*`,
`PackIconPhosphorIcons.CircleLightButtonContent`, `Path.ButtonContent`
**New:** `PackIconMaterial.Indicator`(10²), `PackIconMaterial.FieldAdornment`(24²/Muted),
`PackIconPhosphorIcons.FieldAdornment`

### Colour
Theme-swapped at runtime — `Status.Light.xaml` / `Status.Dark.xaml`:
`Brushes.Valid`, `.Valid.Fill`, `.Invalid`, `.Invalid.Fill`, `.Warning`, `.Warning.Fill`,
`.Muted`, `.Muted.Fill`
MahApps: `Accent`, `Accent2/3/4`, `Highlight`, `IdealForeground`, `Text`, `ThemeBackground`,
`ThemeForeground`, `Gray1`…`Gray10`
**New, deliberately NOT theme-swapped** — `Brushes.OnMap.xaml`:
`Brushes.OnMap.Surface`, `.Text`, `.Border`, `.Halo`, `.HandleFill`

### Effects — **new** `Common.Effects.xaml`
`Effects.Elevation1` (resting), `.Elevation2` (floating panel), `.Elevation3` (overlay)

---

## 6. Must-not-change register

**Read this before any bulk edit.** Each item looks like a violation and is not.

- **`Views/Dialogs/PrintToPdfDialogView.xaml:141-238`** **[verified]** — the Black/White colours
  and `FontSize="7"/"8"` are a **print preview of a paper page**. Paper is white and ink is
  black regardless of UI theme.
- **`Views/Dialogs/EmailSignUpDialogView.xaml:83-114`** **[verified]** —
  `#FFFFC107 / #FFFF3D00 / #FF4CAF50 / #FF1976D2` are the **Google "G" logo**; `#4285F4` is
  Google brand blue. Brand assets must not be themed.
  (The `Foreground="White"` overrides on lines 56/58 and the line 67/130 colours *are* real
  violations — the logo paths are not.)
- **SLD symbolizer colour bindings** — `SelectedColor="{Binding FillColor}"`,
  `StrokeColor`, `HaloColor`, raster colour-map entries. These are the **symbol's own
  appearance being edited by the user**. Data, not chrome. **[verified]**
- **`Views/Symbology/Sld/TextSymbolizerView.xaml:42,52,56,67`** **[audited]** — binds
  `FontFamily`, `FontSize`, `FontStyle`, `FontWeight` to **SLD model properties**
  (`Sld_FontStyle`, `Sld_FontWeight`). A blind "strip inline font attributes" sweep corrupts
  this file. **This is the single most dangerous trap in the codebase.**
- **`Views/MapMarkers/*` (13 files)** — caller-supplied cartography. Their label sizes are
  already disciplined (11 for labels, 10 for counters, 20 for pin glyphs) and their hover
  shadow is byte-identical across all 11 that have one. **[verified]** Leave alone.
- **Over-map surfaces** — `Scalebar.xaml:22`, `SketchBarView.xaml:58`,
  `CoordinatePanelView.xaml:96/98`, `MapExtentPanelView.xaml:100/189`, `ActiveExtentView.xaml`
  handles, MapOptions' `Stroke="White"` dots. **[verified]** These need contrast against
  arbitrary basemap imagery, not app chrome. They are *not* defects — but they had no token
  expressing that intent, which is exactly why `Brushes.OnMap.*` was added in Step 1. Convert
  them to those tokens; do **not** convert them to theme brushes.

  **CORRECTION (2026-08-16, prompted by user report):** an earlier revision of this list also
  named `MapLegendView.xaml:221/314` and `MapLegendItemView.xaml:759` as over-map surfaces.
  **That was wrong.** The legend is a *docked side panel* — Saba hosts it in `MainWindow.xaml`
  inside a `DockPanel`/`Expander` next to a `GridSplitter`, never over the canvas — and those
  three are `Popup` flyouts anchored to toggle buttons inside it. They are ordinary app chrome,
  their `Background="White"` is a genuine dark-theme bug, and `IRI.Maptor.Styles.Border.Popup`
  is exactly the token for them. See §4.4a.

---

## 7. Dead code register

- **`Views/General/DottedBusyIndicatorView - Copy.xaml`** + its `.xaml.cs` **[verified]** —
  declares class `DottedBusyIndicatorView2`, referenced **nowhere** in the repo, hard-codes
  English `Text="LOADING..."` in a Persian RTL app. An abandoned alternative animation. Delete.
- ~~**`myErrorTemplate`** in `SecurityInputStyles.xaml:89-130` — zero references.~~ **[verified,
  DELETED in Step 4]** Re-confirmed at zero references before removal.
- **`FlashErrorIcon`** in `Assets/IRI.Maptor.Animations.xaml:7` **[verified]** — became unreferenced
  in the library when `myErrorTemplate` went, and the only other use in the repo is a *private*
  copy inside `IRI.App.EsiDb/View/NewPersonView.xaml` (an app that does not reference this
  library at all). **Deliberately NOT deleted**: `IRI.Maptor.Animations.xaml` is merged by apps
  that currently cannot be built (see §4.4c), so removal could not be verified.
- ~~**`FeatureTabControl.xaml:14-20`** — `FeatureTableTemplate` never referenced; lines 67-74
  re-declare it inline.~~ **[verified, DELETED in Step 5c]** Re-confirmed: the only occurrence of
  the key in the repo was its own declaration, and `TabControl.ContentTemplate` does carry a
  byte-equivalent inline copy.
- ~~**`SelectedItem.xaml:28-80`** — 53 commented-out lines duplicating the live block.~~
  **[verified, DELETED in Step 5c]** 135 → 82 lines.
- ~~**`DegreeMinuteSecondView.xaml:46-84`** — 39 commented-out lines carrying stale magic numbers.~~
  **[verified, DELETED in Step 5c]** 86 → 47 lines.
- ~~**`Views/Controls/InlineInput.xaml`** — vendor demo code.~~ **[verified, DELETED in Step 5c]**
  One correction to the original audit: its `TextBox.Static.Border` / `.MouseOver.Border` /
  `.Focus.Border` brushes did **not** shadow the WPF system keys globally — they were declared
  inside the control's own `UserControl.Resources`, so their scope was that control alone, which
  was itself unreferenced. `IRI.App.NaghsheYar/View/SettingsView.xaml` declares its own separate
  local copies of the same three keys and is unaffected.

---

## 8. Verification recipe (important — reuse this)

**Building is not sufficient.** XAML `StaticResource` resolves at runtime, so a missing or
misspelled key compiles cleanly and then crashes every consuming app at startup.

Two cheaper checks that both produced **false negatives** in this session, so don't trust them:

- `dotnet build` — passes regardless of unresolvable `StaticResource`.
- A PowerShell script that assigns `ResourceDictionary.Source` and reads `.Keys` — the load is
  deferred, so the dictionary reports zero keys and every lookup returns null even when the
  styles are fine. This falsely reported *all* keys missing, including long-standing ones.

What actually works: a throwaway WPF exe that merges the dictionary in a real `Application`
context, resolves each key, and **applies each style to a live control followed by a
`Measure()` pass** — applying is what forces every setter, `BasedOn` chain and nested
`StaticResource` to evaluate.

The probe used this session lives in the session scratchpad
(`scratchpad/StyleProbe/`, `Probe.cs` + `StyleProbe.csproj`); it is ~150 lines, references
`IRI.Maptor.Jab.Wpf.csproj`, and prints `RESULT: PASS` / `RESULT: FAIL (n)`. Recreate it for
any future step that touches the dictionaries.

Also note: a **library** project's `bin` does not contain `MahApps.Metro.dll`, so probe against
an app's output folder (or use a `ProjectReference`, which is what the probe does).

---

## 9. Step 1 record — DONE

Purely additive: **54 new keys**, no existing key altered, so no current screen changes
appearance.

Files added under `Assets/Styles/`:

| File | Contents |
|---|---|
| `Controls.Section.xaml` | `Border.Section/.Divider/.Banner(+3)/.Popup`; `TextBlock.PanelTitle/.CardTitle/.FieldLabel/.FieldValue/.FieldHint/.Error/.Note`; `Grid.FieldRow` |
| `Controls.PasswordBox.xaml` | `PasswordBox(.Normal/.Large)` mirroring the TextBox styles **including** the `IsPersian` trigger; eye-glyph reveal button replacing the red square |
| `Controls.Inputs.Extra.xaml` | `Slider`, `ColorPicker`, `DatePicker`, `ProgressRing.Inline`, `Label.Form` |
| `Controls.Misc.xaml` | `ListBox.Plain`, `ListBoxItem.Row`, `Separator.Horizontal`, `GridSplitter.*`, `PackIcon*.Indicator/.FieldAdornment` |
| `Common.Effects.xaml` | `Effects.Elevation1/2/3` |
| `Brushes.OnMap.xaml` | `Brushes.OnMap.*` |

Extended in place: `Common.Metrics.xaml`, `Controls.Pill.xaml` (`Pill.Small` +
`Pill.Text.Small`), `Controls.Button.xaml` (`Button.IconSquare`), `Controls.All.xaml` (merge
order).

**Three deliberate deviations from the original plan:**

1. **`Border.Card` was NOT overwritten.** The library's `Border.Card` is a *field-row
   container* (MinHeight 39, padding 4,0) used across the open/export dialogs; Saba's `Card` is
   a *content card* (padding 12,9, bordered, bottom margin). Same name, genuinely different
   concept. The promoted style is `Border.Section`; both now coexist. The probe explicitly
   asserts they are distinct objects (9 setters vs 6).
2. **The six app-shell `Slider` declarations were left in place.** Removing them before any
   view references `IRI.Maptor.Styles.Slider.Normal` would regress every slider to unstyled.
   They come out in Step 6.
3. **`Expander.Section` needed no work** — the library version is already Saba's
   `SectionExpander` with small improvements (`Focusable`, a body top-border, `TemplateBinding`
   for `Padding`). Saba's copy should be re-pointed at it in Step 5.

Deferred out of Step 1: the canonical `Validation.ErrorTemplate`, moved to Step 4 where the
Security views that need it are rewritten.

**Verification**: probe result `RESULT: PASS` — 54/54 new keys, 12/12 pre-existing keys,
`Border.Card` ≠ `Border.Section` confirmed, all styles applied to live controls with no
exception. `IRI.Maptor.Jab.Wpf` and `IRI.App.MakanNegarSaba` both build with **0 errors**.

Files touched (all uncommitted):

```
 M src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Common.Metrics.xaml
 M src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Controls.All.xaml
 M src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Controls.Button.xaml
 M src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Controls.Pill.xaml
?? src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Brushes.OnMap.xaml
?? src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Common.Effects.xaml
?? src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Controls.Inputs.Extra.xaml
?? src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Controls.Misc.xaml
?? src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Controls.PasswordBox.xaml
?? src/IRI.Maptor.Jab/IRI.Maptor.Jab.Wpf/Assets/Styles/Controls.Section.xaml
```

---

## 9b. Step 2 record — DONE

All thirteen bugs in §4.1 fixed. This is the first step that changes how things look.

**New code**

- `Helpers/StatusBrushes.cs` — resolves the semantic status palette from C#. Converters hand a
  `Brush` back to a binding and so cannot use `DynamicResource`; this looks the brush up by key
  with a light-palette fallback for the designer and tests.
  *Known limitation, documented in the file*: the brush is resolved when the binding runs, so a
  theme change after that does not repaint already-realised rows — they update when the binding
  next re-evaluates. Still a large improvement on a literal, which was wrong in dark theme
  permanently.
- `Assets/Converters/StyleConverters/EditorFacingStatusToPillStyleConverter.cs` — plus its
  `...ToPillTextStyleConverter` sibling. Committed → `Pill.Valid`, Rejected → `Pill.Invalid`,
  PendingReview/UnderReview → `Pill.Warning`, InCompetition → `Pill.Accent`, Withdrawn → plain
  `Pill`. Both share one `Lookup` so the border and its caption can never disagree. Registered
  in `IRI.Maptor.Converters.xaml`.

**Changed**

| File | Fix |
|---|---|
| `FeatureStatusToBrushConverter.cs` | literals → `StatusBrushes.*`; `Unchanged` now theme foreground, not `Brushes.Black` |
| `FeatureStatusToBackgroundBrushConverter.cs` | `ModernUIColors` tints → matching `.Fill` brushes, so fg/bg share one semantic colour |
| `MyPendingView.xaml` | status pill bound through the new converters |
| `SecurityInputStyles.xaml` | red-square reveal button → eye glyph; `borderGroup` white → `ThemeBackground`; fixed `Height` → `MinHeight`; merged `Common.Metrics` for the corner-radius token |
| `GoToView.xaml` | `White` → `IdealForeground` on icon and title; subtitle given a foreground at last; hardcoded `Segoe UI` dropped for the base TextBlock style's Persian trigger |
| `LayerSettings_GeneralView.xaml` | new keyed `ModeGlyph` style mirroring the ellipse's own trigger, so the glyph swaps with its background instead of being flat white; `Stroke="Gray"` → `Gray6` |
| `SldEditorView.xaml` | both `LightGray` splitters → `GridSplitter.Vertical/.Horizontal` |
| `TextSymbolizerView.xaml` | removed the nested `ScrollViewer` |
| `SelectedItem.xaml` | `LightRedColor` → `lightRedColor`, restoring the dead mouse-leave animation |
| `ForgetPasswordView.xaml` | `Grid.Row="1"` → `"0"` against its single row definition |
| `EmailSignUpDialogView.xaml` | "or" divider caption masks correctly in both themes; `Black` → theme foreground; removed both inline white button foregrounds (#13) |
| `ChangePasswordDialogView.xaml` | `Black` → theme foreground |
| `ImageViewer.xaml` | `Black` border → `Gray8`; `#FFFAFFFF` canvas → `ThemeBackground` |

**Verification** — the §8 probe, extended to construct each converter and assert behaviour
rather than just resolution: every `FeatureStatus` maps to the expected `IRI.Maptor.Brushes.*`
instance by reference, foreground and background agree, and Rejected/Committed/Withdrawn/
InCompetition/PendingReview all produce **distinct** pill styles. `RESULT: PASS`.
`IRI.Maptor.Jab.Wpf` and `IRI.App.MakanNegarSaba` both build with **0 errors** and no new
warnings (one `CS8603` introduced by the new converter was fixed by returning
`DependencyProperty.UnsetValue` instead of null).

**Not verified by me**: whether these screens *look* right. Worth a visual pass in both themes,
especially the Security fields and the layer-settings mode picker.

---

## 9c. Step 3 record — DONE

All nine SLD views rewritten against the Step-1 vocabulary. This was the user's original
complaint.

**Two latent bugs found — and they were the most valuable part of the step**

Applying `IRI.Maptor.Styles.ComboBox.Normal` for the first time made the SLD views fail to load
at all. Root cause, in both `Controls.ComboBox.xaml` and `Controls.RadioButton.xaml`:

> A `DataTrigger` binds `{StaticResource Localization}`, but the dictionary neither declares an
> `ObjectDataProvider x:Key="Localization"` nor merges one. **A `StaticResource` resolves only
> within its own dictionary and that dictionary's merges — never across sibling dictionaries in
> the same `MergedDictionaries` list.** So the style entry failed to load and the key was
> silently absent at runtime.

Every other dictionary using that trigger (TextBox, TextBlock, Button, CheckBox, TabItem,
ToggleSwitch, MetroWindow) declares its own copy; these two were the odd ones out.
Consequences before the fix:

- `IRI.Maptor.Styles.ComboBox.Normal` was unusable. Nothing referenced it, so nobody noticed.
- `IRI.Maptor.Styles.RadioButton.Form` was unusable **and referenced** — three times in
  `LayerSettings_ExportView.xaml`, which had therefore been rendering unstyled radio buttons.

Both fixed by adding the provider. `Controls.NumericUpDown.xaml` has the same trigger but it is
commented out; `Controls.DataGrid.xaml` and `SecurityInputStyles.xaml` inherit a provider through
their merges. Those three are fine — verified, not assumed.

**Views rewritten**

| File | Change |
|---|---|
| `PointSymbolizerView.xaml` | the reference row pattern: shared label-column token, `TextBlock.FieldLabel`, every editor on its keyed style |
| `LineSymbolizerView.xaml`, `PolygonSymbolizerView.xaml` | same pattern |
| `RasterSymbolizerView.xaml` | + `TextBlock.SectionHeader` for the colour-map heading, `Button.IconSquare` command bar, `IRI.Maptor.Styles.DataGrid`, `ColorPicker.Small` in cells, `TextBlock.Note` footnote |
| `TextSymbolizerView.xaml` | same pattern; the eight repeated `IsEnabled="{Binding EnableHalo}"` hoisted onto one container whose inner grid reuses the same label-column token so alignment holds |
| `ScaleRangeEditorView.xaml` | label column 100 → shared token |
| `SimpleFilterEditorView.xaml` | label column 90 → shared token; inner `GroupBox` → `TextBlock.SectionHeader` (not another card — it is hosted inside the editor's Filter card, and a card in a card reads as a box in a box) |
| `SldEditorView.xaml` | six `GroupBox` → `Border.Section` + `TextBlock.CardTitle`; twelve 32×32 buttons → `Button.IconSquare`; both `ListBox` → `ListBox.Plain`; toolbar separator → `Separator.Vertical`; splitter columns/rows → `Auto` so the splitter styles carry their own grab size; corrected the false comment at the top of the file |
| `SldEditorWindow.xaml` | stock `ToolBar` → `Border.Toolbar` + styled buttons. `ToolBar` coerces its children into its own chrome, which is why the Maptor button styles never reached these three buttons |

**One trap worth remembering**: `IRI.Maptor.Styles.TextBox.Normal` sets `Height="32"`, so it
cannot be used as-is on a multi-line or fill box. The abstract fields override `Height="50"` and
the XML preview overrides `Height="Auto"`, both with `VerticalContentAlignment="Top"`.

**Added in passing**: `IRI.Maptor.Styles.Separator.Vertical` (Step 1 only shipped the horizontal
one) and `IRI.Maptor.Styles.GridLength.FieldLabelColumn`, because `ColumnDefinition.Width` takes a
`GridLength` and will not accept the `Double` token.

**Verification** — the probe was extended to *construct* each view, which is what catches a
missing style key; parsing the XAML resolves every `StaticResource`, and `Measure`/`Arrange`
then forces the styles to apply. It also reads the label-column width back off each constructed
visual tree. Result: 88 keys resolve, all 10 views construct, **all six label columns aligned at
120** (they were 90/100/120 before). `RESULT: PASS`. Both projects build with **0 errors**.

Note the ordering lesson: the first probe run reported `Localization` missing for one view and
`ComboBox.Normal` for another — the same view giving different answers. That was deferred
dictionary realization racing the parse. Forcing every merged dictionary to realize before
constructing anything made the result stable and exposed the real defect. The probe now does
this first.

**Not verified by me**: whether the editor *looks* right. Open it in both themes.

---

## 9d. Step 4 record — DONE (2026-08-17)

**Scope was deliberately narrowed by the user**: "I'm happy with the style and theme of
security/auth views. Apply step 4 but keep in mind not to change them, you may refactor, move
shared styles to better place." So Step 4 became a structural refactor, and the two parts of the
original plan that would have restyled those screens were **dropped, not deferred-by-oversight**:

- ~~Bring the 7 Security views and 2 auth dialogs onto the majority dialog shell~~ — would change
  the layout of screens the user has signed off.
- ~~Add a canonical `Validation.ErrorTemplate` and one error mechanism~~ — would change how errors
  render on those same screens.

If either is ever wanted, it is a fresh decision, not leftover Step 4 work.

**What `SecurityInputStyles.xaml` actually was.** 13 views merged it: the 7 Security views plus 4
Dialogs and 2 LayerSettings views that had nothing to do with security. The non-security six
merged the whole file to reach exactly two icon styles.

| old key | outcome | new home |
|---|---|---|
| `securityIconPhosphor` | **deleted**, callers repointed | `IRI.Maptor.Styles.PackIconPhosphorIcons.FieldAdornment` (Controls.Misc) |
| `securityIconMaterial` | **deleted**, callers repointed | `IRI.Maptor.Styles.PackIconMaterial.FieldAdornment` (Controls.Misc) |
| `securityIconPhosphorConfirmRow` | kept, renamed, rebased | `IRI.Maptor.Styles.Security.IconConfirmRow` |
| `securityPasswordBoxBase` | kept verbatim, renamed | `IRI.Maptor.Styles.Security.PasswordBox` |
| `borderGroup` | kept verbatim, renamed | `IRI.Maptor.Styles.Security.FieldGroup` |
| `securityPathText` | kept verbatim, renamed | `IRI.Maptor.Styles.Security.PathText` |
| `myErrorTemplate` | **deleted** — zero references anywhere in the repo | — |

`SecurityInputStyles.xaml` is deleted. The remainder lives in **`Controls.SecurityInputs.xaml`**,
which is deliberately **not** merged into `Controls.All.xaml`: `FieldGroup` binds to an ancestor
named `root`, so it is only usable by views that expose those properties. The six non-security
views now merge nothing extra at all — they already merged `Controls.All.xaml`, which carries the
`FieldAdornment` keys.

**The one accepted visual change.** The icon styles were *not* equivalent to the Step 1 tokens:

```
securityIcon*            FieldAdornment
  Width      24            24        same
  Height     (unset)       24        CHANGED
  Foreground "Gray"        Muted     CHANGED  light #5F6368 / dark #9AA0A6
  Margin     4             4         same
```

The user chose to adopt `FieldAdornment` rather than move the old styles verbatim, accepting a
small change so the adornment icons finally follow the theme instead of sitting at a fixed
`#808080` in both. This is the only rendering difference in the whole step.

**Saba's `ServerSettingsView` carried a stale private copy of `borderGroup`** — still on
`Background="White"` and fixed `Height="39"`, both of which had been fixed in the library version
during Step 2. It now uses `IRI.Maptor.Styles.Security.FieldGroup`, which fixes the white slab in
dark mode and the height clipping. Its border still does not paint, because the shared style binds
to an element named `root` that this view does not have — that was equally true of the private
copy, so it is not a regression, but it is worth knowing the two `Binding` setters are inert here.

**One trap avoided.** `securityPasswordBoxBase` uses `{StaticResource Localization}`, and the old
file never declared that provider — it happened to reach one through its merge of
`Controls.TextBox.xaml`. `Controls.SecurityInputs.xaml` declares its own, per the Step 3 rule.
`IRI.Maptor.Styles.Security.PasswordBox` is also deliberately **not** based on
`IRI.Maptor.Styles.PasswordBoxBase`: that one carries the `IsPersian` font trigger, which would
have changed the typeface on the auth screens.

**`FlashErrorIcon`** in `IRI.Maptor.Animations.xaml` is now unreferenced in the library (it existed
only for `myErrorTemplate`). Left in place rather than deleted, because apps that cannot currently
be built may merge that dictionary. Recorded in §7.

**Verified.** 58 new keys + 34 pre-existing resolve; all 13 affected views construct under a real
application context; `IRI.Maptor.Jab.Wpf` and `IRI.App.MakanNegarSaba` build with 0 errors. A
renamed key that fails to resolve throws only when the view is realised, never at build time,
which is why the probe covers all 13 rather than relying on the compiler.

---

## 9e. Step 5a record — DONE (2026-08-17)

Step 5 was split into **5a (library dictionaries)**, 5b (re-point Saba's duplicates) and 5c (dead
code). This is 5a.

**The survey contradicted the plan, twice.** Recording both, because the plan in §10 was written
from the audit and the audit was wrong on these points:

1. §4.2 called for "namespacing" `IRI.Maptor.Colors.xaml`'s bare colour names. Measured: all 23
   (`red`, `green`, … `sienna`) had **zero** references — no `StaticResource`, no
   `DynamicResource`, no string lookup from C#. They were deleted, not renamed.
2. `FeatureTableColumnHeaderTemplate` looked dead by XAML grep. It is **not**: it is fetched by
   literal string from `DataGridDictionaryBehavior.cs:202`.

> **Rule that came out of this: grep XAML *and* C# before calling a resource key dead.** A
> `TryFindResource("literal")` is invisible to an XAML search, and renaming past it fails
> **silently** — the template is simply never applied, with no build error and no exception.
> The full set of string-referenced keys in this repo is: `FeatureTableColumnHeaderTemplate`
> (now renamed), `MahApps.Brushes.Highlight`, `bTitr`, `bYekan`, `csv`, `dxf`, `json`, `shp`,
> `expandableStyle`, `landCollectionViewSource`, `landPartsSource`, `mapMarkerResetOnMouseLeave`,
> `zamini1`, plus the `IRI.Maptor.Brushes.*` status keys used by `Helpers/StatusBrushes.cs`.

### Renames

| old | new |
|---|---|
| `mapOptionsButton` | `IRI.Maptor.Styles.Button.MapOption` |
| `mapOptionsPath` | `IRI.Maptor.Styles.Path.MapOption` |
| `menuIcon` | `IRI.Maptor.Styles.PackIconMaterial.Menu` |
| `menuPath` | `IRI.Maptor.Styles.Path.Menu` |
| `menuBorder` | `IRI.Maptor.Styles.Border.Menu` |
| `linkedInBrush` / `stackoverflowBrush` / `githubBrush` / `makanNegarBrush` | `IRI.Maptor.Brushes.Brand.LinkedIn` / `.StackOverflow` / `.GitHub` / `.MakanNegar` |
| `FeatureTableFilter.FlatToggleTemplate` | `IRI.Maptor.Templates.ToggleButton.FeatureTableFilter` |
| `FeatureTableColumnHeaderTemplate` | `IRI.Maptor.Templates.FeatureTable.ColumnHeader` (+ the C# literal) |

**Deliberately not renamed:** `numericFilterOperatorToSymbolConverter` already matches the
established converter convention (camelCase, as in `IRI.Maptor.Converters.xaml`) — the audit
counted it as inconsistent, but it is not. `NumericFilterOperatorValues` is an `x:Array` of enum
values, i.e. data rather than a style, and no convention exists for that; inventing one was not
worth the churn. `mapTehranIr*` (6 keys) stay because **`IRI.App.SanadNegar` resolves
`mapTehranIrLightRedColor` from this dictionary at runtime**.

Also left alone: `menuButton`, `menuTextBlock`, `menuLabel`, `menuRectangle` are **file-private**
resources declared inside `MapLegendView.xaml`, not shared dictionary entries. View-local styles
are legitimate and out of scope.

### The one visual change: map markers

`PointMarker`, `LocationMarker` and `CountableImageMarker` had the Tehran municipal red
(`#FFAD0000`) hardcoded as their fill, in a *generic* library, with **no property to override it** —
`LocationMarker`'s only DP is `Value`, a string. On the user's instruction they now use
`{DynamicResource MahApps.Brushes.Accent}`, so markers follow each app's accent.

**A concern was raised before this was applied and the user accepted it**: markers sit on map
imagery, whose brightness is unrelated to the app theme, and `Brushes.OnMap.xaml`'s own header
states the rule ("if the element has to survive whatever imagery is underneath it, it belongs
here"). Two further specifics to watch when reviewing this on screen:

- `MahApps.Brushes.Accent` is **`#CCF0A30A`, i.e. 80% alpha**. The previous red was fully opaque,
  so markers are now slightly translucent over the basemap.
- `PointMarker` and `CountableImageMarker` have **no `Stroke`** to separate them from imagery;
  only `LocationMarker` has `Stroke="White"`.

If amber-on-imagery reads badly, the fallback is a non-theme-swapped
`IRI.Maptor.Brushes.OnMap.Marker` token, which was the alternative offered.

### Verified

Probe extended with a **negative** check (34 old keys must no longer resolve) and an explicit
lookup of the code-coupled key through the same literal the behaviour uses:

```
DELETED: 34 checked, 0 still present
OK code-coupled key resolves: IRI.Maptor.Templates.FeatureTable.ColumnHeader
NEW: 71 checked, 0 missing
PRE-EXISTING: 34 checked, 0 missing
```

All 4 `MapOptions` views, all 3 markers and `MapLegendView` construct. `IRI.Maptor.Jab.Wpf` and
`IRI.App.MakanNegarSaba` build with 0 errors.

**Unverifiable:** `IRI.App.NaghsheYar/View/AboutMe.xaml` had 4 brand-brush references renamed, and
that app cannot be built (`CS2001`). The edit was confirmed by grep only.

---

## 9f. Flag images were corrupted by line-ending normalisation — FIXED (2026-08-17)

Not part of Step 5. Reported by the user: `LanguageSelectorView` showed no country flags.

**Diagnosis.** `Image.Source` binds to `LanguageItem.FlagUri`, a `pack://` URI. WPF swallows a
failed image load silently, so the symptom is a blank cell and nothing else — no exception, no
binding error. Loading each URI directly in the probe gave:

```
NotSupportedException: No imaging component suitable to complete this operation was found.
```

That is WPF saying *the stream was found but could not be decoded* — a **missing** resource raises
`IOException` instead. So the plumbing was fine (confirmed separately: the assembly's
`.g.resources` contained all 15 entries under `flags/`). The bytes were wrong:

```
expected  89 50 4e 47 0d 0a 1a 0a
actual    89 50 4e 47 0a 1a 0a 00
```

Every `0d` was stripped. The `.gitattributes` rule `* text=auto eol=crlf` had normalised the PNGs
as if they were text when they were first added. `*.png binary` is in that file now, but it only
prevents recurrence.

**Not recoverable, and worth understanding why.** The transform collapsed every `0d 0a` into `0a`,
which is lossy: you cannot know which surviving `0a` bytes originally had a `0d` in front. Blindly
re-expanding all of them corrupts differently — the signature alone demonstrates it, since
`89 50 4e 47 0d 0a 1a 0a` would come back as `89 50 4e 47 0d 0a 1a 0d 0a`. Checked and ruled out:
every revision in `git log --follow`, the original `4d1aab99f feat: localization` commit
(2026-02-21) which already stored the damaged bytes, and every built `IRI.Maptor.Jab.Wpf.dll` in
the tree (all built after that date, so all embed the damage).

**Fix.** All 15 re-fetched from `flagcdn.com` at `w80`, on the user's instruction after being shown
the licensing position (free for commercial use, no attribution; flag designs are public domain).
Each download's PNG signature was validated *before* it was allowed to overwrite the existing
file, so a bad response could not replace a file with something worse.

Verified: repo-wide scan of 248 PNGs reports **0 corrupt**; all 15 now decode from the embedded
resources through the real `FlagUri` path; `git ls-files --eol` reports `-text` for them.

**Aspect ratios changed.** The lost originals were all 32×24 (4:3), `cn` 40×27. The replacements
carry true flag ratios: 80×53 for most (3:2), 80×42 for `us`, 80×40 for `am`/`az`. The view uses
`Stretch="Uniform"` in a 24×18 box, so nothing is distorted, but rendered heights now differ per
flag and the images are `VerticalAlignment="Top"`. If that reads as ragged, centring them is the
tidier fix. **Left unchanged pending a look.**

> **Rule: a binary asset that renders blank is a bytes problem until proven otherwise.** Check the
> file signature before investigating build actions, pack URIs or bindings. The `NotSupportedException`
> vs `IOException` distinction tells you which half to look in.

**User change recorded so it is not reverted:** the centre-dot `Ellipse` in all four `MapOptions`
views moved from `MahApps.Brushes.Accent` to `MahApps.Brushes.Highlight`. Relevant to §9e: `Accent`
is `#CCF0A30A`, i.e. **80% alpha**, while `Highlight` is opaque. The map markers re-pointed in
Step 5a are on `Accent` and may want the same treatment for the same reason.

---

## 9g. Step 5b record — DONE (2026-08-17)

Re-point Saba's duplicated styles at the promoted library keys. `Saba.Styles.xaml` goes from
**20 styles to 6**; only the 3 files that used it were touched (`AppSettingsDialog`,
`RoleManagementView`, `UserManagementView`).

**Every pair was compared setter-by-setter before anything moved**, because Step 4 established that
"looks like a duplicate" is not the same as "is a duplicate". The split was 7 / 7.

### Byte-identical — re-pointed with zero visual change

| Saba style | library key | uses |
|---|---|---|
| `FieldLabel` | `TextBlock.FieldLabel` | 14 |
| `CardTitle` | `TextBlock.CardTitle` | 8 |
| `FieldValue` | `TextBlock.FieldValue` | 7 |
| `Card` | `Border.Section` | 6 |
| `PanelTitle` | `TextBlock.PanelTitle` | 5 |
| `FieldRow` | `Grid.FieldRow` | 2 |
| `FieldHint` | `TextBlock.FieldHint` | 2 |

`Card` matches because `CornerRadius.Control` is 6, and `FieldRow` because
`Thickness.FieldRowGap` is `0,0,0,6`. Note `Card` maps to **`Border.Section`, not `Border.Card`** —
see the §6 register.

### Divergent — re-pointed on the user's instruction, accepting the visual change

| style | difference |
|---|---|
| `PanelHeader` | background `Accent4` (pale tint) → **`Accent`** (strong amber band); radius `5,5,0,0`→`4,4,0,0`; padding `10,7`→`10,6` |
| `Panel` | `CornerRadius` 6 → 5 (`CornerRadius.Surface`) |
| `Pill` | library adds a default `Muted.Fill` background; margin `0,0,6,0` → `2,0` |
| `PillText` | library adds `Foreground=Muted` and `Margin=0` |
| `Pill.Small`, `PillText.Small` | inherit the two above |
| `SectionExpander` | adds `Focusable`; header `MinHeight` 40 → 38; body gains a 1px `Gray9` top rule; body padding switches to `{TemplateBinding Padding}` |

**The expander needed a compensating edit.** Saba's template hardcoded `Padding="12,4,12,10"` on the
body; the library template template-binds it, and an `Expander` with no `Padding` set resolves to
**0**, so the content would have sat flush against the border. `Padding="12,4,12,10"` was added to
all 3 call sites to preserve the current spacing. This is the kind of regression that builds
cleanly and looks broken only at runtime.

Two Saba divergences look deliberate and are now overridden, so they are worth re-checking on
screen: the expander carried a Persian comment explaining that `MinHeight=40` keeps all section
headers equal height when one contains a toggle switch (the library uses 38), and `PanelHeader`
used `Accent4` consistently across 5 usages rather than the library's full-strength `Accent`.

**`Saba.Styles.xaml` no longer merges `Controls.All.xaml`.** That merge existed only so the deleted
text styles could be `BasedOn IRI.Maptor.Styles.TextBlock`; none of the 6 survivors derive from a
library style, and `App.xaml` already merges `Controls.All.xaml` at application level, which is
where `DynamicResource` resolves from anyway.

### Verified

`dotnet build` cannot catch a broken `StaticResource`, and the library probe cannot see Saba, so a
second probe (`scratchpad/SabaProbe/`) mirrors Saba's `App.xaml` merge list and constructs the
three affected views:

```
REMOVED:  14 checked, 0 still present     <- old Saba keys really gone
KEPT:      6 checked, 0 missing           <- app-specific survivors intact
PROMOTED: 14 checked, 0 missing
OK constructed AppSettingsDialog / RoleManagementView / UserManagementView
```

The REMOVED check matters as much as the others: had the old keys survived, every old reference
would still have resolved and a half-finished re-point would have passed silently.

Both `IRI.Maptor.Jab.Wpf` and `IRI.App.MakanNegarSaba` build with 0 errors.

---

## 9h. Step 5c record — DONE (2026-08-17)

Delete the §7 dead-code register. **Every entry was re-verified before removal** rather than
trusted from the audit — four of the five were marked `[audited]`, not `[verified]`, and the 5a
lesson (a key can be reached by string from C#) means an XAML-only grep is not proof. Each grep
covered `*.xaml`, `*.cs` and `*.csproj`.

**Files deleted (4), on the user's confirmation:**

- `Views/General/DottedBusyIndicatorView - Copy.xaml` + `.xaml.cs` — class
  `DottedBusyIndicatorView2`, hardcoded English `"LOADING..."` in a Persian RTL app. Only
  occurrences of the name in the repo were its own two files.
- `Views/Controls/InlineInput.xaml` + `.xaml.cs` — vendor demo code (`CW-` prefixed keys, "Your
  label", "Another Instance"). Same: only self-references.

**Dead markup removed from live files (3):**

| file | removed | result |
|---|---|---|
| `FeatureTabControl.xaml` | unreferenced `FeatureTableTemplate` (7 lines) | `TabControl.ContentTemplate` already re-declares it inline |
| `SelectedItem.xaml` | commented-out `StackPanel` block | 135 → 82 lines |
| `DegreeMinuteSecondView.xaml` | commented-out `StackPanel` block | 86 → 47 lines |

**One audit claim was wrong and is corrected in §7.** `InlineInput`'s
`TextBox.Static.Border` / `.MouseOver.Border` / `.Focus.Border` brushes were described as
shadowing the WPF system keys. They did not: they sat in the control's own
`UserControl.Resources`, so their scope was that one (unreferenced) control.
`IRI.App.NaghsheYar/View/SettingsView.xaml` has its own separate local copies and is untouched.

**Deliberately still not deleted:** `FlashErrorIcon` in `IRI.Maptor.Animations.xaml`. It is
unreferenced in the library, but that dictionary is merged by apps that cannot currently be built,
so removal cannot be verified. See §7.

### Verified

Cutting line ranges out of XAML can leave a file well-formed but structurally wrong, and the build
only catches the first kind of error — so the three edited views were constructed, not just
compiled:

```
OK constructed SelectedItem / FeatureTabControl / DegreeMinuteSecondView
NEW: 71 checked, 0 missing     PRE-EXISTING: 34 checked, 0 missing
```

`SabaProbe` re-run as a regression check on 5b (`REMOVED 0 present / KEPT 0 missing /
PROMOTED 0 missing`, all 3 Saba views construct). Both projects build with 0 errors.

---

## 9i. Step 6 record — DONE (2026-08-18)

**Two of the five items in the original Step 6 description were wrong or empty**, found by survey
before any edit:

- *"`ApprovalQueueView` re-inlines a diverged copy of `ReviewQueueView`'s `BadgesTemplate`"* — no
  longer true. Both already use the shared `Pill.*` styles (Step 2 fixed the vocabulary), and they
  carry **different badge sets** (Review has a "Queued" badge Approval does not), so they are not
  duplicates at all. **Nothing to do.**
- *"remaining Dialogs polish"* — no concrete definition anywhere. **Dropped** rather than invented.

### Sliders — the register had this backwards

It said "remove the six app-shell `Slider` declarations once views reference the keyed style". In
fact there were **8** such declarations and they are **load-bearing, not duplication**: MahApps
publishes its slider look as a *keyed* style, so a bare `<Slider>` renders as the stock Windows
control unless an *implicit* style opts it in. **11 sliders in the repo have no `Style` attribute**
and depend on exactly that.

Fixed properly instead of deleted: the implicit style now lives once in
`Controls.Inputs.Extra.xaml`, so any app merging `Controls.All.xaml` gets it. Four now-redundant
copies were removed — `Saba/MainWindow`, `NaghsheYar/MainWindow`, `SanadNegar/Shell`, and
`LayerSettingsDialogView` (which merges `Controls.All` and contains no sliders of its own).

**Four shells deliberately keep their copy**: `AlborzNegar`, `NiocExpSpatialEditor`, `Shahab`,
`Bag.Geospatial` merge `Controls.All.xaml` only inside individual views, not in `App.xaml`, so the
library's implicit style never reaches their shell. Deleting theirs would regress them.

### SketchBarView — new `OnAccent.Text` token

Its 4 white labels sat on `Accent` / `Accent2` at **2.11:1**. Rather than hardcode a dark ink four
times, added `IRI.Maptor.Brushes.OnAccent.Text` (`#FF1A1A1A`) to `Brushes.OnMap.xaml`, which also
gives that dictionary its first real consumers. Roughly **9:1** now. The token is fixed rather than
themed on purpose, and the file says why: the accent is the same colour in both themes and is light
in both, so no themed foreground can sit on it.

### FullNavigationView — converted, and the risk was tested not assumed

It was a fixed dark widget (a `#868686`→black gradient with white vector glyphs). Now themed:
surface `ThemeBackground`, border `Gray8`, glyphs and slider `ThemeForeground`.

The flagged hazard was that the glyphs are `GeometryDrawing.Brush` values **inside a `DrawingBrush`
resource**, and Freezables can reject `DynamicResource` — silently, leaving the glyph unpainted.
Measured on the constructed control: **6 glyphs inspected, 0 unresolved**, all resolving to
`#FF000000` under the light theme. It works, but only because these resources are not frozen;
adding `PresentationOptions:Freeze` to that dictionary would silently break it. Noted in the file.

### MultiSelectItem — palette converted, with one documented WPF limit

`#999999`/`#CCCCCC` → `Gray3`/`Gray8`; `#34495E` → `MahApps.Colors.Highlight`. The `darkGray` pair
in `MultiSelectItem.xaml` was declared but referenced nowhere and simply went.

**`ColorAnimation.To` needs a `Color`, not a `Brush`, and a `Storyboard` is a Freezable that cannot
take `DynamicResource`.** So the hover animation uses `StaticResource` against a theme *Color*: it
follows the accent the app starts with but will not repaint on a runtime theme switch. That is a
framework limitation, recorded in the file so it is not mistaken for a miss. `MahApps.Colors.Accent`
was rejected for it because it carries alpha (`#CCF0A30A`) and would animate the background to
semi-transparent; `MahApps.Colors.Highlight` (`#FFB17807`) is opaque. `lightRedColor` stays literal:
it is the remove button's hover, where red is semantic, and the status palette defines brushes only,
with no `Color` key to point at.

### Verified

```
implicit Slider style present at app scope: True
bare Slider in LayerSettings_GeneralView has a Style: True   <- the 4 deletions did not regress it
6 glyphs inspected, 0 unresolved                             <- DynamicResource works in DrawingBrush
DELETED 34 / 0 present    NEW 71 / 0 missing    PRE-EXISTING 34 / 0 missing
SabaProbe: REMOVED 0 present, KEPT 0 missing, PROMOTED 0 missing, 3 views constructed
```

Both projects build with 0 errors.

---

## 9j. Step 7 record — DONE (2026-08-18)

**[`AUTHORING.md`](AUTHORING.md) created** — a reference aimed at someone writing a view, not at
someone auditing the project. It carries the setup snippet, the screen grammar with a worked field
row, all 190 keys grouped by what you are trying to do, the colour decision list, how to verify a
view, and the traps. All key names were dumped from the dictionaries rather than copied from §5,
which had gone stale.

The three documents now have distinct jobs, and each links to the others: **AUTHORING** = how to
write a view; **PROGRESS** = the itinerary; **README** = the audit and registers. §5 of this file
is marked superseded rather than deleted, since it records what Step 1 delivered.

**The build guard was declined**, and this was a decision rather than an omission. It would have
hit **47 literal colours in 8 files**, several of which are deliberate and already in the
must-not-change register (§6): the Google logo in `EmailSignUpDialogView`, the paper preview in
`PrintToPdfDialogView`, the marker palette in `TextboxMarker.xaml.cs`. A failing guard needs those
triaged and allowlisted first or it blocks everyone; a warning-only guard would add ~47 warnings on
top of the ~3340 the build already emits, where they would be invisible. The rule is stated in
`AUTHORING.md` §4 with its legitimate exceptions listed, and enforcement is left to review.

If a guard is ever wanted, the work is: triage the 47, allowlist the deliberate ones, then add an
MSBuild target. The list of exceptions in `AUTHORING.md` §4 is the starting point.

---

## 10. Remaining steps

**All seven steps are complete.** The original descriptions are kept below because several turned
out to be wrong in ways worth remembering — see the correction notes.

### ~~Step 5~~ — Consolidate the parallel dictionaries — DONE (§9e, §9g, §9h)
Fold `MapOptionStyles.xaml` and `MenuIconStyles.xaml` into the main convention; rename the
`FeatureTableFilters.xaml` keys; namespace `IRI.Maptor.Colors.xaml`'s bare names. Re-point
Saba's `SectionExpander`, `Card`, `Pill*`, `FieldRow`, `FieldLabel`, `FieldValue` at the
promoted library keys so Saba keeps only genuinely app-specific styles (`Avatar`, `UserRow`,
`RoleCard`, `RoleCheck`, `RoleBadge*`). Delete everything in §7.

> **Correction:** the bare colour names were **deleted, not namespaced** — all 23 had zero
> references. `numericFilterOperatorToSymbolConverter` was left alone; it already matched the
> converter convention. `FeatureTableColumnHeaderTemplate` was *not* dead: C# fetches it by string.

### ~~Step 6~~ — Remaining sweep — DONE (§9i)
Map chrome using the `OnMap` tokens; Versioning queue views (one shared badge template, one
status vocabulary — currently `ReviewQueueView` has a good `BadgesTemplate` that is
file-private, so `ApprovalQueueView` re-inlines a diverged copy); remaining Dialogs polish;
`MultiSelectItem`'s two private 120-line design systems. Remove the six app-shell `Slider`
declarations once views reference the keyed style.

> **Correction:** the badge-template claim was already false — both queue views use the shared
> `Pill.*` styles and carry different badge sets. "Dialogs polish" was never defined and was
> dropped. There were **8** `Slider` declarations, not six, and they are **load-bearing**, not
> duplication; the fix was to move the implicit style into the library, not to delete them.

### ~~Step 7~~ — Documentation and guard — DONE (§9j)
Fold §5 of this document into a proper design-system reference for view authors. Optionally add
a build check that fails on new literal hex under `Views/`.

> The reference is `AUTHORING.md`. The guard was declined with reasons recorded in §9j.

---

## 11. Baseline

`dotnet build IRI.Maptor.Jab.Wpf.csproj` → **0 errors**, ~3342 warnings, ~21s (clean).
`dotnet build IRI.App.MakanNegarSaba.csproj` → **0 errors**, ~480 warnings.
All warnings are pre-existing (`CA1416` platform-compatibility in C#, `CS8618` nullability in
Saba) and unrelated to XAML. After each step: **0 errors and no new warning categories.**

Working tree at audit time was clean apart from untracked `docs/features/sld-symbology-editing.md`.
