# Theme and controls

Every Maptor style token on one page, over a live accent + light/dark switch.

This is two things at once: the reference a view author looks at to see what a token actually looks
like, and the cheapest visual-regression surface the design system has. Flip **Dark** and the
status palette, pills and banners all have to move; the over-map chrome deliberately does not.

## What it shows

- **Semantic status palette** — `IRI.Maptor.Brushes.Valid` / `Invalid` / `Warning` / `Muted` and
  their `.Fill` pairs. MahApps has no valid/invalid colours, so these are Maptor's own and
  `ThemeHelper` swaps the whole dictionary on a mode change.
- **Type scale** — the eight `IRI.Maptor.Styles.Size.Text.*` steps.
- **Text, buttons, pills, banners, inputs, surfaces** — one specimen per shipped style key.
- **Over-map chrome** — `IRI.Maptor.Brushes.OnMap.*` on a stand-in for imagery. These are
  deliberately *not* theme-swapped, so they stay put when you flip the switch. That is correct:
  they sit on satellite tiles, not on a themed surface.

## What the switches do

| control | calls | why |
|---|---|---|
| Accent | `ThemeHelper.SetAccent(colour)` | the mode is none of that control's business |
| Dark | `ThemeHelper.SetMode(mode)` | also switches `FollowWindowsMode` off, so an explicit choice survives the next Windows preference change |
| Follow Windows | `ThemeHelper.FollowWindowsMode` | light/dark follows the OS; the accent stays the user's choice |

The three controls are **outputs as well as inputs** — Windows can change the mode with nobody
touching them, and `SetMode` turns the sync off — so the page subscribes to `ThemeHelper.ThemeChanged`
and re-syncs, behind a re-entrancy guard. Subscription happens in `Loaded` and is released in
`Unloaded`: `ThemeChanged` is a **static** event, and a view that never unsubscribes keeps itself
alive for the life of the process.

## Two things this page caught on its first run

Both are recorded because they are the same mistake in two costumes, and both would have been
reported as library bugs by anyone reading a screenshot.

**1. A page that paints no background is not a dark page.** The first dark-mode render showed dark
cards floating on a white sheet, because the root panel was transparent and composited onto whatever
the host painted. Half the text looked broken. The root now paints
`MahApps.Brushes.ThemeBackground`. This is the same error that once made the legend text invisible —
see `PROGRESS.md` §5: *score contrast against the surface actually painted behind the element*.

**2. Five text styles set no `Foreground` and inherit it from the window.**
`TextBlock.WindowTitle`, `.Title`, `.PanelTitle`, `.CardTitle` and `.Hint` rely on
`TextElement.Foreground`, which MahApps sets on `MetroWindow`. Measured in the dark theme:

| host | those five resolve to |
|---|---|
| `MetroWindow` | `#FFFFFFFF` — correct |
| plain `Window` | `#FF000000` — black on `#252525`, about 1.4:1 |

The gallery's own `MainWindow` is a plain `Window`, so it had the bug. It now sets
`TextElement.Foreground` and `Background` explicitly, and the five measure white again. **If you
host Maptor views outside a `MetroWindow`, set both.**

Neither is a defect in the styles. Both are real dependencies that nothing wrote down until this
page made them visible — which is the argument for the page existing.

## Adding a specimen

Use a real `IRI.Maptor.Styles.*` key and nothing else; hand-styling defeats the point. Colours go
through `DynamicResource`, never `StaticResource` — the status palette is swapped at run time and a
`StaticResource` freezes it at whatever loaded first.
