using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using IRI.Maptor.Jab.Common;
using System;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for DegreeMinuteSecondView.xaml
/// </summary>
public partial class DegreeMinuteSecondView : NotifiableUserControl/*, IDisposable*/
{
    const int _defaultMinInputWidth = 90;

    public DegreeMinuteSecondView()
    {
        InitializeComponent();
    }



    public bool HideUpDownButtons
    {
        get { return (bool)GetValue(HideUpDownButtonsProperty); }
        set { SetValue(HideUpDownButtonsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for HideUpDownButtons.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty HideUpDownButtonsProperty =
        DependencyProperty.Register(nameof(HideUpDownButtons), typeof(bool), typeof(DegreeMinuteSecondView), new PropertyMetadata(false, new PropertyChangedCallback((dpo, dpca) =>
        {
            if ((bool)dpca.NewValue)
            {
                (dpo as DegreeMinuteSecondView).MinInputWidth = 44;
            }
            else
            {
                (dpo as DegreeMinuteSecondView).MinInputWidth = _defaultMinInputWidth;
            }
        })));



    public int MaxDegreeValue
    {
        get { return (int)GetValue(MaxDegreeValueProperty); }
        set { SetValue(MaxDegreeValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MaxDegreeValue.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MaxDegreeValueProperty =
        DependencyProperty.Register(nameof(MaxDegreeValue), typeof(int), typeof(DegreeMinuteSecondView), new PropertyMetadata(180));



    public int MinDegreeValue
    {
        get { return (int)GetValue(MinDegreeValueProperty); }
        set { SetValue(MinDegreeValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MinDegreeValue.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MinDegreeValueProperty =
        DependencyProperty.Register(nameof(MinDegreeValue), typeof(int), typeof(DegreeMinuteSecondView), new PropertyMetadata(-180));


    private int _minInputWidth = _defaultMinInputWidth;

    public int MinInputWidth
    {
        get { return _minInputWidth; }
        set
        {
            _minInputWidth = value;
            RaisePropertyChanged();
        }
    }
     
}
