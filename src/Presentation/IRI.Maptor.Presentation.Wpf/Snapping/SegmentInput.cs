using System.Globalization;

using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Presentation.Wpf.Snapping;

/// <summary>A value of the segment being drawn that can be typed.</summary>
public enum SegmentField
{
    /// <summary>The ground length in metres.</summary>
    Length = 1,

    /// <summary>The turn from the previous segment in degrees, positive to the right.</summary>
    Angle = 2,

    /// <summary>The direction in degrees clockwise from north.</summary>
    Bearing = 3,
}

/// <summary>
/// The values typed for the segment being drawn: which field is being typed, its text, and the
/// <see cref="SegmentConstraint"/> the texts amount to (updated at every key, so the sketch follows the typing).
/// It turns typed characters into values; it does not read the keyboard, work out the point or draw.
/// <para>
/// The angle and the bearing are two ways to give one direction: starting one drops the other.
/// </para>
/// </summary>
public sealed class SegmentInput
{
    private const int MaxLength = 12;

    private string _length = string.Empty;
    private string _angle = string.Empty;

    /// <summary>What the typed values fix; free where nothing (valid) is typed.</summary>
    public SegmentConstraint Constraint { get; } = new();

    /// <summary>The field being typed, or null.</summary>
    public SegmentField? Editing { get; private set; }

    /// <summary>True while a field is being typed or a typed value is in force.</summary>
    public bool IsActive => Editing is not null || Constraint.IsActive;

    /// <summary>The text typed for <paramref name="field"/>; empty when nothing is, or when the direction was given the other way.</summary>
    public string TextOf(SegmentField field) => field switch
    {
        SegmentField.Length => _length,
        SegmentField.Bearing => Constraint.AngleIsBearing ? _angle : string.Empty,
        _ => Constraint.AngleIsBearing ? string.Empty : _angle,
    };

    /// <summary>Starts typing <paramref name="field"/> afresh (its earlier text is dropped; the other value is kept).</summary>
    public void Begin(SegmentField field)
    {
        Editing = field;

        if (field == SegmentField.Length)
        {
            _length = string.Empty;
        }
        else
        {
            _angle = string.Empty;
            Constraint.AngleIsBearing = field == SegmentField.Bearing;
        }

        Apply();
    }

    /// <summary>
    /// Adds a character to the field being typed: a digit (Latin, Persian or Arabic), a decimal point, or a leading
    /// minus for a turn angle. False when no field is being typed or the character does not fit.
    /// </summary>
    public bool Type(char character)
    {
        if (Editing is not { } field)
            return false;

        var text = field == SegmentField.Length ? _length : _angle;

        if (text.Length >= MaxLength)
            return false;

        if (char.IsDigit(character))
        {
            text += (char)('0' + (int)char.GetNumericValue(character));
        }
        else if (character is '.' or '٫' or '/')
        {
            // '/' is the decimal mark on a Persian keyboard's numeric row
            if (text.Contains('.'))
                return false;

            text += '.';
        }
        else if (character == '-')
        {
            if (field != SegmentField.Angle || text.Length > 0)
                return false;

            text = "-";
        }
        else
        {
            return false;
        }

        Set(field, text);

        return true;
    }

    /// <summary>Removes the last character of the field being typed. False when no field is being typed.</summary>
    public bool Backspace()
    {
        if (Editing is not { } field)
            return false;

        var text = field == SegmentField.Length ? _length : _angle;

        if (text.Length > 0)
            Set(field, text.Substring(0, text.Length - 1));

        return true;
    }

    /// <summary>
    /// Moves on to the other value, keeping what is typed: from the length to the direction, and back.
    /// </summary>
    /// <param name="turnAvailable">False for the first segment of a part, where the direction can only be a bearing.</param>
    public void Next(bool turnAvailable)
    {
        if (Editing == SegmentField.Length)
        {
            if (_angle.Length == 0)
                Constraint.AngleIsBearing = !turnAvailable;

            Editing = Constraint.AngleIsBearing ? SegmentField.Bearing : SegmentField.Angle;
        }
        else
        {
            Editing = SegmentField.Length;
        }
    }

    /// <summary>Drops everything typed.</summary>
    public void Clear()
    {
        Editing = null;
        _length = string.Empty;
        _angle = string.Empty;

        Constraint.Clear();
        Constraint.AngleIsBearing = false;
    }

    private void Set(SegmentField field, string text)
    {
        if (field == SegmentField.Length)
            _length = text;
        else
            _angle = text;

        Apply();
    }

    private void Apply()
    {
        Constraint.Length = Parse(_length) is > 0 and var length ? length : null;
        Constraint.Angle = Parse(_angle);
    }

    private static double? Parse(string text)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && !double.IsInfinity(value) ? value : null;
}
