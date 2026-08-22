using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace IRI.Maptor.Extensions;

public static class AnimationHelper
{
    public static DoubleAnimation CreateElasticAnimation(DependencyObject target, PropertyPath propertyPath, double to)
    {
        var result = new DoubleAnimation
        {
            Duration = new Duration(new TimeSpan(5000000)),
            To = to,
            EasingFunction = new ElasticEase
            {
                Oscillations = 2,
                Springiness = 10
            }
        };

        Storyboard.SetTarget(result, target);

        Storyboard.SetTargetProperty(result, propertyPath);

        return result;
    }
}
