using MahApps.Metro.IconPacks;
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

namespace IRI.Maptor.Jab.MultiSelectItem.View
{
    /// <summary>
    /// Interaction logic for MultiSelectItem.xaml
    /// </summary>
    public partial class MultiSelectItem : UserControl
    {
        public MultiSelectItem()
        {
            InitializeComponent();
        }


        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(MultiSelectItem), new PropertyMetadata(string.Empty));




        public int PathSize
        {
            get { return (int)GetValue(PathSizeProperty); }
            set { SetValue(PathSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathSizeProperty =
            DependencyProperty.Register(nameof(PathSize), typeof(int), typeof(MultiSelectItem), new PropertyMetadata(16));


        public PackIconModernKind PathKind
        {
            get { return (PackIconModernKind)GetValue(PathKindProperty); }
            set { SetValue(PathKindProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathKindProperty =
            DependencyProperty.Register("PathKind", typeof(PackIconModernKind), typeof(MultiSelectItem), new PropertyMetadata(PackIconModernKind.Add/*Geometry.Empty*/));




        public Brush ButtonBackground
        {
            get { return (Brush)GetValue(ButtonBackgroundProperty); }
            set { SetValue(ButtonBackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ButtonBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonBackgroundProperty =
            DependencyProperty.Register("ButtonBackground", typeof(Brush), typeof(MultiSelectItem), new PropertyMetadata(Brushes.Transparent));



        public Brush ButtonHighlight
        {
            get { return (Brush)GetValue(ButtonHighlightProperty); }
            set { SetValue(ButtonHighlightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ButtonHighlight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonHighlightProperty =
            DependencyProperty.Register("ButtonHighlight", typeof(Brush), typeof(MultiSelectItem), new PropertyMetadata(Brushes.Transparent));




        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(MultiSelectItem), new PropertyMetadata(null));




        public bool IsAndOrVisible
        {
            get { return (bool)GetValue(IsAndOrVisibleProperty); }
            set { SetValue(IsAndOrVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAndOrVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAndOrVisibleProperty =
            DependencyProperty.Register("IsAndOrVisible", typeof(bool), typeof(MultiSelectItem), new PropertyMetadata(false));

        public bool IsMultiSelectEnabled
        {
            get { return (bool)GetValue(IsMultiSelectEnabledProperty); }
            set { SetValue(IsMultiSelectEnabledProperty, value); }
        }

        //Using a DependencyProperty as the backing store for IsAndOrVisible.This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMultiSelectEnabledProperty =
            DependencyProperty.Register("IsMultiSelectEnabled", typeof(bool), typeof(MultiSelectItem), new PropertyMetadata(true, (sender, e) =>
            {
                var multiSelectItem = (sender as MultiSelectItem);

                if (multiSelectItem == null)
                    return;

                var context = multiSelectItem.DataContext as ViewModel.IMultiSelectItemViewModel;

                if (context == null)
                    return;

                context.IsMultiSelectEnabled = (bool)e.NewValue;
            }));


        public ICommand AddCommand
        {
            get { return (ICommand)GetValue(AddCommandProperty); }
            set { SetValue(AddCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddCommandProperty =
            DependencyProperty.Register("AddCommand", typeof(ICommand), typeof(MultiSelectItem), new PropertyMetadata(null));


        public ICommand RemoveCommand
        {
            get { return (ICommand)GetValue(RemoveCommandProperty); }
            set { SetValue(RemoveCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RemoveCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RemoveCommandProperty =
            DependencyProperty.Register("RemoveCommand", typeof(ICommand), typeof(MultiSelectItem), new PropertyMetadata(null));

        private void ComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var context = this.DataContext as ViewModel.IMultiSelectItemViewModel;

                if (context != null)
                {
                    context.AddItem();
                }
            }

        }
    }
}
