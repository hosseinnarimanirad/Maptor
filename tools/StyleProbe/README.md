# StyleProbe

Runtime verification for the Maptor WPF design system.

```
dotnet run --project tools/StyleProbe
```

Exit code `0` = PASS, `1` = FAIL. Every check prints `ok` or `FAIL` with the measured value, so a
failure tells you what it got, not just that it disagreed.

## Why this exists

**A clean build proves nothing about XAML.** A `StaticResource` resolves at *runtime*: a missing,
misspelled or unreachable key compiles without a murmur and throws when the view is first realised.
Every step of the design-system remediation was verified by constructing views in a real
`Application`, and every step re-authored a throwaway probe in a scratch folder to do it.
[`PROGRESS.md`](../../docs/features/design-system/PROGRESS.md) §6 flagged that waste twice. This is
that probe, kept.

It is not a unit-test suite and does not try to be. It asserts the handful of contracts that were
expensive to establish and have each been broken at least once.

## What it asserts

**1. Host wiring.** A host that merges the MahApps baseline plus
`Assets/Maptor.All.xaml` gets a working application: the `Localization` provider resolves, the
MahApps keys resolve, the ten representative Maptor keys resolve — including the five styles that
are `BasedOn` a MahApps key, which is why the MahApps merges cannot move inside `Maptor.All.xaml` —
and the status palette falls back to **light** when no theme has been applied.

**2. The `ThemeHelper` API contract.** A null argument *keeps what is applied* and never resets it:
`SetAccent` keeps the mode, `SetMode` keeps the accent, a bare `ApplyTheme(colour)` keeps the mode.
That last one is the regression that cost five call sites across three applications.

> Scored on `MahApps.Brushes.ThemeBackground` and the status palette, **never** on the accent — the
> MahApps accent is byte-identical in light and dark, so an unchanged accent proves nothing about
> the mode. Getting this wrong has produced a confident wrong answer before.

**3. ControlzEx integration.** One `ChangeTheme` themes MahApps *and* Fluent.Ribbon (which is why
`ThemeHelper` no longer merges Fluent itself — the probe references Fluent.Ribbon purely to assert
this); an **external** `ThemeManager` change moves `Current`, swaps the status palette and raises
`ThemeChanged`; `FollowWindowsMode` adopts the Windows mode while keeping the chosen accent, and an
explicit `SetMode` switches it off; a runtime-generated accent yields its mode without corrupting
`Current.Color`; ten swaps leave exactly one status dictionary and zero statically merged theme
dictionaries; a throwing `ThemeChanged` subscriber is logged rather than propagated.

**4. Views and styles realise.** The six library views that bind `{StaticResource Localization}`
without declaring the provider all construct, a styled `Button` lays out, and — README §4.4c, which
was silently wrong for months — the **element tree and application scope agree** on the status
palette.

## Adding a check

Two patterns are worth copying from the history in
[`README.md`](../../docs/features/design-system/README.md) §9e and §9g:

- After a rename, assert the **old** key no longer resolves. Checking only that the new one exists
  lets a half-finished rename pass.
- Resolve any key that C# fetches by string literal *through that same literal*. An XAML-only
  search cannot see `TryFindResource("…")`, and renaming past one fails **silently**.

The colour constants at the top (`ValidLight`, `ValidDark`) are the semantic palette's actual
values. If `Status.Light.xaml` or `Status.Dark.xaml` changes, this probe should fail and be updated
deliberately — that palette is the point.

## Not in the solution

`StyleProbe` is intentionally absent from `IRI.Maptor.sln`; it is a tool, referenced by nothing, and
the solution build has its own problems ([`PROGRESS.md`](../../docs/features/design-system/PROGRESS.md)
§6). Run it by path.

## Related: the literal-colour guard

A separate, cheaper check runs on every build of `IRI.Maptor.Presentation.Wpf`:
`build/LiteralColourGuard.targets` fails the build when a view under `Views/` gains a literal
colour that is not on `build/LiteralColours.allow`. The probe checks that the tokens *work*; the
guard checks that views *use* them.
