using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Wpf.ViewModels.Versioning;

namespace IRI.Maptor.Jab.Controls.Versioning;

public partial class CompetitionCompareView : UserControl
{
    public CompetitionCompareView()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        BuildAttributeColumns();
    }

    /// <summary>
    /// The N-way grid has one column per proposal, unknown until runtime — columns are
    /// rebuilt from the view model here. Changed cells get a highlight via a per-column
    /// style triggered on the matching Cells[i].IsChanged flag.
    /// </summary>
    private void BuildAttributeColumns()
    {
        attributeGrid.Columns.Clear();

        if (DataContext is not CompetitionCompareViewModel viewModel)
            return;

        attributeGrid.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationManager.Instance["versioning_compare_field"],
            Binding = new Binding(nameof(AttributeCompareRowViewModel.FieldName)),
            FontWeight = FontWeights.SemiBold,
            Width = new DataGridLength(160),
        });

        attributeGrid.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationManager.Instance["versioning_compare_live"],
            Binding = new Binding(nameof(AttributeCompareRowViewModel.LiveValue)) { TargetNullValue = "—" },
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
        });

        for (int i = 0; i < viewModel.Proposals.Count; i++)
        {
            var proposal = viewModel.Proposals[i];

            var elementStyle = new Style(typeof(TextBlock));

            var changedTrigger = new DataTrigger
            {
                Binding = new Binding($"Cells[{i}].IsChanged"),
                Value = true,
            };
            changedTrigger.Setters.Add(new Setter(TextBlock.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xCD))));
            changedTrigger.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            elementStyle.Triggers.Add(changedTrigger);

            attributeGrid.Columns.Add(new DataGridTextColumn
            {
                Header = proposal.IsDelete
                    ? $"{proposal.Header} ({LocalizationManager.Instance["versioning_review_badgeDelete"]})"
                    : proposal.Header,
                Binding = new Binding($"Cells[{i}].Value") { TargetNullValue = "—" },
                ElementStyle = elementStyle,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            });
        }
    }
}
