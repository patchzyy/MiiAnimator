using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>Playful front clicks and hit-area-specific recoil. Reactions return to the editor idle pose.</summary>
public static class EditorInteractionAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("editor/interactions/front_click/Front_click_ticklish_shrug.miianim", TicklishShrug),
        ("editor/interactions/front_click/Front_click_playful_dodge.miianim", PlayfulDodge),
        ("editor/interactions/front_click/Front_click_mock_scold.miianim", MockScold),
        ("editor/interactions/flinch/Flinch_head.miianim", FlinchHead),
        ("editor/interactions/flinch/Flinch_body.miianim", FlinchBody),
        ("editor/interactions/flinch/Flinch_left_leg.miianim", FlinchLeftLeg),
        ("editor/interactions/flinch/Flinch_right_leg.miianim", FlinchRightLeg),
    ];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootZ = Bone(MiiBone.Root, Channel.PosZ);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestYaw = Bone(MiiBone.Chest, Channel.RotY);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    private static AnimBuilder Begin(string name, int length)
    {
        var b = new AnimBuilder(name, length, female: false);
        b.ArmsRest(0).FeetAt(0, 0, 0).Face(0, MiiExpression.Normal);
        b.Animation.AddEvent(0, "click");
        return b;
    }

    private static MiiAnimation Finish(AnimBuilder b)
    {
        foreach (var id in b.Animation.Tracks.Keys.ToArray())
        {
            var value = ChannelInfo.DefaultValue(id);
            b.Key(id, 0, value).Key(id, b.Animation.Length - 12, value).Key(id, b.Animation.Length, value);
        }
        b.Animation.AddEvent(b.Animation.Length, "done");
        return b.Animation;
    }

    /// <summary>A small recoil followed by two ticklish shoulder pulses and an open-handed shrug.</summary>
    public static MiiAnimation TicklishShrug()
    {
        var b = Begin("Front click ticklish shrug", 180);
        b.Keys(RootZ, (0, 0), (8, -4), (18, -4), (38, 0));
        b.Keys(RootY, (0, 0), (8, -3), (18, -3), (32, -1), (44, -3), (56, -1), (70, -2.5f), (88, -1), (126, 0));
        b.Keys(ChestPitch, (0, 0), (8, -5), (18, -5), (36, 5), (50, -3), (66, 4), (84, -2), (124, 0));
        b.Keys(HeadPitch, (0, 0), (6, -9), (18, -6), (36, 4), (50, -3), (66, 3), (84, -2), (124, 0));
        b.Keys(HeadRoll, (0, 0), (18, -5), (40, 6), (64, -6), (90, 4), (130, 0));
        b.Arms(8, new(43, 86, 10));
        b.Arms(34, new(48, 99, 16), hand: new(-12, 0, 14));
        b.Arms(50, new(46, 92, 14), hand: new(-12, 0, 14));
        b.Arms(68, new(48, 99, 16), hand: new(-12, 0, 14));
        b.Arms(90, new(48, 96, 16), hand: new(-12, 0, 14));
        b.ArmsRest(138);
        b.Animation.AddEvent(8, "react");
        b.Animation.AddEvent(34, "shrug");
        return Finish(b);
    }

    /// <summary>Duck to one side, peek around, then tease the click with a smaller dodge to the other side.</summary>
    public static MiiAnimation PlayfulDodge()
    {
        var b = Begin("Front click playful dodge", 192);
        b.Keys(RootX, (0, 0), (14, 4), (38, 4), (68, -3), (90, -3), (130, 0));
        b.Keys(RootY, (0, 0), (14, -4), (38, -4), (68, -3), (90, -3), (130, 0));
        b.Keys(RootZ, (0, 0), (10, -3), (38, -3), (68, -2), (90, -2), (130, 0));
        b.Keys(ChestRoll, (0, 0), (14, -7), (38, -7), (68, 5), (90, 5), (130, 0));
        b.Keys(ChestYaw, (0, 0), (14, -4), (38, -4), (68, 3), (90, 3), (130, 0));
        b.Keys(HeadYaw, (0, 0), (14, 12), (38, 12), (68, -10), (90, -10), (130, 0));
        b.Keys(HeadRoll, (0, 0), (14, 5), (38, 5), (68, -4), (90, -4), (130, 0));
        b.Arms(14, new(49, 96, 10), hand: new(0, 0, 10));
        b.Arms(38, new(49, 96, 10), hand: new(0, 0, 10));
        b.Arms(68, new(47, 93, 12), hand: new(0, 0, 8));
        b.Arms(90, new(47, 93, 12), hand: new(0, 0, 8));
        b.ArmsRest(138);
        b.Animation.AddEvent(14, "dodge");
        b.Animation.AddEvent(68, "peek");
        return Finish(b);
    }

    /// <summary>Pull back, raise a low "hey, you" palm, wag it twice and give a small mock-serious head shake.</summary>
    public static MiiAnimation MockScold()
    {
        var b = Begin("Front click mock scold", 216);
        b.Keys(RootZ, (0, 0), (10, -4), (24, -4), (48, 0));
        b.Keys(RootY, (0, 0), (10, -3), (24, -3), (48, -1), (120, -1), (166, 0));
        b.Keys(ChestPitch, (0, 0), (10, -5), (24, -5), (48, -2), (120, -2), (166, 0));
        b.Keys(HeadPitch, (0, 0), (8, -10), (24, -6), (48, 2), (118, 2), (146, 5), (172, 0));
        b.Keys(HeadYaw, (0, 0), (44, 0), (60, 9), (80, -9), (102, 6), (124, 0));
        b.Keys(HeadRoll, (0, 0), (42, -4), (120, -4), (172, 0));
        b.Arm(MiiLimb.ArmL, 10, new(36, 73, 8));
        b.Arm(MiiLimb.ArmL, 120, new(36, 73, 8));
        b.Arm(MiiLimb.ArmR, 10, new(-39, 82, 12));
        b.Arm(MiiLimb.ArmR, 42, new(-51, 113, 15), hand: new(-8, 0, -12));
        b.Arm(MiiLimb.ArmR, 120, new(-51, 113, 15), hand: new(-8, 0, -12));
        b.Keys(Bone(MiiBone.HandR, Channel.RotZ), (42, -12), (58, -28), (76, 4), (94, -25), (112, -12));
        b.ArmsRest(174);
        b.Animation.AddEvent(10, "react");
        b.Animation.AddEvent(42, "palm_up");
        return Finish(b);
    }

    /// <summary>Front head tap: a quick backward neck flinch and smaller whole-body retreat, then a soft recovery.</summary>
    public static MiiAnimation FlinchHead()
    {
        var b = Begin("Flinch head", 126);
        b.Keys(RootZ, (0, 0), (4, -2), (10, -6), (18, -6), (34, -2), (52, 0));
        b.Keys(RootY, (0, 0), (10, -3), (18, -3), (34, -1), (52, 0));
        b.Keys(ChestPitch, (0, 0), (10, -4), (18, -4), (32, 2), (56, 0));
        b.Keys(HeadPitch, (0, 0), (6, -17), (14, -14), (30, 4), (44, -2), (62, 0));
        b.Keys(HeadRoll, (0, 0), (8, -3), (14, -3), (30, 2), (54, 0));
        b.Arms(8, new(39, 82, 12));
        b.Arms(18, new(39, 82, 12));
        b.Arms(42, new(36, 73, 8));
        b.ArmsRest(66);
        b.Animation.AddEvent(6, "recoil");
        return Finish(b);
    }

    /// <summary>Front body tap: the chest retreats sharply and the hands briefly spread to regain balance.</summary>
    public static MiiAnimation FlinchBody()
    {
        var b = Begin("Flinch body", 144);
        b.Keys(RootZ, (0, 0), (8, -10), (14, -10), (30, -4), (60, 0));
        b.Keys(RootY, (0, 0), (8, -5), (14, -5), (30, -2), (60, 0));
        b.Keys(ChestPitch, (0, 0), (8, -11), (18, -8), (36, 3), (66, 0));
        b.Keys(HeadPitch, (0, 0), (8, 10), (18, 7), (36, -3), (66, 0));
        b.Arms(8, new(47, 86, 6));
        b.Arms(24, new(36, 78, 16), hand: new(0, 0, 6));
        b.Arms(42, new(38, 73, 10));
        b.ArmsRest(72);
        b.Animation.AddEvent(8, "recoil");
        return Finish(b);
    }

    public static MiiAnimation FlinchLeftLeg() => FlinchLeg(left: true);
    public static MiiAnimation FlinchRightLeg() => FlinchLeg(left: false);

    private static MiiAnimation FlinchLeg(bool left)
    {
        var b = Begin(left ? "Flinch left leg" : "Flinch right leg", 150);
        var side = left ? 1f : -1f; // The Mii's left is +X; the other foot supports the recoil.
        var leg = left ? MiiLimb.LegL : MiiLimb.LegR;
        var arm = left ? MiiLimb.ArmL : MiiLimb.ArmR;
        var otherArm = left ? MiiLimb.ArmR : MiiLimb.ArmL;
        b.Keys(RootX, (0, 0), (8, -side * 3), (18, -side * 3), (54, 0));
        b.Keys(RootY, (0, 0), (8, -5), (18, -5), (36, -2), (56, 0));
        b.Keys(RootZ, (0, 0), (8, -4), (18, -4), (36, -1), (56, 0));
        b.Keys(ChestPitch, (0, 0), (8, -5), (18, -5), (36, 2), (60, 0));
        b.Keys(HeadPitch, (0, 0), (8, -5), (22, 15), (34, 15), (64, 0));
        b.Keys(HeadYaw, (0, 0), (12, side * 18), (34, side * 18), (64, 0));
        b.Keys(HeadRoll, (0, 0), (12, -side * 5), (34, -side * 5), (64, 0));
        b.Keys(Limb(leg, Channel.TargetY), (0, 0), (8, 14), (14, 14), (36, 0));
        b.Keys(Limb(leg, Channel.TargetZ), (0, 0), (8, -13), (14, -13), (36, 0));
        b.Arm(arm, 8, new(side * 46, 88, 10), hand: new(0, 0, side * 8));
        b.Arm(arm, 18, new(side * 46, 88, 10), hand: new(0, 0, side * 8));
        b.Arm(otherArm, 8, new(-side * 36, 75, 12));
        b.Arm(otherArm, 18, new(-side * 36, 75, 12));
        b.ArmsRest(68);
        b.Animation.AddEvent(8, "recoil");
        b.Animation.AddEvent(36, "foot_down");
        return Finish(b);
    }
}
