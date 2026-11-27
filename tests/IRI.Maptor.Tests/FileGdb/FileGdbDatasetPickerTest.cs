using System.IO;
using System.Linq;

using IRI.Maptor.Core.FileGdb;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Presentation.Wpf.Models.FileGdb;
using IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.FileGdb;

/// <summary>
/// The file geodatabase picker's rows (<see cref="FileGdbDatasetPickerItem"/>) and rules
/// (<see cref="FileGdbDatasetPickerViewModel"/>) on the synthetic geodatabase of <see cref="FileGdbDataSourceTest"/>.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class FileGdbDatasetPickerTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "maptor-gdb-picker-" + Guid.NewGuid().ToString("N"));

    private readonly FileGeodatabase _gdb;

    public FileGdbDatasetPickerTest()
    {
        var folder = Path.Combine(_root, "Test.gdb");
        Directory.CreateDirectory(folder);
        FileGdbDataSourceTest.WriteGeodatabase(folder);
        _gdb = FileGeodatabase.Open(folder);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private FileGdbDatasetPickerItem Item(string name, long? rowLimit = null) => FileGdbDatasetPickerItem.Create(_gdb.GetDataset(name)!, rowLimit);

    private FileGdbDatasetPickerViewModel Picker() => new("Test.gdb", _gdb.Datasets.Select(d => FileGdbDatasetPickerItem.Create(d, null)).ToList());

    [Fact]
    public void Rows_SayWhatCanBeAdded_AndWhy()
    {
        var parcels = Item("Parcels");
        Assert.True(parcels.IsAvailable);
        Assert.False(parcels.NeedsSrs);
        Assert.Equal("Land parcels", parcels.Title);
        Assert.True(parcels.ShowsName);
        Assert.Equal("Cadastre", parcels.GroupName);
        Assert.Contains("5", parcels.RowCountText);
        Assert.NotEmpty(parcels.GeometryTypeText);

        var wells = Item("Wells");
        Assert.True(wells.IsAvailable);
        Assert.False(wells.ShowsName);
        Assert.NotEqual("Cadastre", wells.GroupName);                // the root's label

        var plots = Item("Plots");
        Assert.True(plots.IsAvailable);
        Assert.True(plots.NeedsSrs);

        // each with its own reason
        var reasons = new[] { "Owners", "Archive", "Patches" }.Select(n => Item(n)).ToList();
        Assert.All(reasons, r => Assert.False(r.IsAvailable));

        // every text is found: a missing key comes back as "#key"
        Assert.All(new[] { parcels, wells, plots }.Concat(reasons),
                   r => Assert.All(new[] { r.GroupName, r.GeometryTypeText, r.RowCountText, r.UnavailableReason ?? string.Empty },
                                   text => Assert.False(text.StartsWith("#"), text)));
        Assert.Equal(3, reasons.Select(r => r.UnavailableReason).Distinct().Count());

        // over the row limit: blocked, with its own reason
        var large = Item("Parcels", rowLimit: 4);
        Assert.False(large.IsAvailable);
        Assert.DoesNotContain(large.UnavailableReason, reasons.Select(r => r.UnavailableReason));
    }

    [Fact]
    public void UnavailableRows_CannotBeTicked()
    {
        var owners = Item("Owners");

        owners.IsSelected = true;

        Assert.False(owners.IsSelected);
    }

    [Fact]
    public void Picker_TicksEverythingAvailable_UpToTheLimit()
    {
        WpfTestHost.Run(() =>
        {
            var picker = Picker();

            Assert.Equal(3, picker.AvailableCount);
            Assert.Equal(3, picker.SelectedCount);
            Assert.Equal(true, picker.AreAllSelected);

            // the root first (catalog order), then the feature datasets
            Assert.Equal(new[] { "Owners", "Archive", "Patches", "Wells", "Plots", "Parcels" }, picker.Items.Select(i => i.Name));

            // above the limit: nothing ticked at first
            var many = Enumerable.Range(0, FileGdbDatasetPickerViewModel.PreselectLimit + 1).Select(_ => Item("Wells")).ToList();
            var crowded = new FileGdbDatasetPickerViewModel("Big.gdb", many);

            Assert.Equal(0, crowded.SelectedCount);
            Assert.Equal(false, crowded.AreAllSelected);
            Assert.False(crowded.AddCommand.CanExecute(null));
            Assert.Equal(crowded.ValidationMessage, crowded.StatusText);
        });
    }

    [Fact]
    public void SelectAll_AndSome()
    {
        WpfTestHost.Run(() =>
        {
            var picker = Picker();

            picker.AreAllSelected = false;
            Assert.Equal(0, picker.SelectedCount);
            Assert.NotNull(picker.ValidationMessage);

            picker.Items.Single(i => i.Name == "Wells").IsSelected = true;
            Assert.Null(picker.AreAllSelected);                      // some
            Assert.Null(picker.ValidationMessage);
            Assert.True(picker.AddCommand.CanExecute(null));

            // a click on the "some" box ticks all (a two-state box goes from null to true)
            picker.AreAllSelected = true;
            Assert.Equal(3, picker.SelectedCount);
        });
    }

    [Fact]
    public void CrsChoice_OnlyWhenATickedRowNeedsIt()
    {
        WpfTestHost.Run(() =>
        {
            var picker = Picker();

            Assert.True(picker.ShowsSrsChoice);

            picker.SrsChoice.SelectedSrsOption = picker.SrsChoice.AvailableSrsOptions.Single(o => o.IsUtm);
            picker.SrsChoice.UtmZone = 39;
            Assert.Equal(SridHelper.GetUtmSrid(39), picker.SrsChoice.EffectiveSrid);

            picker.AddCommand.Execute(null);

            Assert.Equal(true, picker.DialogResult);
            Assert.Equal(SridHelper.GetUtmSrid(39), picker.Result!.Value.FallbackSrid);
            Assert.Equal(new[] { "Wells", "Plots", "Parcels" }, picker.Result.Value.Datasets.Select(d => d.Name));

            // without Plots ticked: no CRS card, no fallback SRID
            var other = Picker();
            other.Items.Single(i => i.Name == "Plots").IsSelected = false;

            Assert.False(other.ShowsSrsChoice);

            other.AddCommand.Execute(null);
            Assert.Null(other.Result!.Value.FallbackSrid);
        });
    }

    /// <summary>
    /// The picker window and its CRS card build, and their rows lay out: the XAML parses and every StaticResource,
    /// those in the row template included, resolves. Built and measured only, never shown (as ImportOptionsDialogViewTest).
    /// </summary>
    [Fact]
    public void PickerView_Builds_AndLaysOutItsRows()
    {
        WpfTestHost.Run(() =>
        {
            var cache = typeof(IRI.Maptor.Presentation.Wpf.Helpers.SharedResourceDictionary)
                .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .GetValue(null) as System.Collections.IDictionary;

            lock (cache!)
                cache.Clear();

            WpfTestHost.EnsureMahAppsResources();

            // the window's own Style comes from the application, as in the apps
            var styles = new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/IRI.Maptor.Presentation.Wpf;component/Assets/Styles/Controls.All.xaml", UriKind.Absolute) };
            var merged = System.Windows.Application.Current.Resources.MergedDictionaries;
            merged.Add(styles);

            IRI.Maptor.Presentation.Wpf.Controls.Dialogs.FileGdbDatasetPickerDialogView? view = null;

            try
            {
                var picker = Picker();

                // shown off-screen, without focus, so the row template is applied as in the app
                view = new IRI.Maptor.Presentation.Wpf.Controls.Dialogs.FileGdbDatasetPickerDialogView
                {
                    DataContext = picker,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    ShowActivated = false,
                    ShowInTaskbar = false,
                };

                view.Show();
                view.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                // one container per row: the row template was applied
                var list = FindChild<System.Windows.Controls.ItemsControl>(view, c => c.ItemsSource == picker.View);
                Assert.Equal(picker.Items.Count, list.Items.Count);
                Assert.All(picker.Items, i => Assert.NotNull(list.ItemContainerGenerator.ContainerFromItem(i)));

                // the CRS card is there for Plots, and laid out
                var srs = FindChild<IRI.Maptor.Presentation.Wpf.Controls.Dialogs.SrsChoiceView>(view, _ => true);
                Assert.True(srs.ActualHeight > 0);
            }
            finally
            {
                view?.Close();
                merged.Remove(styles);
            }
        });
    }

    private static T FindChild<T>(System.Windows.DependencyObject parent, Func<T, bool> match) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is T typed && match(typed))
                return typed;

            if (FindChildOrNull(child, match) is T found)
                return found;
        }

        throw new Xunit.Sdk.XunitException($"No {typeof(T).Name} found.");
    }

    private static T? FindChildOrNull<T>(System.Windows.DependencyObject parent, Func<T, bool> match) where T : System.Windows.DependencyObject
    {
        try { return FindChild(parent, match); } catch (Xunit.Sdk.XunitException) { return null; }
    }

    [Fact]
    public void Cancel_HasNoResult()
    {
        WpfTestHost.Run(() =>
        {
            var picker = Picker();
            var closed = false;
            picker.RequestClose = () => closed = true;

            picker.CancelCommand.Execute(null);

            Assert.True(closed);
            Assert.Null(picker.DialogResult);
            Assert.Null(picker.Result);
        });
    }
}
