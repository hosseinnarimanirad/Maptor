using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Mathematics;

public class ClassAttributeGroup<TAttribute, TClass>
{
    public TAttribute? Key { get { return Attributes is null ? default : Attributes.FirstOrDefault(); } }

    public List<TAttribute>? Attributes { get; set; }

    public List<ClassFrequency<TClass>>? Classes { get; set; }

    public ClassAttributeGroup(List<TAttribute>? attributes, List<ClassFrequency<TClass>>? classes)
    {
        this.Attributes = attributes;

        this.Classes = classes;
    }

    public ClassAttributeGroup(List<TAttribute>? attributes, List<TClass>? classes)
    {
        this.Classes = classes?.GroupBy(c => c)?.Select(c => new ClassFrequency<TClass>(c.Key, c.Count()))?.ToList();

        this.Attributes = attributes;
    }

    public static ClassAttributeGroup<TAttribute, TClass> Merge(ClassAttributeGroup<TAttribute, TClass> first, ClassAttributeGroup<TAttribute, TClass> second)
    {
        var attributes = first.Attributes?.Union(second.Attributes)?.ToList();

        var classes = first.Classes.Union(second.Classes).GroupBy(c => c.Class).Select(c => new ClassFrequency<TClass>(c.Key, c.Sum(i => i.Count)))?.ToList();

        return new ClassAttributeGroup<TAttribute, TClass>(attributes, classes);
    }

    public static ClassAttributeGroup<TAttribute, TClass> Merge(IEnumerable<ClassAttributeGroup<TAttribute, TClass>> list)
    {
        var attributes = list.SelectMany(s => s.Attributes)?.ToList();

        var classes = list.SelectMany(s => s.Classes).GroupBy(c => c.Class).Select(c => new ClassFrequency<TClass>(c.Key, c.Sum(i => i.Count)))?.ToList();

        return new ClassAttributeGroup<TAttribute, TClass>(attributes, classes);
    }

    public override string ToString()
    {
        return $"[{string.Join(",", Attributes)}]-{string.Join("-", Classes)}";
    }
}
