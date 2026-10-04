using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using MiiAnim.Core.Animation;
using MiiAnimator.Editing;
using MiiAnimator.Panels;
using MiiAnimator.Timeline;

namespace MiiAnimator;

/// <summary>Playback controls and timeline settings above the dope sheet.</summary>
public sealed class TransportBar : Border
{
    private readonly AnimatorDocument _doc;
    private readonly DopeSheet _dopeSheet;
    private readonly Button _play;
    private readonly NumericUpDown _frame;
    private readonly NumericUpDown _length;
    private readonly ComboBox _fps;
    private readonly ToggleButton _autoKey;
    private readonly ToggleButton _onion;
    private readonly ComboBox _easing;
    private readonly TextBlock _easingLabel;
    private bool _updating;

    public ToggleButton CurvesToggle { get; }

    public TransportBar(AnimatorDocument doc, DopeSheet dopeSheet)
    {
        _doc = doc;
        _dopeSheet = dopeSheet;
        Background = new SolidColorBrush(Color.FromRgb(34, 37, 44));
        Padding = new Thickness(8, 5);
        BorderBrush = new SolidColorBrush(Color.FromRgb(20, 21, 26));
        BorderThickness = new Thickness(0, 1, 0, 1);

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        row.Children.Add(IconButton("⏮", "Go to start (Home)", () => doc.SetFrame(0)));
        row.Children.Add(IconButton("◆◀", "Previous key (Up)", () => JumpKey(-1)));
        row.Children.Add(IconButton("◀", "Previous frame (Left)", () => doc.SetFrame(doc.CurrentFrame - 1)));
        _play = IconButton("▶", "Play / Pause (Space)", () => doc.SetPlaying(!doc.IsPlaying));
        _play.Width = 46;
        _play.Classes.Add("accent");
        row.Children.Add(_play);
        row.Children.Add(IconButton("▶", "Next frame (Right)", () => doc.SetFrame(doc.CurrentFrame + 1)));
        row.Children.Add(IconButton("▶◆", "Next key (Down)", () => JumpKey(1)));
        row.Children.Add(IconButton("⏭", "Go to end (End)", () => doc.SetFrame(doc.Animation.Length)));

        row.Children.Add(Spacer());
        row.Children.Add(Label("Frame"));
        _frame = Number(0, 100000, 76);
        _frame.ValueChanged += (_, e) =>
        {
            if (!_updating && e.NewValue is { } v)
                doc.SetFrame((int)v);
        };
        row.Children.Add(_frame);
        row.Children.Add(Label("of"));
        _length = Number(1, 36000, 76);
        ToolTip.SetTip(_length, "Loop length in frames. Playback wraps back to frame 0 after this.");
        _length.ValueChanged += (_, e) =>
        {
            if (!_updating && e.NewValue is { } v)
                doc.SetLength((int)v);
        };
        row.Children.Add(_length);
        _fps = new ComboBox { ItemsSource = new[] { "24 fps", "30 fps", "60 fps" }, Width = 88, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(_fps, "Frames per second. 60 is smooth; 24/30 give a classic cartoon feel.");
        _fps.SelectionChanged += (_, _) =>
        {
            if (!_updating && _fps.SelectedIndex >= 0)
                doc.SetFps(new[] { 24, 30, 60 }[_fps.SelectedIndex]);
        };
        row.Children.Add(_fps);

        row.Children.Add(Spacer());
        _autoKey = Toggle("● Auto-key", "On: every change you make is saved as a key on the current frame.\nOff: changes are a preview until you press K.");
        _autoKey.IsCheckedChanged += (_, _) =>
        {
            doc.AutoKey = _autoKey.IsChecked == true;
            Refresh();
        };
        row.Children.Add(_autoKey);
        _onion = Toggle("Onion skin", "Shows ghosts of the previous (blue) and next (green) keyed pose.");
        _onion.IsCheckedChanged += (_, _) =>
        {
            doc.OnionSkin = _onion.IsChecked == true;
            doc.NotifySettingsChanged();
        };
        row.Children.Add(_onion);
        CurvesToggle = Toggle("Curves", "Show the curve editor to fine-tune the motion of the selected part.");
        row.Children.Add(CurvesToggle);

        row.Children.Add(Spacer());
        _easingLabel = Label("Selected keys:");
        row.Children.Add(_easingLabel);
        _easing = new ComboBox { Width = 170, VerticalAlignment = VerticalAlignment.Center };
        _easing.ItemsSource = InterpolationInfo
            .Presets.Select(p => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new EasingIcon { Interpolation = p, Width = 26, Height = 14 },
                    new TextBlock { Text = InterpolationInfo.FriendlyName(p), Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center },
                },
            })
            .ToList();
        _easing.SelectionChanged += (_, _) =>
        {
            if (!_updating && _easing.SelectedIndex >= 0)
                _dopeSheet.ApplyInterpolationToSelection(InterpolationInfo.Presets[_easing.SelectedIndex]);
        };
        row.Children.Add(_easing);

        Child = new ScrollViewer
        {
            Content = row,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        doc.FrameChanged += Refresh;
        doc.AnimationChanged += Refresh;
        doc.FileChanged += Refresh;
        doc.SettingsChanged += Refresh;
        dopeSheet.SelectedKeysChanged += Refresh;
        Refresh();
    }

    private void JumpKey(int direction)
    {
        var keys = _doc.AllKeyFrames();
        var target = direction < 0 ? keys.Where(f => f < _doc.CurrentFrame).DefaultIfEmpty(0).Max() : keys.Where(f => f > _doc.CurrentFrame).DefaultIfEmpty(_doc.Animation.Length).Min();
        _doc.SetFrame(target);
    }

    public void Refresh()
    {
        _updating = true;
        _play.Content = _doc.IsPlaying ? "⏸" : "▶";
        _frame.Value = _doc.CurrentFrame;
        _frame.Maximum = _doc.Animation.Length;
        _length.Value = _doc.Animation.Length;
        _fps.SelectedIndex = _doc.Animation.Fps switch
        {
            24 => 0,
            30 => 1,
            60 => 2,
            _ => -1,
        };
        _autoKey.IsChecked = _doc.AutoKey;
        _autoKey.Foreground = _doc.AutoKey ? new SolidColorBrush(Color.FromRgb(255, 110, 100)) : null;
        _onion.IsChecked = _doc.OnionSkin;
        var hasKeys = _dopeSheet.HasSelectedKeys;
        _easing.IsVisible = hasKeys;
        _easingLabel.IsVisible = hasKeys;
        if (hasKeys)
        {
            var shared = _dopeSheet.SelectedInterpolation;
            _easing.SelectedIndex = shared is { } s ? Array.IndexOf(InterpolationInfo.Presets, s) : -1;
        }

        _updating = false;
    }

    private static Button IconButton(string text, string tip, Action click)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 34,
            Height = 30,
            Padding = new Thickness(6, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 13,
            Focusable = false,
        };
        ToolTip.SetTip(button, tip);
        button.Click += (_, _) => click();
        return button;
    }

    private static ToggleButton Toggle(string text, string tip)
    {
        var toggle = new ToggleButton
        {
            Content = text,
            Height = 30,
            Padding = new Thickness(10, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12.5,
            Focusable = false,
        };
        ToolTip.SetTip(toggle, tip);
        return toggle;
    }

    private static NumericUpDown Number(double min, double max, double width) =>
        new()
        {
            Minimum = (decimal)min,
            Maximum = (decimal)max,
            Increment = 1,
            FormatString = "0",
            Width = width + 40,
            VerticalAlignment = VerticalAlignment.Center,
            ButtonSpinnerLocation = Location.Right,
        };

    private static TextBlock Label(string text) =>
        new()
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Ui.Muted,
            FontSize = 12.5,
            Margin = new Thickness(4, 0, 2, 0),
        };

    private static Control Spacer() =>
        new Border
        {
            Width = 1,
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            Margin = new Thickness(8, 4),
        };
}
