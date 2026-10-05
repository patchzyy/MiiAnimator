using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using MiiAnimator.Editing;
using MiiAnimator.Timeline;

namespace MiiAnimator.Panels;

/// <summary>
/// Context panel for the current selection. Shows only what matters for the selected part;
/// advanced-only controls (raw scale, IK blend, numeric targets) are hidden in Simple mode.
/// </summary>
public sealed class PosePanel : ScrollViewer
{
    private readonly StackPanel _root = new() { Margin = new Thickness(14, 12, 14, 16) };
    private readonly List<(ValueRow Row, TrackId Id)> _rows = [];
    private readonly List<Action> _refreshers = [];
    private AnimatorDocument? _doc;
    private Ui.GestureBatcher? _batch;
    private Selection _builtFor = new(SelectionKind.None, MiiBone.Hip);
    private bool _builtAdvanced;

    // Subclasses don't pick up the ScrollViewer template on their own; without it nothing scrolls.
    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    public PosePanel()
    {
        Content = _root;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    public void Attach(AnimatorDocument document)
    {
        _doc = document;
        _batch = new Ui.GestureBatcher(document.BeginGesture, document.EndGesture);
        document.SelectionChanged += Sync;
        document.FrameChanged += Sync;
        document.AnimationChanged += Sync;
        document.SettingsChanged += Sync;
        Sync();
    }

    private void Sync()
    {
        if (_doc is null)
            return;
        if (_builtFor != _doc.Selection || _builtAdvanced != _doc.AdvancedMode)
            Build();
        foreach (var (row, id) in _rows)
            row.SetValue(_doc.CurrentValue(id), _doc.IsKeyedHere(id));
        foreach (var refresh in _refreshers)
            refresh();
    }

    private void Build()
    {
        var doc = _doc!;
        _batch?.Flush();
        _builtFor = doc.Selection;
        _builtAdvanced = doc.AdvancedMode;
        _rows.Clear();
        _refreshers.Clear();
        _root.Children.Clear();

        var selection = doc.Selection;
        _root.Children.Add(Ui.Title(selection.DisplayName));

        switch (selection.Kind)
        {
            case SelectionKind.None:
                BuildNothingSelected();
                break;
            case SelectionKind.Face:
                BuildFace();
                break;
            case SelectionKind.Bone:
                BuildBone(selection.Bone);
                break;
            case SelectionKind.Limb:
                BuildLimb(selection.Limb);
                break;
        }

        if (selection.Kind is SelectionKind.Bone or SelectionKind.Limb)
            BuildKeyTools();
        BuildPoseTools();
    }

    private void BuildNothingSelected()
    {
        _root.Children.Add(Ui.Hint("Click any part of your Mii in the view to pose it, or pick one below."));
        var parts = new WrapPanel();
        foreach (var bone in new[] { MiiBone.Root, MiiBone.Hip, MiiBone.Chest, MiiBone.Head })
            parts.Children.Add(Ui.Button(MiiSkeletonInfo.FriendlyName(bone), () => _doc!.Select(Selection.OfBone(bone))));
        foreach (var limb in MiiSkeletonInfo.AllLimbs)
            parts.Children.Add(Ui.Button(MiiSkeletonInfo.FriendlyName(limb), () => SelectLimbSmart(limb)));
        parts.Children.Add(Ui.Button("Face", () => _doc!.Select(Selection.Face)));
        _root.Children.Add(parts);

        _root.Children.Add(Ui.Section("How animating works"));
        _root.Children.Add(
            Ui.Hint(
                "1. Pick a frame on the timeline.\n2. Pose the Mii — a key is saved automatically (Auto-key).\n"
                    + "3. Move to a later frame and pose again. The Mii moves smoothly between keys.\n"
                    + "4. Press Space to play. Right-click a key to change how it moves (Smooth, Bouncy…)."
            )
        );
    }

    private void SelectLimbSmart(MiiLimb limb)
    {
        var doc = _doc!;
        doc.Select(doc.IsLimbIk(limb) ? Selection.OfLimb(limb) : Selection.OfBone(MiiSkeletonInfo.Chain(limb).Upper));
    }

    private void BuildFace()
    {
        _root.Children.Add(Ui.Hint("Pick an expression. It's used from this frame until the next face key."));
        var grid = new UniformGrid { Columns = 2 };
        foreach (var expression in MiiExpressionInfo.All)
        {
            var button = new ToggleButton
            {
                Content = MiiExpressionInfo.FriendlyName(expression),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 4, 4),
                FontSize = 12,
            };
            button.Click += (_, _) => _doc!.SetExpression(expression);
            _refreshers.Add(() =>
                button.IsChecked = MiiExpressionInfo.FromValue(_doc!.CurrentValue(TrackId.Expression)) == expression
            );
            grid.Children.Add(button);
        }

        _root.Children.Add(grid);
        _root.Children.Add(
            Ui.Buttons(Ui.Button("Remove face key here", () => _doc!.DeleteKeysAtCurrentFrame(), "Deletes the expression key on this frame."))
        );
    }

    private void BuildBone(MiiBone bone)
    {
        var doc = _doc!;
        var limb = MiiSkeletonInfo.LimbOf(bone);
        _root.Children.Add(
            Ui.Hint(
                bone == MiiBone.Root
                    ? "Moves and turns the whole Mii. Squash it before a jump and stretch it in the air!"
                    : "Drag the colored rings in the view, or use the sliders."
            )
        );

        if (limb is { } l)
        {
            var leg = MiiSkeletonInfo.IsLeg(l);
            var card = new StackPanel();
            card.Children.Add(
                new TextBlock
                {
                    Text = $"{MiiSkeletonInfo.FriendlyName(l)} uses FK: rotate each bone yourself.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                }
            );
            card.Children.Add(
                Ui.Buttons(
                    Ui.Button(
                        "Switch to IK",
                        () => doc.SetLimbMode(l, ik: true),
                        $"IK: just drag the {(leg ? "foot" : "hand")} and the {(leg ? "knee" : "elbow")} follows. Great for planting feet.",
                        accent: true
                    ),
                    MirrorLimbButton(l)
                )
            );
            _root.Children.Add(Ui.Card(card));
        }

        _root.Children.Add(Ui.Section("Rotate"));
        AddRow("Tilt (X)", TrackId.Bone(bone, Channel.RotX), -180, 180);
        AddRow("Turn (Y)", TrackId.Bone(bone, Channel.RotY), -180, 180);
        AddRow("Roll (Z)", TrackId.Bone(bone, Channel.RotZ), -180, 180);

        if (MiiSkeletonInfo.CanTranslate(bone))
        {
            _root.Children.Add(Ui.Section("Move"));
            AddRow("Left / Right", TrackId.Bone(bone, Channel.PosX), -80, 80);
            AddRow("Up / Down", TrackId.Bone(bone, Channel.PosY), -80, 80);
            AddRow("Back / Forward", TrackId.Bone(bone, Channel.PosZ), -80, 80);
        }

        _root.Children.Add(Ui.Section("Squash & Stretch"));
        AddRow(
            "Squash ↔ Stretch",
            TrackId.Bone(bone, Channel.Stretch),
            0.3,
            2.5,
            "0.##",
            "Below 1 squashes, above 1 stretches. Volume is kept, so it looks bouncy instead of shrinking.",
            0.01
        );

        if (doc.AdvancedMode)
        {
            _root.Children.Add(Ui.Section("Scale"));
            AddRow("Scale X", TrackId.Bone(bone, Channel.ScaleX), 0.1, 3, "0.##", step: 0.01);
            AddRow("Scale Y", TrackId.Bone(bone, Channel.ScaleY), 0.1, 3, "0.##", step: 0.01);
            AddRow("Scale Z", TrackId.Bone(bone, Channel.ScaleZ), 0.1, 3, "0.##", step: 0.01);
        }
    }

    private void BuildLimb(MiiLimb limb)
    {
        var doc = _doc!;
        var leg = MiiSkeletonInfo.IsLeg(limb);
        var end = leg ? "foot" : "hand";
        var joint = leg ? "knee" : "elbow";
        _root.Children.Add(Ui.Hint($"IK: drag the orange square to move the {end}; the {joint} bends by itself."));

        var card = new StackPanel();
        card.Children.Add(new TextBlock { Text = $"Prefer rotating each bone? Switch to FK.", FontSize = 12, TextWrapping = TextWrapping.Wrap });
        card.Children.Add(
            Ui.Buttons(
                Ui.Button("Switch to FK", () => doc.SetLimbMode(limb, ik: false), "FK: rotate the upper, lower and end bone one by one."),
                MirrorLimbButton(limb)
            )
        );
        _root.Children.Add(Ui.Card(card));

        _root.Children.Add(Ui.Section($"{char.ToUpperInvariant(joint[0])}{joint[1..]}"));
        AddRow($"{char.ToUpperInvariant(joint[0])}{joint[1..]} direction", TrackId.Limb(limb, Channel.Swivel), -180, 180, tip: $"Swings the {joint} around while the {end} stays put.");

        var stretchy = new CheckBox { Content = $"Stretchy {(leg ? "leg" : "arm")}", FontSize = 12.5 };
        ToolTip.SetTip(stretchy, $"Lets the {(leg ? "leg" : "arm")} stretch when the {end} is dragged further than it can reach. Cartoony!");
        stretchy.IsCheckedChanged += (_, _) =>
        {
            if (_updatingChecks)
                return;
            doc.SetValue(TrackId.Limb(limb, Channel.IkStretch), stretchy.IsChecked == true ? 1 : 0);
        };
        _refreshers.Add(() =>
        {
            _updatingChecks = true;
            stretchy.IsChecked = doc.CurrentValue(TrackId.Limb(limb, Channel.IkStretch)) >= 0.5f;
            _updatingChecks = false;
        });
        _root.Children.Add(stretchy);

        _root.Children.Add(Ui.Section($"Rotate {end}"));
        AddRow("Tilt (X)", TrackId.Limb(limb, Channel.TargetRotX), -180, 180);
        AddRow("Turn (Y)", TrackId.Limb(limb, Channel.TargetRotY), -180, 180);
        AddRow("Roll (Z)", TrackId.Limb(limb, Channel.TargetRotZ), -180, 180);

        if (doc.AdvancedMode)
        {
            _root.Children.Add(Ui.Section($"{end} position"));
            AddRow("X", TrackId.Limb(limb, Channel.TargetX), -120, 120);
            AddRow("Y", TrackId.Limb(limb, Channel.TargetY), -120, 120);
            AddRow("Z", TrackId.Limb(limb, Channel.TargetZ), -120, 120);
            _root.Children.Add(Ui.Section("Blend"));
            AddRow("FK ↔ IK", TrackId.Limb(limb, Channel.IkBlend), 0, 1, "0.##", "Animate this to smoothly hand over between FK and IK.", 0.01);
        }
    }

    private bool _updatingChecks;

    private Control MirrorLimbButton(MiiLimb limb)
    {
        var end = MiiSkeletonInfo.IsLeg(limb) ? "foot" : "hand";
        var other = MiiSkeletonInfo.Mirror(limb);
        return Ui.Button(
            $"Mirror to other {end}",
            () => _doc!.MirrorLimbToOtherSide(limb),
            $"Copies this pose onto the {MiiSkeletonInfo.FriendlyName(other)}, mirrored. The {MiiSkeletonInfo.FriendlyName(limb)} stays as it is."
        );
    }

    private void BuildKeyTools()
    {
        var doc = _doc!;
        _root.Children.Add(Ui.Section("Key on this frame"));
        _root.Children.Add(
            Ui.Buttons(
                Ui.Button("Key  (K)", () => doc.KeyPose(), "Saves this part's pose on the current frame."),
                Ui.Button("Delete key", () => doc.DeleteKeysAtCurrentFrame(), "Removes this part's key on the current frame."),
                Ui.Button("Reset", () => doc.ResetSelection(), "Back to the relaxed pose.")
            )
        );

        // Easing of the key at this frame, shown as a picture.
        var easingRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var label = new TextBlock { Text = "Moves to next key:", VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5 };
        var combo = new ComboBox { MinWidth = 150, FontSize = 12.5 };
        var presets = InterpolationInfo.Presets;
        combo.ItemsSource = presets.Select(p =>
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new EasingIcon { Interpolation = p, Width = 26, Height = 14 },
                        new TextBlock { Text = InterpolationInfo.FriendlyName(p), Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center },
                    },
                }
            )
            .ToList();
        var updating = false;
        combo.SelectionChanged += (_, _) =>
        {
            if (updating || combo.SelectedIndex < 0)
                return;
            var ids = AnimatorDocument.TracksOf(doc.Selection).Where(doc.IsKeyedHere).ToList();
            if (ids.Count > 0)
                doc.SetInterpolation(ids, [doc.CurrentFrame], presets[combo.SelectedIndex]);
        };
        var description = Ui.Hint("");
        _refreshers.Add(() =>
        {
            updating = true;
            var keyed = AnimatorDocument.TracksOf(doc.Selection).Where(id => !ChannelInfo.IsDiscrete(id.Channel) && doc.IsKeyedHere(id)).ToList();
            combo.IsEnabled = keyed.Count > 0;
            Interpolation? shared = null;
            foreach (var id in keyed)
            {
                var curve = doc.Animation.TryGetCurve(id)!;
                var interpolation = curve.Keys[curve.IndexOf(doc.CurrentFrame)].Interpolation;
                shared = shared is null || shared == interpolation ? interpolation : Interpolation.Custom;
            }

            combo.SelectedIndex = shared is { } s && Array.IndexOf(presets, s) is var index and >= 0 ? index : -1;
            description.Text = keyed.Count == 0
                ? "No key on this frame yet."
                : shared is { } sh ? InterpolationInfo.Description(sh) : "";
            updating = false;
        });
        easingRow.Children.Add(label);
        easingRow.Children.Add(combo);
        _root.Children.Add(easingRow);
        _root.Children.Add(description);
    }

    private void BuildPoseTools()
    {
        var doc = _doc!;
        _root.Children.Add(Ui.Section("Whole pose"));
        var paste = Ui.Button("Paste", () => doc.PastePose(mirrored: false), "Ctrl+V — paste the copied pose on this frame.");
        var pasteMirror = Ui.Button("Paste mirrored", () => doc.PastePose(mirrored: true), "Ctrl+Shift+V — paste with left and right swapped.");
        _refreshers.Add(() =>
        {
            paste.IsEnabled = doc.HasClipboard;
            pasteMirror.IsEnabled = doc.HasClipboard;
        });
        _root.Children.Add(
            Ui.Buttons(
                Ui.Button("Copy", doc.CopyPose, "Ctrl+C — copy the whole pose on this frame."),
                paste,
                pasteMirror,
                Ui.Button("Mirror", doc.MirrorCurrentPose, "M — swap the left and right side of this pose."),
                Ui.Button("Key all", () => doc.KeyPose(wholeBody: true), "Shift+K — key every body part on this frame.")
            )
        );
    }

    private void AddRow(string label, TrackId id, double min, double max, string format = "0.#", string? tip = null, double step = 1)
    {
        var doc = _doc!;
        var row = new ValueRow(
            label,
            min,
            max,
            value =>
            {
                _batch!.Touch();
                doc.SetValue(id, (float)value);
            },
            format,
            tip,
            onKeyToggle: () =>
            {
                if (doc.IsKeyedHere(id))
                    doc.DeleteKeys([id], [doc.CurrentFrame]);
                else
                    doc.KeyTracks([id]);
            },
            step: step
        );
        _rows.Add((row, id));
        _root.Children.Add(row);
    }
}
