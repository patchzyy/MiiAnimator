using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MiiAnimator.Panels;

/// <summary>Small factory for the consistent, uncluttered controls used by the side panels.</summary>
public static class Ui
{
    public static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(150, 156, 170));
    public static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(255, 165, 45));
    public static readonly IBrush CardBackground = new SolidColorBrush(Color.FromRgb(36, 39, 47));

    public static TextBlock Title(string text) =>
        new()
        {
            Text = text,
            FontSize = 17,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 0, 0, 2),
        };

    public static TextBlock Hint(string text) =>
        new()
        {
            Text = text,
            Foreground = Muted,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        };

    public static TextBlock Section(string text) =>
        new()
        {
            Text = text.ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Muted,
            Margin = new Thickness(0, 14, 0, 4),
            LetterSpacing = 0.6,
        };

    public static Button Button(string text, Action onClick, string? tip = null, bool accent = false)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(10, 5),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        if (accent)
            button.Classes.Add("accent");
        if (tip is not null)
            ToolTip.SetTip(button, tip);
        button.Click += (_, _) => onClick();
        return button;
    }

    public static WrapPanel Buttons(params Control[] buttons)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var b in buttons)
            panel.Children.Add(b);
        return panel;
    }

    public static Border Card(Control content) =>
        new()
        {
            Background = CardBackground,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 0, 0, 8),
            Child = content,
        };

    /// <summary>Wraps a burst of continuous edits (slider drags) into one undo step.</summary>
    public sealed class GestureBatcher(Action begin, Action end)
    {
        private DispatcherTimer? _timer;

        public void Touch()
        {
            if (_timer is null)
            {
                begin();
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
                _timer.Tick += (_, _) => Flush();
            }

            _timer.Stop();
            _timer.Start();
        }

        public void Flush()
        {
            if (_timer is null)
                return;
            _timer.Stop();
            _timer = null;
            end();
        }
    }
}

/// <summary>Label · slider · number box, with an optional "keyed here" diamond.</summary>
public sealed class ValueRow : Grid
{
    private readonly Slider _slider;
    private readonly TextBox _box;
    private readonly Border? _keyDot;
    private readonly string _format;
    private bool _updating;

    public ValueRow(string label, double min, double max, Action<double> onChange, string format = "0.#", string? tip = null, Action? onKeyToggle = null, double step = 1)
    {
        _format = format;
        ColumnDefinitions = new ColumnDefinitions("16,92,*,58");
        Margin = new Thickness(0, 1);

        if (onKeyToggle is not null)
        {
            _keyDot = new Border
            {
                Width = 9,
                Height = 9,
                RenderTransform = new RotateTransform(45),
                BorderBrush = Ui.Muted,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            ToolTip.SetTip(_keyDot, "Filled = keyed on this frame. Click to key / unkey.");
            _keyDot.PointerPressed += (_, _) => onKeyToggle();
            Children.Add(_keyDot);
        }

        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        if (tip is not null)
            ToolTip.SetTip(text, tip);
        SetColumn(text, 1);
        Children.Add(text);

        _slider = new Slider
        {
            Maximum = max,
            Minimum = min,
            Value = Math.Clamp(0, min, max),
            SmallChange = step,
            LargeChange = step * 10,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, -6, 6, -6),
        };
        // Attached after initialization: setting Minimum/Value above must not count as a user edit.
        _slider.ValueChanged += (_, e) =>
        {
            if (_updating)
                return;
            SetBox(e.NewValue);
            onChange(e.NewValue);
        };
        SetColumn(_slider, 2);
        Children.Add(_slider);

        _box = new TextBox
        {
            FontSize = 12,
            Padding = new Thickness(4, 2),
            MinHeight = 24,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Right,
        };
        void Commit()
        {
            if (
                double.TryParse(_box.Text?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && _box.Text != _lastText
            )
                onChange(value);
        }

        _box.LostFocus += (_, _) => Commit();
        _box.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                Commit();
        };
        SetColumn(_box, 3);
        Children.Add(_box);
    }

    public void SetValue(double value, bool keyed = false)
    {
        _updating = true;
        _slider.Value = Math.Clamp(value, _slider.Minimum, _slider.Maximum);
        _updating = false;
        if (!_box.IsFocused)
            SetBox(value);
        if (_keyDot is not null)
            _keyDot.Background = keyed ? Ui.Accent : Brushes.Transparent;
    }

    private string? _lastText;

    private void SetBox(double value) => _box.Text = _lastText = value.ToString(_format, CultureInfo.InvariantCulture);
}
