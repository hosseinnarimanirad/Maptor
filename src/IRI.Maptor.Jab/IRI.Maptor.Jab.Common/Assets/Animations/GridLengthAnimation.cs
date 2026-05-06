using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace IRI.Maptor.Jab.Common.Animations;

/// <summary>
/// Animates a grid length value just like the DoubleAnimation animates a double value
/// </summary>
public class GridLengthAnimation : AnimationTimeline
{
    private bool isCompleted;
    /// <summary>
    /// Marks the animation as completed
    /// </summary>
    public bool IsCompleted
    {
        get => isCompleted;
        set => isCompleted = value;
    }

    /// <summary>
    /// Returns the type of object to animate
    /// </summary>
    public override Type TargetPropertyType => typeof(GridLength);


    /// <summary>
    /// Sets the reverse value for the second animation
    /// </summary>
    public double ReverseValue
    {
        get { return (double)GetValue(ReverseValueProperty); }
        set { SetValue(ReverseValueProperty, value); }
    }
      
    public static readonly DependencyProperty ReverseValueProperty =
        DependencyProperty.Register(nameof(ReverseValue), typeof(double), typeof(GridLengthAnimation), new UIPropertyMetadata(0.0));

    
    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public static readonly DependencyProperty FromProperty = DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));


    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));


    AnimationClock clock;

    /// <summary>
    /// registers to the completed event of the animation clock
    /// </summary>
    /// <param name="clock">the animation clock to notify completion status</param>
    void VerifyAnimationCompletedStatus(AnimationClock clock)
    {
        if (this.clock == null)
        {
            this.clock = clock;
            this.clock.Completed += new EventHandler(delegate (object sender, EventArgs e) { isCompleted = true; });
        }
    }

    /// <summary>
    /// Animates the grid let set
    /// </summary>
    /// <param name="defaultOriginValue">The original value to animate</param>
    /// <param name="defaultDestinationValue">The final value</param>
    /// <param name="animationClock">The animation clock (timer)</param>
    /// <returns>Returns the new grid length to set</returns>
    public override object GetCurrentValue(object defaultOriginValue,
        object defaultDestinationValue, AnimationClock animationClock)
    {
        //check the animation clock event
        VerifyAnimationCompletedStatus(animationClock);

        //check if the animation was completed
        if (isCompleted)
        {
            return (GridLength)defaultDestinationValue;
        }

        //if not then create the value to animate
        double fromVal = From.Value;
        double toVal = To.Value;

        //check if the value is already collapsed
        //if (((GridLength)defaultOriginValue).Value == toVal)
        //{
        //    fromVal = toVal;
        //    toVal = this.ReverseValue;
        //}
        //else
        //check to see if this is the last tick of the animation clock.
        if (animationClock.CurrentProgress.Value == 1.0)
            return To;

        if (fromVal > toVal)
            return new GridLength((1 - animationClock.CurrentProgress.Value) *
                (fromVal - toVal) + toVal, From.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
        else
            return new GridLength(animationClock.CurrentProgress.Value *
                (toVal - fromVal) + fromVal, From.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
    }

    /// <summary>
    /// Creates an instance of the animation object
    /// </summary>
    /// <returns>Returns the instance of the GridLengthAnimation</returns>
    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    protected override Clock AllocateClock()
    {
        isCompleted = false;

        return base.AllocateClock();
    }
}