using System;
using System.Collections.Generic;
using IRI.Maptor.Ket.VersioningPersistence;
using Xunit;

namespace IRI.Maptor.Tst.Main.Versioning;

public class Versioning_CanonicalSerializerTest
{
    [Fact]
    public void Serialize_IsByteStableRegardlessOfInsertionOrder()
    {
        var a = new Dictionary<string, object?> { ["voltage"] = 230, ["name"] = "line-1", ["active"] = true };
        var b = new Dictionary<string, object?> { ["active"] = true, ["name"] = "line-1", ["voltage"] = 230 };

        Assert.Equal(CanonicalAttributeSerializer.Serialize(a), CanonicalAttributeSerializer.Serialize(b));
    }

    [Fact]
    public void Serialize_SortsKeysOrdinally()
    {
        var json = CanonicalAttributeSerializer.Serialize(new Dictionary<string, object?>
        {
            ["b"] = 1,
            ["A"] = 2,
            ["a"] = 3,
        });

        // Ordinal order: 'A' (65) < 'a' (97) < 'b' (98)
        Assert.Equal("""{"A":2,"a":3,"b":1}""", json);
    }

    [Fact]
    public void RoundTrip_PreservesPrimitives()
    {
        var source = new Dictionary<string, object?>
        {
            ["nothing"] = null,
            ["flag"] = true,
            ["count"] = 42L,
            ["length"] = 12.5d,
            ["name"] = "پست برق", // Persian text must survive untouched
        };

        var roundTripped = CanonicalAttributeSerializer.Deserialize(CanonicalAttributeSerializer.Serialize(source));

        Assert.Null(roundTripped["nothing"]);
        Assert.Equal(true, roundTripped["flag"]);
        Assert.Equal(42L, roundTripped["count"]);
        Assert.Equal(12.5d, roundTripped["length"]);
        Assert.Equal("پست برق", roundTripped["name"]);
    }

    [Fact]
    public void Serialize_WritesUtcDatesAsIso8601()
    {
        var json = CanonicalAttributeSerializer.Serialize(new Dictionary<string, object?>
        {
            ["at"] = new DateTime(2026, 8, 13, 10, 30, 0, DateTimeKind.Utc),
        });

        Assert.Equal("""{"at":"2026-08-13T10:30:00.0000000Z"}""", json);
    }

    [Fact]
    public void Serialize_LeavesUnspecifiedKindDatesUnshifted()
    {
        var json = CanonicalAttributeSerializer.Serialize(new Dictionary<string, object?>
        {
            ["at"] = new DateTime(2026, 8, 13, 10, 30, 0, DateTimeKind.Unspecified),
        });

        // No zone conversion may be applied to a value whose zone was never declared.
        Assert.Equal("""{"at":"2026-08-13T10:30:00.0000000"}""", json);
    }
}
