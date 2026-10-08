using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;

namespace MiiAnimator.Authoring;

/// <summary>Ready-made looping animations for File → New, so beginners have something to learn from and tweak.</summary>
public static class Templates
{
    public static readonly (string Name, string Description, Func<MiiAnimation> Create)[] All =
    [
        ("Empty", "Start from scratch.", Empty),
        ("Idle", "Gentle breathing, looking around and a blink.", Idle),
        ("Idle breathing", "Quiet standing with soft breathing and an occasional blink.", IdleBreathing),
        ("Idle weight shift", "Relaxed standing, shifting weight and glancing to the side.", IdleWeightShift),
        ("Shirt color surprise", "Notices the new shirt color, looks down, then smiles.", ShirtColorSurprise),
        ("Wave", "Raises an arm and waves hello.", Wave),
        ("Jump", "Squash, stretch, jump and land.", Jump),
        ("Walk in place", "Alternating steps with arm swings.", WalkInPlace),
        ("Dance", "Bouncy hip sway with arm moves.", Dance),
    ];

    public static MiiAnimation Empty() =>
        new()
        {
            Name = "New animation",
            Fps = 60,
            Length = 120,
        };

    public static MiiAnimation Idle()
    {
        var a = new MiiAnimation
        {
            Name = "Idle",
            Fps = 60,
            Length = 240,
        };
        Key(a, Bone(MiiBone.Chest, Channel.RotX), (0, 0), (60, -2.5f), (120, 0), (180, -2.5f), (240, 0));
        Key(a, Bone(MiiBone.Root, Channel.PosY), (0, 0), (60, -0.6f), (120, 0), (180, -0.6f), (240, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotY), (0, 0), (70, 14), (110, 14), (170, -10), (210, -10), (240, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotX), (0, 0), (90, 4), (160, -3), (240, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotZ), (0, 0), (60, 2), (120, 0), (180, 2), (240, 0));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotZ), (0, 0), (60, -2), (120, 0), (180, -2), (240, 0));
        Expression(
            a,
            (0, MiiExpression.Normal),
            (96, MiiExpression.Blink),
            (102, MiiExpression.Normal),
            (200, MiiExpression.Smile),
            (236, MiiExpression.Normal)
        );
        return a;
    }

    public static MiiAnimation IdleBreathing()
    {
        var a = new MiiAnimation { Name = "Idle breathing", Fps = 60, Length = 360 };
        // Small chest movement, with the arms following slightly behind each breath.
        // No global scale: the feet stay on the ground throughout the loop.
        Key(a, Bone(MiiBone.Chest, Channel.RotX), (0, 0), (85, -1.6f), (180, 0), (265, -1.4f), (360, 0));
        Key(a, Bone(MiiBone.Chest, Channel.ScaleZ), (0, 1), (85, 1.018f), (180, 1), (265, 1.016f), (360, 1));
        Key(a, Bone(MiiBone.Head, Channel.RotX), (0, 0), (95, 0.8f), (190, 0), (275, 0.7f), (360, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotZ), (0, 0), (150, 1.2f), (240, 0.4f), (360, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotZ), (0, 0), (96, 1.4f), (190, 0), (276, 1.2f), (360, 0));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotZ), (0, 0), (100, -1.2f), (194, 0), (280, -1.4f), (360, 0));
        Expression(a, (0, MiiExpression.Normal), (137, MiiExpression.Blink), (144, MiiExpression.Normal),
            (306, MiiExpression.Blink), (313, MiiExpression.Normal), (360, MiiExpression.Normal));
        return a;
    }

    public static MiiAnimation IdleWeightShift()
    {
        var a = new MiiAnimation { Name = "Idle weight shift", Fps = 60, Length = 420 };
        // Ease onto one leg, linger, then settle onto the other. Default leg IK pins both feet.
        Key(a, Bone(MiiBone.Root, Channel.PosX), (0, 0), (80, 2.4f), (155, 2.4f), (265, -2.2f), (330, -2.2f), (420, 0));
        Key(a, Bone(MiiBone.Root, Channel.PosY), (0, 0), (80, -0.8f), (155, -0.8f), (210, -0.3f), (265, -0.8f), (330, -0.8f), (420, 0));
        Key(a, Bone(MiiBone.Chest, Channel.RotZ), (0, 0), (90, -1.8f), (155, -1.8f), (275, 1.6f), (330, 1.6f), (420, 0));
        Key(a, Bone(MiiBone.Chest, Channel.RotX), (0, 0), (65, -1.2f), (170, 0), (270, -1.4f), (360, 0), (420, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotY), (0, 0), (100, 0), (142, 10), (195, 10), (242, 0), (420, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotZ), (0, 0), (110, 1.4f), (190, 1.4f), (290, -1.2f), (340, -1.2f), (420, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotZ), (0, 0), (100, 3), (165, 3), (285, 1), (340, 1), (420, 0));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotZ), (0, 0), (100, -1), (165, -1), (285, -3), (340, -3), (420, 0));
        Expression(a, (0, MiiExpression.Normal), (119, MiiExpression.Blink), (126, MiiExpression.Normal),
            (321, MiiExpression.Blink), (328, MiiExpression.Normal), (420, MiiExpression.Normal));
        return a;
    }

    public static MiiAnimation ShirtColorSurprise()
    {
        var a = new MiiAnimation { Name = "Shirt color surprise", Fps = 60, Length = 210 };
        // Play when the shirt changes: a short recoil, then a deliberate inspection of the chest.
        // Positive head pitch looks down; keep the arms out of the way of the shirt and face.
        Key(a, Bone(MiiBone.Root, Channel.PosY), (0, 0), (10, -1.2f), (22, 0), (46, -1.5f), (120, -1.5f), (158, 0), (210, 0));
        Key(a, Bone(MiiBone.Chest, Channel.RotX), (0, 0), (10, -5), (22, -2), (46, 7), (115, 7), (153, -2), (190, 0), (210, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotX), (0, 0), (10, -7), (20, -3), (43, 32), (60, 30), (112, 30), (148, -3), (183, 0), (210, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotY), (0, 0), (47, 0), (67, 9), (82, 9), (101, -9), (116, -9), (140, 0), (210, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotZ), (0, 0), (47, 0), (70, -3), (104, 3), (140, 0), (210, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotZ), (0, 0), (12, 22), (25, 14), (48, 29), (114, 29), (151, 12), (190, 0), (210, 0));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotZ), (0, 0), (14, -24), (27, -14), (50, -29), (116, -29), (153, -12), (192, 0), (210, 0));
        Expression(a, (0, MiiExpression.Normal), (5, MiiExpression.SurpriseOpenMouth),
            (48, MiiExpression.Surprise), (123, MiiExpression.Blink), (130, MiiExpression.Smile),
            (187, MiiExpression.Normal), (210, MiiExpression.Normal));
        // A puff of the new shirt colour around the body as it changes (tinted per Mii by its favourite colour).
        var poof = ParticlePresets.Poof();
        poof.StartFrame = 1;
        poof.Seed = 210;
        a.Particles.Add(poof);
        a.AddEvent(210, "done");
        return a;
    }

    public static MiiAnimation Wave()
    {
        var a = new MiiAnimation
        {
            Name = "Wave",
            Fps = 60,
            Length = 120,
        };
        Key(a, Bone(MiiBone.ArmR1, Channel.RotZ), (0, 0), (14, -140, Interpolation.Overshoot), (96, -140), (116, 0), (120, 0));
        Key(
            a,
            Bone(MiiBone.ArmR2, Channel.RotZ),
            (0, 0),
            (14, -20),
            (26, 20),
            (38, -20),
            (50, 20),
            (62, -20),
            (74, 20),
            (86, -20),
            (98, 0),
            (120, 0)
        );
        Key(a, Bone(MiiBone.ArmR2, Channel.RotY), (0, 0), (14, -25), (96, -25), (116, 0), (120, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotZ), (0, 0), (20, -8), (96, -8), (116, 0), (120, 0));
        Key(a, Bone(MiiBone.Chest, Channel.RotZ), (0, 0), (20, -3), (96, -3), (116, 0), (120, 0));
        Expression(
            a,
            (0, MiiExpression.Normal),
            (10, MiiExpression.SmileOpenMouth),
            (100, MiiExpression.Smile),
            (118, MiiExpression.Normal)
        );
        return a;
    }

    public static MiiAnimation Jump()
    {
        var a = new MiiAnimation
        {
            Name = "Jump",
            Fps = 60,
            Length = 100,
        };
        var root = MiiBone.Root;
        Key(
            a,
            Bone(root, Channel.PosY),
            (0, 0),
            (14, -12, Interpolation.EaseOut),
            (22, 4, Interpolation.EaseOut),
            (36, 38, Interpolation.EaseIn),
            (50, 4),
            (54, -12, Interpolation.EaseOut),
            (68, 1),
            (76, 0),
            (100, 0)
        );
        Key(
            a,
            Bone(root, Channel.Stretch),
            (0, 1),
            (14, 0.86f),
            (22, 1.16f),
            (36, 1),
            (50, 1.1f),
            (54, 0.8f, Interpolation.Overshoot),
            (70, 1),
            (100, 1)
        );
        Key(a, Bone(MiiBone.Chest, Channel.RotX), (0, 0), (14, 14), (24, -6), (36, 0), (54, 12), (72, 0), (100, 0));
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            Key(a, Limb(leg, Channel.TargetY), (0, 0), (22, 0, Interpolation.EaseOut), (36, 24, Interpolation.EaseIn), (50, 0), (100, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotY), (0, 0), (14, 35), (24, -20), (36, 0), (54, 20), (72, 0), (100, 0));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotY), (0, 0), (14, -35), (24, 20), (36, 0), (54, -20), (72, 0), (100, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotZ), (0, 0), (22, 30), (36, 110), (50, 40), (64, 0), (100, 0));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotZ), (0, 0), (22, -30), (36, -110), (50, -40), (64, 0), (100, 0));
        Expression(
            a,
            (0, MiiExpression.Normal),
            (10, MiiExpression.Frustrated),
            (22, MiiExpression.SmileOpenMouth),
            (66, MiiExpression.Smile),
            (96, MiiExpression.Normal)
        );
        return a;
    }

    public static MiiAnimation WalkInPlace()
    {
        var a = new MiiAnimation
        {
            Name = "Walk in place",
            Fps = 60,
            Length = 60,
        };
        Key(a, Limb(MiiLimb.LegL, Channel.TargetY), (0, 0), (8, 0), (15, 12), (22, 0), (60, 0));
        Key(a, Limb(MiiLimb.LegL, Channel.TargetZ), (0, 0), (15, 4), (22, 0), (60, 0));
        Key(a, Limb(MiiLimb.LegR, Channel.TargetY), (0, 0), (38, 0), (45, 12), (52, 0), (60, 0));
        Key(a, Limb(MiiLimb.LegR, Channel.TargetZ), (0, 0), (45, 4), (52, 0), (60, 0));
        Key(a, Bone(MiiBone.Root, Channel.PosY), (0, -1), (15, 0.5f), (30, -1), (45, 0.5f), (60, -1));
        Key(a, Bone(MiiBone.Root, Channel.RotZ), (0, 0), (15, -2), (30, 0), (45, 2), (60, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotY), (0, -25), (30, 25), (60, -25));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotY), (0, -25), (30, 25), (60, -25));
        Key(a, Bone(MiiBone.Head, Channel.RotZ), (0, 0), (15, 2), (30, 0), (45, -2), (60, 0));
        Expression(a, (0, MiiExpression.Normal));
        return a;
    }

    public static MiiAnimation Dance()
    {
        var a = new MiiAnimation
        {
            Name = "Dance",
            Fps = 60,
            Length = 120,
        };
        Key(
            a,
            Bone(MiiBone.Root, Channel.PosY),
            (0, 0),
            (15, -7, Interpolation.Snappy),
            (30, 0),
            (45, -7, Interpolation.Snappy),
            (60, 0),
            (75, -7, Interpolation.Snappy),
            (90, 0),
            (105, -7, Interpolation.Snappy),
            (120, 0)
        );
        Key(
            a,
            Bone(MiiBone.Root, Channel.Stretch),
            (0, 1.04f),
            (15, 0.92f),
            (30, 1.04f),
            (45, 0.92f),
            (60, 1.04f),
            (75, 0.92f),
            (90, 1.04f),
            (105, 0.92f),
            (120, 1.04f)
        );
        Key(a, Bone(MiiBone.Root, Channel.PosX), (0, 0), (30, 6), (60, 0), (90, -6), (120, 0));
        Key(a, Bone(MiiBone.Root, Channel.RotY), (0, 0), (30, 15), (60, 0), (90, -15), (120, 0));
        Key(a, Bone(MiiBone.Chest, Channel.RotZ), (0, 0), (30, -7), (60, 0), (90, 7), (120, 0));
        Key(a, Bone(MiiBone.Head, Channel.RotZ), (0, 0), (15, 8), (45, -8), (75, 8), (105, -8), (120, 0));
        Key(a, Bone(MiiBone.ArmL1, Channel.RotZ), (0, 20), (30, 120, Interpolation.Overshoot), (60, 20), (90, 40), (120, 20));
        Key(a, Bone(MiiBone.ArmR1, Channel.RotZ), (0, -20), (30, -40), (60, -20), (90, -120, Interpolation.Overshoot), (120, -20));
        Key(a, Bone(MiiBone.ArmL2, Channel.RotY), (0, -40), (30, -10), (60, -40), (90, -60), (120, -40));
        Key(a, Bone(MiiBone.ArmR2, Channel.RotY), (0, 40), (30, 60), (60, 40), (90, 10), (120, 40));
        Expression(a, (0, MiiExpression.OpenMouth), (56, MiiExpression.LikeWinkLeft), (66, MiiExpression.OpenMouth));
        return a;
    }

    private static TrackId Bone(MiiBone bone, Channel channel) => TrackId.Bone(bone, channel);

    private static TrackId Limb(MiiLimb limb, Channel channel) => TrackId.Limb(limb, channel);

    private static void Key(MiiAnimation animation, TrackId id, params object[] keys)
    {
        var curve = animation.GetOrCreateCurve(id);
        foreach (var key in keys)
        {
            switch (key)
            {
                case ValueTuple<int, int> k:
                    curve.SetKey(k.Item1, k.Item2, Interpolation.Smooth);
                    break;
                case ValueTuple<int, float> k:
                    curve.SetKey(k.Item1, k.Item2, Interpolation.Smooth);
                    break;
                case ValueTuple<int, int, Interpolation> k:
                    curve.SetKey(k.Item1, k.Item2, k.Item3);
                    break;
                case ValueTuple<int, float, Interpolation> k:
                    curve.SetKey(k.Item1, k.Item2, k.Item3);
                    break;
                default:
                    throw new ArgumentException($"Unsupported key {key}");
            }
        }
    }

    private static void Expression(MiiAnimation animation, params (int Frame, MiiExpression Expression)[] keys)
    {
        var curve = animation.GetOrCreateCurve(TrackId.Expression);
        foreach (var (frame, expression) in keys)
            curve.SetKey(frame, (float)expression, Interpolation.Hold);
    }
}
