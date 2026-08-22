//using IRI.Maptor.Core.Spatial.Primitives;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace IRI.Maptor.Core.MachineLearning.LogisticRegressionUseCases
//{
//    public class LRSampleTrainingData
//    {
//        List<LRSimplificationParameters<Point>> zeroAngleSamples = new List<LRSimplificationParameters<Point>>();

//        public LRSampleTrainingData()
//        {
//            zeroAngleSamples = new List<LRSimplificationParameters<Point>>()
//            {
//                new LRSimplificationParameters<Point>()
//                {
//                    CosineOfAngle = 0,
//                    Area = 100,
//                    DistanceToNext = 100,
//                    DistanceToPrevious = 100,
//                    VerticalDistance = 10,
//                    SquareCosineOfAngle = 0,
//                    IsRetained = false
//                }};
//        }

//        public IEnumerable<LRSimplificationParameters<Point>> Get()
//        {
//            //"100 100, 200 100.5, 300 100"
//            yield return new LRSimplificationParameters<Point>() {   };
//        }
//    }
//}
