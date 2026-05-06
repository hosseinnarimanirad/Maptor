# OGC SLD (Styled Layer Descriptor) Editor UI Components

This folder contains WPF UserControls and ViewModels for creating and editing OGC SLD 1.0.0 styles.

## Overview

The SLD Editor provides a comprehensive user interface for defining and modifying SLD specifications with support for:
- Point, Line, Polygon, and Text symbolizers
- OGC filters for rule-based styling
- Scale-dependent rendering (min/max scale denominators)
- Color pickers and property editors
- Import/Export SLD XML files

## Components

### ViewModels (`ViewModel/Symbology/Sld/`)

#### Core ViewModels
- **`SldEditorViewModel`** - Main ViewModel managing the entire SLD document
  - Manages layer name, style name, title, and abstract
  - Contains collection of `RuleViewModel`
  - Provides import/export functionality
  - Commands: `AddRuleCommand`, `RemoveRuleCommand`, `MoveRuleUpCommand`, `MoveRuleDownCommand`

- **`RuleViewModel`** - Represents a single SLD Rule
  - Properties: Name, Title, Abstract
  - Scale range: MinScale, MaxScale
  - Filter support: HasFilter, Filter, FilterDescription
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

### Views (`View/Symbology/Sld/`)

#### Main Views
- **`SldEditorView`** - Main composite view showing the full editor
  - Style information panel (layer name, style name, title, abstract)
  - Rules list with toolbar
  - Tabbed interface for rule properties and symbolizers
  - XML preview (placeholder)

- **`SldEditorWindow`** - Standalone window wrapping `SldEditorView`
  - Toolbar with import/export buttons
  - Status bar showing rule count and selection

#### Symbolizer Editors
- **`PointSymbolizerView`** - Edit point symbolizer properties
- **`LineSymbolizerView`** - Edit line symbolizer properties
- **`PolygonSymbolizerView`** - Edit polygon symbolizer properties
- **`TextSymbolizerView`** - Edit text symbolizer properties (with scrolling support)

#### Supporting Views
- **`ScaleRangeEditorView`** - Edit min/max scale denominators
- **`SimpleFilterEditorView`** - Basic filter editor (shows filter description, read-only)

#### Utilities
- **`SymbolizerDataTemplateSelector`** - Selects appropriate view based on symbolizer type

## Usage Examples

### Basic Usage - Standalone Window

```csharp
using IRI.Maptor.Jab.Common.View.Symbology.Sld;
using IRI.Maptor.Jab.Common.ViewModel.Symbology.Sld;

// Create and show the SLD editor window
var editor = new SldEditorWindow();
editor.Show();
```

### Programmatic SLD Creation

```csharp
using IRI.Maptor.Jab.Common.ViewModel.Symbology.Sld;
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

### Loading Existing SLD

```csharp
using System.IO;
using System.Xml.Serialization;
using IRI.Maptor.Sta.Ogc.SLD;

// Deserialize existing SLD
var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
StyledLayerDescriptor sld;

using (var stream = File.OpenRead("style.sld"))
{
    sld = (StyledLayerDescriptor)serializer.Deserialize(stream);
}

// Load into editor
var editor = new SldEditorViewModel();
editor.FromStyledLayerDescriptor(sld);

// Show in window
var window = new SldEditorWindow(editor);
window.Show();
```

### Embedding in Your Application

```xml
<!-- In your XAML -->
<Window xmlns:sld="clr-namespace:IRI.Maptor.Jab.Controls.Symbology.Sld">
    <Grid>
        <sld:SldEditorView DataContext="{Binding YourSldEditorViewModel}"/>
    </Grid>
</Window>
```

## Features

### Supported Symbolizers
✅ **PointSymbolizer**
- Well-known marks with fill and stroke
- Size and rotation
- Opacity control

✅ **LineSymbolizer**
- Stroke properties (color, width, opacity)
- Line caps and joins
- Dash patterns

✅ **PolygonSymbolizer**
- Fill and stroke properties
- Line caps and joins

✅ **TextSymbolizer**
- Font properties (family, size, style, weight)
- Text color
- Halo effect for better readability

### Rule Features
✅ Scale-dependent rendering (min/max scale denominators)
✅ OGC Filter support (basic display)
✅ Multiple symbolizers per rule
✅ Rule ordering (move up/down)

### File Operations
✅ Export to SLD XML file
✅ Import from SLD XML file
✅ Proper XML serialization with namespaces

## Architecture

The components follow MVVM pattern:
- **ViewModels** inherit from `Notifier` base class (implements `INotifyPropertyChanged`)
- **Views** are UserControls with no code-behind logic (except constructor)
- **Data binding** for all properties
- **Commands** using `RelayCommand` from `IRI.Maptor.Jab.Common`
- **Template selectors** for dynamic view selection

## Dependencies

- `IRI.Maptor.Sta.Ogc` - SLD data model
- `IRI.Maptor.Jab.Common` - Base classes and utilities
- `MahApps.Metro` - UI controls (ColorPicker, NumericUpDown, etc.)
- `MahApps.Metro.IconPacks.Modern` - Icons

## Future Enhancements

Potential improvements:
- Advanced filter builder UI
- Label placement options editor
- Raster symbolizer support
- SLD validation and error reporting
- Style preview/rendering
- Import from other style formats
- Style library/templates

## Related Classes

See also:
- `IRI.Maptor.Sta.Ogc.SLD` namespace for the underlying data model
- `IRI.Maptor.Sta.Ogc.FilterEncoding` for OGC filter classes
- `IRI.Maptor.Jab.Common.ViewModels.ViewModelBase` for MVVM base classes

## License

Part of the IRI.Maptor framework.

