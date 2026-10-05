using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using MiiAnim.Core.Animation;
using MiiAnimator.Editing;

namespace MiiAnimator.Timeline;

/// <summary>
/// Keyframe overview: one row per body part, a diamond for every key. Click the ruler to scrub,
/// click a key to select it, drag to retime, right-click for easing.
/// </summary>
public sealed class DopeSheet : Control
{
    public const double LabelWidth = 150;
    public const double RulerHeight = 26;
    private const double RowHeight = 24;

    private static readonly IBrush Background = new SolidColorBrush(Color.FromRgb(28, 30, 36));
    private static readonly IBrush LabelBackground = new SolidColorBrush(Color.FromRgb(34, 37, 44));
    private static readonly IBrush RowAlt = new SolidColorBrush(Color.FromRgb(31, 33, 40));
    private static readonly IBrush RowSelected = new SolidColorBrush(Color.FromArgb(70, 255, 170, 40));
    private static readonly IBrush RulerBrush = new SolidColorBrush(Color.FromRgb(40, 43, 51));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(205, 210, 220));
    private static readonly IBrush DimText = new SolidColorBrush(Color.FromRgb(130, 136, 150));
    private static readonly IBrush PlayheadBrush = new SolidColorBrush(Color.FromRgb(255, 150, 40));
    private static readonly IBrush OutOfRange = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
    private static readonly IBrush KeyBrush = new SolidColorBrush(Color.FromRgb(225, 228, 235));
    private static readonly IBrush KeySelected = new SolidColorBrush(Color.FromRgb(255, 190, 60));
    private static readonly IBrush EventBrush = new SolidColorBrush(Color.FromRgb(150, 95, 230));
    private static readonly Pen EventLinePen = new(new SolidColorBrush(Color.FromArgb(70, 150, 95, 230)), 1);
    private static readonly Pen GridPen =new(new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)), 1);
    private static readonly Typeface UiTypeface = new("Inter");

    private AnimatorDocument? _doc;
    private List<TimelineRow> _rows = [];
    private double _pixelsPerFrame = 6;
    private double _scrollFrames;
    private double _scrollRows;
    private bool _fitToWidth = true;

    private readonly HashSet<(int Row, int Frame)> _selectedKeys = [];
    private bool _scrubbing;
    private Point? _dragStart;
    private int _dragOffsetFrames;
    private bool _draggingKeys;

    public event Action? SelectedKeysChanged;

    public DopeSheet()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public void Attach(AnimatorDocument document)
    {
        _doc = document;
        _rows = TimelineRows.Build(document.AdvancedMode);
        document.AnimationChanged += () =>
        {
            PruneSelection();
            InvalidateVisual();
        };
        document.FrameChanged += InvalidateVisual;
        document.SelectionChanged += InvalidateVisual;
        document.SettingsChanged += () =>
        {
            _rows = TimelineRows.Build(document.AdvancedMode);
            _selectedKeys.Clear();
            SelectedKeysChanged?.Invoke();
            InvalidateVisual();
        };
        document.FileChanged += InvalidateVisual;
    }

    public IReadOnlyList<TimelineRow> Rows => _rows;

    /// <summary>Selected keys as (tracks of the row, frame) groups.</summary>
    public IEnumerable<(IReadOnlyList<TrackId> Tracks, int Frame)> SelectedKeys =>
        _selectedKeys.Where(k => k.Row < _rows.Count).Select(k => (_rows[k.Row].Tracks, k.Frame));

    public bool HasSelectedKeys => _selectedKeys.Count > 0;

    /// <summary>Easing shared by all selected keys, or null if mixed.</summary>
    public Interpolation? SelectedInterpolation
    {
        get
        {
            Interpolation? result = null;
            foreach (var (tracks, frame) in SelectedKeys)
            foreach (var id in tracks)
            {
                if (ChannelInfo.IsDiscrete(id.Channel) || _doc!.Animation.TryGetCurve(id) is not { } curve)
                    continue;
                var index = curve.IndexOf(frame);
                if (index < 0)
                    continue;
                var interpolation = curve.Keys[index].Interpolation;
                if (result is null)
                    result = interpolation;
                else if (result != interpolation)
                    return null;
            }

            return result;
        }
    }

    public void ApplyInterpolationToSelection(Interpolation interpolation)
    {
        if (_doc is null)
            return;
        _doc.BeginGesture();
        foreach (var group in SelectedKeys.GroupBy(k => k.Frame))
            _doc.SetInterpolation(group.SelectMany(g => g.Tracks).Distinct().ToList(), [group.Key], interpolation);
        _doc.EndGesture();
    }

    public void DeleteSelectedKeys()
    {
        if (_doc is null || _selectedKeys.Count == 0)
            return;
        _doc.BeginGesture();
        foreach (var (tracks, frame) in SelectedKeys.ToList())
            _doc.DeleteKeys(tracks, [frame]);
        _doc.EndGesture();
        _selectedKeys.Clear();
        SelectedKeysChanged?.Invoke();
    }

    private void PruneSelection()
    {
        if (_doc is null)
            return;
        var removed = _selectedKeys.RemoveWhere(k => k.Row >= _rows.Count || !RowHasKey(_rows[k.Row], k.Frame));
        if (removed > 0)
            SelectedKeysChanged?.Invoke();
    }

    private bool RowHasKey(TimelineRow row, int frame) => row.Tracks.Any(id => _doc!.Animation.TryGetCurve(id)?.HasKeyAt(frame) == true);

    // ---------- Layout helpers ----------

    private double TrackWidth => Math.Max(10, Bounds.Width - LabelWidth - 12);

    private void UpdateFit()
    {
        if (_fitToWidth && _doc is not null)
        {
            _pixelsPerFrame = TrackWidth / Math.Max(1, _doc.Animation.Length);
            _scrollFrames = 0;
        }
    }

    private double FrameToX(double frame) => LabelWidth + 6 + (frame - _scrollFrames) * _pixelsPerFrame;

    private double XToFrame(double x) => (x - LabelWidth - 6) / _pixelsPerFrame + _scrollFrames;

    private int RowAt(double y) => (int)Math.Floor((y - RulerHeight) / RowHeight + _scrollRows);

    private double RowTop(int row) => RulerHeight + (row - _scrollRows) * RowHeight;

    // ---------- Rendering ----------

    public override void Render(DrawingContext context)
    {
        if (_doc is null)
            return;
        UpdateFit();
        var doc = _doc;
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Background, bounds);

        // Rows.
        for (var i = 0; i < _rows.Count; i++)
        {
            var top = RowTop(i);
            if (top + RowHeight < RulerHeight || top > Bounds.Height)
                continue;
            var row = _rows[i];
            var rowRect = new Rect(0, top, Bounds.Width, RowHeight);
            if (i % 2 == 1)
                context.FillRectangle(RowAlt, rowRect);
            if (TimelineRows.Matches(row, doc.Selection))
                context.FillRectangle(RowSelected, rowRect);
        }

        // Frame grid.
        var step = TickStep();
        var firstFrame = Math.Max(0, (int)Math.Floor(_scrollFrames / step) * step);
        var lastFrame = (int)Math.Ceiling(XToFrame(Bounds.Width));
        for (var f = firstFrame; f <= lastFrame; f += step)
        {
            var x = Math.Round(FrameToX(f)) + 0.5;
            context.DrawLine(GridPen, new Point(x, RulerHeight), new Point(x, Bounds.Height));
        }

        // Beyond-the-end shading.
        var endX = FrameToX(doc.Animation.Length);
        if (endX < Bounds.Width)
            context.FillRectangle(OutOfRange, new Rect(endX, RulerHeight, Bounds.Width - endX, Bounds.Height - RulerHeight));

        // Keys.
        using (context.PushClip(new Rect(LabelWidth, RulerHeight, Math.Max(0, Bounds.Width - LabelWidth), Math.Max(0, Bounds.Height - RulerHeight))))
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                var top = RowTop(i);
                if (top + RowHeight < RulerHeight || top > Bounds.Height)
                    continue;
                foreach (var (frame, interpolation) in KeysInRow(_rows[i]))
                {
                    var selected = _selectedKeys.Contains((i, frame));
                    var drawFrame = frame + (selected && _draggingKeys ? _dragOffsetFrames : 0);
                    DrawKey(context, new Point(FrameToX(drawFrame), top + RowHeight / 2), interpolation, selected);
                }
            }
        }

        // Labels.
        context.FillRectangle(LabelBackground, new Rect(0, RulerHeight, LabelWidth, Bounds.Height));
        for (var i = 0; i < _rows.Count; i++)
        {
            var top = RowTop(i);
            if (top + RowHeight < RulerHeight || top > Bounds.Height)
                continue;
            var selected = TimelineRows.Matches(_rows[i], doc.Selection);
            if (selected)
                context.FillRectangle(RowSelected, new Rect(0, top, LabelWidth, RowHeight));
            var text = Text(_rows[i].Label, 12, selected ? Brushes.White : TextBrush);
            context.DrawText(text, new Point(12, top + (RowHeight - text.Height) / 2));
        }

        // Ruler.
        context.FillRectangle(RulerBrush, new Rect(0, 0, Bounds.Width, RulerHeight));
        for (var f = firstFrame; f <= lastFrame; f += step)
        {
            var x = FrameToX(f);
            if (x < LabelWidth)
                continue;
            context.DrawLine(new Pen(DimText, 1), new Point(x, RulerHeight - 7), new Point(x, RulerHeight));
            var label = Text(f.ToString(CultureInfo.InvariantCulture), 10.5, DimText);
            context.DrawText(label, new Point(x + 3, 4));
        }

        var secondsText = Text($"{doc.Animation.DurationSeconds:0.##}s @ {doc.Animation.Fps}fps", 10.5, DimText);
        context.DrawText(secondsText, new Point(10, 6));

        // Event markers: a flag on the ruler and a faint line through the rows.
        using (context.PushClip(new Rect(LabelWidth, 0, Math.Max(0, Bounds.Width - LabelWidth), Bounds.Height)))
        {
            foreach (var animEvent in doc.Animation.Events)
            {
                var x = FrameToX(animEvent.Frame);
                if (x < LabelWidth - 60 || x > Bounds.Width)
                    continue;
                context.DrawLine(EventLinePen, new Point(x, RulerHeight), new Point(x, Bounds.Height));
                var name = Text(animEvent.Name, 10, Brushes.White);
                var flag = new Rect(x, RulerHeight - 13, Math.Min(name.Width + 8, 90), 12);
                context.DrawRectangle(EventBrush, null, flag, 2, 2);
                using (context.PushClip(flag))
                    context.DrawText(name, new Point(flag.X + 4, flag.Y + (flag.Height - name.Height) / 2));
                context.DrawLine(new Pen(EventBrush, 1.5), new Point(x, RulerHeight - 13), new Point(x, RulerHeight));
            }
        }

        // Playhead.
        var px = FrameToX(doc.DisplayFrame);
        if (px >= LabelWidth - 1)
        {
            context.DrawLine(new Pen(PlayheadBrush, 1.5), new Point(px, RulerHeight), new Point(px, Bounds.Height));
            var label = Text(doc.CurrentFrame.ToString(CultureInfo.InvariantCulture), 11, Brushes.Black);
            var box = new Rect(px - label.Width / 2 - 5, 3, label.Width + 10, RulerHeight - 6);
            context.DrawRectangle(PlayheadBrush, null, box, 4, 4);
            context.DrawText(label, new Point(box.X + 5, box.Y + (box.Height - label.Height) / 2));
        }
    }

    private IEnumerable<(int Frame, Interpolation Interpolation)> KeysInRow(TimelineRow row)
    {
        var result = new SortedDictionary<int, Interpolation>();
        foreach (var id in row.Tracks)
        {
            if (_doc!.Animation.TryGetCurve(id) is not { } curve)
                continue;
            foreach (var key in curve.Keys)
            {
                var interpolation = ChannelInfo.IsDiscrete(id.Channel) ? Interpolation.Hold : key.Interpolation;
                if (!result.TryGetValue(key.Frame, out var existing) || ChannelInfo.IsDiscrete(id.Channel) == false && existing == Interpolation.Hold && row.Tracks.Count > 1)
                    result[key.Frame] = interpolation;
            }
        }

        return result.Select(kv => (kv.Key, kv.Value));
    }

    /// <summary>Key shape tells the easing at a glance: ● smooth, ◆ linear/eases, ■ hold, ★-ish for bouncy/overshoot.</summary>
    public static void DrawKey(DrawingContext context, Point center, Interpolation interpolation, bool selected)
    {
        var fill = selected ? KeySelected : KeyBrush;
        var pen = new Pen(Brushes.Black, 1);
        const double r = 5.5;
        switch (interpolation)
        {
            case Interpolation.Smooth:
                context.DrawEllipse(fill, pen, center, r - 0.5, r - 0.5);
                break;
            case Interpolation.Hold:
                context.DrawRectangle(fill, pen, new Rect(center.X - r + 1, center.Y - r + 1, (r - 1) * 2, (r - 1) * 2));
                break;
            case Interpolation.Bouncy or Interpolation.Overshoot or Interpolation.Snappy:
            {
                var geometry = new StreamGeometry();
                using (var g = geometry.Open())
                {
                    for (var i = 0; i < 10; i++)
                    {
                        var angle = -Math.PI / 2 + i * Math.PI / 5;
                        var radius = i % 2 == 0 ? r + 1 : r * 0.5;
                        var p = new Point(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
                        if (i == 0)
                            g.BeginFigure(p, true);
                        else
                            g.LineTo(p);
                    }

                    g.EndFigure(true);
                }

                context.DrawGeometry(fill, pen, geometry);
                break;
            }
            default:
            {
                var geometry = new StreamGeometry();
                using (var g = geometry.Open())
                {
                    g.BeginFigure(new Point(center.X, center.Y - r), true);
                    g.LineTo(new Point(center.X + r, center.Y));
                    g.LineTo(new Point(center.X, center.Y + r));
                    g.LineTo(new Point(center.X - r, center.Y));
                    g.EndFigure(true);
                }

                context.DrawGeometry(fill, pen, geometry);
                if (interpolation == Interpolation.Custom)
                    context.DrawEllipse(Brushes.Black, null, center, 1.6, 1.6);
                break;
            }
        }
    }

    private int TickStep()
    {
        foreach (var step in new[] { 1, 2, 5, 10, 15, 30, 60, 120, 300 })
        {
            if (step * _pixelsPerFrame >= 42)
                return step;
        }

        return 600;
    }

    private static FormattedText Text(string text, double size, IBrush brush) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, UiTypeface, size, brush);

    // ---------- Input ----------

    private (int Row, int Frame)? KeyAt(Point point)
    {
        if (_doc is null || point.X < LabelWidth || point.Y < RulerHeight)
            return null;
        var row = RowAt(point.Y);
        if (row < 0 || row >= _rows.Count)
            return null;
        (int, int)? best = null;
        var bestDistance = 8.0;
        foreach (var (frame, _) in KeysInRow(_rows[row]))
        {
            var d = Math.Abs(FrameToX(frame) - point.X);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = (row, frame);
            }
        }

        return best;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (_doc is null)
            return;
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;

        if (point.Y < RulerHeight && point.X >= LabelWidth)
        {
            _scrubbing = true;
            _doc.SetPlaying(false);
            _doc.SetFrame((int)Math.Round(XToFrame(point.X)));
            e.Pointer.Capture(this);
            return;
        }

        var row = RowAt(point.Y);
        if (point.X < LabelWidth)
        {
            if (row >= 0 && row < _rows.Count)
                _doc.Select(_rows[row].Select);
            return;
        }

        var key = KeyAt(point);
        if (properties.IsRightButtonPressed)
        {
            if (key is { } k && !_selectedKeys.Contains(k))
            {
                _selectedKeys.Clear();
                _selectedKeys.Add(k);
                SelectedKeysChanged?.Invoke();
                InvalidateVisual();
            }

            if (_selectedKeys.Count > 0)
                ShowKeyMenu();
            return;
        }

        if (!properties.IsLeftButtonPressed)
            return;

        if (key is { } hit)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (!_selectedKeys.Add(hit))
                    _selectedKeys.Remove(hit);
            }
            else if (!_selectedKeys.Contains(hit))
            {
                _selectedKeys.Clear();
                _selectedKeys.Add(hit);
            }

            _doc.Select(_rows[hit.Row].Select);
            _dragStart = point;
            _dragOffsetFrames = 0;
            e.Pointer.Capture(this);
            SelectedKeysChanged?.Invoke();
            InvalidateVisual();
            return;
        }

        // Empty space: move playhead there and select the row.
        _selectedKeys.Clear();
        SelectedKeysChanged?.Invoke();
        if (row >= 0 && row < _rows.Count)
            _doc.Select(_rows[row].Select);
        _scrubbing = true;
        _doc.SetPlaying(false);
        _doc.SetFrame((int)Math.Round(XToFrame(point.X)));
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_doc is null)
            return;
        var point = e.GetPosition(this);
        if (_scrubbing)
        {
            _doc.SetFrame((int)Math.Round(XToFrame(point.X)));
            return;
        }

        if (_dragStart is { } start)
        {
            var offset = (int)Math.Round((point.X - start.X) / _pixelsPerFrame);
            if (Math.Abs(point.X - start.X) > 3)
                _draggingKeys = true;
            if (_draggingKeys && offset != _dragOffsetFrames)
            {
                _dragOffsetFrames = offset;
                InvalidateVisual();
            }

            return;
        }

        Cursor = KeyAt(point) is not null ? new Cursor(StandardCursorType.SizeWestEast) : Cursor.Default;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _scrubbing = false;
        if (_doc is not null && _draggingKeys && _dragOffsetFrames != 0)
        {
            var offset = _dragOffsetFrames;
            _doc.BeginGesture();
            foreach (var group in _selectedKeys.GroupBy(k => k.Row).ToList())
                _doc.MoveKeys(_rows[group.Key].Tracks.ToList(), group.Select(k => k.Frame).ToList(), offset);
            _doc.EndGesture();
            var moved = _selectedKeys.Select(k => (k.Row, Math.Clamp(k.Frame + offset, 0, _doc.Animation.Length))).ToList();
            _selectedKeys.Clear();
            foreach (var k in moved)
                _selectedKeys.Add(k);
            SelectedKeysChanged?.Invoke();
        }

        _dragStart = null;
        _draggingKeys = false;
        _dragOffsetFrames = 0;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_doc is null)
            return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var anchor = XToFrame(e.GetPosition(this).X);
            _fitToWidth = false;
            _pixelsPerFrame = Math.Clamp(_pixelsPerFrame * (e.Delta.Y > 0 ? 1.2 : 1 / 1.2), 0.5, 60);
            _scrollFrames = Math.Max(0, anchor - (e.GetPosition(this).X - LabelWidth - 6) / _pixelsPerFrame);
            var fit = TrackWidth / Math.Max(1, _doc.Animation.Length);
            if (_pixelsPerFrame <= fit)
                _fitToWidth = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) || Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y))
        {
            if (!_fitToWidth)
                _scrollFrames = Math.Max(0, _scrollFrames - (e.Delta.Y + e.Delta.X) * 40 / _pixelsPerFrame);
        }
        else
        {
            var visibleRows = (Bounds.Height - RulerHeight) / RowHeight;
            _scrollRows = Math.Clamp(_scrollRows - e.Delta.Y, 0, Math.Max(0, _rows.Count - visibleRows + 0.5));
        }

        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Delete or Key.Back && _selectedKeys.Count > 0)
        {
            DeleteSelectedKeys();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void ShowKeyMenu()
    {
        var menu = new ContextMenu();
        var items = new List<object>();
        var current = SelectedInterpolation;
        foreach (var interpolation in InterpolationInfo.Presets)
        {
            var item = new MenuItem
            {
                Header = InterpolationInfo.FriendlyName(interpolation),
                Icon = new EasingIcon { Interpolation = interpolation, Width = 28, Height = 16 },
                IsChecked = current == interpolation,
                ToggleType = MenuItemToggleType.Radio,
            };
            ToolTip.SetTip(item, InterpolationInfo.Description(interpolation));
            item.Click += (_, _) => ApplyInterpolationToSelection(interpolation);
            items.Add(item);
        }

        items.Add(new Separator());
        var delete = new MenuItem { Header = "Delete key" };
        delete.Click += (_, _) => DeleteSelectedKeys();
        items.Add(delete);
        menu.ItemsSource = items;
        menu.Open(this);
    }
}
