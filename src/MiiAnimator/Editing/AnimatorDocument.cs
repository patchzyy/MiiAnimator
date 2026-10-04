using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnimator.MiiData.Domain;

namespace MiiAnimator.Editing;

/// <summary>
/// All editor state: the animation, the Mii, the playhead, selection, settings and undo history.
/// UI controls observe the events and call the edit methods; nothing else mutates the animation.
/// </summary>
public sealed class AnimatorDocument
{
    private const int MaxUndo = 200;

    private readonly Stack<Snapshot> _undo = new();
    private readonly Stack<Snapshot> _redo = new();
    private readonly Dictionary<TrackId, float> _pending = new();
    private readonly Dictionary<bool, MiiRig> _rigs = new();
    private Snapshot? _gestureStart;

    public AnimatorDocument()
    {
        Mii = DefaultMii.Create();
        RefreshStudioData();
    }

    // ---------- State ----------

    public MiiAnimation Animation { get; private set; } = new();
    public string? FilePath { get; private set; }
    public bool IsDirty { get; private set; }

    public Mii Mii { get; private set; }
    public string StudioData { get; private set; } = "";

    public MiiRig Rig
    {
        get
        {
            if (!_rigs.TryGetValue(Mii.IsGirl, out var rig))
                _rigs[Mii.IsGirl] = rig = new MiiRig(MiiBodyModel.Get(Mii.IsGirl));
            return rig;
        }
    }

    public Vector3 BodyScale => MiiBodyModel.CalculateBodyScale(Mii.Weight.Value, Mii.Height.Value);

    /// <summary>Whole-frame playhead used for editing.</summary>
    public int CurrentFrame { get; private set; }

    /// <summary>Fractional playhead while playing.</summary>
    public float DisplayFrame { get; private set; }

    public bool IsPlaying { get; private set; }
    public Selection Selection { get; private set; } = Selection.None;
    public bool AutoKey { get; set; } = true;
    public bool OnionSkin { get; set; }
    public bool AdvancedMode { get; set; }
    public bool HasPendingChanges => _pending.Count > 0;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event Action? AnimationChanged;
    public event Action? FrameChanged;
    public event Action? SelectionChanged;
    public event Action? MiiChanged;
    public event Action? SettingsChanged;
    public event Action? FileChanged;

    public string Title => $"{(FilePath is null ? Animation.Name : Path.GetFileName(FilePath))}{(IsDirty ? " •" : "")}";

    // ---------- Evaluation ----------

    public MiiPose EvaluateDisplayPose()
    {
        var frame = DisplayFrame;
        return Rig.Evaluate(id => !IsPlaying && _pending.TryGetValue(id, out var value) ? value : Animation.Evaluate(id, frame));
    }

    public MiiPose EvaluatePoseAt(float frame) => Rig.Evaluate(Animation, frame);

    /// <summary>The value the user currently sees for a channel (including unkeyed edits).</summary>
    public float CurrentValue(TrackId id) =>
        !IsPlaying && _pending.TryGetValue(id, out var value) ? value : Animation.Evaluate(id, CurrentFrame);

    public bool IsKeyedHere(TrackId id) => Animation.Tracks.TryGetValue(id, out var curve) && curve.HasKeyAt(CurrentFrame);

    // ---------- Playhead ----------

    public void SetFrame(int frame)
    {
        frame = Math.Clamp(frame, 0, Animation.Length);
        _pending.Clear();
        CurrentFrame = frame;
        DisplayFrame = frame;
        FrameChanged?.Invoke();
    }

    public void SetPlaying(bool playing)
    {
        if (IsPlaying == playing)
            return;
        IsPlaying = playing;
        _pending.Clear();
        if (!playing)
        {
            CurrentFrame = Math.Clamp((int)MathF.Round(DisplayFrame), 0, Animation.Length);
            DisplayFrame = CurrentFrame;
        }

        FrameChanged?.Invoke();
    }

    /// <summary>Advances playback by real time, looping.</summary>
    public void Tick(double seconds)
    {
        if (!IsPlaying || Animation.Length <= 0)
            return;
        DisplayFrame = (float)((DisplayFrame + seconds * Animation.Fps) % Animation.Length);
        CurrentFrame = (int)DisplayFrame;
        FrameChanged?.Invoke();
    }

    public void Select(Selection selection)
    {
        if (Selection == selection)
            return;
        Selection = selection;
        SelectionChanged?.Invoke();
    }

    public void NotifySettingsChanged() => SettingsChanged?.Invoke();

    // ---------- Undo ----------

    /// <summary>Call at the start of a user gesture (drag, button press). Everything until <see cref="EndGesture"/> is one undo step.</summary>
    public void BeginGesture() => _gestureStart ??= TakeSnapshot();

    public void EndGesture()
    {
        if (_gestureStart is null)
            return;
        var start = _gestureStart;
        _gestureStart = null;
        if (start.Equals(TakeSnapshot()))
            return;
        _undo.Push(start);
        while (_undo.Count > MaxUndo)
            TrimOldest();
        _redo.Clear();
    }

    private void Edit(Action action, bool animationChanged = true)
    {
        var standalone = _gestureStart is null;
        if (standalone)
            BeginGesture();
        action();
        IsDirty = true;
        if (standalone)
            EndGesture();
        if (animationChanged)
            AnimationChanged?.Invoke();
        FileChanged?.Invoke();
    }

    public void Undo() => Restore(_undo, _redo);

    public void Redo() => Restore(_redo, _undo);

    private void Restore(Stack<Snapshot> from, Stack<Snapshot> to)
    {
        if (from.Count == 0)
            return;
        to.Push(TakeSnapshot());
        var snapshot = from.Pop();
        var miiChanged = !snapshot.Mii.AsSpan().SequenceEqual(SerializeMii());
        Animation = snapshot.Animation.Clone();
        if (miiChanged)
            SetMiiInternal(MiiSerializer.Deserialize(snapshot.Mii).Value);
        _pending.Clear();
        CurrentFrame = Math.Clamp(CurrentFrame, 0, Animation.Length);
        DisplayFrame = CurrentFrame;
        IsDirty = true;
        AnimationChanged?.Invoke();
        FrameChanged?.Invoke();
        FileChanged?.Invoke();
    }

    private void TrimOldest()
    {
        var items = _undo.ToArray();
        _undo.Clear();
        for (var i = items.Length - 2; i >= 0; i--)
            _undo.Push(items[i]);
    }

    private Snapshot TakeSnapshot() => new(Animation.Clone(), SerializeMii());

    private byte[] SerializeMii()
    {
        var result = MiiSerializer.Serialize(Mii);
        return result.IsSuccess ? result.Value : [];
    }

    private sealed record Snapshot(MiiAnimation Animation, byte[] Mii)
    {
        public bool Equals(Snapshot? other)
        {
            if (other is null || !Mii.AsSpan().SequenceEqual(other.Mii))
                return false;
            return MiiAnimFormat.Write(Animation).AsSpan().SequenceEqual(MiiAnimFormat.Write(other.Animation));
        }

        public override int GetHashCode() => 0;
    }

    // ---------- Posing ----------

    /// <summary>Changes channel values at the current frame. Keys them if auto-key is on, otherwise they stay pending.</summary>
    public void SetValues(IEnumerable<(TrackId Id, float Value)> values)
    {
        if (IsPlaying)
            SetPlaying(false);
        var list = values.Select(v => (v.Id, Value: Quantize(v.Id.Channel, v.Value))).ToList();
        if (AutoKey)
        {
            Edit(() =>
            {
                foreach (var (id, value) in list)
                    Animation.GetOrCreateCurve(id).SetKey(CurrentFrame, value, ChannelInfo.IsDiscrete(id.Channel) ? Interpolation.Hold : null);
            });
        }
        else
        {
            foreach (var (id, value) in list)
                _pending[id] = value;
            AnimationChanged?.Invoke();
        }
    }

    public void SetValue(TrackId id, float value) => SetValues([(id, value)]);

    /// <summary>Keys every channel of the selection (or the whole body) at the current frame with what is on screen.</summary>
    public void KeyPose(bool wholeBody = false)
    {
        var ids = wholeBody ? AllPoseTracks().ToList() : TracksOf(Selection).ToList();
        if (ids.Count == 0)
            return;
        Edit(() =>
        {
            foreach (var id in ids)
                Animation.GetOrCreateCurve(id).SetKey(CurrentFrame, CurrentValue(id), ChannelInfo.IsDiscrete(id.Channel) ? Interpolation.Hold : null);
            _pending.Clear();
        });
    }

    /// <summary>Keys specific channels at the current frame with their on-screen values.</summary>
    public void KeyTracks(IReadOnlyCollection<TrackId> ids) =>
        Edit(() =>
        {
            foreach (var id in ids)
            {
                Animation.GetOrCreateCurve(id).SetKey(CurrentFrame, CurrentValue(id), ChannelInfo.IsDiscrete(id.Channel) ? Interpolation.Hold : null);
                _pending.Remove(id);
            }
        });

    public void DeleteKeysAtCurrentFrame(bool wholeBody = false)
    {
        var ids = wholeBody ? AllPoseTracks().Append(TrackId.Expression).ToList() : TracksOf(Selection).ToList();
        Edit(() =>
        {
            foreach (var id in ids)
            {
                if (Animation.Tracks.TryGetValue(id, out var curve))
                    curve.RemoveKey(CurrentFrame);
            }

            Animation.Prune();
            _pending.Clear();
        });
    }

    /// <summary>Puts the selection back to its rest pose (keyed if auto-key).</summary>
    public void ResetSelection()
    {
        var ids = TracksOf(Selection).Where(id => id.Channel != Channel.IkBlend).ToList();
        SetValues(ids.Select(id => (id, ChannelInfo.DefaultValue(id))));
    }

    public static IEnumerable<TrackId> TracksOf(Selection selection) =>
        selection.Kind switch
        {
            SelectionKind.Bone => ChannelInfo
                .BoneChannels.Where(c => MiiSkeletonInfo.CanTranslate(selection.Bone) || c is not (Channel.PosX or Channel.PosY or Channel.PosZ))
                .Select(c => TrackId.Bone(selection.Bone, c)),
            SelectionKind.Limb => ChannelInfo.LimbChannels.Select(c => TrackId.Limb(selection.Limb, c)),
            SelectionKind.Face => [TrackId.Expression],
            _ => [],
        };

    public static IEnumerable<TrackId> AllPoseTracks() =>
        MiiSkeletonInfo
            .AllBones.SelectMany(b => TracksOf(Selection.OfBone(b)))
            .Concat(MiiSkeletonInfo.AllLimbs.SelectMany(l => TracksOf(Selection.OfLimb(l))));

    private static float Quantize(Channel channel, float value) =>
        channel switch
        {
            Channel.Expression or Channel.IkStretch => MathF.Round(value),
            _ => MathF.Round(value * 100f) / 100f,
        };

    // ---------- IK / FK ----------

    /// <summary>
    /// Switches a limb between FK and IK without the limb jumping: the new mode is matched to the current pose.
    /// </summary>
    public void SetLimbMode(MiiLimb limb, bool ik)
    {
        var pose = EvaluateDisplayPose();
        var values = new List<(TrackId, float)> { (TrackId.Limb(limb, Channel.IkBlend), ik ? 1f : 0f) };
        var (upper, middle, end) = MiiSkeletonInfo.Chain(limb);
        if (ik)
        {
            // Put the IK target where the hand/foot currently is.
            var offset = pose.WorldPosition[(int)end] - Rig.RestWorldPosition(end);
            values.Add((TrackId.Limb(limb, Channel.TargetX), offset.X));
            values.Add((TrackId.Limb(limb, Channel.TargetY), offset.Y));
            values.Add((TrackId.Limb(limb, Channel.TargetZ), offset.Z));
            var rotation = Quaternion.Concatenate(Quaternion.Inverse(Rig.RestWorldRotation(end)), pose.WorldRotation[(int)end]);
            var euler = MiiRig.ToEulerDegrees(rotation);
            values.Add((TrackId.Limb(limb, Channel.TargetRotX), euler.X));
            values.Add((TrackId.Limb(limb, Channel.TargetRotY), euler.Y));
            values.Add((TrackId.Limb(limb, Channel.TargetRotZ), euler.Z));
        }
        else
        {
            // Bake the IK result into the FK bone rotations.
            foreach (var bone in new[] { upper, middle, end })
            {
                var delta = Quaternion.Concatenate(pose.LocalRotation[(int)bone], Quaternion.Inverse(Rig.Body.RestRotation[(int)bone]));
                var euler = MiiRig.ToEulerDegrees(delta);
                values.Add((TrackId.Bone(bone, Channel.RotX), euler.X));
                values.Add((TrackId.Bone(bone, Channel.RotY), euler.Y));
                values.Add((TrackId.Bone(bone, Channel.RotZ), euler.Z));
            }
        }

        SetValues(values);
        Select(ik ? Selection.OfLimb(limb) : Selection.OfBone(upper));
    }

    public bool IsLimbIk(MiiLimb limb) => CurrentValue(TrackId.Limb(limb, Channel.IkBlend)) >= 0.5f;

    // ---------- Keys (dope sheet) ----------

    public void MoveKeys(IReadOnlyCollection<TrackId> ids, IReadOnlyCollection<int> frames, int delta)
    {
        if (delta == 0 || frames.Count == 0)
            return;
        Edit(() =>
        {
            foreach (var id in ids)
            {
                if (!Animation.Tracks.TryGetValue(id, out var curve))
                    continue;
                var moving = curve.Keys.Where(k => frames.Contains(k.Frame)).ToList();
                foreach (var key in moving)
                    curve.RemoveKey(key.Frame);
                foreach (var key in moving)
                {
                    var moved = key;
                    moved.Frame = Math.Clamp(key.Frame + delta, 0, Animation.Length);
                    curve.SetKey(moved);
                }
            }
        });
    }

    public void DeleteKeys(IReadOnlyCollection<TrackId> ids, IReadOnlyCollection<int> frames) =>
        Edit(() =>
        {
            foreach (var id in ids)
            {
                if (!Animation.Tracks.TryGetValue(id, out var curve))
                    continue;
                foreach (var frame in frames)
                    curve.RemoveKey(frame);
            }

            Animation.Prune();
        });

    public void SetInterpolation(IReadOnlyCollection<TrackId> ids, IReadOnlyCollection<int> frames, Interpolation interpolation) =>
        Edit(() =>
        {
            foreach (var id in ids)
            {
                if (!Animation.Tracks.TryGetValue(id, out var curve) || ChannelInfo.IsDiscrete(id.Channel))
                    continue;
                for (var i = 0; i < curve.Count; i++)
                {
                    var key = curve.Keys[i];
                    if (!frames.Contains(key.Frame))
                        continue;
                    if (interpolation == Interpolation.Custom && key.Interpolation != Interpolation.Custom)
                        (key.HandleOut, key.HandleIn) = curve.SuggestHandles(i);
                    key.Interpolation = interpolation;
                    curve.ReplaceAt(i, key);
                }
            }
        });

    /// <summary>Directly replaces one key (curve editor).</summary>
    public void ReplaceKey(TrackId id, int oldFrame, Keyframe key)
    {
        Edit(() =>
        {
            var curve = Animation.GetOrCreateCurve(id);
            if (oldFrame != key.Frame)
            {
                curve.RemoveKey(oldFrame);
                curve.RemoveKey(key.Frame);
            }

            key.Frame = Math.Clamp(key.Frame, 0, Animation.Length);
            key.Value = Quantize(id.Channel, key.Value);
            curve.SetKey(key);
        });
    }

    public void SetExpression(MiiExpression expression) => SetValue(TrackId.Expression, (float)expression);

    // ---------- Pose clipboard ----------

    private Dictionary<TrackId, float>? _clipboard;

    public bool HasClipboard => _clipboard is not null;

    public void CopyPose()
    {
        _clipboard = AllPoseTracks().Append(TrackId.Expression).ToDictionary(id => id, CurrentValue);
        SettingsChanged?.Invoke();
    }

    public void PastePose(bool mirrored)
    {
        if (_clipboard is null)
            return;
        var source = _clipboard;
        SetValues(mirrored ? Mirror(source) : source.Select(kv => (kv.Key, kv.Value)));
    }

    /// <summary>Swaps the left and right side of the current pose.</summary>
    public void MirrorCurrentPose()
    {
        var current = AllPoseTracks().ToDictionary(id => id, CurrentValue);
        SetValues(Mirror(current));
    }

    private static IEnumerable<(TrackId, float)> Mirror(IReadOnlyDictionary<TrackId, float> values)
    {
        foreach (var (id, value) in values)
        {
            var target = id.Target.Kind switch
            {
                TrackTargetKind.Bone => TrackTarget.Bone(MiiSkeletonInfo.Mirror(id.Target.AsBone)),
                TrackTargetKind.Limb => TrackTarget.Limb(MiiSkeletonInfo.Mirror(id.Target.AsLimb)),
                _ => id.Target,
            };
            var expression = id.Channel == Channel.Expression ? MirrorExpression(value) : value * ChannelInfo.MirrorSign(id.Channel);
            yield return (new TrackId(target, id.Channel), expression);
        }
    }

    private static float MirrorExpression(float value) =>
        (MiiExpression)(int)value switch
        {
            MiiExpression.WinkLeft => (float)MiiExpression.WinkRight,
            MiiExpression.WinkRight => (float)MiiExpression.WinkLeft,
            MiiExpression.WinkLeftOpenMouth => (float)MiiExpression.WinkRightOpenMouth,
            MiiExpression.WinkRightOpenMouth => (float)MiiExpression.WinkLeftOpenMouth,
            MiiExpression.LikeWinkLeft => (float)MiiExpression.LikeWinkRight,
            MiiExpression.LikeWinkRight => (float)MiiExpression.LikeWinkLeft,
            _ => value,
        };

    // ---------- Animation settings ----------

    public void SetLength(int length) =>
        Edit(() =>
        {
            Animation.Length = Math.Clamp(length, 1, 60 * 600);
            if (CurrentFrame > Animation.Length)
                CurrentFrame = Animation.Length;
        });

    public void SetFps(int fps) => Edit(() => Animation.Fps = Math.Clamp(fps, 1, 240));

    public void SetName(string name) => Edit(() => Animation.Name = string.IsNullOrWhiteSpace(name) ? "Untitled" : name.Trim(), animationChanged: false);

    // ---------- Mii ----------

    public event Action? HeadInvalidated;

    /// <summary>Replace the Mii (from the editor or an import). Undoable.</summary>
    public void SetMii(Mii mii) =>
        Edit(
            () =>
            {
                SetMiiInternal(mii);
            },
            animationChanged: false
        );

    private void SetMiiInternal(Mii mii)
    {
        Mii = mii;
        RefreshStudioData();
        MiiChanged?.Invoke();
        HeadInvalidated?.Invoke();
    }

    private void RefreshStudioData()
    {
        var result = MiiStudioDataSerializer.Serialize(Mii);
        StudioData = result.IsSuccess ? result.Value : "";
    }

    public byte[] ExportMii() => SerializeMii();

    public OperationResult ImportMii(byte[] data)
    {
        var result = MiiSerializer.Deserialize(data);
        if (result.IsFailure)
            return result.Error!;
        SetMii(result.Value);
        return Ok();
    }

    // ---------- Files ----------

    public void New(MiiAnimation animation)
    {
        Animation = animation;
        FilePath = null;
        IsDirty = false;
        _undo.Clear();
        _redo.Clear();
        _pending.Clear();
        CurrentFrame = 0;
        DisplayFrame = 0;
        IsPlaying = false;
        Selection = Selection.None;
        AnimationChanged?.Invoke();
        FrameChanged?.Invoke();
        SelectionChanged?.Invoke();
        FileChanged?.Invoke();
    }

    public void Open(string path)
    {
        var animation = MiiAnimFormat.Read(File.ReadAllBytes(path));
        if (animation.AuthorMii is { } miiBytes)
        {
            var mii = MiiSerializer.Deserialize(miiBytes);
            if (mii.IsSuccess)
                SetMiiInternal(mii.Value);
        }

        New(animation);
        FilePath = path;
        FileChanged?.Invoke();
    }

    public void Save(string path)
    {
        Animation.AuthorMii = SerializeMii() is { Length: MiiAnimation.MiiDataLength } bytes ? bytes : null;
        Animation.Prune();
        File.WriteAllBytes(path, MiiAnimFormat.Write(Animation));
        FilePath = path;
        IsDirty = false;
        FileChanged?.Invoke();
    }

    /// <summary>All key frames across every track (for onion skin and jumping between keys).</summary>
    public SortedSet<int> AllKeyFrames() => new(Animation.Tracks.Values.SelectMany(c => c.Keys.Select(k => k.Frame)));
}
