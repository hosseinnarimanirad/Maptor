using System.Security.Cryptography;
using System.Text;

namespace IRI.Maptor.Sta.Versioning;

public readonly struct FieldSignature
{
    public FieldSignature(string fieldName, string storeType, bool isNullable)
    {
        FieldName = fieldName;
        StoreType = storeType;
        IsNullable = isNullable;
    }

    public string FieldName { get; }
    public string StoreType { get; }
    public bool IsNullable { get; }
}

/// <summary>
/// Hash of a layer's field schema. Each proposal stamps the signature at submission;
/// a mismatch against the layer's current signature means the serialized attributes may
/// no longer fit the table (schema drift) and the commit gate must map or block.
/// </summary>
public static class SchemaSignatureCalculator
{
    /// <summary>
    /// Field order does not matter (sorted ordinally here); casing of names does.
    /// Extraction from the EF model lives in Ket.VersioningPersistence.
    /// </summary>
    public static string Calculate(IEnumerable<FieldSignature> fields)
    {
        var canonical = string.Join(
            ";",
            fields
                .OrderBy(f => f.FieldName, StringComparer.Ordinal)
                .Select(f => $"{f.FieldName}:{f.StoreType}:{(f.IsNullable ? "1" : "0")}"));

        using var sha = SHA256.Create();

        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));

        var builder = new StringBuilder(32);

        // 16 bytes → 32 hex chars: plenty for drift detection, compact in storage
        for (int i = 0; i < 16; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }

        return builder.ToString();
    }
}
