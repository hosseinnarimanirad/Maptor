using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IRI.Maptor.Core.Topology.Findings;

/// <summary>
/// What makes two findings "the same issue": the rule, the kind, the features and the discriminator, but not
/// the location, the value or the message. A tower that moves from 30 m to 2 m off its route is still the same
/// issue.
/// </summary>
public static class IssueIdentity
{
    /// <summary>
    /// The identity as one canonical line: <c>rule|kind|layer:id;layer:id;…|discriminator</c>, features sorted
    /// by layer name, then id. Invariant culture, so every computer writes the same line.
    /// </summary>
    public static string CanonicalLine(TopologyFinding finding)
    {
        if (finding is null)
            throw new ArgumentNullException(nameof(finding));

        var features = finding.Features
            .Select(f => (f.EntityName, f.FeatureId))
            .Distinct()
            .OrderBy(f => f.EntityName, StringComparer.Ordinal)
            .ThenBy(f => f.FeatureId)
            .Select(f => f.EntityName + ":" + f.FeatureId.ToString(CultureInfo.InvariantCulture));

        return string.Join("|", finding.RuleId, finding.Kind, string.Join(";", features), finding.Discriminator ?? string.Empty);
    }

    /// <summary>The SHA-256 of <see cref="CanonicalLine"/>, as 64 lower-case hex digits.</summary>
    public static string Hash(TopologyFinding finding)
    {
        var bytes = Encoding.UTF8.GetBytes(CanonicalLine(finding));

        using var sha = SHA256.Create();

        var hash = sha.ComputeHash(bytes);

        var text = new StringBuilder(hash.Length * 2);

        foreach (var b in hash)
            text.Append(b.ToString("x2", CultureInfo.InvariantCulture));

        return text.ToString();
    }
}
