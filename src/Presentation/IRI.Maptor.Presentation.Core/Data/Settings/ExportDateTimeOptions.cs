namespace IRI.Maptor.Presentation.Core.Data;

/// <summary>Calendar of the date/time columns written by the feature table's Excel export.</summary>
/// <remarks>Persisted as its number in the settings file: never renumber.</remarks>
public enum ExportCalendar
{
    Persian = 0,
    Gregorian = 1,
}

/// <summary>Time zone of the date/time columns written by the feature table's Excel export.</summary>
/// <remarks>Persisted as its number in the settings file: never renumber.</remarks>
public enum ExportTimeZone
{
    /// <summary>Iran Standard Time.</summary>
    Iran = 0,

    Utc = 1,

    /// <summary>The time zone of this computer.</summary>
    Local = 2,
}
