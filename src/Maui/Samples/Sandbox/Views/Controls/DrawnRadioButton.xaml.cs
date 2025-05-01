using AppoMobi.Maui.Gestures;

namespace Sandbox.Views.Controls;

public partial class DrawnRadioButton
{
    public DrawnRadioButton()
    {
        InitializeComponent();
    }

    public override ISkiaGestureListener ProcessGestures(SkiaGesturesParameters args, GestureEventProcessingInfo apply)
    {

        if (args.Type == TouchActionResult.Tapped)
        {
            var ptsInsideControl = GetOffsetInsideControlInPoints(args.Event.Location, apply.childOffset);
            this.PlayRippleAnimation(Colors.CornflowerBlue, ptsInsideControl.X, ptsInsideControl.Y);
        }

        return base.ProcessGestures(args, apply);
    }
}