using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Rig;
using MiiAnimator.Editing;
using MiiAnimator.MiiData.Rendering;

namespace MiiAnimator.Viewport;

/// <summary>
/// The 3D viewport: an OpenGL view plus an input/gizmo overlay drawn with Avalonia.
/// Click a body part to select it, drag the colored rings to rotate, drag square handles to move.
/// Right-drag orbits, middle-drag pans, wheel zooms.
/// </summary>
public sealed class ViewportHost : Panel
{
    private static readonly Vector4 ClearColor = new(0.118f, 0.125f, 0.145f, 1f);
    private static readonly IBrush XBrush = new SolidColorBrush(Color.FromRgb(235, 80, 80));
    private static readonly IBrush YBrush = new SolidColorBrush(Color.FromRgb(110, 210, 90));
    private static readonly IBrush ZBrush = new SolidColorBrush(Color.FromRgb(80, 140, 245));
    private static readonly IBrush ViewBrush = new SolidColorBrush(Color.FromArgb(200, 235, 235, 235));
    private static readonly IBrush HandleBrush = new SolidColorBrush(Color.FromRgb(255, 170, 40));
    private static readonly IBrush HandleIdleBrush = new SolidColorBrush(Color.FromArgb(150, 255, 170, 40));
    private static readonly IBrush HoverBrush = new SolidColorBrush(Color.FromRgb(255, 235, 120));
    private static readonly IBrush HintBrush = new SolidColorBrush(Color.FromArgb(190, 220, 225, 235));
    private static readonly IBrush HintBackground = new SolidColorBrush(Color.FromArgb(150, 20, 22, 28));
    private static readonly Typeface UiTypeface = new("Inter");

    private readonly MiiGlView _gl = new();
    private readonly OverlayLayer _overlay;
    private readonly OrbitCamera _camera = new();
    private AnimatorDocument? _document;
    private HeadModelCache? _heads;
    private MiiPose? _pose;
    private string? _glError;

    private Handle? _hover;
    private Drag? _drag;
    private int _hoverMask;
    private Point _lastPointer;
    private bool _orbiting;
    private bool _panning;

    public ViewportHost()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;
        Focusable = true;
        Children.Add(_gl);
        _gl.IsHitTestVisible = false;
        _overlay = new OverlayLayer(this) { IsHitTestVisible = false };
        Children.Add(_overlay);
        _gl.RenderError += message => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _glError = message;
            _overlay.InvalidateVisual();
        });
    }

    public OrbitCamera Camera => _camera;

    public void Attach(AnimatorDocument document, HeadModelCache heads)
    {
        _document = document;
        _heads = heads;
        document.AnimationChanged += Refresh;
        document.FrameChanged += Refresh;
        document.SelectionChanged += Refresh;
        document.SettingsChanged += Refresh;
        // While the Mii editor is being dragged only build the visible expression; build the rest once it settles.
        var prewarm = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        prewarm.Tick += (_, _) =>
        {
            prewarm.Stop();
            heads.Prewarm(document.StudioData);
        };
        document.MiiChanged += () =>
        {
            _gl.RetainHeads(heads.Retain(document.StudioData));
            prewarm.Stop();
            prewarm.Start();
            Refresh();
        };
        heads.HeadReady += Refresh;
        heads.Prewarm(document.StudioData);
        Refresh();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Refresh();
    }

    public void FrameCharacter()
    {
        _camera.Reset();
        Refresh();
    }

    // ---------- Scene ----------

    private Vector3 RenderScale => (_document?.BodyScale ?? Vector3.One) * MiiBodyModel.CanonicalToRenderUnits;

    public void Refresh()
    {
        if (_document is null || _heads is null)
            return;
        var doc = _document;
        _pose = doc.EvaluateDisplayPose();

        var ghosts = new List<MiiDrawItem>();
        if (doc.OnionSkin && !doc.IsPlaying)
        {
            var keys = doc.AllKeyFrames();
            var previous = keys.GetViewBetween(0, Math.Max(0, doc.CurrentFrame - 1)).Where(f => f < doc.CurrentFrame).DefaultIfEmpty(-1).Max();
            var next = keys.Where(f => f > doc.CurrentFrame).DefaultIfEmpty(-1).FirstOrDefault();
            if (previous < 0)
                previous = Math.Max(0, doc.CurrentFrame - 10);
            if (next < 0)
                next = Math.Min(doc.Animation.Length, doc.CurrentFrame + 10);
            if (previous != doc.CurrentFrame)
                ghosts.Add(Ghost(doc.EvaluatePoseAt(previous), new Vector4(0.35f, 0.6f, 1f, 0.75f)));
            if (next != doc.CurrentFrame)
                ghosts.Add(Ghost(doc.EvaluatePoseAt(next), new Vector4(0.45f, 1f, 0.5f, 0.75f)));
        }

        var head = HeadFor(_pose.Expression);
        var frame = new SceneFrame
        {
            Body = doc.Rig.Body,
            BodyScale = doc.BodyScale,
            BodyColor = NativeMiiRenderer.FavoriteColor((int)doc.Mii.MiiFavoriteColor),
            Main = new MiiDrawItem(
                _pose,
                head.Data,
                head.Key,
                Vector4.Zero,
                1f,
                doc.IsPlaying ? 0 : doc.Selection.HighlightMask,
                doc.IsPlaying ? 0 : _hoverMask
            ),
            Ghosts = ghosts,
            Scene = doc.Scene,
            CameraPosition = _camera.Position,
        };

        var aspect = (float)Math.Max(1, Bounds.Width) / (float)Math.Max(1, Bounds.Height);
        _gl.Present(frame, _camera.View, _camera.Projection(aspect), ClearColor);
        _overlay.InvalidateVisual();
    }

    private MiiDrawItem Ghost(MiiPose pose, Vector4 tint)
    {
        var head = HeadFor(pose.Expression);
        return new MiiDrawItem(pose, head.Data, head.Key, tint, 0.28f);
    }

    private (IReadOnlyList<HeadMeshData>? Data, string? Key) HeadFor(MiiExpression expression)
    {
        var studio = _document!.StudioData;
        var head = _heads!.Get(studio, expression);
        if (head is not null)
            return (head, HeadModelCache.Key(studio, expression));
        var fallback = _heads.Get(studio, MiiExpression.Normal);
        return fallback is not null ? (fallback, HeadModelCache.Key(studio, MiiExpression.Normal)) : (null, null);
    }

    // ---------- Gizmo model ----------

    private enum HandleKind
    {
        RingX,
        RingY,
        RingZ,
        RingView,
        Move,
        IkTarget,
        Swivel,
    }

    private sealed record Handle(HandleKind Kind, MiiLimb? Limb = null);

    private sealed record Drag(Handle Handle, Point Start, Vector3 Center, Quaternion StartRotation, Quaternion ParentRotation, Vector3 Axis, float StartAngle, float[] StartValues, Vector3 PreviousEuler);

    private Vector3 ToRender(Vector3 canonicalCharacterSpace) => canonicalCharacterSpace * RenderScale;

    private Vector3 BonePosition(MiiBone bone) => ToRender(_pose!.WorldPositionOf(bone));

    private Vector3 TargetPosition(MiiLimb limb) => ToRender(Vector3.Transform(_pose!.Limbs[(int)limb].Target, _pose.GlobalMatrix));

    /// <summary>Center and orientation of the rotation gizmo for the current selection.</summary>
    private bool TryGetGizmo(out Vector3 center, out Quaternion rotation)
    {
        center = default;
        rotation = Quaternion.Identity;
        if (_document is null || _pose is null)
            return false;
        var selection = _document.Selection;
        switch (selection.Kind)
        {
            case SelectionKind.Bone:
                center = BonePosition(selection.Bone);
                rotation = _pose.WorldRotation[(int)selection.Bone];
                return true;
            case SelectionKind.Limb:
                center = TargetPosition(selection.Limb);
                rotation = _pose.Limbs[(int)selection.Limb].TargetRotation;
                return true;
            default:
                return false;
        }
    }

    private float RingRadius => _camera.Distance * 0.07f;

    private static Vector3 AxisOf(HandleKind kind, Quaternion rotation) =>
        kind switch
        {
            HandleKind.RingX => Vector3.Transform(Vector3.UnitX, rotation),
            HandleKind.RingY => Vector3.Transform(Vector3.UnitY, rotation),
            _ => Vector3.Transform(Vector3.UnitZ, rotation),
        };

    private Vector3 ViewAxis(Vector3 center) => Vector3.Normalize(_camera.Position - center);

    private List<(Vector2 A, Vector2 B, bool Front)> RingSegments(Vector3 center, Vector3 axis, float radius)
    {
        var segments = new List<(Vector2, Vector2, bool)>();
        var u = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
        var v = Vector3.Cross(axis, u);
        var toCamera = ViewAxis(center);
        const int count = 72;
        Vector2? previous = null;
        var previousFront = true;
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        for (var i = 0; i <= count; i++)
        {
            var t = i / (float)count * MathF.Tau;
            var offset = (MathF.Cos(t) * u + MathF.Sin(t) * v) * radius;
            var front = Vector3.Dot(offset, toCamera) >= -radius * 0.05f;
            if (!_camera.Project(center + offset, w, h, out var screen, out _))
            {
                previous = null;
                continue;
            }

            if (previous is { } p)
                segments.Add((p, screen, front && previousFront));
            previous = screen;
            previousFront = front;
        }

        return segments;
    }

    private IEnumerable<(Handle Handle, Func<Vector2, float> Distance)> Handles()
    {
        if (_document is null || _pose is null || _document.IsPlaying)
            yield break;
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        var selection = _document.Selection;

        // IK handles of every limb in IK mode are always grabbable.
        foreach (var limb in MiiSkeletonInfo.AllLimbs)
        {
            if (_pose.Limbs[(int)limb].IkBlend < 0.5f)
                continue;
            if (_camera.Project(TargetPosition(limb), w, h, out var s, out _))
                yield return (new Handle(HandleKind.IkTarget, limb), p => Vector2.Distance(p, s) - 2f);
        }

        if (selection.Kind == SelectionKind.Limb && _pose.Limbs[(int)selection.Limb].IkBlend >= 0.5f)
        {
            var middle = MiiSkeletonInfo.Chain(selection.Limb).Middle;
            if (_camera.Project(BonePosition(middle) + _pose.Limbs[(int)selection.Limb].PoleDirection * RingRadius * 0.6f, w, h, out var s, out _))
                yield return (new Handle(HandleKind.Swivel, selection.Limb), p => Vector2.Distance(p, s) - 2f);
        }

        if (selection.Kind == SelectionKind.Bone && MiiSkeletonInfo.CanTranslate(selection.Bone))
        {
            if (_camera.Project(BonePosition(selection.Bone), w, h, out var s, out _))
                yield return (new Handle(HandleKind.Move), p => Vector2.Distance(p, s) - 3f);
        }

        if (!TryGetGizmo(out var center, out var rotation))
            yield break;
        foreach (var kind in new[] { HandleKind.RingX, HandleKind.RingY, HandleKind.RingZ })
        {
            var segments = RingSegments(center, AxisOf(kind, rotation), RingRadius);
            yield return (new Handle(kind), p => DistanceToSegments(p, segments, frontOnly: true));
        }

        var viewSegments = RingSegments(center, ViewAxis(center), RingRadius * 1.3f);
        yield return (new Handle(HandleKind.RingView), p => DistanceToSegments(p, viewSegments, frontOnly: false));
    }

    private static float DistanceToSegments(Vector2 p, List<(Vector2 A, Vector2 B, bool Front)> segments, bool frontOnly)
    {
        var best = float.MaxValue;
        foreach (var (a, b, front) in segments)
        {
            if (frontOnly && !front)
                continue;
            var ab = b - a;
            var t = ab.LengthSquared() < 1e-6f ? 0f : Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
            best = MathF.Min(best, Vector2.Distance(p, a + ab * t));
        }

        return best;
    }

    private Handle? HitHandle(Point point)
    {
        var p = new Vector2((float)point.X, (float)point.Y);
        Handle? best = null;
        var bestDistance = 9f;
        foreach (var (handle, distance) in Handles())
        {
            var d = distance(p);
            // Square handles win ties against rings so they stay grabbable inside them.
            if (handle.Kind is HandleKind.Move or HandleKind.IkTarget or HandleKind.Swivel)
                d -= 4f;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = handle;
            }
        }

        return best;
    }

    // ---------- Picking ----------

    /// <summary>Returns the body part under the pointer (head counts as <see cref="MiiBone.Head"/>).</summary>
    private MiiBone? PickBone(Point point)
    {
        if (_document is null || _pose is null)
            return null;
        var (origin, direction) = _camera.Ray(new Vector2((float)point.X, (float)point.Y), (float)Bounds.Width, (float)Bounds.Height);
        var renderScale = Matrix4x4.CreateScale(RenderScale);
        var bestT = float.MaxValue;
        MiiBone? best = null;

        foreach (var mesh in _document.Rig.Body.Meshes)
        {
            var skinned = new Vector3[mesh.Positions.Length];
            var bones = new int[mesh.Positions.Length];
            for (var v = 0; v < skinned.Length; v++)
            {
                var dominant = 0;
                for (var k = 1; k < 4; k++)
                {
                    if (mesh.Weights[v * 4 + k] > mesh.Weights[v * 4 + dominant])
                        dominant = k;
                }

                bones[v] = mesh.Joints[v * 4 + dominant];
                skinned[v] = Vector3.Transform(mesh.Positions[v], _pose.SkinMatrix[bones[v]] * renderScale);
            }

            for (var i = 0; i + 2 < mesh.Indices.Length; i += 3)
            {
                if (RayTriangle(origin, direction, skinned[mesh.Indices[i]], skinned[mesh.Indices[i + 1]], skinned[mesh.Indices[i + 2]], out var t) && t < bestT)
                {
                    bestT = t;
                    best = (MiiBone)bones[mesh.Indices[i]];
                }
            }
        }

        var head = _heads?.Get(_document.StudioData, _pose.Expression) ?? _heads?.Get(_document.StudioData, MiiExpression.Normal);
        if (head is not null)
        {
            var headMatrix = _pose.HeadMatrixForRender(_document.BodyScale);
            // Some FFL draw calls (masks/overlays) extend below the face; only count hits above the neck.
            var neck = BonePosition(MiiBone.Head);
            var up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, _pose.WorldRotation[(int)MiiBone.Head]));
            foreach (var mesh in head)
            {
                var positions = mesh.Positions.Select(p => Vector3.Transform(p, headMatrix)).ToArray();
                for (var i = 0; i + 2 < mesh.Indices.Length; i += 3)
                {
                    if (
                        RayTriangle(origin, direction, positions[mesh.Indices[i]], positions[mesh.Indices[i + 1]], positions[mesh.Indices[i + 2]], out var t)
                        && t < bestT
                        && Vector3.Dot(origin + direction * t - neck, up) > 0
                    )
                    {
                        bestT = t;
                        best = MiiBone.Head;
                    }
                }
            }
        }

        return best;
    }

    private static bool RayTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, out float t)
    {
        t = 0;
        var e1 = b - a;
        var e2 = c - a;
        var p = Vector3.Cross(direction, e2);
        var det = Vector3.Dot(e1, p);
        if (MathF.Abs(det) < 1e-7f)
            return false;
        var inv = 1f / det;
        var s = origin - a;
        var u = Vector3.Dot(s, p) * inv;
        if (u is < 0 or > 1)
            return false;
        var q = Vector3.Cross(s, e1);
        var v = Vector3.Dot(direction, q) * inv;
        if (v < 0 || u + v > 1)
            return false;
        t = Vector3.Dot(e2, q) * inv;
        return t > 0;
    }

    private Selection SelectionFor(MiiBone bone)
    {
        var limb = MiiSkeletonInfo.LimbOf(bone);
        if (limb is { } l && _document!.IsLimbIk(l))
            return Selection.OfLimb(l);
        return Selection.OfBone(bone);
    }

    private int MaskFor(MiiBone? bone)
    {
        if (bone is not { } b)
            return 0;
        var selection = SelectionFor(b);
        return selection.HighlightMask;
    }

    // ---------- Input ----------

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (_document is null)
            return;
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        _lastPointer = point;

        if (properties.IsRightButtonPressed || (properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                _panning = true;
            else
                _orbiting = true;
            e.Pointer.Capture(this);
            return;
        }

        if (properties.IsMiddleButtonPressed)
        {
            _panning = true;
            e.Pointer.Capture(this);
            return;
        }

        if (!properties.IsLeftButtonPressed)
            return;

        if (_document.IsPlaying)
            _document.SetPlaying(false);

        var handle = HitHandle(point);
        if (handle is not null)
        {
            if (handle.Kind == HandleKind.IkTarget && handle.Limb is { } limb)
                _document.Select(Selection.OfLimb(limb));
            StartDrag(handle, point);
            e.Pointer.Capture(this);
            return;
        }

        var bone = PickBone(point);
        _document.Select(bone is { } b ? SelectionFor(b) : Selection.None);
    }

    private void StartDrag(Handle handle, Point point)
    {
        var doc = _document!;
        var pose = _pose!;
        TryGetGizmo(out var center, out var rotation);
        var parentRotation = Quaternion.Identity;
        float[] startValues = [];
        var previousEuler = Vector3.Zero;
        var selection = doc.Selection;

        switch (handle.Kind)
        {
            case HandleKind.RingX or HandleKind.RingY or HandleKind.RingZ or HandleKind.RingView:
                if (selection.Kind == SelectionKind.Bone)
                {
                    var parent = MiiSkeletonInfo.Parent(selection.Bone);
                    parentRotation = parent >= 0 ? pose.WorldRotation[parent] : Quaternion.Identity;
                    previousEuler = new Vector3(
                        doc.CurrentValue(TrackId.Bone(selection.Bone, Channel.RotX)),
                        doc.CurrentValue(TrackId.Bone(selection.Bone, Channel.RotY)),
                        doc.CurrentValue(TrackId.Bone(selection.Bone, Channel.RotZ))
                    );
                }
                else
                {
                    previousEuler = new Vector3(
                        doc.CurrentValue(TrackId.Limb(selection.Limb, Channel.TargetRotX)),
                        doc.CurrentValue(TrackId.Limb(selection.Limb, Channel.TargetRotY)),
                        doc.CurrentValue(TrackId.Limb(selection.Limb, Channel.TargetRotZ))
                    );
                }

                break;
            case HandleKind.Move:
                center = BonePosition(selection.Bone);
                startValues =
                [
                    doc.CurrentValue(TrackId.Bone(selection.Bone, Channel.PosX)),
                    doc.CurrentValue(TrackId.Bone(selection.Bone, Channel.PosY)),
                    doc.CurrentValue(TrackId.Bone(selection.Bone, Channel.PosZ)),
                ];
                break;
            case HandleKind.IkTarget:
                center = TargetPosition(handle.Limb!.Value);
                startValues =
                [
                    doc.CurrentValue(TrackId.Limb(handle.Limb!.Value, Channel.TargetX)),
                    doc.CurrentValue(TrackId.Limb(handle.Limb!.Value, Channel.TargetY)),
                    doc.CurrentValue(TrackId.Limb(handle.Limb!.Value, Channel.TargetZ)),
                ];
                break;
            case HandleKind.Swivel:
                startValues = [doc.CurrentValue(TrackId.Limb(handle.Limb!.Value, Channel.Swivel))];
                break;
        }

        var axis = handle.Kind == HandleKind.RingView ? ViewAxis(center) : AxisOf(handle.Kind, rotation);
        var startAngle = ScreenAngle(center, point);
        doc.BeginGesture();
        _drag = new Drag(handle, point, center, rotation, parentRotation, axis, startAngle, startValues, previousEuler);
    }

    private float ScreenAngle(Vector3 center, Point point)
    {
        _camera.Project(center, (float)Bounds.Width, (float)Bounds.Height, out var c, out _);
        return MathF.Atan2((float)point.Y - c.Y, (float)point.X - c.X);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        var delta = point - _lastPointer;
        _lastPointer = point;

        if (_orbiting)
        {
            _camera.Orbit(-(float)delta.X * 0.4f, (float)delta.Y * 0.3f);
            Refresh();
            return;
        }

        if (_panning)
        {
            _camera.Pan((float)delta.X, (float)delta.Y);
            Refresh();
            return;
        }

        if (_drag is not null)
        {
            ContinueDrag(_drag, point, e.KeyModifiers.HasFlag(KeyModifiers.Control));
            return;
        }

        // Hover feedback.
        var handle = HitHandle(point);
        var mask = handle is null ? MaskFor(PickBone(point)) : 0;
        if (handle != _hover || mask != _hoverMask)
        {
            _hover = handle;
            _hoverMask = mask;
            Cursor = handle is not null || mask != 0 ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
            Refresh();
        }
    }

    private void ContinueDrag(Drag drag, Point point, bool snap)
    {
        var doc = _document!;
        var selection = doc.Selection;
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        var pixel = new Vector2((float)(point.X - drag.Start.X), (float)(point.Y - drag.Start.Y));

        switch (drag.Handle.Kind)
        {
            case HandleKind.RingX or HandleKind.RingY or HandleKind.RingZ or HandleKind.RingView:
            {
                var angle = ScreenAngle(drag.Center, point) - drag.StartAngle;
                var facing = Vector3.Dot(drag.Axis, ViewAxis(drag.Center)) >= 0 ? 1f : -1f;
                var theta = -angle * facing;
                if (snap)
                    theta = MathF.Round(theta / (MathF.PI / 12f)) * (MathF.PI / 12f);
                var newWorld = Quaternion.Concatenate(drag.StartRotation, Quaternion.CreateFromAxisAngle(Vector3.Normalize(drag.Axis), theta));
                if (selection.Kind == SelectionKind.Bone)
                {
                    var local = Quaternion.Concatenate(newWorld, Quaternion.Inverse(drag.ParentRotation));
                    var deltaRotation = Quaternion.Concatenate(local, Quaternion.Inverse(doc.Rig.Body.RestRotation[(int)selection.Bone]));
                    var euler = EulerNear(MiiRig.ToEulerDegrees(deltaRotation), drag.PreviousEuler);
                    doc.SetValues(
                        [
                            (TrackId.Bone(selection.Bone, Channel.RotX), euler.X),
                            (TrackId.Bone(selection.Bone, Channel.RotY), euler.Y),
                            (TrackId.Bone(selection.Bone, Channel.RotZ), euler.Z),
                        ]
                    );
                }
                else if (selection.Kind == SelectionKind.Limb)
                {
                    var end = MiiSkeletonInfo.Chain(selection.Limb).End;
                    var deltaRotation = Quaternion.Concatenate(Quaternion.Inverse(doc.Rig.RestWorldRotation(end)), newWorld);
                    var euler = EulerNear(MiiRig.ToEulerDegrees(deltaRotation), drag.PreviousEuler);
                    doc.SetValues(
                        [
                            (TrackId.Limb(selection.Limb, Channel.TargetRotX), euler.X),
                            (TrackId.Limb(selection.Limb, Channel.TargetRotY), euler.Y),
                            (TrackId.Limb(selection.Limb, Channel.TargetRotZ), euler.Z),
                        ]
                    );
                }

                break;
            }
            case HandleKind.Move:
            {
                var moved = _camera.DragInViewPlane(drag.Center, pixel, w, h);
                var deltaCanonical = (moved - drag.Center) / RenderScale / _pose!.LocalScale[(int)MiiBone.Root];
                if (selection.Bone == MiiBone.Hip)
                    deltaCanonical = Vector3.Transform(deltaCanonical, Quaternion.Inverse(_pose.WorldRotation[(int)MiiBone.Root]));
                doc.SetValues(
                    [
                        (TrackId.Bone(selection.Bone, Channel.PosX), drag.StartValues[0] + deltaCanonical.X),
                        (TrackId.Bone(selection.Bone, Channel.PosY), drag.StartValues[1] + deltaCanonical.Y),
                        (TrackId.Bone(selection.Bone, Channel.PosZ), drag.StartValues[2] + deltaCanonical.Z),
                    ]
                );
                break;
            }
            case HandleKind.IkTarget:
            {
                var limb = drag.Handle.Limb!.Value;
                var moved = _camera.DragInViewPlane(drag.Center, pixel, w, h);
                var deltaCanonical = (moved - drag.Center) / RenderScale / _pose!.LocalScale[(int)MiiBone.Root];
                var targetY = drag.StartValues[1] + deltaCanonical.Y;
                // Feet: TargetY 0 is standing on the floor, so keep it from going below and snap onto it when close.
                if (doc.Scene.SnapFeetToFloor && MiiSkeletonInfo.IsLeg(limb) && targetY < SceneProps.FloorSnapDistance)
                    targetY = 0f;
                doc.SetValues(
                    [
                        (TrackId.Limb(limb, Channel.TargetX), drag.StartValues[0] + deltaCanonical.X),
                        (TrackId.Limb(limb, Channel.TargetY), targetY),
                        (TrackId.Limb(limb, Channel.TargetZ), drag.StartValues[2] + deltaCanonical.Z),
                    ]
                );
                break;
            }
            case HandleKind.Swivel:
            {
                var value = drag.StartValues[0] + pixel.X * 0.6f;
                if (snap)
                    value = MathF.Round(value / 15f) * 15f;
                doc.SetValue(TrackId.Limb(drag.Handle.Limb!.Value, Channel.Swivel), value);
                break;
            }
        }
    }

    /// <summary>Picks the euler solution closest to the previous one so curves never jump by 360°.</summary>
    private static Vector3 EulerNear(Vector3 euler, Vector3 previous)
    {
        static float Wrap(float value, float reference)
        {
            while (value - reference > 180f)
                value -= 360f;
            while (value - reference < -180f)
                value += 360f;
            return value;
        }

        var a = new Vector3(Wrap(euler.X, previous.X), Wrap(euler.Y, previous.Y), Wrap(euler.Z, previous.Z));
        var alternate = new Vector3(180f - euler.X, euler.Y + 180f, euler.Z + 180f);
        var b = new Vector3(Wrap(alternate.X, previous.X), Wrap(alternate.Y, previous.Y), Wrap(alternate.Z, previous.Z));
        return Vector3.DistanceSquared(a, previous) <= Vector3.DistanceSquared(b, previous) ? a : b;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _orbiting = false;
        _panning = false;
        if (_drag is not null)
        {
            _drag = null;
            _document?.EndGesture();
            Refresh();
        }

        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _camera.Zoom(e.Delta.Y > 0 ? 0.9f : 1.1f);
        Refresh();
        e.Handled = true;
    }

    // ---------- Overlay drawing ----------

    private sealed class OverlayLayer(ViewportHost host) : Control
    {
        public override void Render(DrawingContext context) => host.DrawOverlay(context);
    }

    private void DrawOverlay(DrawingContext context)
    {
        if (_document is null || _pose is null)
            return;
        var doc = _document;
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;

        if (!doc.IsPlaying)
        {
            // IK handles.
            foreach (var limb in MiiSkeletonInfo.AllLimbs)
            {
                if (_pose.Limbs[(int)limb].IkBlend < 0.5f || !_camera.Project(TargetPosition(limb), w, h, out var s, out _))
                    continue;
                var selected = doc.Selection.Kind == SelectionKind.Limb && doc.Selection.Limb == limb;
                var hovered = _hover?.Kind == HandleKind.IkTarget && _hover.Limb == limb;
                var size = selected ? 7 : 5.5;
                context.DrawRectangle(
                    selected || hovered ? HandleBrush : HandleIdleBrush,
                    new Pen(Brushes.Black, 1),
                    new Rect(s.X - size, s.Y - size, size * 2, size * 2),
                    2,
                    2
                );
            }

            if (doc.Selection.Kind == SelectionKind.Limb && _pose.Limbs[(int)doc.Selection.Limb].IkBlend >= 0.5f)
            {
                var limb = doc.Selection.Limb;
                var middle = BonePosition(MiiSkeletonInfo.Chain(limb).Middle);
                var knob = middle + _pose.Limbs[(int)limb].PoleDirection * RingRadius * 0.6f;
                if (_camera.Project(middle, w, h, out var m, out _) && _camera.Project(knob, w, h, out var k, out _))
                {
                    var hovered = _hover?.Kind == HandleKind.Swivel;
                    context.DrawLine(new Pen(HandleIdleBrush, 1.5, new DashStyle([3, 3], 0)), ToPoint(m), ToPoint(k));
                    context.DrawEllipse(hovered ? HoverBrush : HandleBrush, new Pen(Brushes.Black, 1), ToPoint(k), 6, 6);
                }
            }

            if (doc.Selection.Kind == SelectionKind.Bone && MiiSkeletonInfo.CanTranslate(doc.Selection.Bone))
            {
                if (_camera.Project(BonePosition(doc.Selection.Bone), w, h, out var s, out _))
                {
                    var hovered = _hover?.Kind == HandleKind.Move || _drag?.Handle.Kind == HandleKind.Move;
                    context.DrawRectangle(hovered ? HoverBrush : HandleBrush, new Pen(Brushes.Black, 1), new Rect(s.X - 7, s.Y - 7, 14, 14));
                }
            }

            if (TryGetGizmo(out var center, out var rotation))
            {
                DrawRing(context, center, ViewAxis(center), RingRadius * 1.3f, ViewBrush, HandleKind.RingView, frontOnly: false);
                DrawRing(context, center, AxisOf(HandleKind.RingX, rotation), RingRadius, XBrush, HandleKind.RingX, frontOnly: true);
                DrawRing(context, center, AxisOf(HandleKind.RingY, rotation), RingRadius, YBrush, HandleKind.RingY, frontOnly: true);
                DrawRing(context, center, AxisOf(HandleKind.RingZ, rotation), RingRadius, ZBrush, HandleKind.RingZ, frontOnly: true);
            }
        }

        DrawHints(context, doc);
    }

    private void DrawRing(DrawingContext context, Vector3 center, Vector3 axis, float radius, IBrush brush, HandleKind kind, bool frontOnly)
    {
        var active = _drag?.Handle.Kind == kind || (_drag is null && _hover?.Kind == kind);
        var front = new Pen(active ? HoverBrush : brush, active ? 3.5 : 2.2);
        var back = new Pen(brush, 1) { };
        foreach (var (a, b, isFront) in RingSegments(center, axis, radius))
        {
            if (isFront || !frontOnly)
                context.DrawLine(front, ToPoint(a), ToPoint(b));
            else
                context.DrawLine(new Pen(new SolidColorBrush(((ISolidColorBrush)brush).Color, 0.25), 1), ToPoint(a), ToPoint(b));
        }

        _ = back;
    }

    private void DrawHints(DrawingContext context, AnimatorDocument doc)
    {
        string? hint = null;
        if (_glError is not null)
            hint = "3D view error: " + _glError;
        else if (_heads is { IsAvailable: false })
            hint = "Head not shown: FFLResHigh.dat not found. Use File → Locate FFLResHigh.dat (WheelWizard downloads it).";
        else if (doc.IsPlaying)
            hint = null;
        else if (_drag is not null)
            hint = doc.AutoKey ? "Hold Ctrl to snap. Release to finish." : "Hold Ctrl to snap. Not keyed yet — press K to key this pose.";
        else if (doc.Selection.Kind == SelectionKind.None)
            hint = doc.Animation.Tracks.Count == 0
                ? "Click a body part to select it, then drag the colored rings to pose it. Every change makes a key at the current frame."
                : "Click a body part to pose it  •  Right-drag: orbit  •  Middle-drag: pan  •  Wheel: zoom";
        else if (doc.Selection.Kind == SelectionKind.Limb)
            hint = "Drag the orange square to move the " + (MiiSkeletonInfo.IsLeg(doc.Selection.Limb) ? "foot" : "hand") + ", the round knob to aim the "
                + (MiiSkeletonInfo.IsLeg(doc.Selection.Limb) ? "knee" : "elbow") + ", the rings to rotate it.";
        else if (doc.Selection.Kind == SelectionKind.Bone && MiiSkeletonInfo.CanTranslate(doc.Selection.Bone))
            hint = "Drag the orange square to move, the rings to rotate. Use Squash / Stretch in the panel for bouncy motion.";
        else if (doc.Selection.Kind == SelectionKind.Bone)
            hint = "Drag a colored ring to rotate around that axis, or the white ring to rotate towards the screen.";
        else if (!doc.AutoKey && doc.HasPendingChanges)
            hint = "Not keyed yet — press K to key this pose.";

        if (hint is null)
            return;
        var text = new FormattedText(hint, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, UiTypeface, 12.5, HintBrush);
        var rect = new Rect(12, Bounds.Height - text.Height - 22, text.Width + 20, text.Height + 10);
        context.DrawRectangle(HintBackground, null, rect, 6, 6);
        context.DrawText(text, new Point(rect.X + 10, rect.Y + 5));
    }

    private static Point ToPoint(Vector2 v) => new(v.X, v.Y);
}
