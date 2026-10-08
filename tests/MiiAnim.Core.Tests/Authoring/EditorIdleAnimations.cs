using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>Quiet editor loops. Every face stays Normal, so the selected features remain visible.</summary>
public static class EditorIdleAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("editor/idle/Editor_idle_calm_breathing.miianim", CalmBreathing),
        ("editor/idle/Editor_idle_weight_left.miianim", WeightLeft),
        ("editor/idle/Editor_idle_weight_right.miianim", WeightRight),
        ("editor/idle/Editor_idle_slow_sway.miianim", SlowSway),
        ("editor/idle/Editor_idle_relaxed_shoulders.miianim", RelaxedShoulders),
        ("editor/idle/Editor_idle_hands_settle.miianim", HandsSettle),
        ("editor/idle/Editor_idle_sleeve_glance.miianim", SleeveGlance),
        ("editor/idle/Editor_idle_quiet_palm.miianim", QuietPalm),
        ("editor/idle/Editor_idle_loose_arm.miianim", LooseArm),
        ("editor/idle/Editor_idle_thoughtful_pause.miianim", ThoughtfulPause),
        ("editor/idle/Editor_idle_attentive_upper.miianim", AttentiveUpper),
        ("editor/idle/Editor_idle_glance_left_upper.miianim", GlanceLeftUpper),
        ("editor/idle/Editor_idle_glance_right_upper.miianim", GlanceRightUpper),
        ("editor/idle/Editor_idle_curious_tilt_upper.miianim", CuriousTiltUpper),
        ("editor/idle/Editor_idle_quiet_nod_upper.miianim", QuietNodUpper),
        ("editor/idle/Editor_idle_upward_thought_upper.miianim", UpwardThoughtUpper),
        ("editor/idle/Editor_idle_downward_glance_upper.miianim", DownwardGlanceUpper),
        ("editor/idle/Editor_idle_look_between_upper.miianim", LookBetweenUpper),
        ("editor/idle/Editor_idle_relaxed_listen_upper.miianim", RelaxedListenUpper),
        ("editor/idle/Editor_idle_neck_settle_upper.miianim", NeckSettleUpper),
    ];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestYaw = Bone(MiiBone.Chest, Channel.RotY);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    private static AnimBuilder Begin(string name, int seconds, bool upper = false)
    {
        var b = new AnimBuilder("Editor idle " + name, seconds * 60, female: false);
        b.ArmsRest(0).Face(0, MiiExpression.Normal);
        if (upper)
            b.FkLegs();
        else
            b.FeetAt(0, 0, 0);
        return b;
    }

    private static MiiAnimation Finish(AnimBuilder b)
    {
        // A half-second neutral hold on either side makes every idle compatible with every other
        // idle and with the action reactions. Smooth segments arrive with zero velocity.
        foreach (var id in b.Animation.Tracks.Keys.ToArray())
        {
            var value = id.Target.Kind == TrackTargetKind.Limb && id.Channel == Channel.IkBlend
                ? b.Animation.Evaluate(id, 0)
                : ChannelInfo.DefaultValue(id);
            b.Key(id, 0, value).Key(id, 30, value)
                .Key(id, b.Animation.Length - 30, value).Key(id, b.Animation.Length, value);
        }
        return b.Animation;
    }

    public static MiiAnimation CalmBreathing()
    {
        var b = Begin("calm breathing", 12);
        b.Keys(RootY, (80, 0), (188, -1.4f), (290, 0), (360, 0), (466, -1.1f), (578, 0));
        b.Keys(ChestPitch, (80, 0), (180, -1.2f), (286, 0), (360, 0), (462, -0.9f), (580, 0));
        b.Keys(HeadPitch, (80, 0), (188, 0.7f), (290, 0), (360, 0), (466, 0.6f), (578, 0));
        return Finish(b);
    }

    public static MiiAnimation WeightLeft()
    {
        var b = Begin("weight left", 14);
        b.Keys(RootX, (106, 0), (220, 3.5f), (344, 3.5f), (482, 0));
        b.Keys(RootY, (106, 0), (220, -2), (344, -2), (482, 0));
        b.Keys(ChestRoll, (106, 0), (220, -1.4f), (344, -1.4f), (482, 0));
        b.Keys(HeadRoll, (106, 0), (240, 2), (344, 2), (500, 0));
        b.Keys(HeadYaw, (174, 0), (274, 4), (344, 4), (462, 0));
        return Finish(b);
    }

    public static MiiAnimation WeightRight()
    {
        var b = Begin("weight right", 13);
        b.Keys(RootX, (84, 0), (214, -3.2f), (340, -3.2f), (486, 0));
        b.Keys(RootY, (84, 0), (214, -1.8f), (340, -1.8f), (486, 0));
        b.Keys(ChestRoll, (84, 0), (214, 1.3f), (340, 1.3f), (486, 0));
        b.Keys(HeadRoll, (112, 0), (230, -1.8f), (340, -1.8f), (502, 0));
        b.Keys(HeadPitch, (196, 0), (284, 1.6f), (346, 1.6f), (442, 0));
        return Finish(b);
    }

    public static MiiAnimation SlowSway()
    {
        var b = Begin("slow sway", 16);
        b.Keys(RootX, (100, 0), (250, 2.4f), (340, 2.4f), (540, -2.4f), (630, -2.4f), (810, 0));
        b.Keys(RootY, (100, 0), (250, -1.4f), (340, -1.4f), (440, -0.8f), (540, -1.4f), (630, -1.4f), (810, 0));
        b.Keys(ChestRoll, (100, 0), (250, -1), (340, -1), (540, 1), (630, 1), (810, 0));
        b.Keys(HeadRoll, (100, 0), (270, 1.2f), (350, 1.2f), (560, -1.2f), (640, -1.2f), (828, 0));
        return Finish(b);
    }

    public static MiiAnimation RelaxedShoulders()
    {
        var b = Begin("relaxed shoulders", 12);
        b.Keys(ChestPitch, (110, 0), (214, -1.3f), (258, -1.3f), (376, 0));
        b.Keys(HeadPitch, (110, 0), (226, 1.6f), (258, 1.6f), (388, 0));
        b.ArmsRest(110);
        b.Arms(214, new(35, 73, 6), hand: new(0, 0, 3));
        b.Arms(258, new(35, 73, 6), hand: new(0, 0, 3));
        b.ArmsRest(390);
        return Finish(b);
    }

    public static MiiAnimation HandsSettle()
    {
        var b = Begin("hands settle", 11);
        b.ArmsRest(100);
        b.Arms(210, new(36, 67, 8), hand: new(0, 8, 5));
        b.Arms(280, new(36, 67, 8), hand: new(0, 8, 5));
        b.ArmsRest(410);
        b.Keys(HeadPitch, (126, 0), (232, 2), (286, 2), (408, 0));
        b.Keys(HeadYaw, (320, 0), (396, 2.5f), (474, 0));
        return Finish(b);
    }

    public static MiiAnimation SleeveGlance()
    {
        var b = Begin("sleeve glance", 13);
        b.Keys(HeadPitch, (116, 0), (206, 6), (294, 6), (402, 0));
        b.Keys(HeadYaw, (116, 0), (216, 8), (294, 8), (416, 0));
        b.Keys(ChestYaw, (116, 0), (216, 1.5f), (294, 1.5f), (416, 0));
        b.ArmRest(MiiLimb.ArmL, 116);
        b.Arm(MiiLimb.ArmL, 216, new(42, 79, 12), hand: new(0, 0, 8));
        b.Arm(MiiLimb.ArmL, 294, new(42, 79, 12), hand: new(0, 0, 8));
        b.ArmRest(MiiLimb.ArmL, 420);
        return Finish(b);
    }

    public static MiiAnimation QuietPalm()
    {
        var b = Begin("quiet palm", 12);
        b.ArmRest(MiiLimb.ArmR, 140);
        b.Arm(MiiLimb.ArmR, 248, new(-40, 76, 16), hand: new(-18, 0, -8));
        b.Arm(MiiLimb.ArmR, 314, new(-40, 76, 16), hand: new(-18, 0, -8));
        b.ArmRest(MiiLimb.ArmR, 452);
        b.Keys(HeadRoll, (140, 0), (240, -1.8f), (314, -1.8f), (456, 0));
        b.Keys(HeadYaw, (180, 0), (284, -3.5f), (350, -3.5f), (454, 0));
        return Finish(b);
    }

    public static MiiAnimation LooseArm()
    {
        var b = Begin("loose arm", 15);
        b.ArmRest(MiiLimb.ArmL, 180);
        b.Arm(MiiLimb.ArmL, 300, new(48, 86, 5), swivel: -5, hand: new(0, 0, 4));
        b.Arm(MiiLimb.ArmL, 368, new(48, 86, 5), swivel: -5, hand: new(0, 0, 4));
        b.Arm(MiiLimb.ArmL, 450, new(38, 73, 8), hand: new(0, 0, 2));
        b.ArmRest(MiiLimb.ArmL, 544);
        b.Keys(ChestRoll, (180, 0), (300, -1.5f), (368, -1.5f), (544, 0));
        b.Keys(HeadRoll, (180, 0), (320, 1.8f), (384, 1.8f), (560, 0));
        return Finish(b);
    }

    public static MiiAnimation ThoughtfulPause()
    {
        var b = Begin("thoughtful pause", 14);
        b.Keys(ChestPitch, (160, 0), (270, 1.6f), (388, 1.6f), (520, 0));
        b.Keys(ChestYaw, (160, 0), (270, -2), (388, -2), (520, 0));
        b.Keys(HeadPitch, (160, 0), (270, -4), (388, -4), (518, 0));
        b.Keys(HeadYaw, (160, 0), (282, -5), (388, -5), (536, 0));
        b.Keys(HeadRoll, (208, 0), (320, 2), (388, 2), (528, 0));
        return Finish(b);
    }

    public static MiiAnimation AttentiveUpper()
    {
        var b = Begin("attentive upper", 11, upper: true);
        b.Keys(HeadPitch, (114, 0), (218, -1.6f), (306, -1.6f), (414, 0));
        b.Keys(HeadYaw, (188, 0), (284, 2.4f), (354, 2.4f), (446, 0));
        return Finish(b);
    }

    public static MiiAnimation GlanceLeftUpper()
    {
        var b = Begin("glance left upper", 13, upper: true);
        b.Keys(HeadYaw, (144, 0), (246, -8), (308, -8), (440, 0));
        b.Keys(HeadPitch, (164, 0), (256, 0.8f), (308, 0.8f), (440, 0));
        b.Keys(HeadRoll, (164, 0), (256, 0.8f), (308, 0.8f), (452, 0));
        return Finish(b);
    }

    public static MiiAnimation GlanceRightUpper()
    {
        var b = Begin("glance right upper", 12, upper: true);
        b.Keys(HeadYaw, (118, 0), (228, 8), (304, 8), (430, 0));
        b.Keys(HeadPitch, (144, 0), (244, -1.2f), (304, -1.2f), (438, 0));
        return Finish(b);
    }

    public static MiiAnimation CuriousTiltUpper()
    {
        var b = Begin("curious tilt upper", 14, upper: true);
        b.Keys(HeadRoll, (164, 0), (284, -3.5f), (382, -3.5f), (534, 0));
        b.Keys(HeadYaw, (184, 0), (304, 2.5f), (382, 2.5f), (534, 0));
        b.Keys(HeadPitch, (204, 0), (324, 1.2f), (382, 1.2f), (534, 0));
        return Finish(b);
    }

    public static MiiAnimation QuietNodUpper()
    {
        var b = Begin("quiet nod upper", 10, upper: true);
        b.Keys(HeadPitch, (122, 0), (198, 3.5f), (268, 0), (324, -0.7f), (402, 0));
        b.Keys(HeadRoll, (144, 0), (228, 0.8f), (320, 0));
        return Finish(b);
    }

    public static MiiAnimation UpwardThoughtUpper()
    {
        var b = Begin("upward thought upper", 15, upper: true);
        b.Keys(HeadPitch, (196, 0), (318, -4), (448, -4), (606, 0));
        b.Keys(HeadYaw, (216, 0), (340, -3.5f), (448, -3.5f), (620, 0));
        b.Keys(HeadRoll, (268, 0), (382, 1.8f), (448, 1.8f), (620, 0));
        return Finish(b);
    }

    public static MiiAnimation DownwardGlanceUpper()
    {
        var b = Begin("downward glance upper", 12, upper: true);
        b.Keys(HeadPitch, (156, 0), (268, 5), (330, 5), (460, 0));
        b.Keys(HeadYaw, (190, 0), (286, 1.8f), (330, 1.8f), (460, 0));
        return Finish(b);
    }

    public static MiiAnimation LookBetweenUpper()
    {
        var b = Begin("look between upper", 16, upper: true);
        b.Keys(HeadYaw, (144, 0), (254, 6), (310, 6), (402, 0), (478, 0), (594, -5), (658, -5), (780, 0));
        b.Keys(HeadPitch, (190, 0), (286, -1), (336, -1), (420, 0), (520, 0), (626, 1.2f), (674, 1.2f), (798, 0));
        return Finish(b);
    }

    public static MiiAnimation RelaxedListenUpper()
    {
        var b = Begin("relaxed listen upper", 14, upper: true);
        b.Keys(HeadRoll, (184, 0), (294, 2.4f), (370, 2.4f), (484, 1), (600, 0));
        b.Keys(HeadYaw, (214, 0), (324, -2.2f), (370, -2.2f), (500, 0));
        b.Keys(HeadPitch, (280, 0), (362, 1.6f), (440, 0), (512, -1), (600, 0));
        return Finish(b);
    }

    public static MiiAnimation NeckSettleUpper()
    {
        var b = Begin("neck settle upper", 13, upper: true);
        b.Keys(HeadPitch, (118, 0), (224, 2), (280, 2), (414, -1.2f), (476, -1.2f), (588, 0));
        b.Keys(HeadYaw, (118, 0), (224, 3), (280, 3), (414, -2), (476, -2), (588, 0));
        b.Keys(HeadRoll, (160, 0), (250, -0.8f), (280, -0.8f), (426, 0.8f), (476, 0.8f), (600, 0));
        return Finish(b);
    }
}
