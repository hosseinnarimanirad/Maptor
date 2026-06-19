using System.Collections;
using System.Windows.Input;

using IRI.Maptor.Jab.Maui.Projects;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace IRI.Maptor.Jab.Maui.Controls;

/// <summary>
/// A panel that slides in over the <see cref="MapViewer"/> listing saved <see cref="Project"/>s.
/// Each row shows the project name and layer count; tapping it raises
/// <see cref="SelectProjectCommand"/> (the host loads that project's layers), and the trash
/// button raises <see cref="DeleteProjectCommand"/>. A "New project" button raises
/// <see cref="AddProjectCommand"/>. Drive <see cref="SlideOverSidebar.IsOpen"/> from the
/// toolbar's "more" button.
/// </summary>
public class ProjectsSidebar : SlideOverSidebar
{
    private readonly CollectionView _list;

    public ProjectsSidebar()
    {
        _list = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(CreateRow),
            EmptyView = new Label
            {
                Text = "No projects yet. Tap “＋ New project”.",
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 18),
                TextColor = SecondaryText,
            },
        };

        SetPanelContent(BuildPanelContent());
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ProjectsSidebar), null, propertyChanged: OnItemsSourceChanged);

    /// <summary>The saved projects to list.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty AddProjectCommandProperty = BindableProperty.Create(
        nameof(AddProjectCommand), typeof(ICommand), typeof(ProjectsSidebar), null);

    /// <summary>Raised by the "New project" button.</summary>
    public ICommand? AddProjectCommand
    {
        get => (ICommand?)GetValue(AddProjectCommandProperty);
        set => SetValue(AddProjectCommandProperty, value);
    }

    public static readonly BindableProperty SelectProjectCommandProperty = BindableProperty.Create(
        nameof(SelectProjectCommand), typeof(ICommand), typeof(ProjectsSidebar), null);

    /// <summary>Raised (with the tapped <see cref="Project"/>) when a row is selected.</summary>
    public ICommand? SelectProjectCommand
    {
        get => (ICommand?)GetValue(SelectProjectCommandProperty);
        set => SetValue(SelectProjectCommandProperty, value);
    }

    public static readonly BindableProperty DeleteProjectCommandProperty = BindableProperty.Create(
        nameof(DeleteProjectCommand), typeof(ICommand), typeof(ProjectsSidebar), null);

    /// <summary>Raised (with the <see cref="Project"/>) when a row's trash button is tapped.</summary>
    public ICommand? DeleteProjectCommand
    {
        get => (ICommand?)GetValue(DeleteProjectCommandProperty);
        set => SetValue(DeleteProjectCommandProperty, value);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((ProjectsSidebar)bindable)._list.ItemsSource = newValue as IEnumerable;
    }

    private View BuildPanelContent()
    {
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto }, // header
                new RowDefinition { Height = GridLength.Auto }, // new-project button
                new RowDefinition { Height = GridLength.Auto }, // divider
                new RowDefinition { Height = GridLength.Star },  // project list
            },
            RowSpacing = 10,
        };

        grid.Add(BuildHeader(), 0, 0);
        grid.Add(BuildAddButton(), 0, 1);
        grid.Add(new BoxView { HeightRequest = 1, Color = Divider }, 0, 2);
        grid.Add(_list, 0, 3);

        return grid;
    }

    private View BuildHeader()
    {
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
            },
        };

        var title = new Label
        {
            Text = "Projects",
            TextColor = PrimaryText,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
        };

        header.Add(title, 0);
        header.Add(CreateCloseButton(), 1);

        return header;
    }

    private View BuildAddButton()
    {
        var add = new Button
        {
            Text = "＋ New project",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = PrimaryText,
            BackgroundColor = Accent,
            HeightRequest = 42,
            CornerRadius = 6,
        };
        add.SetBinding(Button.CommandProperty, new Binding(nameof(AddProjectCommand), source: this));

        return add;
    }

    private View CreateRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto }, // icon
                new ColumnDefinition { Width = GridLength.Star },  // name + count
                new ColumnDefinition { Width = GridLength.Auto }, // delete
            },
            ColumnSpacing = 8,
            Padding = new Thickness(0, 8),
        };

        var icon = new Label
        {
            Text = "🗂",
            FontSize = 20,
            VerticalOptions = LayoutOptions.Center,
        };
        grid.Add(icon, 0);

        var name = new Label
        {
            TextColor = PrimaryText,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
        };
        name.SetBinding(Label.TextProperty, nameof(Project.Name));

        var count = new Label
        {
            TextColor = SecondaryText,
            FontSize = 12,
        };
        count.SetBinding(Label.TextProperty, new Binding($"{nameof(Project.Layers)}.Count", stringFormat: "{0} layers"));

        var textStack = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            Children = { name, count },
        };

        // Tap the row to open the project.
        var tap = new TapGestureRecognizer();
        tap.SetBinding(TapGestureRecognizer.CommandProperty, new Binding(nameof(SelectProjectCommand), source: this));
        tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
        textStack.GestureRecognizers.Add(tap);
        grid.Add(textStack, 1);

        var remove = new Button
        {
            Text = "🗑",
            FontSize = 16,
            BackgroundColor = Colors.Transparent,
            TextColor = SecondaryText,
            WidthRequest = 38,
            HeightRequest = 38,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center,
        };
        remove.SetBinding(Button.CommandProperty, new Binding(nameof(DeleteProjectCommand), source: this));
        remove.SetBinding(Button.CommandParameterProperty, new Binding("."));
        grid.Add(remove, 2);

        return grid;
    }
}
