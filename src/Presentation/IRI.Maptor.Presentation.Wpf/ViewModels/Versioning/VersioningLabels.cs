using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Core.Versioning;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Versioning;

/// <summary>
/// Localized captions for the versioning enums. Views must never bind an enum directly:
/// a raw ProposalChangeType renders as "Update" inside an otherwise Persian column.
/// </summary>
public static class VersioningLabels
{
    public static string ChangeType(ProposalChangeType changeType) => changeType switch
    {
        ProposalChangeType.Create => LocalizationManager.Instance["versioning_changeType_create"],
        ProposalChangeType.Update => LocalizationManager.Instance["versioning_changeType_update"],
        ProposalChangeType.Delete => LocalizationManager.Instance["versioning_changeType_delete"],
        _ => string.Empty,
    };
}
