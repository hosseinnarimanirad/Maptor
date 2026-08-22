using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Services;

public class OdResult
{
    public Metadata metadata { get; set; }
    public Result[] results { get; set; }

    public override string ToString()
    {
        return string.Join(System.Environment.NewLine, results.Select((i, index) => $"results({index}): \n{i.ToString()}"));
    }

    public string AsFlatString()
    {
        return string.Join(System.Environment.NewLine, results.Select(i => i.AsFlatString()));
    }
}

public class Metadata
{
    public string provider { get; set; }
}

public class Result
{
    public string id { get; set; }
    public string language { get; set; }
    public Lexicalentry[] lexicalEntries { get; set; }
    public string type { get; set; }
    public string word { get; set; }

    public override string ToString()
    {
        return $"{word}\n {string.Join(System.Environment.NewLine, lexicalEntries.Select((i, index) => $"  {i.GetPhonetic()} {i.lexicalCategory} :\n{i.ToString()}"))}";
    }

    public string AsFlatString()
    {
        return $"{word}\n {string.Join(System.Environment.NewLine, lexicalEntries.Select(i => $"{i.GetPhonetic()} {i.lexicalCategory} :\n{i.AsFlatString()}"))}";
    }
}

public class Lexicalentry
{
    public Entry[] entries { get; set; }
    public string language { get; set; }
    public string lexicalCategory { get; set; }
    public Pronunciation[] pronunciations { get; set; }
    public string text { get; set; }

    public override string ToString()
    {
        return string.Join(System.Environment.NewLine, entries.Select((i, index) => $"\tentries({index}) {i.GetPhonetic()}:\n {i.ToString()}"));
    }

    public string AsFlatString()
    {
        return string.Join(System.Environment.NewLine, entries.Select(i => $"{i.GetPhonetic()}{i.AsFlatString()}"));
    }

    public string GetPhonetic()
    {
        if (pronunciations == null)
        {
            return string.Empty;
        }
        else
        {
            return string.Join(", ", pronunciations?.Select(i => $"/{i.ToString()}/"));
        }
    }
}

public class Entry
{
    public string[] etymologies { get; set; }
    public Grammaticalfeature[] grammaticalFeatures { get; set; }
    public string homographNumber { get; set; }
    public Pronunciation[] pronunciations { get; set; }
    public Sens[] senses { get; set; }

    public override string ToString()
    {
        return string.Join(System.Environment.NewLine, senses.Select((i, index) => $"\t\tsense({index}):\n {i.ToString()}"));
    }

    public string AsFlatString()
    {
        return string.Join(System.Environment.NewLine, senses.Select(i => $"{i.AsFlatString()}\n"));
    }


    public string GetPhonetic()
    {
        if (pronunciations == null)
        {
            return string.Empty;
        }
        else
        {
            return string.Join(", ", pronunciations?.Select(i => $"/{i.ToString()}/"));
        }
    }
}

public class Grammaticalfeature
{
    public string text { get; set; }
    public string type { get; set; }
}


//A sense is a particular meaning of a word.
public class Sens
{
    public string[] definitions { get; set; }
    public Example[] examples { get; set; }
    public string id { get; set; }
    public Sens[] subsenses { get; set; }
    public string[] regions { get; set; }
    public string[] registers { get; set; }
    public string[] crossReferenceMarkers { get; set; }
    public Crossreference[] crossReferences { get; set; }
    public string[] domains { get; set; }

    public override string ToString()
    {
        List<string> values = new List<string>();

        if (definitions != null)
        {
            values.AddRange(definitions?.Select((i, index) => $"\t\t\tdefinition({index}): {i}"));
        }

        if (examples != null)
        {
            values.AddRange(examples.Select((i, index) => $"\t\t\texample({index}): {i}"));
        }

        if (subsenses != null)
        {
            values.AddRange(subsenses.Select((i, index) => $"\n\t\t\tsubSense({index}):\n {i.ToString()}"));
        }

        if (crossReferences != null)
        {
            values.AddRange(crossReferences.Select((i, index) => $"\t\t\tcrossReferences({index}): {i.ToString()}"));
        }

        return string.Join(System.Environment.NewLine, values);
    }

    public string AsFlatString()
    {
        List<string> values = new List<string>();

        if (definitions != null)
        {
            values.AddRange(definitions.Select(i => $"\t{i.ToString()}"));
            //values[0] = $"{values[0]}";
        }

        if (examples != null)
        {
            values.AddRange(examples.Select(i => $"\t\t{i.ToString()}"));
        }

        if (subsenses != null)
        {
            values.AddRange(subsenses.Select(i => $"\n{i.AsFlatString()}"));
        }

        if (crossReferences != null)
        {
            values.AddRange(crossReferences.Select(i => $"\t{i.ToString()}"));
        }

        //if (string.IsNullOrEmpty(values?.Last()))
        //{
        //    values.Add(string.Empty);
        //}

        return string.Join(System.Environment.NewLine, values);
    }
}

//public class Subsens
//{
//    public string[] definitions { get; set; }
//    public Example[] examples { get; set; }
//    public string id { get; set; }
//    public string[] domains { get; set; }
//    public string[] registers { get; set; }

//    public override string ToString()
//    {
//        var defs = definitions?.Select((i, index) => $"\t\t\t\tdefinition{index}: {i}");

//        var exs = examples?.Select((i, index) => $"\t\t\t\texample({index}): {i}");

//        return string.Join(System.Environment.NewLine, defs.Concat(exs ?? new List<string>()));

//    }
//}

public class Example
{
    public string text { get; set; }

    public override string ToString()
    {
        return text;
    }
}

public class Crossreference
{
    public string id { get; set; }
    public string text { get; set; }
    public string type { get; set; }

    public override string ToString()
    {
        return $"{type}; {text}";
    }
}


public class Pronunciation
{
    public string audioFile { get; set; }
    public string[] dialects { get; set; }
    public string phoneticNotation { get; set; }
    public string phoneticSpelling { get; set; }

    public override string ToString()
    {
        return phoneticSpelling;
    }
}
