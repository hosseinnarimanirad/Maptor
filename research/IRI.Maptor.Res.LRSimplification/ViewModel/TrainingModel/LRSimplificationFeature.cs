
using IRI.Maptor.Core.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Res.LRSimplification.ViewModel.TrainingModel;

public class LRSimplificationFeature
{
    public LRSimplificationFeatures Value { get; set; }


    public override string ToString()
    {
        return $"Value: {Value}";
    }
}
