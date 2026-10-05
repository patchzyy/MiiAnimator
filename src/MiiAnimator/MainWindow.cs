using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Format;
using MiiAnimator.Editing;
using MiiAnimator.Panels;
using MiiAnimator.Timeline;
using MiiAnimator.Viewport;

namespace MiiAnimator;

public sealed class MainWindow : Window
{
    private static readonly FilePickerFileType MiiAnimType = new("Mii animation (.miianim)") { Patterns = ["*" + MiiAnimFormat.FileExtension] };

    private readonly AnimatorDocument _doc = new();
    private readonly HeadModelCache _heads = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly ViewportHost _viewport = new();
    private readonly DopeSheet _dopeSheet = new();
    private readonly CurveEditor _curveEditor = new();
    private readonly PosePanel _posePanel = new();
    private readonly MiiEditorPanel _miiPanel = new();
    private readonly TransportBar _transport;
    private readonly Grid _timelineArea;
    private readonly ToggleButton _advancedToggle;
    private readonly MenuItem _undoItem;
    private readonly MenuItem _redoItem;
    private bool _playbackLoopRunning;
    private TimeSpan? _lastFrameTime;
    private bool _closeConfirmed;

    public MainWindow()
    {
        Title = "Mii Animator";
        Width = 1440;
        Height = 900;
        MinWidth = 960;
        MinHeight = 600;
        Background = new SolidColorBrush(Color.FromRgb(24, 26, 31));
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _doc.AdvancedMode = _settings.AdvancedMode;
        _heads.SetResourcePath(_settings.ResolveFflPath());
        _doc.New(Templates.Empty());

        _viewport.Attach(_doc, _heads);
        _dopeSheet.Attach(_doc);
        _curveEditor.Attach(_doc);
        _posePanel.Attach(_doc);
        _miiPanel.Attach(_doc);
        _transport = new TransportBar(_doc, _dopeSheet);

        // ---------- Top bar ----------
        _undoItem = new MenuItem { Header = "_Undo", InputGesture = new KeyGesture(Key.Z, KeyModifiers.Control) };
        _undoItem.Click += (_, _) => _doc.Undo();
        _redoItem = new MenuItem { Header = "_Redo", InputGesture = new KeyGesture(Key.Y, KeyModifiers.Control) };
        _redoItem.Click += (_, _) => _doc.Redo();

        var menu = new Menu
        {
            ItemsSource = new[]
            {
                new MenuItem { Header = "_File", ItemsSource = BuildFileMenu() },
                new MenuItem
                {
                    Header = "_Edit",
                    ItemsSource = new object[]
                    {
                        _undoItem,
                        _redoItem,
                        new Separator(),
                        Item("Copy pose", "Ctrl+C", _doc.CopyPose),
                        Item("Paste pose", "Ctrl+V", () => _doc.PastePose(false)),
                        Item("Paste pose mirrored", "Ctrl+Shift+V", () => _doc.PastePose(true)),
                        Item("Mirror pose", "M", _doc.MirrorCurrentPose),
                        new Separator(),
                        Item("Key selection", "K", () => _doc.KeyPose()),
                        Item("Key whole pose", "Shift+K", () => _doc.KeyPose(wholeBody: true)),
                        Item("Delete keys on this frame", "Shift+Delete", () => _doc.DeleteKeysAtCurrentFrame(wholeBody: true)),
                    },
                },
                new MenuItem
                {
                    Header = "_View",
                    ItemsSource = new object[]
                    {
                        Item("Reset camera", "F", _viewport.FrameCharacter),
                        Item("Toggle onion skin", "O", ToggleOnion),
                        Item("Toggle curve editor", "G", () => _transport!.CurvesToggle.IsChecked = !_transport.CurvesToggle.IsChecked),
                    },
                },
                new MenuItem { Header = "_Help", ItemsSource = new object[] { Item("Controls & shortcuts", "F1", ShowHelp) } },
            },
        };

        _advancedToggle = new ToggleButton
        {
            Content = "Advanced",
            IsChecked = _doc.AdvancedMode,
            Padding = new Thickness(12, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
        };
        ToolTip.SetTip(_advancedToggle, "Simple: grouped timeline rows and just the essentials.\nAdvanced: every bone, raw scale, IK blend and exact IK positions.");
        _advancedToggle.IsCheckedChanged += (_, _) =>
        {
            _doc.AdvancedMode = _advancedToggle.IsChecked == true;
            _settings.AdvancedMode = _doc.AdvancedMode;
            _settings.Save();
            _doc.NotifySettingsChanged();
        };

        var titleText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.Muted, FontSize = 12.5 };
        var topBar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Background = new SolidColorBrush(Color.FromRgb(30, 32, 38)) };
        topBar.Children.Add(menu);
        Grid.SetColumn(titleText, 1);
        titleText.HorizontalAlignment = HorizontalAlignment.Center;
        topBar.Children.Add(titleText);
        var rightTools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(8, 3) };
        rightTools.Children.Add(Small("↶", "Undo (Ctrl+Z)", _doc.Undo));
        rightTools.Children.Add(Small("↷", "Redo (Ctrl+Y)", _doc.Redo));
        rightTools.Children.Add(_advancedToggle);
        rightTools.Children.Add(Small("?", "Controls & shortcuts (F1)", ShowHelp));
        Grid.SetColumn(rightTools, 2);
        topBar.Children.Add(rightTools);

        // ---------- Side panel ----------
        var animationPanel = BuildAnimationPanel();
        var tabs = new TabControl
        {
            ItemsSource = new[]
            {
                new TabItem { Header = "Pose", Content = _posePanel, FontSize = 14 },
                new TabItem { Header = "Mii", Content = _miiPanel, FontSize = 14 },
                new TabItem { Header = "Animation", Content = animationPanel, FontSize = 14 },
            },
            Padding = new Thickness(0),
        };
        var side = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 32, 38)),
            Child = tabs,
            BorderBrush = new SolidColorBrush(Color.FromRgb(20, 21, 26)),
            BorderThickness = new Thickness(1, 0, 0, 0),
        };

        // ---------- Timeline ----------
        _timelineArea = new Grid { ColumnDefinitions = new ColumnDefinitions("*,0,0") };
        _timelineArea.Children.Add(_dopeSheet);
        var curveSplitter = new GridSplitter { Background = new SolidColorBrush(Color.FromRgb(20, 21, 26)), ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(curveSplitter, 1);
        _timelineArea.Children.Add(curveSplitter);
        Grid.SetColumn(_curveEditor, 2);
        _timelineArea.Children.Add(_curveEditor);
        _transport.CurvesToggle.IsChecked = _settings.ShowCurveEditor;
        _transport.CurvesToggle.IsCheckedChanged += (_, _) => UpdateCurveLayout();
        UpdateCurveLayout();

        var timeline = new DockPanel();
        DockPanel.SetDock(_transport, Dock.Top);
        timeline.Children.Add(_transport);
        timeline.Children.Add(_timelineArea);

        // ---------- Layout ----------
        var main = new Grid { ColumnDefinitions = new ColumnDefinitions("*,4,340") };
        main.Children.Add(_viewport);
        var sideSplitter = new GridSplitter { Background = Brushes.Transparent, ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(sideSplitter, 1);
        main.Children.Add(sideSplitter);
        Grid.SetColumn(side, 2);
        main.Children.Add(side);

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,4,250") };
        root.Children.Add(topBar);
        Grid.SetRow(main, 1);
        root.Children.Add(main);
        var timelineSplitter = new GridSplitter { Background = new SolidColorBrush(Color.FromRgb(20, 21, 26)), ResizeDirection = GridResizeDirection.Rows };
        Grid.SetRow(timelineSplitter, 2);
        root.Children.Add(timelineSplitter);
        Grid.SetRow(timeline, 3);
        root.Children.Add(timeline);
        Content = root;

        // ---------- Wiring ----------
        void UpdateTitle()
        {
            Title = $"{_doc.Title} — Mii Animator";
            titleText.Text = _doc.Title;
            _undoItem.IsEnabled = _doc.CanUndo;
            _redoItem.IsEnabled = _doc.CanRedo;
        }

        _doc.FileChanged += UpdateTitle;
        _doc.AnimationChanged += UpdateTitle;
        _doc.FrameChanged += EnsurePlaybackLoop;
        UpdateTitle();

        // Open a .miianim passed on the command line (also used for file associations).
        var startupFile = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(a => a.EndsWith(MiiAnimFormat.FileExtension, StringComparison.OrdinalIgnoreCase));
        if (startupFile is not null && File.Exists(startupFile))
        {
            try
            {
                _doc.Open(startupFile);
            }
            catch
            {
                // Fall back to an empty animation.
            }
        }

        AddHandler(KeyDownEvent, OnWindowKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Closing += OnClosing;
    }

    // ---------- Menus ----------

    private object[] BuildFileMenu()
    {
        var newItems = Templates
            .All.Select(t =>
            {
                var item = new MenuItem { Header = t.Name };
                ToolTip.SetTip(item, t.Description);
                item.Click += async (_, _) =>
                {
                    if (await ConfirmDiscard())
                        _doc.New(t.Create());
                };
                return item;
            })
            .ToArray();

        return
        [
            new MenuItem { Header = "_New", ItemsSource = newItems },
            Item("_Open…", "Ctrl+O", () => _ = OpenAsync()),
            Item("_Save", "Ctrl+S", () => _ = SaveAsync(false)),
            Item("Save _As…", "Ctrl+Shift+S", () => _ = SaveAsync(true)),
            new Separator(),
            Item("Locate FFLResHigh.dat…", null, () => _ = LocateFflAsync()),
            new Separator(),
            Item("E_xit", null, Close),
        ];
    }

    private static MenuItem Item(string header, string? gesture, Action action)
    {
        var item = new MenuItem { Header = header };
        if (gesture is not null)
            item.InputGesture = KeyGesture.Parse(gesture);
        item.Click += (_, _) => action();
        return item;
    }

    private static Button Small(string text, string tip, Action action)
    {
        var button = new Button
        {
            Content = text,
            Width = 32,
            Height = 28,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = false,
        };
        ToolTip.SetTip(button, tip);
        button.Click += (_, _) => action();
        return button;
    }

    private Control BuildAnimationPanel()
    {
        var root = new StackPanel { Margin = new Thickness(14, 12, 14, 16) };
        root.Children.Add(Ui.Title("Animation"));
        root.Children.Add(Ui.Hint("Animations loop: after the last frame they start again from frame 0. Make your first and last pose the same for a seamless loop."));
        var name = new TextBox { Watermark = "Name", MaxLength = 64 };
        name.LostFocus += (_, _) => _doc.SetName(name.Text ?? "");
        root.Children.Add(Ui.Section("Name"));
        root.Children.Add(name);

        var info = Ui.Hint("");
        root.Children.Add(Ui.Section("Length"));
        root.Children.Add(info);
        root.Children.Add(
            Ui.Buttons(
                Ui.Button("1 s", () => _doc.SetLength(_doc.Animation.Fps)),
                Ui.Button("2 s", () => _doc.SetLength(_doc.Animation.Fps * 2)),
                Ui.Button("4 s", () => _doc.SetLength(_doc.Animation.Fps * 4)),
                Ui.Button("8 s", () => _doc.SetLength(_doc.Animation.Fps * 8))
            )
        );
        root.Children.Add(Ui.Section("Loop"));
        root.Children.Add(
            Ui.Buttons(
                Ui.Button(
                    "Copy first pose to end",
                    () =>
                    {
                        var frame = _doc.CurrentFrame;
                        _doc.BeginGesture();
                        _doc.SetFrame(0);
                        _doc.CopyPose();
                        _doc.SetFrame(_doc.Animation.Length);
                        var autoKey = _doc.AutoKey;
                        _doc.AutoKey = true;
                        _doc.PastePose(false);
                        _doc.AutoKey = autoKey;
                        _doc.EndGesture();
                        _doc.SetFrame(frame);
                    },
                    "Keys the frame-0 pose on the last frame so the loop has no jump."
                )
            )
        );

        root.Children.Add(Ui.Section("Events"));
        root.Children.Add(
            Ui.Hint("Named markers that apps like WheelWizard react to when playback reaches them (e.g. swap_gender, play_sound).")
        );
        var eventList = new StackPanel { Spacing = 4 };
        root.Children.Add(eventList);
        root.Children.Add(Ui.Buttons(Ui.Button("Add event here", _doc.AddEvent, "Adds an event marker on the current frame.")));
        List<AnimEvent> shownEvents = [];

        void RefreshEvents()
        {
            if (shownEvents.SequenceEqual(_doc.Animation.Events))
                return;
            shownEvents = [.. _doc.Animation.Events];
            eventList.Children.Clear();
            foreach (var animEvent in shownEvents)
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("58,*,Auto,Auto"), ColumnSpacing = 4 };
                var frameBox = new TextBox { Text = animEvent.Frame.ToString(CultureInfo.InvariantCulture), FontSize = 12.5 };
                var nameBox = new TextBox { Text = animEvent.Name, FontSize = 12.5, MaxLength = AnimEvent.MaxNameLength };
                void Commit()
                {
                    var frame = int.TryParse(frameBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var f) ? f : animEvent.Frame;
                    _doc.ReplaceEvent(animEvent, frame, nameBox.Text ?? "");
                }

                foreach (var box in new[] { frameBox, nameBox })
                {
                    box.LostFocus += (_, _) => Commit();
                    box.KeyDown += (_, e) =>
                    {
                        if (e.Key == Key.Enter)
                            Commit();
                    };
                }

                var go = Ui.Button("Go", () => _doc.SetFrame(animEvent.Frame), "Jump to this event.");
                var remove = Ui.Button("✕", () => _doc.RemoveEvent(animEvent), "Delete this event.");
                Grid.SetColumn(nameBox, 1);
                Grid.SetColumn(go, 2);
                Grid.SetColumn(remove, 3);
                row.Children.Add(frameBox);
                row.Children.Add(nameBox);
                row.Children.Add(go);
                row.Children.Add(remove);
                eventList.Children.Add(row);
            }
        }

        var file = Ui.Hint("");
        root.Children.Add(Ui.Section("File"));
        root.Children.Add(file);

        void Refresh()
        {
            if (!name.IsFocused)
                name.Text = _doc.Animation.Name;
            info.Text = $"{_doc.Animation.Length} frames at {_doc.Animation.Fps} fps = {_doc.Animation.DurationSeconds:0.##} seconds.";
            var keys = _doc.Animation.Tracks.Sum(t => t.Value.Count);
            file.Text = $"{keys} keys on {_doc.Animation.Tracks.Count} channels · about {MiiAnimFormat.Write(_doc.Animation).Length + 74} bytes as .miianim"
                + (_doc.FilePath is null ? "" : $"\n{_doc.FilePath}");
            RefreshEvents();
        }

        _doc.AnimationChanged += Refresh;
        _doc.FileChanged += Refresh;
        Refresh();
        return new ScrollViewer { Content = root };
    }

    private void UpdateCurveLayout()
    {
        var show = _transport.CurvesToggle.IsChecked == true;
        _settings.ShowCurveEditor = show;
        _settings.Save();
        _timelineArea.ColumnDefinitions[1].Width = new GridLength(show ? 4 : 0);
        _timelineArea.ColumnDefinitions[2].Width = show ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        _curveEditor.IsVisible = show;
    }

    private void ToggleOnion()
    {
        _doc.OnionSkin = !_doc.OnionSkin;
        _doc.NotifySettingsChanged();
    }

    // ---------- Playback ----------

    private void EnsurePlaybackLoop()
    {
        if (!_doc.IsPlaying || _playbackLoopRunning)
            return;
        _playbackLoopRunning = true;
        _lastFrameTime = null;
        RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan time)
    {
        if (!_doc.IsPlaying)
        {
            _playbackLoopRunning = false;
            return;
        }

        if (_lastFrameTime is { } last)
            _doc.Tick(Math.Min(0.1, (time - last).TotalSeconds));
        _lastFrameTime = time;
        RequestAnimationFrame(OnAnimationFrame);
    }

    // ---------- Keyboard ----------

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (FocusManager?.GetFocusedElement() is TextBox or NumericUpDown)
            return;
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var handled = true;
        switch (e.Key)
        {
            case Key.Space:
                _doc.SetPlaying(!_doc.IsPlaying);
                break;
            case Key.Left:
                _doc.SetFrame(_doc.CurrentFrame - (shift ? 10 : 1));
                break;
            case Key.Right:
                _doc.SetFrame(_doc.CurrentFrame + (shift ? 10 : 1));
                break;
            case Key.Up:
                _doc.SetFrame(_doc.AllKeyFrames().Where(f => f < _doc.CurrentFrame).DefaultIfEmpty(0).Max());
                break;
            case Key.Down:
                _doc.SetFrame(_doc.AllKeyFrames().Where(f => f > _doc.CurrentFrame).DefaultIfEmpty(_doc.Animation.Length).Min());
                break;
            case Key.Home:
                _doc.SetFrame(0);
                break;
            case Key.End:
                _doc.SetFrame(_doc.Animation.Length);
                break;
            case Key.K:
                _doc.KeyPose(wholeBody: shift);
                break;
            case Key.Delete when shift:
                _doc.DeleteKeysAtCurrentFrame(wholeBody: true);
                break;
            case Key.Delete or Key.Back when _dopeSheet.HasSelectedKeys:
                _dopeSheet.DeleteSelectedKeys();
                break;
            case Key.Delete:
                _doc.DeleteKeysAtCurrentFrame();
                break;
            case Key.Z when ctrl && shift:
            case Key.Y when ctrl:
                _doc.Redo();
                break;
            case Key.Z when ctrl:
                _doc.Undo();
                break;
            case Key.C when ctrl:
                _doc.CopyPose();
                break;
            case Key.V when ctrl:
                _doc.PastePose(shift);
                break;
            case Key.S when ctrl:
                _ = SaveAsync(shift);
                break;
            case Key.O when ctrl:
                _ = OpenAsync();
                break;
            case Key.M:
                _doc.MirrorCurrentPose();
                break;
            case Key.O:
                ToggleOnion();
                break;
            case Key.G:
                _transport.CurvesToggle.IsChecked = !_transport.CurvesToggle.IsChecked;
                break;
            case Key.F when !_curveEditor.IsFocused:
                _viewport.FrameCharacter();
                break;
            case Key.Escape:
                _doc.Select(Selection.None);
                break;
            case Key.F1:
                ShowHelp();
                break;
            default:
                handled = false;
                break;
        }

        if (handled)
            e.Handled = true;
    }

    // ---------- Files ----------

    private async Task<bool> ConfirmDiscard()
    {
        if (!_doc.IsDirty)
            return true;
        var choice = await MessageBox.Ask(this, "Unsaved changes", $"Save changes to \"{_doc.Animation.Name}\" first?", "Save", "Don't save", "Cancel");
        return choice switch
        {
            0 => await SaveAsync(false),
            1 => true,
            _ => false,
        };
    }

    private async Task OpenAsync()
    {
        if (!await ConfirmDiscard())
            return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Open animation", FileTypeFilter = [MiiAnimType] });
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path)
            return;
        try
        {
            _doc.Open(path);
        }
        catch (Exception exception)
        {
            await MessageBox.Show(this, "Couldn't open animation", exception.Message);
        }
    }

    private async Task<bool> SaveAsync(bool saveAs)
    {
        var path = _doc.FilePath;
        if (saveAs || path is null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save animation",
                    SuggestedFileName = _doc.Animation.Name + MiiAnimFormat.FileExtension,
                    DefaultExtension = MiiAnimFormat.FileExtension.TrimStart('.'),
                    FileTypeChoices = [MiiAnimType],
                }
            );
            path = file?.TryGetLocalPath();
            if (path is null)
                return false;
        }

        try
        {
            _doc.Save(path);
            return true;
        }
        catch (Exception exception)
        {
            await MessageBox.Show(this, "Couldn't save animation", exception.Message);
            return false;
        }
    }

    private async Task LocateFflAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Locate FFLResHigh.dat",
                FileTypeFilter = [new FilePickerFileType("FFLResHigh.dat") { Patterns = ["FFLResHigh.dat", "*.dat"] }],
            }
        );
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path)
            return;
        _settings.FflResourcePath = path;
        _settings.Save();
        _heads.SetResourcePath(path);
        _heads.Prewarm(_doc.StudioData);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || !_doc.IsDirty)
            return;
        e.Cancel = true;
        if (await ConfirmDiscard())
        {
            _closeConfirmed = true;
            Close();
        }
    }

    private void ShowHelp()
    {
        const string text = """
            POSING
              Click a body part ............ select it (hover highlights what you'll get)
              Colored rings ................ rotate around that axis (Ctrl = snap 15°)
              White ring ................... rotate towards the screen
              Orange square ................ move the body / drag a hand or foot (IK)
              Round knob (IK) .............. aim the elbow / knee
              Esc .......................... deselect

            CAMERA
              Right-drag / Alt+drag ........ orbit
              Middle-drag / Shift+right .... pan
              Wheel ........................ zoom      F ... reset camera

            TIMELINE
              Space ........................ play / pause
              ← / → (Shift = 10) ........... previous / next frame
              ↑ / ↓ ........................ previous / next key
              K / Shift+K .................. key selection / key whole pose
              Delete / Shift+Delete ........ delete keys (selected / whole frame)
              Click a key, drag ............ retime it   (right-click: easing)
              Ctrl+wheel ................... zoom timeline
              G ............................ curve editor   O ... onion skin

            POSE
              Ctrl+C / Ctrl+V .............. copy / paste pose
              Ctrl+Shift+V ................. paste mirrored     M ... mirror pose
              Ctrl+Z / Ctrl+Y .............. undo / redo
            """;
        var window = new Window
        {
            Title = "Controls & shortcuts",
            Width = 560,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas, Menlo, monospace"),
                FontSize = 13,
                Margin = new Thickness(20),
            },
        };
        window.ShowDialog(this);
    }
}
