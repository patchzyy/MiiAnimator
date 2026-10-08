using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using MiiAnimator.Authoring;
using MiiAnimator.Editing;
using SkiaSharp;
using Svg.Skia;

namespace MiiAnimator.Panels;

/// <summary>
/// Lists the animation's particle effects and edits the selected one. Effects are stored in the .miianim file,
/// so whatever is set up here plays the same in WheelWizard.
/// </summary>
public sealed class ParticlePanel : ScrollViewer
{
    private readonly StackPanel _root = new() { Margin = new Thickness(14, 12, 14, 16) };
    private readonly StackPanel _list = new() { Spacing = 4 };
    private readonly StackPanel _editor = new();
    private readonly List<Action> _refreshers = [];
    private AnimatorDocument? _doc;
    private Ui.GestureBatcher? _batch;
    private string _listSignature = "";
    private (int Index, int Count, int Actors) _builtFor = (-2, -1, -1);

    // Subclasses don't pick up the ScrollViewer template on their own; without it nothing scrolls.
    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    public ParticlePanel()
    {
        Content = _root;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    public void Attach(AnimatorDocument document)
    {
        _doc = document;
        _batch = new Ui.GestureBatcher(document.BeginGesture, document.EndGesture);
        _root.Children.Add(Ui.Title("Particles"));
        _root.Children.Add(
            Ui.Hint(
                "Effects are saved inside the .miianim, so they play in WheelWizard without extra code. "
                    + "They're recalculated from the frame, so scrubbing shows exactly what plays."
            )
        );
        _root.Children.Add(Ui.Section("Effects"));
        _root.Children.Add(_list);
        _root.Children.Add(Ui.Section("Add effect at the playhead"));
        _root.Children.Add(
            Ui.Buttons(ParticlePresets.All.Select(p => (Control)Ui.Button(p.Name, () => document.AddParticle(p.Create()), p.Description)).ToArray())
        );
        _root.Children.Add(_editor);

        document.AnimationChanged += Sync;
        document.FileChanged += Sync;
        document.ParticleSelectionChanged += Sync;
        Sync();
    }

    private void Sync()
    {
        var doc = _doc!;
        var effects = doc.Root.Particles;
        var signature = string.Join("|", effects.Select((e, i) => $"{e.Name}:{e.StartFrame}:{e.Duration}:{(i == doc.SelectedParticle ? 1 : 0)}"));
        if (signature != _listSignature)
        {
            _listSignature = signature;
            RebuildList();
        }

        var builtFor = (doc.SelectedParticle, effects.Count, doc.Root.ActorCount);
        if (builtFor != _builtFor)
        {
            _builtFor = builtFor;
            RebuildEditor();
        }

        foreach (var refresh in _refreshers)
            refresh();
    }

    private void RebuildList()
    {
        var doc = _doc!;
        _list.Children.Clear();
        if (doc.Root.Particles.Count == 0)
            _list.Children.Add(Ui.Hint("No effects yet. Move the playhead to where it should happen and add one below."));
        for (var i = 0; i < doc.Root.Particles.Count; i++)
        {
            var index = i;
            var effect = doc.Root.Particles[i];
            var end = effect.StartFrame + effect.Duration;
            var button = new Button
            {
                Content = $"{effect.Name}   ·   frame {effect.StartFrame}{(effect.Duration > 0 ? $"–{end}" : "")}",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 5),
            };
            if (index == doc.SelectedParticle)
                button.Classes.Add("accent");
            button.Click += (_, _) =>
            {
                doc.SelectParticle(index);
                doc.SetFrame(effect.StartFrame);
            };
            _list.Children.Add(button);
        }
    }

    private void RebuildEditor()
    {
        var doc = _doc!;
        _editor.Children.Clear();
        _refreshers.Clear();
        var index = doc.SelectedParticle;
        if (index < 0 || index >= doc.Root.Particles.Count)
            return;

        ParticleEffect Effect() => doc.Root.Particles[Math.Min(index, doc.Root.Particles.Count - 1)];
        void Change(Action<ParticleEffect> change)
        {
            _batch!.Touch();
            doc.EditParticle(index, change);
        }

        _editor.Children.Add(Ui.Section("Selected effect"));
        var name = new TextBox { MaxLength = ParticleEffect.MaxNameLength, Watermark = "Name" };
        name.LostFocus += (_, _) =>
        {
            var text = (name.Text ?? "").Trim();
            if (text.Length > 0 && text != Effect().Name)
                doc.EditParticle(index, e => e.Name = text);
        };
        _refreshers.Add(() =>
        {
            if (!name.IsFocused)
                name.Text = Effect().Name;
        });
        _editor.Children.Add(name);
        _editor.Children.Add(
            Ui.Buttons(
                Ui.Button("Play from here", () => doc.SetFrame(Effect().StartFrame), "Jump to where this effect starts."),
                Ui.Button("New random look", () => doc.EditParticle(index, e => e.Seed = (uint)Random.Shared.Next(1, int.MaxValue)), "Same settings, different random particles."),
                Ui.Button("Duplicate", () => doc.DuplicateParticle(index)),
                Ui.Button("Delete", () => doc.RemoveParticle(index))
            )
        );

        // When
        _editor.Children.Add(Ui.Section("When"));
        Number("Start frame", 0, Math.Max(1, doc.Animation.Length), e => e.StartFrame, (e, v) => e.StartFrame = (int)Math.Round(v), "0", "Frame the effect starts.");
        Number("Spread over", 0, Math.Max(1, doc.Animation.Length), e => e.Duration, (e, v) => e.Duration = (int)Math.Round(v), "0", "0 = all at once (a burst). Otherwise particles keep coming for this many frames.");
        Number("Count", 1, 200, e => e.Count, (e, v) => e.Count = (int)Math.Round(v), "0", "How many particles in total.");

        // Where
        _editor.Children.Add(Ui.Section("Where"));
        if (doc.Root.ActorCount > 1)
            Choice("Mii", Enumerable.Range(0, doc.Root.ActorCount).Select(doc.ActorName).ToArray(), e => e.Actor, (e, v) => e.Actor = v);
        var bones = MiiSkeletonInfo.AllBones;
        Choice(
            "Starts at",
            new[] { "Ground under the Mii" }.Concat(bones.Select(MiiSkeletonInfo.FriendlyName)).ToArray(),
            e => e.Bone is { } bone ? Array.IndexOf(bones, bone) + 1 : 0,
            (e, v) => e.Bone = v == 0 ? null : bones[v - 1]
        );
        Number("Offset X", -150, 150, e => e.Offset.X, (e, v) => e.Offset = e.Offset with { X = (float)v }, tip: "Moves the start point (character space, + = Mii's left).");
        Number("Offset Y", -150, 150, e => e.Offset.Y, (e, v) => e.Offset = e.Offset with { Y = (float)v }, tip: "+ = up.");
        Number("Offset Z", -150, 150, e => e.Offset.Z, (e, v) => e.Offset = e.Offset with { Z = (float)v }, tip: "+ = in front of the Mii.");
        _editor.Children.Add(Ui.Hint("Particles start in an ellipsoid around that point. Set Y to 0 for a disc on the ground, and the shell to 0 for a ring."));
        Number("Area X", 0, 150, e => e.SpawnRadius.X, (e, v) => e.SpawnRadius = e.SpawnRadius with { X = (float)v }, tip: "Radius sideways.");
        Number("Area Y", 0, 150, e => e.SpawnRadius.Y, (e, v) => e.SpawnRadius = e.SpawnRadius with { Y = (float)v }, tip: "Radius up and down.");
        Number("Area Z", 0, 150, e => e.SpawnRadius.Z, (e, v) => e.SpawnRadius = e.SpawnRadius with { Z = (float)v }, tip: "Radius front to back.");
        Number("Area shell", 0, 1, e => e.SpawnShell, (e, v) => e.SpawnShell = (float)v, "0.##", "1 = anywhere inside, 0 = only on the edge.", 0.05);

        // Motion
        _editor.Children.Add(Ui.Section("Motion"));
        _editor.Children.Add(Ui.Hint("Direction 0, 0, 0 means \"outwards\" from the spawn area (good for poofs and dust rings)."));
        Number("Direction X", -1, 1, e => e.Direction.X, (e, v) => e.Direction = e.Direction with { X = (float)v }, "0.##", step: 0.05);
        Number("Direction Y", -1, 1, e => e.Direction.Y, (e, v) => e.Direction = e.Direction with { Y = (float)v }, "0.##", step: 0.05);
        Number("Direction Z", -1, 1, e => e.Direction.Z, (e, v) => e.Direction = e.Direction with { Z = (float)v }, "0.##", step: 0.05);
        Number("Spread", 0, 180, e => e.Spread, (e, v) => e.Spread = (float)v, tip: "Cone around the direction, in degrees (180 = every way).");
        Number("Speed min", 0, 600, e => e.SpeedMin, (e, v) => e.SpeedMin = (float)v, tip: "Units per second (a Mii is about 220 tall).");
        Number("Speed max", 0, 600, e => e.SpeedMax, (e, v) => e.SpeedMax = (float)v);
        Number("Gravity", -900, 400, e => e.Gravity, (e, v) => e.Gravity = (float)v, tip: "Negative falls, positive rises like smoke.");
        Number("Air drag", 0, 10, e => e.Drag, (e, v) => e.Drag = (float)v, "0.#", "Higher = slows down sooner.", 0.1);
        Number("Spin min", -720, 720, e => e.SpinMin, (e, v) => e.SpinMin = (float)v, tip: "Degrees per second.");
        Number("Spin max", -720, 720, e => e.SpinMax, (e, v) => e.SpinMax = (float)v);
        Check("Stop at the floor", e => e.StopAtFloor, (e, v) => e.StopAtFloor = v);

        // Look
        _editor.Children.Add(Ui.Section("Look"));
        ShapePicker();
        Choice("Facing", ["Spins", "Upright", "Along its motion"], e => (int)e.Orientation, (e, v) => e.Orientation = (ParticleOrientation)v);
        Number("Width", 0.05, 4, e => e.Aspect, (e, v) => e.Aspect = (float)v, "0.##", "Width compared to height (1 = as drawn, 0.4 = narrow).", 0.05);
        Number("Motion stretch", 0, 0.5, e => e.Stretch, (e, v) => e.Stretch = (float)v, "0.###", "Stretches the image along its height by how far it moves in this many seconds (sparks).", 0.01);
        Number("Life min", 0.05, 4, e => e.LifeMin, (e, v) => e.LifeMin = (float)v, "0.##", "Seconds each particle lives.", 0.05);
        Number("Life max", 0.05, 4, e => e.LifeMax, (e, v) => e.LifeMax = (float)v, "0.##", step: 0.05);
        Number("Size start", 0, 200, e => e.SizeStart, (e, v) => e.SizeStart = (float)v);
        Number("Size end", 0, 200, e => e.SizeEnd, (e, v) => e.SizeEnd = (float)v);
        ColorRow("Colour start", e => e.ColorStart, (e, v) => e.ColorStart = v);
        ColorRow("Colour end", e => e.ColorEnd, (e, v) => e.ColorEnd = v);
        _editor.Children.Add(Ui.Hint("Colours blend from start to end over a particle's life. Alpha 00 at the end fades it out."));
        Choice("Tint", ["None", "Mii's favourite colour"], e => (int)e.Tint, (e, v) => e.Tint = (ParticleTint)v);
        Check("Glow (additive)", e => e.Additive, (e, v) => e.Additive = v);

        void Number(string label, double min, double max, Func<ParticleEffect, double> get, Action<ParticleEffect, double> set, string format = "0.#", string? tip = null, double step = 1)
        {
            var row = new ValueRow(label, min, max, v => Change(e => set(e, v)), format, tip, step: step);
            _refreshers.Add(() => row.SetValue(get(Effect())));
            _editor.Children.Add(row);
        }

        void Choice(string label, string[] options, Func<ParticleEffect, int> get, Action<ParticleEffect, int> set)
        {
            var combo = new ComboBox { ItemsSource = options, HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 12.5 };
            var updating = false;
            combo.SelectionChanged += (_, _) =>
            {
                if (!updating && combo.SelectedIndex >= 0 && combo.SelectedIndex != get(Effect()))
                    doc.EditParticle(index, e => set(e, combo.SelectedIndex));
            };
            _refreshers.Add(() =>
            {
                updating = true;
                combo.SelectedIndex = Math.Clamp(get(Effect()), 0, options.Length - 1);
                updating = false;
            });
            _editor.Children.Add(Labeled(label, combo));
        }

        // The built-in images plus any other image the animation already uses (e.g. an imported one).
        void ShapePicker()
        {
            var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 12.5 };
            List<ParticleShape?> options = [];
            var updating = false;
            combo.SelectionChanged += (_, _) =>
            {
                if (!updating && combo.SelectedIndex >= 0 && !Equals(options[combo.SelectedIndex], Effect().Shape))
                    doc.EditParticle(index, e => e.Shape = options[combo.SelectedIndex]);
            };
            _refreshers.Add(() =>
            {
                var current = Effect().Shape;
                List<ParticleShape?> wanted =
                [
                    null,
                    .. BuiltInShapes.All.Concat(doc.Root.Particles.Select(p => p.Shape).OfType<ParticleShape>()).Append(current).OfType<ParticleShape>().Distinct(),
                ];
                updating = true;
                if (!wanted.SequenceEqual(options))
                {
                    options = wanted;
                    combo.ItemsSource = options.Select(ShapeItem).ToList();
                }

                combo.SelectedIndex = options.IndexOf(current);
                updating = false;
            });
            var import = Ui.Button("Import SVG…", () => ImportShapeAsync(index), "Use your own image. White parts take the particle colour; the top of the image points up.");
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
            row.Children.Add(combo);
            Grid.SetColumn(import, 1);
            row.Children.Add(import);
            _editor.Children.Add(Labeled("Shape", row));
        }

        void Check(string label, Func<ParticleEffect, bool> get, Action<ParticleEffect, bool> set)
        {
            var check = new CheckBox { Content = label, FontSize = 12.5 };
            var updating = false;
            check.IsCheckedChanged += (_, _) =>
            {
                if (!updating)
                    doc.EditParticle(index, e => set(e, check.IsChecked == true));
            };
            _refreshers.Add(() =>
            {
                updating = true;
                check.IsChecked = get(Effect());
                updating = false;
            });
            _editor.Children.Add(check);
        }

        void ColorRow(string label, Func<ParticleEffect, Vector4> get, Action<ParticleEffect, Vector4> set)
        {
            var swatch = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(4),
                BorderBrush = Ui.Muted,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var box = new TextBox { FontSize = 12, Watermark = "#RRGGBBAA" };
            ToolTip.SetTip(box, "Hex colour with alpha, e.g. #FFFFFFFF (white) or #FF4080C0.");
            void Commit()
            {
                if (TryParseColor(box.Text, out var color) && color != Quantized(get(Effect())))
                    doc.EditParticle(index, e => set(e, color));
            }

            box.LostFocus += (_, _) => Commit();
            box.KeyDown += (_, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                    Commit();
            };
            _refreshers.Add(() =>
            {
                var color = get(Effect());
                swatch.Background = new SolidColorBrush(ToColor(color));
                if (!box.IsFocused)
                    box.Text = ToHex(color);
            });
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
            row.Children.Add(box);
            Grid.SetColumn(swatch, 1);
            row.Children.Add(swatch);
            _editor.Children.Add(Labeled(label, row));
        }
    }

    private static readonly Dictionary<ParticleShape, Bitmap?> Thumbnails = new();

    private static Control ShapeItem(ParticleShape? shape)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        if (shape is not null)
        {
            if (!Thumbnails.TryGetValue(shape, out var thumbnail))
                Thumbnails[shape] = thumbnail = Thumbnail(shape);
            row.Children.Add(new Image { Source = thumbnail, Width = 18, Height = 18 });
        }
        else
            row.Children.Add(new Border { Width = 14, Height = 14, Margin = new Thickness(2), Background = Brushes.White });

        row.Children.Add(new TextBlock { Text = shape?.Name ?? "Plain square", VerticalAlignment = VerticalAlignment.Center });
        return row;
    }

    /// <summary>The SVG drawn into a 36 px square (kept in proportion), or null if it can't be drawn.</summary>
    private static Bitmap? Thumbnail(ParticleShape shape)
    {
        const int size = 36;
        try
        {
            using var svg = new SKSvg();
            if (svg.FromSvg(shape.Svg) is not { CullRect: { Width: > 0, Height: > 0 } bounds } picture)
                return null;
            using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                var scale = size / Math.Max(bounds.Width, bounds.Height);
                canvas.Translate((size - bounds.Width * scale) / 2, (size - bounds.Height * scale) / 2);
                canvas.Scale(scale);
                canvas.Translate(-bounds.Left, -bounds.Top);
                canvas.DrawPicture(picture);
            }

            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return new Bitmap(png.AsStream());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async void ImportShapeAsync(int index)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null || _doc is not { } doc)
            return;
        var files = await top.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Import particle image",
                FileTypeFilter = [new FilePickerFileType("SVG image") { Patterns = ["*.svg"], MimeTypes = ["image/svg+xml"] }],
            }
        );
        if (files.Count == 0)
            return;
        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();
        if (!ParticleShape.IsSvg(text) || Thumbnail(new ParticleShape("Check", text)) is null)
        {
            await MessageBox.Show(top, "Couldn't import image", $"Particle shapes have to be SVG images (up to {ParticleShape.MaxSvgLength / 1024} KB) that can be drawn.");
            return;
        }

        var shape = new ParticleShape(Path.GetFileNameWithoutExtension(files[0].Name), text);
        if (index < doc.Root.Particles.Count)
            doc.EditParticle(index, e => e.Shape = shape);
    }

    private static Control Labeled(string label, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("108,*"), Margin = new Thickness(0, 2) };
        grid.Children.Add(new TextBlock { Text = label, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }

    private static byte ToByte(float v) => (byte)Math.Clamp(MathF.Round(v * 255f), 0f, 255f);

    private static Color ToColor(Vector4 c) => Color.FromArgb(ToByte(c.W), ToByte(c.X), ToByte(c.Y), ToByte(c.Z));

    private static string ToHex(Vector4 c) => $"#{ToByte(c.X):X2}{ToByte(c.Y):X2}{ToByte(c.Z):X2}{ToByte(c.W):X2}";

    private static Vector4 Quantized(Vector4 c) => new(ToByte(c.X) / 255f, ToByte(c.Y) / 255f, ToByte(c.Z) / 255f, ToByte(c.W) / 255f);

    private static bool TryParseColor(string? text, out Vector4 color)
    {
        color = default;
        var hex = (text ?? "").Trim().TrimStart('#');
        if (hex.Length == 6)
            hex += "FF";
        if (hex.Length != 8 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return false;
        color = new Vector4((value >> 24 & 0xFF) / 255f, (value >> 16 & 0xFF) / 255f, (value >> 8 & 0xFF) / 255f, (value & 0xFF) / 255f);
        return true;
    }
}
