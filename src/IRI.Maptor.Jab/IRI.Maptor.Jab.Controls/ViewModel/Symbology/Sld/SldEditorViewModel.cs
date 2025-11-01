using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.Ogc.SLD;

namespace IRI.Maptor.Jab.Controls.ViewModel.Symbology.Sld;

public class SldEditorViewModel : Notifier
{
    private string _layerName;
    public string LayerName
    {
        get => _layerName;
        set
        {
            _layerName = value;
            RaisePropertyChanged();
        }
    }

    private string _styleName;
    public string StyleName
    {
        get => _styleName;
        set
        {
            _styleName = value;
            RaisePropertyChanged();
        }
    }

    private string _styleTitle;
    public string StyleTitle
    {
        get => _styleTitle;
        set
        {
            _styleTitle = value;
            RaisePropertyChanged();
        }
    }

    private string _styleAbstract;
    public string StyleAbstract
    {
        get => _styleAbstract;
        set
        {
            _styleAbstract = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<RuleViewModel> Rules { get; } = new ObservableCollection<RuleViewModel>();

    private RuleViewModel _selectedRule;
    public RuleViewModel SelectedRule
    {
        get => _selectedRule;
        set
        {
            _selectedRule = value;
            RaisePropertyChanged();
        }
    }

    public ICommand AddRuleCommand { get; }
    public ICommand RemoveRuleCommand { get; }
    public ICommand MoveRuleUpCommand { get; }
    public ICommand MoveRuleDownCommand { get; }
    public ICommand ImportSldCommand { get; }
    public ICommand ExportSldCommand { get; }

    public SldEditorViewModel()
    {
        AddRuleCommand = new RelayCommand(_ => AddRule());
        RemoveRuleCommand = new RelayCommand(_ => RemoveRule(), _ => SelectedRule != null);
        MoveRuleUpCommand = new RelayCommand(_ => MoveRuleUp(), _ => SelectedRule != null && Rules.IndexOf(SelectedRule) > 0);
        MoveRuleDownCommand = new RelayCommand(_ => MoveRuleDown(), _ => SelectedRule != null && Rules.IndexOf(SelectedRule) < Rules.Count - 1);
        ImportSldCommand = new RelayCommand(param => ImportSld(param as string));
        ExportSldCommand = new RelayCommand(param => ExportSld(param as string));

        // Initialize with default values
        LayerName = "NewLayer";
        StyleName = "default";
        StyleTitle = "Default Style";
    }

    private void AddRule()
    {
        var rule = new RuleViewModel
        {
            Name = $"Rule{Rules.Count + 1}",
            Title = $"Rule {Rules.Count + 1}"
        };
        Rules.Add(rule);
        SelectedRule = rule;
    }

    private void RemoveRule()
    {
        if (SelectedRule != null)
        {
            var index = Rules.IndexOf(SelectedRule);
            Rules.Remove(SelectedRule);
            
            if (Rules.Count > 0)
            {
                SelectedRule = Rules[Math.Min(index, Rules.Count - 1)];
            }
        }
    }

    private void MoveRuleUp()
    {
        if (SelectedRule == null) return;

        var index = Rules.IndexOf(SelectedRule);
        if (index > 0)
        {
            Rules.Move(index, index - 1);
        }
    }

    private void MoveRuleDown()
    {
        if (SelectedRule == null) return;

        var index = Rules.IndexOf(SelectedRule);
        if (index < Rules.Count - 1)
        {
            Rules.Move(index, index + 1);
        }
    }

    public StyledLayerDescriptor ToStyledLayerDescriptor()
    {
        var sld = new StyledLayerDescriptor();

        var namedLayer = new NamedLayer
        {
            Name = LayerName
        };

        var userStyle = new UserStyle
        {
            Name = StyleName,
            Title = StyleTitle,
            Abstract = StyleAbstract,
            IsDefault = true
        };

        var featureTypeStyle = new FeatureTypeStyle
        {
            Rules = Rules.Select(r => r.ToRule()).ToList()
        };

        userStyle.FeatureTypeStyles.Add(featureTypeStyle);
        namedLayer.UserStyles.Add(userStyle);
        sld.NamedLayers.Add(namedLayer);

        return sld;
    }

    public void FromStyledLayerDescriptor(StyledLayerDescriptor sld)
    {
        if (sld == null || sld.NamedLayers.Count == 0)
            return;

        var namedLayer = sld.NamedLayers.First();
        LayerName = namedLayer.Name;

        if (namedLayer.UserStyles.Count > 0)
        {
            var userStyle = namedLayer.UserStyles.First();
            StyleName = userStyle.Name;
            StyleTitle = userStyle.Title;
            StyleAbstract = userStyle.Abstract;

            Rules.Clear();
            if (userStyle.FeatureTypeStyles.Count > 0)
            {
                var featureTypeStyle = userStyle.FeatureTypeStyles.First();
                foreach (var rule in featureTypeStyle.Rules ?? Enumerable.Empty<Rule>())
                {
                    var ruleVm = new RuleViewModel();
                    ruleVm.FromRule(rule);
                    Rules.Add(ruleVm);
                }
            }
        }
    }

    private void ImportSld(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        try
        {
            var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
            using (var stream = File.OpenRead(filePath))
            {
                var sld = (StyledLayerDescriptor)serializer.Deserialize(stream);
                FromStyledLayerDescriptor(sld);
            }
        }
        catch (Exception ex)
        {
            // Handle error - in production, show error dialog
            System.Diagnostics.Debug.WriteLine($"Error importing SLD: {ex.Message}");
        }
    }

    private void ExportSld(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            var sld = ToStyledLayerDescriptor();
            var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
            
            using (var stream = File.Create(filePath))
            {
                serializer.Serialize(stream, sld);
            }
        }
        catch (Exception ex)
        {
            // Handle error - in production, show error dialog
            System.Diagnostics.Debug.WriteLine($"Error exporting SLD: {ex.Message}");
        }
    }
}

