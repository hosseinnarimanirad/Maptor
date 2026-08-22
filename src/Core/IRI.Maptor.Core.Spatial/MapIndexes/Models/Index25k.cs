using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.Spatial.MapIndexes;

public class Index25k : IndexBase
{
    public override double Height { get => GeodeticIndexes._25kSize; }

    public override double Width { get => GeodeticIndexes._25kSize; }

    public string ShortSheetNumber { get { return SheetNumber?.Split(' ').LastOrDefault() ?? string.Empty; } }

    public override Feature<Point> AsFeature()
    {
        return new Feature<Point>()
        {
            TheGeometry = TheGeometry,
            LabelAttribute = nameof(SheetNumber),
            Attributes = new Dictionary<string, object>()
            {
                {nameof(Height), Height },
                {nameof(Id), Id},
                {nameof(MinLatitude), MinLatitude},
                {nameof(MinLongitude), MinLongitude },
                {nameof(SheetNameEn), SheetNameEn },
                {nameof(SheetNameFa), SheetNameFa },
                {nameof(SheetNumber), SheetNumber },
                {nameof(Width), Width },
                {nameof(ShortSheetNumber), ShortSheetNumber},
            },
        };
    }
}
