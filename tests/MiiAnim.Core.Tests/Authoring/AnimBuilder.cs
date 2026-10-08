using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Rig;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Small helper for writing animations in code. Arms are posed by hand position (solved with the rig's own IK)
/// and stored as plain FK rotations, so they follow the body and stay editable in the animator.
/// </summary>
public sealed class AnimBuilder
{
    public AnimBuilder(string name, int length, bool female)
        : this(new MiiAnimation { Name = name, Fps = 60, Length = length }, female) { }

    /// <summary>Builds into an existing animation, e.g. an extra actor's <see cref="AnimActor.Motion"/>.</summary>
    public AnimBuilder(MiiAnimation target, bool female)
    {
        Animation = target;
        Rig = new MiiRig(MiiBodyModel.Get(female));
    }

    public MiiAnimation Animation { get; }

    public MiiRig Rig { get; }

    private static readonly TrackId RootX = TrackId.Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = TrackId.Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootYaw = TrackId.Bone(MiiBone.Root, Channel.RotY);

    /// <summary>Where a foot stands when the Mii is at <paramref name="x"/> facing <paramref name="yaw"/> (world, canonical).</summary>
    public Vector3 NaturalFoot(MiiLimb leg, float x, float yaw, float z = 0f)
    {
        var rest = Rig.RestWorldPosition(MiiSkeletonInfo.Chain(leg).End);
        var rotated = Vector3.Transform(new Vector3(rest.X, 0f, rest.Z), Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw * MathF.PI / 180f));
        return new Vector3(x + rotated.X, rest.Y, z + rotated.Z);
    }

    /// <summary>Puts an IK foot at a world position (canonical), pointing the way the body faces.</summary>
    public AnimBuilder Foot(MiiLimb leg, int frame, Vector3 world, float yaw, Interpolation interpolation = Interpolation.Smooth)
    {
        var offset = world - Rig.RestWorldPosition(MiiSkeletonInfo.Chain(leg).End);
        Key(Limb(leg, Channel.TargetX), frame, offset.X, interpolation);
        Key(Limb(leg, Channel.TargetY), frame, offset.Y, interpolation);
        Key(Limb(leg, Channel.TargetZ), frame, offset.Z, interpolation);
        Key(Limb(leg, Channel.TargetRotY), frame, yaw, interpolation);
        return this;
    }

    public AnimBuilder FeetAt(int frame, float x, float yaw, Interpolation interpolation = Interpolation.Smooth)
    {
        Foot(MiiLimb.LegL, frame, NaturalFoot(MiiLimb.LegL, x, yaw), yaw, interpolation);
        return Foot(MiiLimb.LegR, frame, NaturalFoot(MiiLimb.LegR, x, yaw), yaw, interpolation);
    }

    /// <summary>
    /// Walks along X facing <paramref name="yaw"/> with IK feet that stay planted while the body passes over them
    /// (no sliding). The body moves at a constant speed; key easing before/after yourself. Lowers the hips a little
    /// so the legs can reach their strides. Returns the frame the last foot lands on.
    /// </summary>
    public int WalkX(int from, int to, float fromX, float toX, float yaw, int stepFrames, float lift = 7f, float armSwing = 14f, bool leftFirst = true, float hipDrop = 4f)
    {
        // Half steps to start and stop (so the back foot never trails too far), full steps in between.
        var steps = Math.Max(2, (int)MathF.Round((to - from) / (float)stepFrames) + 1);
        var full = (to - from) / (float)(steps - 1);
        int Boundary(int k) => k == 0 ? from : k == steps ? to : from + (int)MathF.Round(full * (k - 0.5f));
        float BodyX(float frame) => fromX + (toX - fromX) * Math.Clamp((frame - from) / (to - from), 0f, 1f);
        var speed = (toX - fromX) / (to - from);

        Key(RootX, from, fromX, Interpolation.Linear).Key(RootX, to, toX, Interpolation.Linear);
        Key(RootYaw, from, yaw).Key(RootYaw, to, yaw);

        var plants = new Dictionary<MiiLimb, Vector3>
        {
            [MiiLimb.LegL] = NaturalFoot(MiiLimb.LegL, fromX, yaw),
            [MiiLimb.LegR] = NaturalFoot(MiiLimb.LegR, fromX, yaw),
        };
        foreach (var leg in plants.Keys)
            Foot(leg, from, plants[leg], yaw);

        for (var k = 0; k < steps; k++)
        {
            var start = Boundary(k);
            var end = Boundary(k + 1);
            var leg = (k % 2 == 0) == leftFirst ? MiiLimb.LegL : MiiLimb.LegR;
            var other = leg == MiiLimb.LegL ? MiiLimb.LegR : MiiLimb.LegL;
            var last = k == steps - 1;
            // Land half a step ahead of the body (feet together on the last step).
            var landX = BodyX(end) + (last ? 0f : speed * full * 0.5f);
            var land = NaturalFoot(leg, landX, yaw);
            var mid = Vector3.Lerp(plants[leg], land, 0.5f) + new Vector3(0f, lift, 0f);
            Foot(leg, start, plants[leg], yaw);
            Foot(leg, (start + end) / 2, mid, yaw);
            Foot(leg, end, land, yaw);
            Foot(other, start, plants[other], yaw);
            Foot(other, end, plants[other], yaw);
            plants[leg] = land;

            Key(RootY, start, -hipDrop - 0.8f).Key(RootY, (start + end) / 2, -hipDrop + 0.8f);
            if (armSwing != 0f)
            {
                var swing = (leg == MiiLimb.LegL ? 1f : -1f) * (last ? 0f : armSwing);
                Key(Bone(MiiBone.ArmL1, Channel.RotY), end, swing).Key(Bone(MiiBone.ArmR1, Channel.RotY), end, swing);
            }
        }

        Key(RootY, to, -hipDrop * 0.5f);
        return to;
    }

    /// <summary>Turns on the spot with two small steps so the legs never twist.</summary>
    public AnimBuilder TurnInPlace(int from, int to, float x, float yawFrom, float yawTo, float lift = 5f)
    {
        Key(RootYaw, from, yawFrom).Key(RootYaw, to, yawTo);
        var half = (from + to) / 2;
        var legs = new[] { MiiLimb.LegL, MiiLimb.LegR };
        var yawMid = (yawFrom + yawTo) / 2f;
        foreach (var (leg, start, end) in new[] { (legs[0], from, half + 2), (legs[1], half - 2, to) })
        {
            var a = NaturalFoot(leg, x, yawFrom);
            var b = NaturalFoot(leg, x, yawTo);
            Foot(leg, from, a, yawFrom);
            Foot(leg, start, a, yawFrom);
            Foot(leg, (start + end) / 2, Vector3.Lerp(a, b, 0.5f) + new Vector3(0, lift, 0), yawMid);
            Foot(leg, end, b, yawTo);
            Foot(leg, to, b, yawTo);
        }

        return this;
    }

    public static TrackId Bone(MiiBone bone, Channel channel) => TrackId.Bone(bone, channel);

    public static TrackId Limb(MiiLimb limb, Channel channel) => TrackId.Limb(limb, channel);

    public AnimBuilder Key(TrackId id, int frame, float value, Interpolation interpolation = Interpolation.Smooth)
    {
        Animation.GetOrCreateCurve(id).SetKey(frame, value, ChannelInfo.IsDiscrete(id.Channel) ? Interpolation.Hold : interpolation);
        return this;
    }

    /// <summary>Keys a channel from (frame, value) pairs, all with the same easing.</summary>
    public AnimBuilder Keys(TrackId id, Interpolation interpolation, params (int Frame, float Value)[] keys)
    {
        foreach (var (frame, value) in keys)
            Key(id, frame, value, interpolation);
        return this;
    }

    public AnimBuilder Keys(TrackId id, params (int Frame, float Value)[] keys) => Keys(id, Interpolation.Smooth, keys);

    /// <summary>Changes the easing of an existing key (the segment that starts at it).</summary>
    public AnimBuilder Ease(TrackId id, int frame, Interpolation interpolation)
    {
        var curve = Animation.GetOrCreateCurve(id);
        var index = curve.IndexOf(frame);
        var key = curve.Keys[index];
        key.Interpolation = interpolation;
        curve.ReplaceAt(index, key);
        return this;
    }

    /// <summary>Bone rotation in degrees (x = tilt, y = turn, z = roll).</summary>
    public AnimBuilder Rot(MiiBone bone, int frame, float x, float y, float z, Interpolation interpolation = Interpolation.Smooth)
    {
        Key(Bone(bone, Channel.RotX), frame, x, interpolation);
        Key(Bone(bone, Channel.RotY), frame, y, interpolation);
        Key(Bone(bone, Channel.RotZ), frame, z, interpolation);
        return this;
    }

    /// <summary>
    /// A gravity arc: rising decelerates (quadratic ease-out), falling accelerates (quadratic ease-in).
    /// Uses exact bezier handles so the motion reads as real physics.
    /// </summary>
    public AnimBuilder Arc(TrackId id, int fromFrame, float from, int toFrame, float to, bool rising)
    {
        var delta = to - from;
        var key = new Keyframe(fromFrame, from, Interpolation.Custom)
        {
            HandleOut = rising ? new Vector2(1f / 3f, 2f * delta / 3f) : new Vector2(1f / 3f, 0f),
            HandleIn = rising ? new Vector2(1f / 3f, 0f) : new Vector2(1f / 3f, -2f * delta / 3f),
        };
        var curve = Animation.GetOrCreateCurve(id);
        curve.SetKey(key);
        if (!curve.HasKeyAt(toFrame))
            curve.SetKey(toFrame, to, Interpolation.Smooth);
        return this;
    }

    public AnimBuilder Face(int frame, MiiExpression expression) => Key(TrackId.Expression, frame, (float)expression);

    /// <summary>
    /// Poses an arm so the wrist lands on <paramref name="wrist"/> (rest-pose character space, canonical units).
    /// <paramref name="swivel"/> swings the elbow around the shoulder→wrist line; <paramref name="hand"/> bends the wrist.
    /// </summary>
    public AnimBuilder Arm(
        MiiLimb limb,
        int frame,
        Vector3 wrist,
        float swivel = 0,
        Vector3 hand = default,
        Interpolation interpolation = Interpolation.Smooth
    )
    {
        var (upper, middle, end) = MiiSkeletonInfo.Chain(limb);
        var (u, m) = SolveArm(limb, wrist, swivel);
        SetEuler(upper, frame, u, interpolation);
        SetEuler(middle, frame, m, interpolation);
        SetEuler(end, frame, hand, interpolation);
        return this;
    }

    /// <summary>
    /// Like <see cref="Arm"/>, but <paramref name="worldWrist"/> is where the wrist should be in the scene on that frame
    /// (e.g. on a window sill) taking the body and chest movement already keyed at that frame into account.
    /// Key the body/chest first.
    /// </summary>
    public AnimBuilder ArmWorld(
        MiiLimb limb,
        int frame,
        Vector3 worldWrist,
        float swivel = 0,
        Vector3 hand = default,
        Interpolation interpolation = Interpolation.Smooth
    )
    {
        var pose = Rig.Evaluate(Animation, frame);
        var chest = (int)MiiBone.Chest;
        var local = Vector3.Transform(worldWrist - pose.WorldPosition[chest], Quaternion.Inverse(pose.WorldRotation[chest]));
        var restSpace = Rig.RestWorldPosition(MiiBone.Chest) + Vector3.Transform(local, Rig.RestWorldRotation(MiiBone.Chest));
        return Arm(limb, frame, restSpace, swivel, hand, interpolation);
    }

    public AnimBuilder ArmsWorld(int frame, Vector3 leftWorldWrist, float swivel = 0, Vector3 hand = default, Interpolation interpolation = Interpolation.Smooth)
    {
        ArmWorld(MiiLimb.ArmL, frame, leftWorldWrist, swivel, hand, interpolation);
        ArmWorld(MiiLimb.ArmR, frame, MirrorX(leftWorldWrist), -swivel, new Vector3(hand.X, -hand.Y, -hand.Z), interpolation);
        return this;
    }

    /// <summary>FK legs for the whole animation (for shots where the legs are hidden and the body moves a lot).</summary>
    public AnimBuilder FkLegs()
    {
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 0));
        return this;
    }

    /// <summary>Same as <see cref="Arm"/> but for the right arm with the left-side coordinates mirrored.</summary>
    public AnimBuilder Arms(int frame, Vector3 leftWrist, float swivel = 0, Vector3 hand = default, Interpolation interpolation = Interpolation.Smooth)
    {
        Arm(MiiLimb.ArmL, frame, leftWrist, swivel, hand, interpolation);
        Arm(MiiLimb.ArmR, frame, MirrorX(leftWrist), -swivel, new Vector3(hand.X, -hand.Y, -hand.Z), interpolation);
        return this;
    }

    public AnimBuilder ArmRest(MiiLimb limb, int frame, Interpolation interpolation = Interpolation.Smooth)
    {
        var (upper, middle, end) = MiiSkeletonInfo.Chain(limb);
        foreach (var bone in new[] { upper, middle, end })
            Rot(bone, frame, 0, 0, 0, interpolation);
        return this;
    }

    public AnimBuilder ArmsRest(int frame, Interpolation interpolation = Interpolation.Smooth) =>
        ArmRest(MiiLimb.ArmL, frame, interpolation).ArmRest(MiiLimb.ArmR, frame, interpolation);

    /// <summary>Raw FK arm rotations (upper arm, forearm), for poses that are easier to think of as angles.</summary>
    public AnimBuilder ArmFk(MiiLimb limb, int frame, Vector3 upper, Vector3 forearm, Vector3 hand = default, Interpolation interpolation = Interpolation.Smooth)
    {
        var (u, m, e) = MiiSkeletonInfo.Chain(limb);
        SetEuler(u, frame, upper, interpolation);
        SetEuler(m, frame, forearm, interpolation);
        SetEuler(e, frame, hand, interpolation);
        return this;
    }

    public static Vector3 MirrorX(Vector3 v) => new(-v.X, v.Y, v.Z);

    public static Vector3 MirrorEuler(Vector3 e) => new(e.X, -e.Y, -e.Z);

    private void SetEuler(MiiBone bone, int frame, Vector3 euler, Interpolation interpolation)
    {
        // Keep each channel continuous with the previous key (no 359° → -1° flips).
        Key(Bone(bone, Channel.RotX), frame, Unwrap(Bone(bone, Channel.RotX), frame, euler.X), interpolation);
        Key(Bone(bone, Channel.RotY), frame, Unwrap(Bone(bone, Channel.RotY), frame, euler.Y), interpolation);
        Key(Bone(bone, Channel.RotZ), frame, Unwrap(Bone(bone, Channel.RotZ), frame, euler.Z), interpolation);
    }

    private float Unwrap(TrackId id, int frame, float value)
    {
        var curve = Animation.TryGetCurve(id);
        var previous = curve?.Keys.LastOrDefault(k => k.Frame < frame);
        if (previous is not { } p || curve!.Count == 0 || curve.Keys[0].Frame >= frame)
            return value;
        while (value - p.Value > 180f)
            value -= 360f;
        while (value - p.Value < -180f)
            value += 360f;
        return value;
    }

    private (Vector3 Upper, Vector3 Middle) SolveArm(MiiLimb limb, Vector3 wrist, float swivel)
    {
        var (upper, middle, end) = MiiSkeletonInfo.Chain(limb);
        var offset = wrist - Rig.RestWorldPosition(end);
        var values = new Dictionary<TrackId, float>
        {
            [Limb(limb, Channel.IkBlend)] = 1,
            [Limb(limb, Channel.TargetX)] = offset.X,
            [Limb(limb, Channel.TargetY)] = offset.Y,
            [Limb(limb, Channel.TargetZ)] = offset.Z,
            [Limb(limb, Channel.Swivel)] = swivel,
        };
        var pose = Rig.Evaluate(id => values.TryGetValue(id, out var v) ? v : float.NaN);
        var reached = pose.WorldPosition[(int)end];
        if (Vector3.Distance(reached, wrist) > 1.5f)
            throw new InvalidOperationException($"{limb} can't reach {wrist} (got {reached}).");

        Vector3 Delta(MiiBone bone) =>
            MiiRig.ToEulerDegrees(Quaternion.Concatenate(pose.LocalRotation[(int)bone], Quaternion.Inverse(Rig.Body.RestRotation[(int)bone])));

        return (Delta(upper), Delta(middle));
    }
}
