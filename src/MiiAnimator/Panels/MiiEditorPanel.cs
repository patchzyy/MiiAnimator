using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MiiAnimator.Editing;
using MiiAnimator.MiiData.Domain;

namespace MiiAnimator.Panels;

/// <summary>
/// Edit every part of the loaded Mii (same sections and ranges as WheelWizard's Mii editor).
/// Changes show instantly in the viewport and are undoable.
/// </summary>
public sealed class MiiEditorPanel : ScrollViewer
{
    private readonly StackPanel _root = new() { Margin = new Thickness(14, 12, 14, 16) };
    private readonly List<Action> _refreshers = [];
    private AnimatorDocument? _doc;
    private Ui.GestureBatcher? _batch;
    private bool _updating;

    // Subclasses don't pick up the ScrollViewer template on their own; without it nothing scrolls.
    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    public MiiEditorPanel()
    {
        Content = _root;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    public void Attach(AnimatorDocument document)
    {
        _doc = document;
        _batch = new Ui.GestureBatcher(document.BeginGesture, document.EndGesture);
        Build();
        document.MiiChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        _updating = true;
        foreach (var refresh in _refreshers)
            refresh();
        _updating = false;
    }

    private Mii M => _doc!.Mii;

    /// <summary>Applies a change to a copy of the Mii and hands it to the document.</summary>
    private void Change(Action<Mii> change)
    {
        if (_updating || _doc is null)
            return;
        var clone = MiiSerializer.Deserialize(MiiSerializer.Serialize(M).Value).Value;
        try
        {
            change(clone);
        }
        catch (ArgumentException)
        {
            return;
        }

        _batch!.Touch();
        _doc.SetMii(clone);
    }

    private void Build()
    {
        _root.Children.Add(Ui.Title("Your Mii"));
        _root.Children.Add(Ui.Hint("Change anything about the Mii. Animations work on every Mii, so this only changes the preview."));
        _root.Children.Add(
            Ui.Buttons(
                Ui.Button("Import .mii", ImportAsync, "Load a 74-byte Wii Mii file."),
                Ui.Button("Export .mii", ExportAsync, "Save this Mii as a 74-byte Wii Mii file."),
                Ui.Button("Default", () => _doc!.SetMii(DefaultMii.Create()), "Back to the default animator Mii.")
            )
        );

        // General
        var general = Expander("General", expanded: true);
        var name = new TextBox { MaxLength = 10, Watermark = "Name" };
        name.LostFocus += (_, _) => Change(m => m.Name = new MiiName(name.Text ?? ""));
        _refreshers.Add(() => name.Text = M.Name.ToString());
        general.Add(Labeled("Name", name));
        general.Add(
            Labeled(
                "Gender",
                Choice(["Boy", "Girl"], () => M.IsGirl ? 1 : 0, i => Change(m => m.IsGirl = i == 1))
            )
        );
        general.Add(Swatches("Favorite color", MiiColorMappings.FavoriteColor, () => M.MiiFavoriteColor, c => Change(m => m.MiiFavoriteColor = c)));
        general.Add(IntRow("Height", 0, 127, () => M.Height.Value, v => Change(m => m.Height = new MiiScale((byte)v))));
        general.Add(IntRow("Build", 0, 127, () => M.Weight.Value, v => Change(m => m.Weight = new MiiScale((byte)v))));

        // Face
        var face = Expander("Face");
        face.Add(
            Labeled(
                "Shape",
                EnumChoice(() => M.MiiFacialFeatures.FaceShape, v => Change(m => m.MiiFacialFeatures = With(m.MiiFacialFeatures, shape: v)))
            )
        );
        face.Add(Swatches("Skin", MiiColorMappings.SkinColor, () => M.MiiFacialFeatures.SkinColor, c => Change(m => m.MiiFacialFeatures = With(m.MiiFacialFeatures, skin: c))));
        face.Add(
            Labeled(
                "Feature",
                EnumChoice(() => M.MiiFacialFeatures.FacialFeature, v => Change(m => m.MiiFacialFeatures = With(m.MiiFacialFeatures, feature: v)))
            )
        );

        // Hair
        var hair = Expander("Hair");
        hair.Add(IntRow("Style", 0, 71, () => M.MiiHair.HairType, v => Change(m => m.MiiHair = new MiiHair(v, m.MiiHair.MiiHairColor, m.MiiHair.HairFlipped))));
        hair.Add(Swatches("Color", MiiColorMappings.HairColor, () => M.MiiHair.MiiHairColor, c => Change(m => m.MiiHair = new MiiHair(m.MiiHair.HairType, c, m.MiiHair.HairFlipped))));
        hair.Add(Check("Flip parting", () => M.MiiHair.HairFlipped, v => Change(m => m.MiiHair = new MiiHair(m.MiiHair.HairType, m.MiiHair.MiiHairColor, v))));

        // Eyebrows
        var brows = Expander("Eyebrows");
        MiiEyebrow B(Mii m, int? type = null, int? rot = null, MiiHairColor? color = null, int? size = null, int? vertical = null, int? spacing = null) =>
            new(type ?? m.MiiEyebrows.Type, rot ?? m.MiiEyebrows.Rotation, color ?? m.MiiEyebrows.Color, size ?? m.MiiEyebrows.Size, vertical ?? m.MiiEyebrows.Vertical, spacing ?? m.MiiEyebrows.Spacing);
        brows.Add(IntRow("Style", 0, 23, () => M.MiiEyebrows.Type, v => Change(m => m.MiiEyebrows = B(m, type: v))));
        brows.Add(Swatches("Color", MiiColorMappings.HairColor, () => M.MiiEyebrows.Color, c => Change(m => m.MiiEyebrows = B(m, color: c))));
        brows.Add(IntRow("Size", 0, 8, () => M.MiiEyebrows.Size, v => Change(m => m.MiiEyebrows = B(m, size: v))));
        brows.Add(IntRow("Rotation", 0, 11, () => M.MiiEyebrows.Rotation, v => Change(m => m.MiiEyebrows = B(m, rot: v))));
        brows.Add(IntRow("Height", 3, 18, () => M.MiiEyebrows.Vertical, v => Change(m => m.MiiEyebrows = B(m, vertical: v)), invert: true));
        brows.Add(IntRow("Spacing", 0, 12, () => M.MiiEyebrows.Spacing, v => Change(m => m.MiiEyebrows = B(m, spacing: v))));

        // Eyes
        var eyes = Expander("Eyes");
        MiiEye E(Mii m, int? type = null, int? rot = null, int? vertical = null, MiiEyeColor? color = null, int? size = null, int? spacing = null) =>
            new(type ?? m.MiiEyes.Type, rot ?? m.MiiEyes.Rotation, vertical ?? m.MiiEyes.Vertical, color ?? m.MiiEyes.Color, size ?? m.MiiEyes.Size, spacing ?? m.MiiEyes.Spacing);
        eyes.Add(IntRow("Style", 0, 47, () => M.MiiEyes.Type, v => Change(m => m.MiiEyes = E(m, type: v))));
        eyes.Add(Swatches("Color", MiiColorMappings.EyeColor, () => M.MiiEyes.Color, c => Change(m => m.MiiEyes = E(m, color: c))));
        eyes.Add(IntRow("Size", 0, 7, () => M.MiiEyes.Size, v => Change(m => m.MiiEyes = E(m, size: v))));
        eyes.Add(IntRow("Rotation", 0, 7, () => M.MiiEyes.Rotation, v => Change(m => m.MiiEyes = E(m, rot: v))));
        eyes.Add(IntRow("Height", 0, 18, () => M.MiiEyes.Vertical, v => Change(m => m.MiiEyes = E(m, vertical: v)), invert: true));
        eyes.Add(IntRow("Spacing", 0, 12, () => M.MiiEyes.Spacing, v => Change(m => m.MiiEyes = E(m, spacing: v))));

        // Nose
        var nose = Expander("Nose");
        nose.Add(Labeled("Style", EnumChoice(() => M.MiiNose.Type, v => Change(m => m.MiiNose = new MiiNose(v, m.MiiNose.Size, m.MiiNose.Vertical)))));
        nose.Add(IntRow("Size", 0, 8, () => M.MiiNose.Size, v => Change(m => m.MiiNose = new MiiNose(m.MiiNose.Type, v, m.MiiNose.Vertical))));
        nose.Add(IntRow("Height", 0, 18, () => M.MiiNose.Vertical, v => Change(m => m.MiiNose = new MiiNose(m.MiiNose.Type, m.MiiNose.Size, v)), invert: true));

        // Mouth
        var mouth = Expander("Mouth");
        MiiLip L(Mii m, int? type = null, MiiLipColor? color = null, int? size = null, int? vertical = null) =>
            new(type ?? m.MiiLips.Type, color ?? m.MiiLips.Color, size ?? m.MiiLips.Size, vertical ?? m.MiiLips.Vertical);
        mouth.Add(IntRow("Style", 0, 23, () => M.MiiLips.Type, v => Change(m => m.MiiLips = L(m, type: v))));
        mouth.Add(Swatches("Color", MiiColorMappings.LipBottomColor, () => M.MiiLips.Color, c => Change(m => m.MiiLips = L(m, color: c))));
        mouth.Add(IntRow("Size", 0, 8, () => M.MiiLips.Size, v => Change(m => m.MiiLips = L(m, size: v))));
        mouth.Add(IntRow("Height", 0, 18, () => M.MiiLips.Vertical, v => Change(m => m.MiiLips = L(m, vertical: v)), invert: true));

        // Glasses
        var glasses = Expander("Glasses");
        MiiGlasses G(Mii m, MiiGlassesType? type = null, MiiGlassesColor? color = null, int? size = null, int? vertical = null) =>
            new(type ?? m.MiiGlasses.Type, color ?? m.MiiGlasses.Color, size ?? m.MiiGlasses.Size, vertical ?? m.MiiGlasses.Vertical);
        glasses.Add(Labeled("Style", EnumChoice(() => M.MiiGlasses.Type, v => Change(m => m.MiiGlasses = G(m, type: v)))));
        glasses.Add(Swatches("Color", MiiColorMappings.GlassesColor, () => M.MiiGlasses.Color, c => Change(m => m.MiiGlasses = G(m, color: c))));
        glasses.Add(IntRow("Size", 0, 7, () => M.MiiGlasses.Size, v => Change(m => m.MiiGlasses = G(m, size: v))));
        glasses.Add(IntRow("Height", 0, 20, () => M.MiiGlasses.Vertical, v => Change(m => m.MiiGlasses = G(m, vertical: v)), invert: true));

        // Facial hair
        var facialHair = Expander("Facial hair");
        MiiFacialHair H(Mii m, MiiMustacheType? mustache = null, MiiBeardType? beard = null, MiiHairColor? color = null, int? size = null, int? vertical = null) =>
            new(mustache ?? m.MiiFacialHair.MiiMustacheType, beard ?? m.MiiFacialHair.MiiBeardType, color ?? m.MiiFacialHair.Color, size ?? m.MiiFacialHair.Size, vertical ?? m.MiiFacialHair.Vertical);
        facialHair.Add(Labeled("Mustache", EnumChoice(() => M.MiiFacialHair.MiiMustacheType, v => Change(m => m.MiiFacialHair = H(m, mustache: v)))));
        facialHair.Add(Labeled("Beard", EnumChoice(() => M.MiiFacialHair.MiiBeardType, v => Change(m => m.MiiFacialHair = H(m, beard: v)))));
        facialHair.Add(Swatches("Color", MiiColorMappings.HairColor, () => M.MiiFacialHair.Color, c => Change(m => m.MiiFacialHair = H(m, color: c))));
        facialHair.Add(IntRow("Size", 0, 8, () => M.MiiFacialHair.Size, v => Change(m => m.MiiFacialHair = H(m, size: v))));
        facialHair.Add(IntRow("Height", 0, 16, () => M.MiiFacialHair.Vertical, v => Change(m => m.MiiFacialHair = H(m, vertical: v)), invert: true));

        // Mole
        var mole = Expander("Mole");
        MiiMole O(Mii m, bool? exists = null, int? size = null, int? vertical = null, int? horizontal = null) =>
            new(exists ?? m.MiiMole.Exists, size ?? m.MiiMole.Size, vertical ?? m.MiiMole.Vertical, horizontal ?? m.MiiMole.Horizontal);
        mole.Add(Check("Show mole", () => M.MiiMole.Exists, v => Change(m => m.MiiMole = O(m, exists: v))));
        mole.Add(IntRow("Size", 0, 8, () => M.MiiMole.Size, v => Change(m => m.MiiMole = O(m, size: v))));
        mole.Add(IntRow("Left / Right", 0, 16, () => M.MiiMole.Horizontal, v => Change(m => m.MiiMole = O(m, horizontal: v))));
        mole.Add(IntRow("Height", 0, 30, () => M.MiiMole.Vertical, v => Change(m => m.MiiMole = O(m, vertical: v)), invert: true));
    }

    private static MiiFacialFeatures With(MiiFacialFeatures f, MiiFaceShape? shape = null, MiiSkinColor? skin = null, MiiFacialFeature? feature = null) =>
        new(shape ?? f.FaceShape, skin ?? f.SkinColor, feature ?? f.FacialFeature, f.MingleOff, f.Downloaded);

    // ---------- Builders ----------

    private Controls Expander(string header, bool expanded = false)
    {
        var content = new StackPanel { Spacing = 2 };
        var expander = new Expander
        {
            Header = header,
            Content = content,
            IsExpanded = expanded,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 6),
        };
        _root.Children.Add(expander);
        return content.Children;
    }

    private static Control Labeled(string label, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("100,*"), Margin = new Thickness(0, 2) };
        var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5 };
        grid.Children.Add(text);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }

    private Control IntRow(string label, int min, int max, Func<int> get, Action<int> set, bool invert = false)
    {
        // Mii "vertical" values grow downwards; invert so dragging right moves the part up.
        var row = new ValueRow(label, min, max, v => set(invert ? max + min - (int)Math.Round(v) : (int)Math.Round(v)), "0");
        _refreshers.Add(() => row.SetValue(invert ? max + min - get() : get()));
        return row;
    }

    private Control Check(string label, Func<bool> get, Action<bool> set)
    {
        var check = new CheckBox { Content = label, FontSize = 12.5 };
        check.IsCheckedChanged += (_, _) => set(check.IsChecked == true);
        _refreshers.Add(() => check.IsChecked = get());
        return check;
    }

    private Control Choice(string[] options, Func<int> get, Action<int> set)
    {
        var combo = new ComboBox { ItemsSource = options, HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 12.5 };
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedIndex >= 0)
                set(combo.SelectedIndex);
        };
        _refreshers.Add(() => combo.SelectedIndex = get());
        return combo;
    }

    private Control EnumChoice<T>(Func<T> get, Action<T> set)
        where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return Choice(values.Select(v => Humanize(v.ToString())).ToArray(), () => Array.IndexOf(values, get()), i => set(values[i]));
    }

    private Control Swatches<T>(string label, Dictionary<T, Color> colors, Func<T> get, Action<T> set)
        where T : struct, Enum
    {
        var panel = new WrapPanel();
        var borders = new List<(Border Border, T Value)>();
        foreach (var value in Enum.GetValues<T>())
        {
            if (!colors.TryGetValue(value, out var color))
                continue;
            var swatch = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 5, 5),
                BorderThickness = new Thickness(2),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            ToolTip.SetTip(swatch, Humanize(value.ToString()));
            var captured = value;
            swatch.PointerPressed += (_, _) => set(captured);
            borders.Add((swatch, value));
            panel.Children.Add(swatch);
        }

        _refreshers.Add(() =>
        {
            var current = get();
            foreach (var (border, value) in borders)
                border.BorderBrush = EqualityComparer<T>.Default.Equals(value, current) ? Brushes.White : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
        });
        return Labeled(label, panel);
    }

    private static string Humanize(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]) ? " " + char.ToLowerInvariant(c) : c.ToString()));

    // ---------- Import / export ----------

    private async void ImportAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null)
            return;
        var files = await top.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Import Mii",
                FileTypeFilter = [new FilePickerFileType("Wii Mii (.mii)") { Patterns = ["*.mii", "*.miigx", "*.bin"] }, FilePickerFileTypes.All],
            }
        );
        if (files.Count == 0)
            return;
        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var result = _doc!.ImportMii(ms.ToArray());
        if (result.IsFailure)
            await MessageBox.Show(top, "Couldn't import Mii", result.Error!.Message);
    }

    private async void ExportAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null)
            return;
        var file = await top.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export Mii",
                SuggestedFileName = $"{M.Name}.mii",
                DefaultExtension = "mii",
                FileTypeChoices = [new FilePickerFileType("Wii Mii (.mii)") { Patterns = ["*.mii"] }],
            }
        );
        if (file is null)
            return;
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(_doc!.ExportMii());
    }
}
