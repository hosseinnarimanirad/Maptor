using IRI.Maptor.Extensions;
using IRI.Maptor.Core.SpatialReferenceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.IO.Prj;

public class EsriPrjTreeNode
{
    const string specialCharacter = "~";

    static readonly char[] trimCharacters = new char[] { '\0', ' ', '\n', '\r', '\"' };

    //e.g. GEOGCS
    public string Name { get; set; }

    public List<EsriPrjTreeNode> Children { get; set; }

    public List<string> Values { get; set; }

    public string ValueString
    {
        get
        {
            if (Values.Count > 0)
            {
                return string.Join(",", Values.Select((i, index) => index == 0 ? $"\"{i}\"" : i));
            }

            return string.Empty;
        }
    }

    public static EsriPrjTreeNode Parse(string wktString)
    {
        wktString = wktString.Replace(Environment.NewLine, string.Empty).Trim();

        var start = wktString.IndexOf('[');

        var end = wktString.LastIndexOf(']');

        var content = wktString.Substring(start + 1, end - start - 1);

        var name = wktString.Replace($"[{content}]", string.Empty);

        return new EsriPrjTreeNode(name, content);
    }

    public EsriPrjTreeNode()
    {

    }

    public EsriPrjTreeNode(string name, params string[] values)
    {
        Name = name;

        Values = values.ToList();
    }

    public EsriPrjTreeNode(IEllipsoid ellipsoid, string title, int srid)
    {
        Name = EsriPrjFile._geogcs;

        Values = new List<string>() { title ?? $"GCS_{ellipsoid.EsriName}" };

        //esri write zero for Inverse Flattening of spheres!
        var inverseFlattening = double.IsInfinity(ellipsoid.InverseFlattening) ? 0 : ellipsoid.InverseFlattening;

        var spheroid = new EsriPrjTreeNode(EsriPrjFile._spheroid, ellipsoid.EsriName, ellipsoid.SemiMajorAxis.Value.AsExactString(), inverseFlattening.AsExactString());

        var datum = new EsriPrjTreeNode(EsriPrjFile._datum, $"D_{ellipsoid.EsriName}")
        {
            Children = new List<EsriPrjTreeNode>() { spheroid }
        };

        var primem = new EsriPrjTreeNode(EsriPrjFile._primem, EsriPrjFile._greenwich, "0.0");

        var unit = new EsriPrjTreeNode(EsriPrjFile._unit, EsriPrjFile._degree, EsriPrjFile._degreeValue);

        Children = new List<EsriPrjTreeNode>() { datum, primem, unit };

        if (srid != 0)
        {
            var authority = new EsriPrjTreeNode(EsriPrjFile._authority, EsriPrjFile._epsg, srid.ToString());

            Children.Add(authority);
        }

    }

    private EsriPrjTreeNode(string name, string content)
    {
        Name = name.Replace(specialCharacter, string.Empty).Trim(trimCharacters);

        Children = new List<EsriPrjTreeNode>();

        Values = new List<string>();

        var matches = GetParts(content);

        string temporary = content;

        foreach (string match in matches)
        {
            temporary = temporary.Replace($"[{match}]", specialCharacter);
        }

        var parts = temporary.Split(',');

        int matchIndex = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Contains(specialCharacter))
            {
                Children.Add(new EsriPrjTreeNode(parts[i], matches[matchIndex]));

                matchIndex++;
            }
            else
            {
                Values.Add(parts[i].Trim(trimCharacters));
            }
        }

    }

    public override string ToString()
    {
        return $"Name: {Name}, Children: {Children.Count}, Values:{string.Join(",", Values)}";
    }

    public string AsEsriCrsWkt()
    {
        if (Children?.Count > 0)
        {
            return $"{Name}[{ValueString},{string.Join(",", Children.Select(i => i.AsEsriCrsWkt()))}]";
        }
        else
        {
            return $"{Name}[{ValueString}]";
        }
    }

    private static List<string> GetParts(string input)
    {
        var brackets = input.Select((i, index) => new Bracket { Character = i, Index = index }).Where(i => i.Character == ']' || i.Character == '[').ToList();

        int level = 0;

        for (int i = 0; i < brackets.Count; i++)
        {
            if (brackets[i].Character == '[')
            {
                brackets[i].Level = level;

                level++;
            }
            else
            {
                level--;

                brackets[i].Level = level;
            }
        }

        var zeroLevelBrackets = brackets.Where(i => i.Level == 0).ToList();

        List<Tuple<int, int>> ranges = new List<Tuple<int, int>>();

        for (int i = 0; i < zeroLevelBrackets.Count / 2; i++)
        {
            ranges.Add(new Tuple<int, int>(zeroLevelBrackets[2 * i].Index, zeroLevelBrackets[2 * i + 1].Index));
        }

        return ranges.Select(i => input.Substring(i.Item1 + 1, i.Item2 - i.Item1 - 1)).ToList();
    }


    private static EsriPrjTreeNode _meter;

    public static EsriPrjTreeNode MeterUnit
    {
        get
        {
            if (_meter == null)
            {
                _meter = new EsriPrjTreeNode()
                {
                    Name = EsriPrjFile._unit,
                    Values = new List<string>() { "Meter", "1.0" }
                };
            }

            return _meter;
        }
    }

    class Bracket
    {
        public int Level { get; set; }

        public char Character { get; set; }

        public int Index { get; set; }
    }
}

