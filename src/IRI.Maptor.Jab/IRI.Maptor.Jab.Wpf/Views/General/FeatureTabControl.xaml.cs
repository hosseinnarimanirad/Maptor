using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRI.Maptor.Jab.Controls;
/// <summary>
/// Interaction logic for FeatureTabControl.xaml
/// </summary>
public partial class FeatureTabControl : UserControl
{
    public FeatureTabControl()
    {
        InitializeComponent();
    }

    public bool IsZoomToGeometryEnabled
    {
        get { return (bool)GetValue(IsZoomToGeometryEnabledProperty); }
        set { SetValue(IsZoomToGeometryEnabledProperty, value); }
    }
    public static readonly DependencyProperty IsZoomToGeometryEnabledProperty =
      DependencyProperty.Register(nameof(IsZoomToGeometryEnabled), typeof(bool), typeof(FeatureTabControl), new PropertyMetadata(false));


    public bool CanUserEditGeometry
    {
        get { return (bool)GetValue(CanUserEditGeometryProperty); }
        set { SetValue(CanUserEditGeometryProperty, value); }
    }
    public static readonly DependencyProperty CanUserEditGeometryProperty =
        DependencyProperty.Register(nameof(CanUserEditGeometry), typeof(bool), typeof(FeatureTabControl), new PropertyMetadata(false));



    public bool CanUserEditAttribute
    {
        get { return (bool)GetValue(CanUserEditAttributeProperty); }
        set { SetValue(CanUserEditAttributeProperty, value); }
    }
    public static readonly DependencyProperty CanUserEditAttributeProperty =
        DependencyProperty.Register(nameof(CanUserEditAttribute), typeof(bool), typeof(FeatureTabControl), new PropertyMetadata(false));


    /// <summary>
    /// When false (default) the tab headers stay on a single, horizontally-scrollable row with
    /// left/right scroll buttons. When true the headers wrap onto multiple rows instead of scrolling.
    /// </summary>
    public bool UseMultiRowTabs
    {
        get { return (bool)GetValue(UseMultiRowTabsProperty); }
        set { SetValue(UseMultiRowTabsProperty, value); }
    }
    public static readonly DependencyProperty UseMultiRowTabsProperty =
        DependencyProperty.Register(nameof(UseMultiRowTabs), typeof(bool), typeof(FeatureTabControl), new PropertyMetadata(false));

}
