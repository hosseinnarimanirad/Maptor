# SLD editor UI components

This folder contains WPF UserControls and ViewModels for creating and editing OGC SLD 1.0.0 styles.

## Overview

The SLD Editor provides a comprehensive user interface for defining and modifying SLD specifications with support for:
- Point, Line, Polygon, Text, and Raster symbolizers
- Editable single-condition OGC filters for rule-based styling
- Scale-dependent rendering (min/max scale denominators)
- Color pickers and property editors
- Live XML preview and Import/Export of SLD XML files

> **Namespaces:** views live in `IRI.Maptor.Jab.Controls.Symbology.Sld`; view-models live in `IRI.Maptor.Jab.Wpf.ViewModels.Symbology`.

## Components

### ViewModels (`ViewModels/Symbology/Sld/`, namespace `IRI.Maptor.Jab.Wpf.ViewModels.Symbology`)

#### Core ViewModels
- **`SldEditorViewModel`** - Main ViewModel managing the entire SLD document
  - Manages layer name, style name, title, and abstract
  - Contains collection of `RuleViewModel`
  - Provides import/export functionality
  - Commands: `AddRuleCommand`, `RemoveRuleCommand`, `MoveRuleUpCommand`, `MoveRuleDownCommand`

- **`RuleViewModel`** - Represents a single SLD Rule
  - Properties: Name, Title, Abstract
  - Scale range: MinScale, MaxScale
  - Filter support: HasFilter, FilterPropertyName, FilterOperator, FilterValue, FilterDescription
  - Contains collection of symbolizers
  - Commands for adding/removing symbolizers

#### Symbolizer ViewModels
All inherit from `SymbolizerViewModelBase`:

- **`PointSymbolizerViewModel`**
  - Well-known marks (circle, square, triangle, star, cross, x)
  - Fill color and opacity
  - Stroke color, width, and opacity
  - Size and rotation

- **`LineSymbolizerViewModel`**
  - Stroke color, width, and opacity
  - Line cap (butt, round, square)
  - Line join (mitre, round, bevel)
  - Dash array and dash offset

- **`PolygonSymbolizerViewModel`**
  - Fill color and opacity
  - Stroke color, width, and opacity
  - Line cap and line join

- **`TextSymbolizerViewModel`**
  - Label property name
  - Font family, size, style (normal, italic, oblique), and weight (normal, bold)
  - Font color
  - Optional halo (outline) with radius, color, and opacity

- **`RasterSymbolizerViewModel`**
  - Raster opacity
  - Editable color map (color, quantity, label, per-entry opacity)

### Views (`Views/Symbology/Sld/`)

#### Main views
- **`SldEditorView`** - Main composite view showing the full editor
  - Style information panel (layer name, style name, title, abstract)
  - Rules list with toolbar
  - Tabbed interface for rule properties and symbolizers
  - Live XML preview (regenerated when the tab is opened or refreshed)

- **`SldEditorWindow`** - Standalone window wrapping `SldEditorView`
  - Toolbar with import/export buttons
  - Status bar showing rule count and selection

#### Symbolizer editors
- **`PointSymbolizerView`** - Edit point symbolizer properties
- **`LineSymbolizerView`** - Edit line symbolizer properties
- **`PolygonSymbolizerView`** - Edit polygon symbolizer properties
- **`TextSymbolizerView`** - Edit text symbolizer properties (with scrolling support)
- **`RasterSymbolizerView`** - Edit raster opacity and color-map entries

#### Supporting views
- **`ScaleRangeEditorView`** - Edit min/max scale denominators
- **`SimpleFilterEditorView`** - Editable single-condition filter (property / operator / value)

#### Utilities
- **`SymbolizerDataTemplateSelector`** - Selects appropriate view based on symbolizer type

## Usage examples

### Basic usage - standalone window

```csharp
using IRI.Maptor.Jab.Controls.Symbology.Sld;   // Views (SldEditorWindow, SldEditorView, ...)
using IRI.Maptor.Jab.Wpf.ViewModels.Symbology; // ViewModels (SldEditorViewModel, ...)

// Create and show the SLD editor window
var editor = new SldEditorWindow();
editor.Show();
```

### Programmatic SLD creation

```csharp
using IRI.Maptor.Jab.Wpf.ViewModels.Symbology;
using IRI.Maptor.Sta.Ogc.SLD;

// Create a new SLD editor
var editor = new SldEditorViewModel
{
    LayerName = "MyLayer",
    StyleName = "default",
    StyleTitle = "My Custom Style"
};

// Add a rule with a point symbolizer
var rule = new RuleViewModel
{
    Name = "Cities",
    Title = "City Markers"
};

var pointSymbolizer = new PointSymbolizerViewModel
{
    WellKnownMarkType = WellKnownMark.circle,
    Size = 10,
    FillColor = Colors.Red,
    StrokeColor = Colors.Black,
    StrokeWidth = 1
};

rule.Symbolizers.Add(pointSymbolizer);
editor.Rules.Add(rule);

// Export to SLD XML
var sld = editor.ToStyledLayerDescriptor();
```

### Loading existing SLD

```csharp
using System.IO;
using IRI.Maptor.Sta.Ogc.SLD;

// Deserialize existing SLD
StyledLayerDescriptor? sld = SldHelper.Parse(File.ReadAllText("style.sld"));

// Load into editor
var editor = new SldEditorViewModel();
if (sld != null)
    editor.FromStyledLayerDescriptor(sld);

// Show in window
var window = new SldEditorWindow(editor);
window.Show();
```

### Embedding in your application

```xml
<!-- In your XAML -->
<Window xmlns:sld="clr-namespace:IRI.Maptor.Jab.Controls.Symbology.Sld">
    <Grid>
        <sld:SldEditorView DataContext="{Binding YourSldEditorViewModel}"/>
    </Grid>
</Window>
```

## Features

### Supported symbolizers
**PointSymbolizer**
- Well-known marks with fill and stroke
- Size and rotation
- Opacity control

**LineSymbolizer**
- Stroke properties (color, width, opacity)
- Line caps and joins
- Dash patterns

**PolygonSymbolizer**
- Fill and stroke properties
- Line caps and joins

**TextSymbolizer**
- Font properties (family, size, style, weight)
- Text color
- Halo effect (radius, color, opacity) for better readability

**RasterSymbolizer**
- Raster opacity
- Editable color map (color, quantity, label, per-entry opacity)

### Rule features
- Scale-dependent rendering (min/max scale denominators)
- Editable single-condition OGC filter (property / operator / value); more complex loaded filters are preserved on round-trip
- Multiple symbolizers per rule
- Rule ordering (move up/down)

### File operations
- Export to SLD XML file (via `SldHelper.Save`)
- Import from SLD XML file (via `SldHelper.Parse`)
- Live XML preview (via `SldHelper.Serialize`)
- Proper XML serialization with namespaces

## Architecture

The components follow MVVM pattern:
- **ViewModels** inherit from `Notifier` base class (implements `INotifyPropertyChanged`)
- **Views** are UserControls with no code-behind logic (except constructor)
- **Data binding** for all properties
- **Commands** using `RelayCommand` from `IRI.Maptor.Jab.Wpf`
- **Template selectors** for dynamic view selection

## Dependencies

- `IRI.Maptor.Sta.Ogc` - SLD data model
- `IRI.Maptor.Jab.Wpf` - Base classes and utilities
- `MahApps.Metro` - UI controls (ColorPicker, NumericUpDown, etc.)
- `MahApps.Metro.IconPacks.Modern` - Icons

## Future enhancements

Potential improvements:
- Advanced filter builder UI (nested AND/OR, spatial/like/between operators)
- Label placement options editor
- SLD validation and error reporting
- Style preview/rendering
- Import from other style formats
- Style library/templates

## Related classes

See also:
- `IRI.Maptor.Sta.Ogc.SLD` namespace for the underlying data model (and `SldHelper` for read/write)
- `IRI.Maptor.Sta.Ogc` for OGC filter classes (`OgcFilter`, `OgcPropertyIsEqualTo`, ...)
- `IRI.Maptor.Jab.Core.Notifier` for the MVVM base class

---
[Back to IRI.Maptor.Jab.Wpf](../../../README.md)

