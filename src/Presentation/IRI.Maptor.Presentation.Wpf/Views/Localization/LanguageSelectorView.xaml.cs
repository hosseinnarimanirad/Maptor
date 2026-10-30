using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Interaction logic for LanguageSelectorView.xaml
/// </summary>
public partial class LanguageSelectorView : UserControl
{
    public LanguageSelectorView()
    {
        InitializeComponent();
    }

    // Close the hosting pop-up before the command switches the language (Click runs before the command), so the
    // window mirrors with no pop-up left open under the pointer, and a second click cannot change it again.
    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (Parent is Popup popup)
            popup.IsOpen = false;
    }
}
