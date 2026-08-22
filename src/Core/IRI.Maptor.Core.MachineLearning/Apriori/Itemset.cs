using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.MachineLearning
{
    public class Itemset
    {
        public List<string> SortedTuple { get; set; }

        public string Signature { get; }

        public int Frequency { get; }

        public Itemset(List<string> tuple, int frequency)
        {
            this.SortedTuple = tuple.OrderBy(i => i).ToList();

            this.Signature = string.Join(",", SortedTuple);

            this.Frequency = frequency;
        }

        public override bool Equals(object obj)
        {
            if (obj is Itemset itemset)
            {
                return itemset.Signature == this.Signature;
            }
            else
            {
                return false;
            }

        }

        public override int GetHashCode()
        {
            return this.Signature.GetHashCode();
        }

        public bool Contains(string value)
        {
            return SortedTuple.Contains(value);
        }

        public bool CoveredBy(string[] values)
        {
            //if (values == null || SortedTuple == null || values.Length == 0 || SortedTuple.Count == 0)
            //{

            //}

            return SortedTuple.All(v => values.Contains(v));
        }

        public Itemset Extend(string value)
        {
            if (SortedTuple.Contains(value))
            {
                return null;
            }

            List<string> values = new List<string>();

            values.AddRange(SortedTuple);

            values.Add(value);

            return new Itemset(values, 1);
        }

        public override string ToString()
        {
            return Signature;
        }
    }
}
