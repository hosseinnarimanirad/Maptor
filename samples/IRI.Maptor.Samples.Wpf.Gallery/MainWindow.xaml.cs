using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using IRI.Maptor.Samples.Wpf.Gallery.Shell;

namespace IRI.Maptor.Samples.Wpf.Gallery;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var view = new ListCollectionView((System.Collections.IList)SampleCatalog.All);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SampleInfo.Category)));

        samplesList.ItemsSource = view;
        samplesList.SelectedIndex = 0;
    }

    private void OnSampleSelected(object sender, SelectionChangedEventArgs e)
    {
        if (samplesList.SelectedItem is not SampleInfo sample)
            return;

        sampleTitle.Text = sample.Title;
        sampleDescription.Text = sample.Description;
        sampleSource.NavigateUri = new Uri(sample.SourceUrl);
        sampleHost.Content = sample.View;
    }

    private void OnNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
