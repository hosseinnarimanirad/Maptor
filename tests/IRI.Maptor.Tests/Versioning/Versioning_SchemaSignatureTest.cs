using System.Linq;
using IRI.Maptor.Core.Versioning;
using Xunit;

namespace IRI.Maptor.Tests.Versioning;

public class Versioning_SchemaSignatureTest
{
    private static readonly FieldSignature[] _fields =
    {
        new("voltage", "int", true),
        new("name", "nvarchar(50)", false),
        new("SHAPE", "geography", true),
    };

    [Fact]
    public void Calculate_IsOrderIndependent()
    {
        var reversed = _fields.Reverse().ToArray();

        Assert.Equal(SchemaSignatureCalculator.Calculate(_fields), SchemaSignatureCalculator.Calculate(reversed));
    }

    [Fact]
    public void Calculate_Produces32HexChars()
    {
        var signature = SchemaSignatureCalculator.Calculate(_fields);

        Assert.Equal(32, signature.Length);
        Assert.Matches("^[0-9a-f]{32}$", signature);
    }

    [Fact]
    public void Calculate_ChangesWhenAFieldChanges()
    {
        var original = SchemaSignatureCalculator.Calculate(_fields);

        var renamed = new[] { _fields[0], new FieldSignature("full_name", "nvarchar(50)", false), _fields[2] };
        var retyped = new[] { new FieldSignature("voltage", "bigint", true), _fields[1], _fields[2] };
        var nullabilityFlipped = new[] { new FieldSignature("voltage", "int", false), _fields[1], _fields[2] };
        var fieldRemoved = new[] { _fields[0], _fields[1] };

        Assert.NotEqual(original, SchemaSignatureCalculator.Calculate(renamed));
        Assert.NotEqual(original, SchemaSignatureCalculator.Calculate(retyped));
        Assert.NotEqual(original, SchemaSignatureCalculator.Calculate(nullabilityFlipped));
        Assert.NotEqual(original, SchemaSignatureCalculator.Calculate(fieldRemoved));
    }
}
