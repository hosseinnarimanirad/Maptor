using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Controls.Security;

public class SecurityInputUserControl : UserControl
{
    public Brush InputBorderBrush
    {
        get { return (Brush)GetValue(InputBorderBrushProperty); }
        set { SetValue(InputBorderBrushProperty, value); }
    }

    public static readonly DependencyProperty InputBorderBrushProperty =
        DependencyProperty.Register(nameof(InputBorderBrush), typeof(Brush), typeof(SecurityInputUserControl), new PropertyMetadata(null));


    public Thickness InputBorderThickness
    {
        get { return (Thickness)GetValue(InputBorderThicknessProperty); }
        set { SetValue(InputBorderThicknessProperty, value); }
    }

    public static readonly DependencyProperty InputBorderThicknessProperty =
        DependencyProperty.Register(nameof(InputBorderThickness), typeof(Thickness), typeof(SecurityInputUserControl), new PropertyMetadata(new Thickness()));

}
