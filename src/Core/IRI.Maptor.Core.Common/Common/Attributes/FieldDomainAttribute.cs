using System;

using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Common.Attributes;

/// <summary>
/// Gives a property's <see cref="Field.Domain"/> when <see cref="Field.GetFields{T}"/> describes it. An application
/// derives from it to attach its own domains (e.g. a data model's coded-value domains); without one, a
/// <see cref="FieldAttribute.AllowedValues"/> list becomes a plain domain.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public abstract class FieldDomainAttribute : Attribute
{
    /// <summary>The domain; null when it cannot be found (the field is then described without one).</summary>
    public abstract FieldDomain? GetDomain();
}
