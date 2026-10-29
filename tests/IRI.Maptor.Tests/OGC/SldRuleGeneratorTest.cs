using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Core.Ogc;
using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.OGC;

// 2026-10-22: the SLD editor's "Generate rules from field values" dialog (synthetic fields and features).
public class SldRuleGeneratorTest
{
    private static Field Material() => new()
    {
        Name = "Material",
        Alias = "جنس",
        TypeFullName = typeof(string).FullName!,
        DataType = FieldDataType.Text,
        Domain = new FieldDomain
        {
            Name = "CI_Material",
            Values = [new() { Value = "PE", Code = 1 }, new() { Value = "Steel", Code = 2 }, new() { Value = "Copper", Code = 3 }],
        },
    };

    private static Field Note() => new() { Name = "Note", TypeFullName = typeof(string).FullName!, DataType = FieldDataType.Text };

    private static Feature<Point> F(object? material) => new(null!, new Dictionary<string, object> { ["Material"] = material!, ["Note"] = "n" });

    // PE ×3, Steel ×1, an out-of-domain "Plastic" ×2, an empty value
    private static readonly List<Feature<Point>> Features = [F("PE"), F("PE"), F("PE"), F("Steel"), F("Plastic"), F("Plastic"), F(null)];

    private static System.Func<string, CancellationToken, Task<IReadOnlyList<SldValueCount>>> Values() =>
        SldRuleGeneratorViewModel.CountValuesOf(() => Task.FromResult<IEnumerable<Feature<Point>>?>(Features));

    private static Task<IReadOnlyDictionary<string, string>?> Colors(FieldDomain domain, CancellationToken token) =>
        Task.FromResult<IReadOnlyDictionary<string, string>?>(new Dictionary<string, string> { ["PE"] = "rgba(79,6,250,1)", ["Steel"] = "#00ff00" });

    [Fact]
    public async Task Defaults_ValuesInTheLayer_PaletteColours_DomainFieldsFirst()
    {
        var vm = new SldRuleGeneratorViewModel([Note(), Material()], SldGeometryKind.Line, null, Values(), Colors);
        await vm.RefreshAsync();

        Assert.Equal("Material", vm.SelectedField!.Field.Name);      // the domain field is offered first
        Assert.Equal(SldRuleValueSource.Data, vm.ValueSource);
        Assert.Equal(SldRuleColorSource.Palette, vm.ColorSource);
        Assert.True(vm.IncludeOtherValues);

        // the domain's order for the values that occur, then the others by count; empty values are left to "other"
        Assert.Equal(new[] { "PE", "Steel", "Plastic" }, vm.PreviewItems.Select(p => p.Title));
        Assert.Equal(new[] { "3", "1", "2" }, vm.PreviewItems.Select(p => p.Count));
    }

    [Fact]
    public async Task DomainValues_ListTheWholeDomain()
    {
        var vm = new SldRuleGeneratorViewModel([Material()], SldGeometryKind.Line, null, Values(), Colors) { ValueSource = SldRuleValueSource.Domain };
        await vm.RefreshAsync();

        Assert.Equal(new[] { "PE", "Steel", "Copper" }, vm.PreviewItems.Select(p => p.Title));
    }

    [Fact]
    public async Task DomainColours_AreUsed_AndThePaletteFillsTheGaps()
    {
        var vm = new SldRuleGeneratorViewModel([Material()], SldGeometryKind.Polygon, null, Values(), Colors) { ColorSource = SldRuleColorSource.Domain };
        await vm.RefreshAsync();

        vm.GenerateCommand.Execute(null);

        var rules = vm.Result!;
        string Fill(Rule rule) => ((PolygonSymbolizer)rule.Symbolizers[0]).Fill.CssParameters.First(p => p.Name == "fill").Value;

        Assert.Equal(4, rules.Count);                                   // PE, Steel, Plastic, other
        Assert.Equal("#4f06fa", Fill(rules[0]));
        Assert.Equal("#00ff00", Fill(rules[1]));
        Assert.Equal(SldCategoryRules.Palette[0], Fill(rules[2]));     // Plastic has no domain colour
        Assert.NotNull(rules[3].ElseFilter);
    }

    [Fact]
    public async Task WithoutData_OnlyDomainValuesAreOffered()
    {
        var vm = new SldRuleGeneratorViewModel([Material()], SldGeometryKind.Point, null, valuesProvider: null, domainColorsProvider: null);
        await vm.RefreshAsync();

        Assert.False(vm.CanUseDataValues);
        Assert.False(vm.CanUseDomainColors);
        Assert.Equal(SldRuleValueSource.Domain, vm.ValueSource);
        Assert.Equal(3, vm.PreviewItems.Count);
    }

    [Fact]
    public async Task Editor_ReplacesItsRulesWithTheGeneratedOnes_KeepingTheTemplateStyle()
    {
        var template = new StyledLayerDescriptor();
        var layer = new NamedLayer { Name = "pipes" };
        var style = new UserStyle { Name = "s" };
        style.FeatureTypeStyles.Add(new FeatureTypeStyle
        {
            Rules =
            {
                new Rule
                {
                    Name = "all",
                    Symbolizers = { new LineSymbolizer { Stroke = new Stroke { CssParameters = { new CssParameter { Name = "stroke", Value = "#000000" }, new CssParameter { Name = "stroke-width", Value = "4" } } } } },
                },
            },
        });
        layer.UserStyles.Add(style);
        template.NamedLayers.Add(layer);

        var editor = SldEditorViewModel.Create("pipes", template, null, SpatialModelMode.Polyline, [Material(), Note()]);
        editor.ValuesProvider = Values();

        editor.RequestShowRuleGenerator = generator =>
        {
            generator.RefreshAsync().GetAwaiter().GetResult();
            generator.GenerateCommand.Execute(null);
            return true;
        };

        Assert.True(editor.GenerateRulesCommand.CanExecute(null));

        editor.GenerateRulesCommand.Execute(null);

        var rules = editor.ToStyledLayerDescriptor().NamedLayers[0].UserStyles[0].FeatureTypeStyles.SelectMany(f => f.Rules).ToList();

        Assert.Equal(new[] { "PE", "Steel", "Plastic", "other" }, rules.Select(r => r.Name));
        Assert.All(rules, r => Assert.Equal("4", ((LineSymbolizer)r.Symbolizers[0]).Stroke.CssParameters.First(p => p.Name == "stroke-width").Value));
        Assert.Equal("PE", editor.SelectedRule!.ToRule().Name);
    }

    [Fact]
    public void Editor_WithFieldNamesOnly_DoesNotOfferTheGenerator()
    {
        var editor = SldEditorViewModel.Create("x", null, ["a", "b"], SpatialModelMode.Point);
        editor.RequestShowRuleGenerator = _ => true;

        Assert.False(editor.GenerateRulesCommand.CanExecute(null));
    }
}
