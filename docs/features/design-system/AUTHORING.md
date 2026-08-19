# Writing a view — design-system reference

For anyone building or editing a WPF view in `IRI.Maptor.Jab.Wpf` or in an app on top of it.

The other two documents in this folder are history: [`README.md`](README.md) is the audit and the
registers, [`PROGRESS.md`](PROGRESS.md) is what was done and what is left. **This file is the only
one you need to write a view.**

190 keys exist across 32 dictionaries. You do not need to know them all — you need §2, §3 and §6.

---

## 1. Setting up a view

Merge one dictionary. `Controls.All.xaml` pulls in everything below except the four specialised
dictionaries listed after it.

```xml
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Jab.Wpf;component/Assets/Styles/Controls.All.xaml"/>
            <ResourceDictionary Source="pack://application:,,,/IRI.Maptor.Jab.Wpf;component/Assets/IRI.Maptor.Converters.xaml"/>
        </ResourceDictionary.MergedDictionaries>
        <ObjectDataProvider x:Key="Localization" ObjectInstance="{x:Static localization:LocalizationManager.Instance}"/>
    </ResourceDictionary>
</UserControl.Resources>
```

**Declare the `Localization` provider in your own file** if you bind any localized string, and also
if you use any style with an `IsPersian` trigger. See §6.

Not in `Controls.All.xaml`, merge explicitly when needed:

| dictionary | when |
|---|---|
| `Controls.SecurityInputs.xaml` | login / signup / change-password screens |
| `MapOptionStyles.xaml` | the round map-option buttons |
| `MenuIconStyles.xaml` | legend menu glyphs |
| `FeatureTableFilters.xaml` | feature-table column filters |

---

## 2. The screen grammar

A screen nests in this order. Use it and your view will match every other view without any
decisions on your part.

```
Border.Panel                       outer shell
 ├─ Border.PanelHeader             accent title band
 │   └─ TextBlock.PanelTitle
 └─ Border.Section                 a titled group of rows
     ├─ TextBlock.CardTitle
     └─ Grid.FieldRow              one label + editor row
         ├─ TextBlock.FieldLabel   left column, width = GridLength.FieldLabelColumn
         └─ <the editor>           right column
```

A field row, complete:

```xml
<Grid Style="{StaticResource IRI.Maptor.Styles.Grid.FieldRow}">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="{StaticResource IRI.Maptor.Styles.GridLength.FieldLabelColumn}"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <TextBlock Grid.Column="0" Style="{StaticResource IRI.Maptor.Styles.TextBlock.FieldLabel}"
               Text="{Binding [some_key], Source={StaticResource Localization}}"/>
    <TextBox   Grid.Column="1" Style="{StaticResource IRI.Maptor.Styles.TextBox.Normal}"/>
</Grid>
```

Always use `GridLength.FieldLabelColumn` for the label column. Hardcoding 90 / 100 / 120 is what
made labels jump between tabs of the SLD editor.

**`Border.Card` is not the content card.** It is a field-row container with `MinHeight 39`, used by
the open/export dialogs. The content card is `Border.Section`.

---

## 3. Tokens

### Surfaces — `Controls.Border.xaml`, `Controls.Section.xaml`

`Border.Panel` · `Border.PanelHeader` · `Border.Section` · `Border.Card` · `Border.Toolbar` ·
`Border.DialogFooter` · `Border.Divider` · `Border.Popup` ·
`Border.Banner` (+`.Warning` `.Invalid` `.Accent`)

### Text — `Controls.TextBlock.xaml`, `Controls.Section.xaml`

`TextBlock` (base) · `.WindowTitle` · `.HeaderTitle` · `.SectionHeader` · `.Title` · `.Normal` ·
`.Normal.Bold` · `.Caption` · `.Hint` · `.EmptyState` · `.ButtonContent` (+`.Small`) ·
`.PanelTitle` · `.CardTitle` · `.FieldLabel` · `.FieldValue` · `.FieldHint` · `.Error` · `.Note`

### Inputs

| control | keys |
|---|---|
| TextBox | `TextBox`, `.Normal`, `.Large`, `.Normal.En`, `.Large.En`, `.Small.En` |
| ComboBox | `ComboBox.Normal`, `.Large`, `.Normal.Latin`, `.Small.Latin`, `.Large.Latin` |
| PasswordBox | `PasswordBox`, `.Normal`, `.Large` |
| CheckBox / RadioButton | `CheckBox.Normal`, `.Small`, `RadioButton.Form` |
| NumericUpDown | `NumericUpDown.Normal`, `.Small` |
| Slider / ColorPicker / DatePicker | `Slider.Normal`, `.Small`, `ColorPicker.Normal`, `.Small`, `DatePicker.Normal`, `.Small` |
| ToggleSwitch | `ToggleSwitch.Normal`, `.Small` |
| Label | `Label.Form` |

`.En` / `.Latin` variants exist because the base ones carry an `IsPersian` font trigger. Use them
for content that is always Latin (coordinates, SRS codes, URLs).

### Buttons — `Controls.Button.xaml`

`Button.Primary` / `.Secondary` (+`.Large` `.Small`) · `Button.Dialogs.Primary` / `.Secondary`
(+`.Circle`) · `Button.CircleLight` / `.CircleDark` · `Button.IconSquare` · `Button.TabClose`

Pair a button with its glyph style: `PackIconMaterial.ButtonContent`,
`PackIconMaterial.CircleLightButtonContent`, `Path.ButtonContent`.

### Status pills — `Controls.Pill.xaml`

`Pill` + `Pill.Text`, and the variants `.Valid` `.Invalid` `.Warning` `.Accent`, plus `.Small`.
**Pair the border and caption by suffix** — `Pill.Warning` with `Pill.Text.Warning`. A `Border`
cannot restyle its own child, which is why they are two styles.

### Metrics — `Common.Metrics.xaml`

`GridLength.FieldLabelColumn` · `Size.FieldLabelColumn` · `Thickness.ViewContent` ·
`Thickness.FieldRowGap` · `Thickness.FieldGap` · `Thickness.SectionGap` ·
`Thickness.DialogContent` · `Thickness.DialogFooter` · `Thickness.RowGap` ·
`CornerRadius.Control` (6) · `CornerRadius.Surface` (5) · `CornerRadius.TabTrack` ·
`Size.Icon.Field` (24) · `Size.Icon.Small` (16) · `Size.Icon.Indicator` (10) ·
`Size.Button.IconSquare` (32) · `Opacity.Disabled`

### Other

`ListBox.Plain` · `ListBoxItem.Row` · `Separator.Horizontal` / `.Vertical` ·
`GridSplitter.Vertical` / `.Horizontal` · `Expander.Section` · `DataGrid` (+`.ReadOnly`) ·
`TabControl` (+`.Form` `.Card` `.Centered` `.Scroll`) · `TabItem` (+`.Form`) ·
`MetroWindow.Localized` / `.Dialog` · `Effects.Elevation1` / `2` / `3` · `ScrollBar.Slim`

---

## 4. Colour — pick in this order

**Never write a literal colour in a view.** Work down this list and stop at the first match.

1. **Ordinary chrome** → a MahApps theme brush, with `DynamicResource`:
   `MahApps.Brushes.ThemeBackground` · `ThemeForeground` · `Gray3` (secondary text) ·
   `Gray8` (borders) · `Gray10` (subtle fill) · `Accent` · `Accent2/3/4` (tints) · `Highlight`.
2. **A state** (valid / invalid / warning / muted) → `IRI.Maptor.Brushes.Valid` etc., and the
   `.Fill` variant for the background behind it. These follow light/dark via `Status.Light/Dark.xaml`.
3. **Text sitting on an accent fill** → `IRI.Maptor.Brushes.OnAccent.Text`. Not a theme brush: the
   accent is the same colour in both themes and light in both, so no themed foreground can sit on
   it. White on accent measures **2.11:1**.
4. **Something drawn directly on map imagery with no panel behind it** →
   `IRI.Maptor.Brushes.OnMap.Surface` / `.Text` / `.Border` / `.Halo` / `.HandleFill`. Fixed on
   purpose: basemap brightness has nothing to do with the app theme.
5. **Still nothing fits** → add a token. Do not inline a hex.

`DynamicResource` for brushes, always. `StaticResource` resolves once and will not follow a runtime
theme switch.

**The legitimate literal-colour exceptions**, all in the must-not-change register: the Google logo
in `EmailSignUpDialogView`, the paper preview in `PrintToPdfDialogView`, the marker palette in
`TextboxMarker.xaml.cs`, `Brushes.OnMap.xaml` and `IRI.Maptor.Colors.xaml` themselves, and the
`FontFamily`/`FontSize` bindings in `TextSymbolizerView`, which are **SLD data, not styling**.

---

## 5. Verifying your view

**A build proving nothing is the single most important fact here.** XAML `StaticResource` resolves
at *runtime*: a missing or misspelled key compiles cleanly and throws when the view is first shown.

Construct the view in a real `Application` context and lay it out:

```csharp
var app = new Application();
// merge the same dictionaries App.xaml merges
var view = new MyView();
view.Measure(new Size(900, 900));
view.Arrange(new Rect(0, 0, 900, 900));
view.UpdateLayout();
```

`README.md` §8 describes the throwaway probe used throughout this work, and §9e/§9g show the two
checks worth copying: assert the **old** key no longer resolves after a rename, and resolve any key
that C# fetches by string literal through that same literal.

---

## 6. Traps that have already cost time

**A `StaticResource` resolves only within its own dictionary and that dictionary's merges** — never
across sibling dictionaries in one `MergedDictionaries` list. This silently disabled
`ComboBox.Normal` and `RadioButton.Form`: the style entry failed to load and the key was simply
absent at runtime. If you write a style with an `IsPersian` trigger, declare the `Localization`
provider **in that same file**.

**Element-tree resources beat `Application.Resources`.** A dictionary merged into a view's own
`Resources` is found first, so an app-level runtime swap can never override it. This is why the
status palette was permanently light until `Status.Light.xaml` was removed from `Controls.All.xaml`.

**A resource key can be fetched from C# by string literal** (`TryFindResource("...")`), which no
XAML search will find. Grep `*.cs` too before renaming or deleting a key — renaming past one fails
*silently*.

**Score contrast against the surface actually painted behind the element**, not the token you
assume is there. A `Transparent` control composites onto whatever its container's template paints.

**Composite translucent brushes before judging them.** `Accent2/3/4` are one colour at
`0x99`/`0x66`/`0x33` alpha; treating them as opaque gives meaningless numbers. `Accent` itself is
`#CC` alpha — if you need an opaque accent, use `Highlight`.

**`ColorAnimation.To` needs a `Color`, not a `Brush`, and a `Storyboard` cannot take
`DynamicResource`.** Use `StaticResource` against a theme *Color* (`MahApps.Colors.*`) and accept
that it will not repaint on a runtime theme switch.

**`DynamicResource` does work inside a `DrawingBrush`** — but only while the resource is unfrozen.
Adding `PresentationOptions:Freeze="True"` to such a dictionary breaks it silently.

**An implicit style (no `x:Key`) is load-bearing.** MahApps publishes most looks as *keyed* styles,
so a bare control falls back to the stock Windows look unless an implicit style opts it in. Do not
delete `<Style TargetType="{x:Type X}">` declarations as "duplication" without checking what has no
explicit style.

**XML comments cannot contain `--`.**

**Binary assets:** if an image renders blank, check the file signature before the build action or
the pack URI. `NotSupportedException: No imaging component suitable` means *found but undecodable*;
`IOException` means *not found*. `.gitattributes` must keep binary extensions out of line-ending
normalisation — it silently destroyed every flag PNG once.
