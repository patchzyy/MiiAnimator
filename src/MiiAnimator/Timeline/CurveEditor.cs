using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using MiiAnim.Core.Animation;
using MiiAnimator.Editing;

namespace MiiAnimator.Timeline;

/// <summary>
/// Graph editor for fine-tuning motion. Shows the curves of the selected body part.
/// Drag keys to change timing/value, drag the round handles to shape the curve (makes it a Custom curve).
/// Wheel zooms values, Ctrl+wheel zooms time, middle-drag pans, F or double-click fits the view.
/// </summary>
public sealed class CurveEditor : Control
{
    private static readonly IBrush Background = new SolidColorBrush(Color.FromRgb(26, 28, 34));
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)), 1);
    private static readonly IPen ZeroPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1);
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(140, 146, 160));
    private static readonly IBrush PlayheadBrush = new SolidColorBrush(Color.FromRgb(255, 150, 40));
    private static readonly Typeface UiTypeface = new("Inter");

    private AnimatorDocument? _doc;
    private List<TrackId> _tracks = [];
    private readonly HashSet<TrackId> _hidden = [];
    private double _frameMin;
    private double _frameMax = 120;
    private double _valueMin = -10;
    private double _valueMax = 10;
    private Selection _fittedFor = Selection.None;

    private (TrackId Id, int Frame)? _selectedKey;
    private DragKind _dragKind;
    private Point _lastPointer;
    private bool _panning;
    private readonly List<(Rect Rect, TrackId Id)> _legend = [];

    private enum DragKind
    {
        None,
        Key,
        HandleOut,
        HandleIn,
    }

    public CurveEditor()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public void Attach(AnimatorDocument document)
    {
        _doc = document;
        document.AnimationChanged += Rebuild;
        document.SelectionChanged += Rebuild;
        document.FrameChanged += InvalidateVisual;
        document.FileChanged += Rebuild;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_doc is null)
            return;
        _tracks = TracksForSelection().Where(id => !ChannelInfo.IsDiscrete(id.Channel) && _doc.Animation.TryGetCurve(id) is not null).ToList();
        if (_selectedKey is { } k && (!_tracks.Contains(k.Id) || _doc.Animation.TryGetCurve(k.Id)?.HasKeyAt(k.Frame) != true))
            _selectedKey = null;
        if (_fittedFor != _doc.Selection && _dragKind == DragKind.None)
        {
            _fittedFor = _doc.Selection;
            Fit();
        }

        InvalidateVisual();
    }

    private IEnumerable<TrackId> TracksForSelection()
    {
        var selection = _doc!.Selection;
        var tracks = AnimatorDocument.TracksOf(selection);
        if (selection.Kind == SelectionKind.Limb && !_doc.AdvancedMode)
        {
            var (u, m, e) = MiiAnim.Core.Rig.MiiSkeletonInfo.Chain(selection.Limb);
            tracks = tracks.Concat(new[] { u, m, e }.SelectMany(b => AnimatorDocument.TracksOf(Selection.OfBone(b))));
        }

        return tracks;
    }

    public void Fit()
    {
        if (_doc is null)
            return;
        _frameMin = -2;
        _frameMax = _doc.Animation.Length + 2;
        var min = double.MaxValue;
        var max = double.MinValue;
        foreach (var id in _tracks.Where(t => !_hidden.Contains(t)))
        {
            for (var f = 0; f <= _doc.Animation.Length; f += Math.Max(1, _doc.Animation.Length / 200))
            {
                var v = _doc.Animation.Evaluate(id, f);
                min = Math.Min(min, v);
                max = Math.Max(max, v);
            }
        }

        if (min > max)
        {
            min = -10;
            max = 10;
        }

        var pad = Math.Max(1, (max - min) * 0.15);
        _valueMin = min - pad;
        _valueMax = max + pad;
        InvalidateVisual();
    }

    // ---------- Coordinates ----------

    private const double Left = 44;
    private const double Top = 28;

    private double PlotWidth => Math.Max(10, Bounds.Width - Left - 8);
    private double PlotHeight => Math.Max(10, Bounds.Height - Top - 8);

    private Point ToScreen(double frame, double value) =>
        new(Left + (frame - _frameMin) / (_frameMax - _frameMin) * PlotWidth, Top + (1 - (value - _valueMin) / (_valueMax - _valueMin)) * PlotHeight);

    private (double Frame, double Value) FromScreen(Point p) =>
        (_frameMin + (p.X - Left) / PlotWidth * (_frameMax - _frameMin), _valueMin + (1 - (p.Y - Top) / PlotHeight) * (_valueMax - _valueMin));

    private static Color ColorFor(Channel channel) =>
        channel switch
        {
            Channel.PosX or Channel.RotX or Channel.ScaleX or Channel.TargetX or Channel.TargetRotX => Color.FromRgb(240, 90, 90),
            Channel.PosY or Channel.RotY or Channel.ScaleY or Channel.TargetY or Channel.TargetRotY => Color.FromRgb(120, 215, 95),
            Channel.PosZ or Channel.RotZ or Channel.ScaleZ or Channel.TargetZ or Channel.TargetRotZ => Color.FromRgb(90, 150, 250),
            Channel.Stretch => Color.FromRgb(240, 170, 60),
            Channel.Swivel => Color.FromRgb(200, 120, 240),
            _ => Color.FromRgb(200, 200, 200),
        };

    private string LabelFor(TrackId id)
    {
        var prefix = id.Target.Kind == TrackTargetKind.Bone && _doc!.Selection.Kind == SelectionKind.Limb ? id.Target.FriendlyName + " " : "";
        return prefix + ChannelInfo.FriendlyName(id.Channel);
    }

    // ---------- Rendering ----------

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Background, new Rect(Bounds.Size));
        if (_doc is null)
            return;

        if (_tracks.Count == 0)
        {
            var hint = _doc.Selection.Kind == SelectionKind.None
                ? "Select a body part to see its motion curves."
                : "No keys yet for " + _doc.Selection.DisplayName + ". Pose it to create keys, then shape the motion here.";
            var text = Text(hint, 12.5, TextBrush);
            context.DrawText(text, new Point((Bounds.Width - text.Width) / 2, (Bounds.Height - text.Height) / 2));
            return;
        }

        // Grid.
        var valueStep = NiceStep((_valueMax - _valueMin) / 6);
        for (var v = Math.Ceiling(_valueMin / valueStep) * valueStep; v <= _valueMax; v += valueStep)
        {
            var y = ToScreen(0, v).Y;
            context.DrawLine(Math.Abs(v) < valueStep * 0.01 ? ZeroPen : GridPen, new Point(Left, y), new Point(Bounds.Width, y));
            context.DrawText(Text(v.ToString("0.##", CultureInfo.InvariantCulture), 10, TextBrush), new Point(4, y - 7));
        }

        var frameStep = NiceStep((_frameMax - _frameMin) / 10);
        for (var f = Math.Ceiling(_frameMin / frameStep) * frameStep; f <= _frameMax; f += frameStep)
        {
            var x = ToScreen(f, 0).X;
            context.DrawLine(GridPen, new Point(x, Top), new Point(x, Bounds.Height));
        }

        var endX = ToScreen(_doc.Animation.Length, 0).X;
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), new Rect(endX, Top, Math.Max(0, Bounds.Width - endX), PlotHeight + 8));

        using (context.PushClip(new Rect(Left, Top, PlotWidth + 8, PlotHeight + 8)))
        {
            foreach (var id in _tracks)
            {
                if (_hidden.Contains(id))
                    continue;
                DrawCurve(context, id);
            }

            if (_selectedKey is { } selected && !_hidden.Contains(selected.Id))
                DrawHandles(context, selected.Id, selected.Frame);

            var px = ToScreen(_doc.DisplayFrame, 0).X;
            context.DrawLine(new Pen(PlayheadBrush, 1.2), new Point(px, Top), new Point(px, Bounds.Height));
        }

        // Legend chips (click to show/hide a channel).
        _legend.Clear();
        var lx = Left;
        foreach (var id in _tracks)
        {
            var visible = !_hidden.Contains(id);
            var text = Text(LabelFor(id), 11, visible ? Brushes.White : TextBrush);
            var rect = new Rect(lx, 5, text.Width + 22, 18);
            if (rect.Right > Bounds.Width - 4)
                break;
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(visible ? 60 : 20), 255, 255, 255)), null, rect, 9, 9);
            context.DrawEllipse(new SolidColorBrush(ColorFor(id.Channel), visible ? 1 : 0.35), null, new Point(rect.X + 9, rect.Y + 9), 4, 4);
            context.DrawText(text, new Point(rect.X + 16, rect.Y + 2));
            _legend.Add((rect, id));
            lx = rect.Right + 6;
        }
    }

    private void DrawCurve(DrawingContext context, TrackId id)
    {
        var curve = _doc!.Animation.TryGetCurve(id)!;
        var color = ColorFor(id.Channel);
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            var first = true;
            for (var x = Left; x <= Left + PlotWidth; x += 2)
            {
                var (frame, _) = FromScreen(new Point(x, 0));
                var p = ToScreen(frame, _doc.Animation.Evaluate(id, (float)frame));
                if (first)
                {
                    g.BeginFigure(p, false);
                    first = false;
                }
                else
                {
                    g.LineTo(p);
                }
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(new SolidColorBrush(color), 1.8), geometry);
        foreach (var key in curve.Keys)
        {
            var selected = _selectedKey is { } s && s.Id == id && s.Frame == key.Frame;
            var p = ToScreen(key.Frame, key.Value);
            context.DrawEllipse(selected ? Brushes.White : new SolidColorBrush(color), new Pen(Brushes.Black, 1), p, selected ? 5.5 : 4.5, selected ? 5.5 : 4.5);
        }
    }

    private void DrawHandles(DrawingContext context, TrackId id, int frame)
    {
        foreach (var (kind, point, anchor, isCustom) in HandlePoints(id, frame))
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(isCustom ? 200 : 90), 255, 255, 255)), 1, isCustom ? null : new DashStyle([3, 3], 0));
            context.DrawLine(pen, anchor, point);
            context.DrawEllipse(isCustom ? Brushes.White : new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), new Pen(Brushes.Black, 1), point, 4, 4);
            _ = kind;
        }
    }

    /// <summary>Handle positions for a key: its outgoing handle and the incoming handle of the segment before it.</summary>
    private IEnumerable<(DragKind Kind, Point Point, Point Anchor, bool IsCustom)> HandlePoints(TrackId id, int frame)
    {
        var curve = _doc!.Animation.TryGetCurve(id);
        if (curve is null)
            yield break;
        var index = curve.IndexOf(frame);
        if (index < 0)
            yield break;
        var key = curve.Keys[index];
        var anchor = ToScreen(key.Frame, key.Value);
        if (index < curve.Count - 1 && key.Interpolation != Interpolation.Hold)
        {
            var next = curve.Keys[index + 1];
            var span = next.Frame - key.Frame;
            var (outHandle, _) = curve.SuggestHandles(index);
            yield return (DragKind.HandleOut, ToScreen(key.Frame + outHandle.X * span, key.Value + outHandle.Y), anchor, key.Interpolation == Interpolation.Custom);
        }

        if (index > 0 && curve.Keys[index - 1].Interpolation != Interpolation.Hold)
        {
            var previous = curve.Keys[index - 1];
            var span = key.Frame - previous.Frame;
            var (_, inHandle) = curve.SuggestHandles(index - 1);
            yield return (DragKind.HandleIn, ToScreen(key.Frame - inHandle.X * span, key.Value + inHandle.Y), anchor, previous.Interpolation == Interpolation.Custom);
        }
    }

    private static double NiceStep(double raw)
    {
        if (raw <= 0)
            return 1;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var n = raw / magnitude;
        return (n < 1.5 ? 1 : n < 3 ? 2 : n < 7 ? 5 : 10) * magnitude;
    }

    private static FormattedText Text(string text, double size, IBrush brush) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, UiTypeface, size, brush);

    // ---------- Input ----------

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (_doc is null)
            return;
        var point = e.GetPosition(this);
        _lastPointer = point;
        var properties = e.GetCurrentPoint(this).Properties;

        if (properties.IsMiddleButtonPressed || (properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {
            _panning = true;
            e.Pointer.Capture(this);
            return;
        }

        foreach (var (rect, id) in _legend)
        {
            if (!rect.Contains(point))
                continue;
            if (!_hidden.Remove(id))
                _hidden.Add(id);
            InvalidateVisual();
            return;
        }

        if (e.ClickCount == 2)
        {
            Fit();
            return;
        }

        // Handles of the selected key first.
        if (_selectedKey is { } selected)
        {
            foreach (var (kind, handlePoint, _, _) in HandlePoints(selected.Id, selected.Frame))
            {
                if (Distance(point, handlePoint) > 8)
                    continue;
                _dragKind = kind;
                _doc.BeginGesture();
                e.Pointer.Capture(this);
                return;
            }
        }

        // Keys.
        foreach (var id in _tracks.Where(t => !_hidden.Contains(t)))
        {
            foreach (var key in _doc.Animation.TryGetCurve(id)!.Keys)
            {
                if (Distance(point, ToScreen(key.Frame, key.Value)) > 7)
                    continue;
                _selectedKey = (id, key.Frame);
                if (properties.IsRightButtonPressed)
                {
                    ShowKeyMenu(id, key.Frame);
                    InvalidateVisual();
                    return;
                }

                _dragKind = DragKind.Key;
                _doc.BeginGesture();
                e.Pointer.Capture(this);
                InvalidateVisual();
                return;
            }
        }

        _selectedKey = null;
        if (properties.IsLeftButtonPressed)
        {
            var (frame, _) = FromScreen(point);
            _doc.SetFrame((int)Math.Round(frame));
        }

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_doc is null)
            return;
        var point = e.GetPosition(this);
        var delta = point - _lastPointer;
        _lastPointer = point;

        if (_panning)
        {
            var framesPerPixel = (_frameMax - _frameMin) / PlotWidth;
            var valuesPerPixel = (_valueMax - _valueMin) / PlotHeight;
            _frameMin -= delta.X * framesPerPixel;
            _frameMax -= delta.X * framesPerPixel;
            _valueMin += delta.Y * valuesPerPixel;
            _valueMax += delta.Y * valuesPerPixel;
            InvalidateVisual();
            return;
        }

        if (_selectedKey is not { } selected || _dragKind == DragKind.None)
            return;
        var curve = _doc.Animation.TryGetCurve(selected.Id);
        if (curve is null)
            return;
        var index = curve.IndexOf(selected.Frame);
        if (index < 0)
            return;
        var (mouseFrame, mouseValue) = FromScreen(point);
        var key = curve.Keys[index];

        switch (_dragKind)
        {
            case DragKind.Key:
            {
                var newFrame = Math.Clamp((int)Math.Round(mouseFrame), 0, _doc.Animation.Length);
                if (newFrame != key.Frame && curve.HasKeyAt(newFrame))
                    newFrame = key.Frame;
                var oldFrame = key.Frame;
                key.Frame = newFrame;
                key.Value = (float)mouseValue;
                _doc.ReplaceKey(selected.Id, oldFrame, key);
                _selectedKey = (selected.Id, newFrame);
                break;
            }
            case DragKind.HandleOut when index < curve.Count - 1:
            {
                var next = curve.Keys[index + 1];
                var span = next.Frame - key.Frame;
                if (key.Interpolation != Interpolation.Custom)
                    (key.HandleOut, key.HandleIn) = curve.SuggestHandles(index);
                key.Interpolation = Interpolation.Custom;
                key.HandleOut = new Vector2((float)Math.Clamp((mouseFrame - key.Frame) / span, 0, 1), (float)(mouseValue - key.Value));
                _doc.ReplaceKey(selected.Id, key.Frame, key);
                break;
            }
            case DragKind.HandleIn when index > 0:
            {
                var previous = curve.Keys[index - 1];
                var span = key.Frame - previous.Frame;
                if (previous.Interpolation != Interpolation.Custom)
                    (previous.HandleOut, previous.HandleIn) = curve.SuggestHandles(index - 1);
                previous.Interpolation = Interpolation.Custom;
                previous.HandleIn = new Vector2((float)Math.Clamp((key.Frame - mouseFrame) / span, 0, 1), (float)(mouseValue - key.Value));
                _doc.ReplaceKey(selected.Id, previous.Frame, previous);
                break;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragKind != DragKind.None)
            _doc?.EndGesture();
        _dragKind = DragKind.None;
        _panning = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var (frame, value) = FromScreen(e.GetPosition(this));
        var factor = e.Delta.Y > 0 ? 0.85 : 1 / 0.85;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _frameMin = frame - (frame - _frameMin) * factor;
            _frameMax = frame + (_frameMax - frame) * factor;
        }
        else
        {
            _valueMin = value - (value - _valueMin) * factor;
            _valueMax = value + (_valueMax - value) * factor;
        }

        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F)
        {
            Fit();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Delete or Key.Back && _selectedKey is { } selected && _doc is not null)
        {
            _doc.DeleteKeys([selected.Id], [selected.Frame]);
            _selectedKey = null;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void ShowKeyMenu(TrackId id, int frame)
    {
        var items = new List<object>();
        foreach (var interpolation in InterpolationInfo.Presets)
        {
            var item = new MenuItem
            {
                Header = InterpolationInfo.FriendlyName(interpolation),
                Icon = new EasingIcon { Interpolation = interpolation, Width = 28, Height = 16 },
            };
            ToolTip.SetTip(item, InterpolationInfo.Description(interpolation));
            item.Click += (_, _) => _doc!.SetInterpolation([id], [frame], interpolation);
            items.Add(item);
        }

        items.Add(new Separator());
        var delete = new MenuItem { Header = "Delete key" };
        delete.Click += (_, _) => _doc!.DeleteKeys([id], [frame]);
        items.Add(delete);
        new ContextMenu { ItemsSource = items }.Open(this);
    }

    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
