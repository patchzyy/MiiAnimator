using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MiiAnim.Core.Animation;

namespace MiiAnimator.Timeline;

/// <summary>Tiny picture of an easing curve so beginners can see what "Ease Out" or "Bouncy" means.</summary>
public sealed class EasingIcon : Control
{
    public static readonly StyledProperty<Interpolation> InterpolationProperty = AvaloniaProperty.Register<EasingIcon, Interpolation>(
        nameof(Interpolation)
    );

    private static readonly IPen CurvePen = new Pen(new SolidColorBrush(Color.FromRgb(255, 180, 60)), 1.6);
    private static readonly IPen FramePen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1);

    static EasingIcon() => AffectsRender<EasingIcon>(InterpolationProperty);

    public Interpolation Interpolation
    {
        get => GetValue(InterpolationProperty);
        set => SetValue(InterpolationProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        const double pad = 2;
        context.DrawLine(FramePen, new Point(pad, h - pad), new Point(w - pad, h - pad));

        var curve = new AnimCurve();
        curve.SetKey(0, 0f, Interpolation);
        curve.SetKey(1000, 1f, Interpolation.Linear);
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            const int samples = 32;
            for (var i = 0; i <= samples; i++)
            {
                var u = i / (float)samples;
                var v = curve.Evaluate(u * 1000f, 0f);
                var p = new Point(pad + u * (w - pad * 2), h - pad - v * (h - pad * 2) * 0.82 - (h - pad * 2) * 0.06);
                if (i == 0)
                    g.BeginFigure(p, false);
                else
                    g.LineTo(p);
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, CurvePen, geometry);
    }
}
