# SLD Editor - Practical Usage Examples

## Example 1: Simple Point Style for Cities

```csharp
using IRI.Maptor.Presentation.Wpf.ViewModel.Symbology.Sld;
using IRI.Maptor.Core.Ogc.SLD;
using System.Windows.Media;

public void CreateCityPointStyle()
{
    var editor = new SldEditorViewModel
    {
        LayerName = "Cities",
        StyleName = "city_points",
        StyleTitle = "City Point Markers"
    };

    // Create a rule for cities
    var cityRule = new RuleViewModel
    {
        Name = "city",
        Title = "Cities"
    };

    // Add a red circle point symbolizer
    var pointSymbolizer = new PointSymbolizerViewModel
    {
        WellKnownMarkType = WellKnownMark.circle,
        Size = 12,
        FillColor = Colors.Red,
        FillOpacity = 0.8,
        StrokeColor = Colors.DarkRed,
        StrokeWidth = 2,
        StrokeOpacity = 1.0
    };

    cityRule.Symbolizers.Add(pointSymbolizer);
    editor.Rules.Add(cityRule);

    // Export to file
    var sld = editor.ToStyledLayerDescriptor();
    SaveToFile(sld, "cities.sld");
}
```

Generated SLD:
```xml
<?xml version="1.0" encoding="utf-8"?>
<StyledLayerDescriptor xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
                       xmlns:xlink="http://www.w3.org/1999/xlink" 
                       xmlns:ogc="http://www.opengis.net/ogc" 
                       xmlns="http://www.opengis.net/sld" 
                       version="1.0.0">
  <NamedLayer>
    <Name>Cities</Name>
    <UserStyle>
      <Name>city_points</Name>
      <Title>City Point Markers</Title>
      <IsDefault>true</IsDefault>
      <FeatureTypeStyle>
        <Rule>
          <Name>city</Name>
          <Title>Cities</Title>
          <PointSymbolizer>
            <Graphic>
              <Mark>
                <WellKnownName>circle</WellKnownName>
                <Fill>
                  <CssParameter name="fill">#FFFF0000</CssParameter>
                  <CssParameter name="fill-opacity">0.8</CssParameter>
                </Fill>
                <Stroke>
                  <CssParameter name="stroke">#FF8B0000</CssParameter>
                  <CssParameter name="stroke-width">2</CssParameter>
                  <CssParameter name="stroke-opacity">1</CssParameter>
                </Stroke>
              </Mark>
              <Size>12</Size>
            </Graphic>
          </PointSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
```

## Example 2: Road Network with Scale-Dependent Rendering

```csharp
public void CreateRoadStyleWithScales()
{
    var editor = new SldEditorViewModel
    {
        LayerName = "Roads",
        StyleName = "roads",
        StyleTitle = "Road Network Style"
    };

    // Highways - visible at all scales
    var highwayRule = new RuleViewModel
    {
        Name = "highway",
        Title = "Highways",
        MaxScale = null,  // No maximum
        MinScale = null   // No minimum
    };

    var highwaySymbolizer = new LineSymbolizerViewModel
    {
        StrokeColor = Color.FromRgb(255, 100, 0),  // Orange
        StrokeWidth = 5,
        StrokeOpacity = 1.0,
        LineCap = Sld_StrokeLineCap.Round,
        LineJoin = Sld_StrokeLineJoin.Round
    };
    highwayRule.Symbolizers.Add(highwaySymbolizer);
    editor.Rules.Add(highwayRule);

    // Main roads - visible from 1:100000
    var mainRoadRule = new RuleViewModel
    {
        Name = "main_road",
        Title = "Main Roads",
        MaxScale = 100000,
        MinScale = null
    };

    var mainRoadSymbolizer = new LineSymbolizerViewModel
    {
        StrokeColor = Color.FromRgb(255, 200, 0),  // Yellow
        StrokeWidth = 3,
        StrokeOpacity = 1.0
    };
    mainRoadRule.Symbolizers.Add(mainRoadSymbolizer);
    editor.Rules.Add(mainRoadRule);

    // Local streets - only visible at large scales
    var localRule = new RuleViewModel
    {
        Name = "local",
        Title = "Local Streets",
        MaxScale = 25000,
        MinScale = null
    };

    var localSymbolizer = new LineSymbolizerViewModel
    {
        StrokeColor = Colors.Gray,
        StrokeWidth = 1,
        StrokeOpacity = 0.7
    };
    localRule.Symbolizers.Add(localSymbolizer);
    editor.Rules.Add(localRule);

    // Export
    var sld = editor.ToStyledLayerDescriptor();
    SaveToFile(sld, "roads.sld");
}
```

## Example 3: Land Use Polygons with Labels

```csharp
public void CreateLandUseStyle()
{
    var editor = new SldEditorViewModel
    {
        LayerName = "LandUse",
        StyleName = "landuse",
        StyleTitle = "Land Use Style"
    };

    // Residential areas
    var residentialRule = new RuleViewModel
    {
        Name = "residential",
        Title = "Residential Areas"
    };

    // Polygon symbolizer
    var polySymbolizer = new PolygonSymbolizerViewModel
    {
        FillColor = Color.FromRgb(255, 240, 200),  // Light beige
        FillOpacity = 0.6,
        StrokeColor = Color.FromRgb(180, 150, 100),
        StrokeWidth = 1,
        StrokeOpacity = 0.8
    };
    residentialRule.Symbolizers.Add(polySymbolizer);

    // Text symbolizer for labels
    var labelSymbolizer = new TextSymbolizerViewModel
    {
        LabelPropertyName = "name",
        FontFamily = "Arial",
        FontSize = 12,
        FontStyle = Sld_FontStyle.Normal,
        FontWeight = Sld_FontWeight.Bold,
        FontColor = Color.FromRgb(100, 80, 50),
        EnableHalo = true,
        HaloRadius = 2,
        HaloColor = Colors.White,
        HaloOpacity = 0.8
    };
    residentialRule.Symbolizers.Add(labelSymbolizer);

    editor.Rules.Add(residentialRule);

    // Export
    var sld = editor.ToStyledLayerDescriptor();
    SaveToFile(sld, "landuse.sld");
}
```

## Example 4: Opening the Editor Window

```csharp
using IRI.Maptor.Presentation.Wpf.View.Symbology.Sld;
using IRI.Maptor.Presentation.Wpf.ViewModel.Symbology.Sld;

// Option 1: Simple standalone window
public void OpenSldEditor()
{
    var editor = new SldEditorWindow();
    editor.Show();
}

// Option 2: With existing ViewModel
public void OpenSldEditorWithData(SldEditorViewModel viewModel)
{
    var editor = new SldEditorWindow(viewModel);
    editor.ShowDialog(); // Modal dialog
}

// Option 3: As part of a larger application
public void IntegrateIntoApp()
{
    var mainWindow = new YourMainWindow();
    
    // Create the SLD editor view model
    var sldEditor = new SldEditorViewModel
    {
        LayerName = "MyLayer",
        StyleName = "default"
    };
    
    // Set it as data context for a panel in your main window
    mainWindow.SldEditorPanel.DataContext = sldEditor;
    
    mainWindow.Show();
}
```

## Example 5: Loading and Modifying Existing SLD

```csharp
using System.IO;
using System.Xml.Serialization;
using IRI.Maptor.Core.Ogc.SLD;

public void LoadAndModifyExistingSld(string filePath)
{
    // Load existing SLD
    var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
    StyledLayerDescriptor sld;
    
    using (var stream = File.OpenRead(filePath))
    {
        sld = (StyledLayerDescriptor)serializer.Deserialize(stream);
    }

    // Create editor and load SLD
    var editor = new SldEditorViewModel();
    editor.FromStyledLayerDescriptor(sld);

    // Modify - add a new rule
    var newRule = new RuleViewModel
    {
        Name = "new_rule",
        Title = "New Rule"
    };

    var pointSymbolizer = new PointSymbolizerViewModel
    {
        WellKnownMarkType = WellKnownMark.star,
        Size = 15,
        FillColor = Colors.Gold,
        StrokeColor = Colors.Orange,
        StrokeWidth = 2
    };

    newRule.Symbolizers.Add(pointSymbolizer);
    editor.Rules.Add(newRule);

    // Save modified version
    var modifiedSld = editor.ToStyledLayerDescriptor();
    SaveToFile(modifiedSld, "modified_" + Path.GetFileName(filePath));
}
```

## Example 6: Dashed Line Pattern for Boundaries

```csharp
public void CreateBoundaryStyle()
{
    var editor = new SldEditorViewModel
    {
        LayerName = "Boundaries",
        StyleName = "boundary_dashed",
        StyleTitle = "Dashed Boundary Lines"
    };

    var boundaryRule = new RuleViewModel
    {
        Name = "international",
        Title = "International Boundaries"
    };

    var lineSymbolizer = new LineSymbolizerViewModel
    {
        StrokeColor = Colors.Red,
        StrokeWidth = 2,
        StrokeOpacity = 1.0,
        LineCap = Sld_StrokeLineCap.Butt,
        LineJoin = Sld_StrokeLineJoin.Mitre,
        DashArray = "10 5 2 5",  // Long dash, space, dot, space
        DashOffset = 0
    };

    boundaryRule.Symbolizers.Add(lineSymbolizer);
    editor.Rules.Add(boundaryRule);

    var sld = editor.ToStyledLayerDescriptor();
    SaveToFile(sld, "boundaries.sld");
}
```

## Example 7: Multiple Symbolizers (Cased Line)

```csharp
public void CreateCasedRoadStyle()
{
    var editor = new SldEditorViewModel
    {
        LayerName = "Roads",
        StyleName = "cased_roads",
        StyleTitle = "Roads with Black Outline"
    };

    var roadRule = new RuleViewModel
    {
        Name = "main_roads",
        Title = "Main Roads with Casing"
    };

    // First symbolizer: outer black stroke (casing)
    var casingSymbolizer = new LineSymbolizerViewModel
    {
        StrokeColor = Colors.Black,
        StrokeWidth = 7,
        StrokeOpacity = 1.0,
        LineCap = Sld_StrokeLineCap.Round,
        LineJoin = Sld_StrokeLineJoin.Round
    };
    roadRule.Symbolizers.Add(casingSymbolizer);

    // Second symbolizer: inner yellow stroke (fill)
    var fillSymbolizer = new LineSymbolizerViewModel
    {
        StrokeColor = Color.FromRgb(255, 220, 0),
        StrokeWidth = 5,
        StrokeOpacity = 1.0,
        LineCap = Sld_StrokeLineCap.Round,
        LineJoin = Sld_StrokeLineJoin.Round
    };
    roadRule.Symbolizers.Add(fillSymbolizer);

    editor.Rules.Add(roadRule);

    var sld = editor.ToStyledLayerDescriptor();
    SaveToFile(sld, "cased_roads.sld");
}
```

## Helper Method: Save to File

```csharp
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using IRI.Maptor.Core.Ogc.SLD;

private void SaveToFile(StyledLayerDescriptor sld, string filename)
{
    var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
    
    var settings = new XmlWriterSettings
    {
        Indent = true,
        IndentChars = "  ",
        NewLineChars = "\n"
    };

    using (var stream = File.Create(filename))
    using (var writer = XmlWriter.Create(stream, settings))
    {
        serializer.Serialize(writer, sld);
    }
    
    Console.WriteLine($"SLD saved to: {filename}");
}
```

## Example 8: Interactive Editor Window with Callbacks

```csharp
using System.Windows;

public void ShowInteractiveSldEditor()
{
    var viewModel = new SldEditorViewModel
    {
        LayerName = "MyLayer",
        StyleName = "default"
    };

    var window = new SldEditorWindow(viewModel);
    
    // Handle window closing to save changes
    window.Closing += (sender, e) =>
    {
        var result = MessageBox.Show(
            "Do you want to save changes?",
            "Save Changes",
            MessageBoxButton.YesNoCancel);

        if (result == MessageBoxResult.Yes)
        {
            var sld = viewModel.ToStyledLayerDescriptor();
            SaveToFile(sld, "style.sld");
        }
        else if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true; // Don't close
        }
    };

    window.ShowDialog();
}
```

## Example 9: Embed in TabControl

```xml
<!-- In your XAML -->
<TabControl>
    <TabItem Header="Map">
        <!-- Your map control -->
    </TabItem>
    
    <TabItem Header="Style Editor">
        <local:SldEditorView DataContext="{Binding SldEditorViewModel}"/>
    </TabItem>
    
    <TabItem Header="Layer Properties">
        <!-- Other controls -->
    </TabItem>
</TabControl>
```

```csharp
// In your ViewModel
public class MainViewModel
{
    public SldEditorViewModel SldEditorViewModel { get; set; }
    
    public MainViewModel()
    {
        SldEditorViewModel = new SldEditorViewModel();
        
        // Load default style
        SldEditorViewModel.LayerName = "CurrentLayer";
        SldEditorViewModel.StyleName = "default";
    }
    
    public void ApplyStyleToMap()
    {
        var sld = SldEditorViewModel.ToStyledLayerDescriptor();
        
        // Apply to your map rendering engine
        YourMapEngine.ApplyStyle(sld);
    }
}
```

## Example 10: Validate Before Export

```csharp
public void ValidateAndExportSld()
{
    var editor = new SldEditorViewModel();
    // ... configure editor ...

    // Basic validation
    if (string.IsNullOrWhiteSpace(editor.LayerName))
    {
        MessageBox.Show("Layer name is required", "Validation Error");
        return;
    }

    if (editor.Rules.Count == 0)
    {
        MessageBox.Show("At least one rule is required", "Validation Error");
        return;
    }

    foreach (var rule in editor.Rules)
    {
        if (rule.Symbolizers.Count == 0)
        {
            MessageBox.Show($"Rule '{rule.Name}' has no symbolizers", "Validation Error");
            return;
        }
    }

    // Export
    try
    {
        var sld = editor.ToStyledLayerDescriptor();
        SaveToFile(sld, "validated_style.sld");
        MessageBox.Show("Style exported successfully!", "Success");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error exporting style: {ex.Message}", "Error");
    }
}
```

## Tips and Best Practices

1. **Rule Ordering**: Rules are evaluated in order. Put more specific rules first.

2. **Scale Ranges**: 
   - MaxScale = smaller number = more zoomed in
   - MinScale = larger number = more zoomed out
   - Example: MaxScale=10000, MinScale=100000 shows between 1:10,000 and 1:100,000

3. **Multiple Symbolizers**: 
   - Order matters - they're drawn in sequence
   - Use for effects like road casing (draw wide line, then narrow line on top)

4. **Text Labels with Halo**:
   - Always use halo for better readability
   - White halo works well on most backgrounds

5. **Color Opacity**:
   - Use for overlapping features
   - Values: 0.0 (transparent) to 1.0 (opaque)

6. **Dash Arrays**:
   - Format: "dash space dash space..."
   - Example: "5 2" = 5px dash, 2px space
   - Example: "10 5 2 5" = long dash, space, dot, space

7. **File Organization**:
   - Save SLD files with descriptive names
   - Keep backups before modifications
   - Use version control for style files

