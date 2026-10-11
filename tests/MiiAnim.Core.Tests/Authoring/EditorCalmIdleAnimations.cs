using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Casual full-body editor idles that join Editor_idle_calm_breathing. Every face stays Normal, both feet end where
/// they started and each clip begins and ends with a half-second neutral hold, so any idle can follow any other.
/// </summary>
public static class EditorCalmIdleAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("editor/idle/Editor_idle_easy_arm_swing.miianim", EasyArmSwing),
        ("editor/idle/Editor_idle_hands_clasped.miianim", HandsClasped),
        ("editor/idle/Editor_idle_hand_on_hip.miianim", HandOnHip),
        ("editor/idle/Editor_idle_knee_bounce.miianim", KneeBounce),
        ("editor/idle/Editor_idle_hum_along.miianim", HumAlong),
        ("editor/idle/Editor_idle_easy_twist.miianim", EasyTwist),
        ("editor/idle/Editor_idle_lazy_lean_back.miianim", LazyLeanBack),
        ("editor/idle/Editor_idle_look_around.miianim", LookAround),
        ("editor/idle/Editor_idle_toe_tap.miianim", ToeTap),
        ("editor/idle/Editor_idle_shoulder_shrug.miianim", ShoulderShrug),
        ("editor/idle/Editor_idle_loose_wrists.miianim", LooseWrists),
        ("editor/idle/Editor_idle_rock_heel_toe.miianim", RockHeelToe),
        ("editor/idle/Editor_idle_deep_sigh.miianim", DeepSigh),
    ];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestYaw = Bone(MiiBone.Chest, Channel.RotY);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    // Rest-pose shoulder and the arm's resting reach, used to swing a hanging arm about the shoulder.
    private const float ShoulderX = 12f;
    private const float ShoulderY = 102.66f;
    private const float Spread = 15.6f;
    private const float Reach = 44.2f;

    /// <summary>The left wrist of a hanging arm swung <paramref name="degrees"/> forward (negative = back).</summary>
    private static Vector3 Swung(float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        return new Vector3(ShoulderX + Spread, ShoulderY - Reach * MathF.Cos(radians), Reach * MathF.Sin(radians));
    }

    private static AnimBuilder Begin(string name, int seconds)
    {
        var b = new AnimBuilder("Editor idle " + name, seconds * 60, female: false);
        b.ArmsRest(0).Face(0, MiiExpression.Normal);
        b.FeetAt(0, 0, 0);
        return b;
    }

    private static MiiAnimation Finish(AnimBuilder b)
    {
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

    /// <summary>Arms swing a little, opposite each other, like a slow idle stroll without the walking.</summary>
    public static MiiAnimation EasyArmSwing()
    {
        var b = Begin("easy arm swing", 12);
        b.ArmsRest(80);
        var swing = new[] { (140, 9f), (220, -7f), (300, 9f), (380, -7f), (460, 8f) };
        foreach (var (frame, degrees) in swing)
        {
            b.Arm(MiiLimb.ArmL, frame, Swung(degrees));
            b.Arm(MiiLimb.ArmR, frame, MirrorX(Swung(-degrees)));
        }
        b.ArmsRest(560);
        b.Keys(ChestYaw, (80, 0), (140, -1.2f), (220, 1f), (300, -1.2f), (380, 1f), (460, -1f), (560, 0));
        b.Keys(HeadYaw, (80, 0), (140, 0.8f), (220, -0.7f), (300, 0.8f), (380, -0.7f), (460, 0.6f), (560, 0));
        return Finish(b);
    }

    /// <summary>Both hands drift together in front of the waist, fidget for a moment, then drop.</summary>
    public static MiiAnimation HandsClasped()
    {
        var b = Begin("hands clasped", 13);
        b.ArmsRest(90);
        b.Arms(160, new(26, 64, 14), hand: new(0, 0, 3));
        b.Arms(220, new(7, 66, 22), hand: new(0, 0, 4));
        b.Arms(290, new(6, 65, 22), hand: new(0, 0, 4));
        b.Arms(350, new(8, 66, 22), hand: new(0, 0, 6));
        b.Arms(480, new(8, 66, 22), hand: new(0, 0, 6));
        b.Arms(545, new(26, 64, 14), hand: new(0, 0, 3));
        b.ArmsRest(630);
        b.Keys(HeadPitch, (150, 0), (240, 1.2f), (480, 1.2f), (630, 0));
        b.Keys(ChestPitch, (150, 0), (240, 0.8f), (480, 0.8f), (630, 0));
        b.Keys(HeadRoll, (290, 0), (360, 1.1f), (440, 1.1f), (540, 0));
        return Finish(b);
    }

    /// <summary>A hand rests on the hip while the body leans easily into that side, then lets go.</summary>
    public static MiiAnimation HandOnHip()
    {
        var b = Begin("hand on hip", 13);
        b.ArmRest(MiiLimb.ArmR, 100);
        b.Arm(MiiLimb.ArmR, 160, MirrorX(new(31, 63, 4)), swivel: 9, hand: new(0, 0, 4));
        b.Arm(MiiLimb.ArmR, 220, MirrorX(new(25, 70, 5)), swivel: 18, hand: new(0, 0, 8));
        b.Arm(MiiLimb.ArmR, 500, MirrorX(new(25, 70, 5)), swivel: 18, hand: new(0, 0, 8));
        b.Arm(MiiLimb.ArmR, 560, MirrorX(new(31, 63, 4)), swivel: 9, hand: new(0, 0, 4));
        b.ArmRest(MiiLimb.ArmR, 640);
        b.Keys(RootX, (110, 0), (230, -2f), (500, -2f), (640, 0));
        b.Keys(RootY, (110, 0), (230, -1.2f), (500, -1.2f), (630, 0));
        b.Keys(ChestRoll, (110, 0), (230, 1.2f), (500, 1.2f), (630, 0));
        b.Keys(HeadRoll, (130, 0), (260, -1.6f), (500, -1.6f), (650, 0));
        b.Keys(HeadYaw, (330, 0), (410, 3f), (460, 3f), (600, 0));
        return Finish(b);
    }

    /// <summary>Little soft knee dips, as if waiting with a tune in mind. Arms hang loose and trail the beat.</summary>
    public static MiiAnimation KneeBounce()
    {
        var b = Begin("knee bounce", 10);
        b.ArmsRest(70);
        b.Keys(RootY, (70, 0), (110, -2.3f), (150, 0), (190, -2.3f), (230, 0), (270, -2.3f), (310, 0), (350, -2.3f), (390, 0), (450, 0));
        b.Keys(ChestPitch, (70, 0), (110, 0.8f), (150, 0), (190, 0.8f), (230, 0), (270, 0.8f), (310, 0), (350, 0.8f), (390, 0), (450, 0));
        b.Keys(HeadPitch, (70, 0), (126, 0.8f), (166, 0), (206, 0.8f), (246, 0), (286, 0.8f), (326, 0), (366, 0.8f), (406, 0), (460, 0));
        foreach (var (frame, degrees) in new[] { (110, 2.5f), (190, 2.5f), (270, 2.5f), (350, 2.5f), (150, -1f), (230, -1f), (310, -1f), (390, -1f) })
        {
            b.Arm(MiiLimb.ArmL, frame, Swung(degrees));
            b.Arm(MiiLimb.ArmR, frame, MirrorX(Swung(degrees)));
        }
        b.ArmsRest(470);
        return Finish(b);
    }

    /// <summary>Head and shoulders sway side to side in a slow, contented hum.</summary>
    public static MiiAnimation HumAlong()
    {
        var b = Begin("hum along", 14);
        b.Keys(HeadRoll, (90, 0), (190, 2.4f), (310, -2.4f), (430, 2.4f), (550, -2.2f), (650, 0));
        b.Keys(ChestRoll, (90, 0), (200, -1f), (320, 1f), (440, -1f), (560, 1f), (660, 0));
        b.Keys(HeadYaw, (90, 0), (200, -1.5f), (320, 1.5f), (440, -1.5f), (560, 1.5f), (660, 0));
        b.Keys(RootX, (90, 0), (200, 1.2f), (320, -1.2f), (440, 1.2f), (560, -1.2f), (660, 0));
        b.Keys(HeadPitch, (90, 0), (260, 0.9f), (380, -0.5f), (500, 0.9f), (660, 0));
        b.ArmsRest(90);
        b.Arm(MiiLimb.ArmL, 200, Swung(-3.5f));
        b.Arm(MiiLimb.ArmR, 200, MirrorX(Swung(3.5f)));
        b.Arm(MiiLimb.ArmL, 320, Swung(3.5f));
        b.Arm(MiiLimb.ArmR, 320, MirrorX(Swung(-3.5f)));
        b.Arm(MiiLimb.ArmL, 440, Swung(-3.5f));
        b.Arm(MiiLimb.ArmR, 440, MirrorX(Swung(3.5f)));
        b.ArmsRest(670);
        return Finish(b);
    }

    /// <summary>The torso turns one way and then the other from the waist, arms following a beat behind.</summary>
    public static MiiAnimation EasyTwist()
    {
        var b = Begin("easy twist", 13);
        b.Keys(ChestYaw, (90, 0), (220, 5.5f), (320, 5.5f), (470, -5.5f), (570, -5.5f), (690, 0));
        b.Keys(HeadYaw, (90, 0), (220, 1.5f), (320, 1.5f), (470, -1.5f), (570, -1.5f), (700, 0));
        b.Keys(ChestRoll, (90, 0), (230, -0.8f), (320, -0.8f), (480, 0.8f), (570, 0.8f), (700, 0));
        b.ArmsRest(90);
        b.Arm(MiiLimb.ArmL, 250, Swung(-6f));
        b.Arm(MiiLimb.ArmR, 250, MirrorX(Swung(6f)));
        b.Arm(MiiLimb.ArmL, 360, Swung(-3f));
        b.Arm(MiiLimb.ArmR, 360, MirrorX(Swung(3f)));
        b.Arm(MiiLimb.ArmL, 500, Swung(6f));
        b.Arm(MiiLimb.ArmR, 500, MirrorX(Swung(-6f)));
        b.Arm(MiiLimb.ArmL, 600, Swung(3f));
        b.Arm(MiiLimb.ArmR, 600, MirrorX(Swung(-3f)));
        b.ArmsRest(730);
        return Finish(b);
    }

    /// <summary>Leans back a touch with the chin up as if looking at a slow cloud, then eases forward again.</summary>
    public static MiiAnimation LazyLeanBack()
    {
        var b = Begin("lazy lean back", 13);
        b.Keys(ChestPitch, (110, 0), (250, -2.6f), (500, -2.6f), (660, 0));
        b.Keys(HeadPitch, (110, 0), (260, -4.2f), (500, -4.2f), (670, 0));
        b.Keys(HeadYaw, (200, 0), (310, 3.5f), (500, 3.5f), (650, 0));
        b.Keys(RootY, (110, 0), (250, -0.8f), (500, -0.8f), (660, 0));
        b.ArmsRest(110);
        b.Arm(MiiLimb.ArmL, 260, Swung(-5f));
        b.Arm(MiiLimb.ArmR, 260, MirrorX(Swung(-5f)));
        b.Arm(MiiLimb.ArmL, 500, Swung(-5f));
        b.Arm(MiiLimb.ArmR, 500, MirrorX(Swung(-5f)));
        b.ArmsRest(680);
        return Finish(b);
    }

    /// <summary>Slowly looks to one side, then the other, with a pause facing forward in between.</summary>
    public static MiiAnimation LookAround()
    {
        var b = Begin("look around", 15);
        b.Keys(HeadYaw, (100, 0), (210, -8f), (300, -8f), (420, 0), (500, 0), (610, 8f), (700, 8f), (830, 0));
        b.Keys(ChestYaw, (100, 0), (220, -2.5f), (300, -2.5f), (430, 0), (500, 0), (620, 2.5f), (700, 2.5f), (840, 0));
        b.Keys(HeadPitch, (150, 0), (240, 1.2f), (300, 1.2f), (420, 0), (540, 0), (640, -1f), (700, -1f), (830, 0));
        b.Keys(HeadRoll, (150, 0), (240, 1f), (300, 1f), (420, 0), (540, 0), (640, -1f), (700, -1f), (830, 0));
        return Finish(b);
    }

    /// <summary>One foot taps along softly while the head nods to the same slow beat.</summary>
    public static MiiAnimation ToeTap()
    {
        var b = Begin("toe tap", 11);
        var planted = b.NaturalFoot(MiiLimb.LegR, 0, 0);
        b.Foot(MiiLimb.LegR, 90, planted, 0);
        var beats = new[] { 130, 190, 250, 310 };
        foreach (var beat in beats)
        {
            b.Foot(MiiLimb.LegR, beat - 18, planted + new Vector3(0, 0.3f, 0.2f), 0);
            b.Foot(MiiLimb.LegR, beat, planted + new Vector3(0, 3.2f, 1.6f), 0);
            b.Foot(MiiLimb.LegR, beat + 12, planted, 0);
        }
        b.Foot(MiiLimb.LegR, 360, planted, 0);
        b.Keys(HeadPitch, (90, 0), (142, 1f), (172, 0), (202, 1f), (232, 0), (262, 1f), (292, 0), (322, 1f), (362, 0));
        b.Keys(RootY, (90, 0), (142, -0.6f), (172, 0), (202, -0.6f), (232, 0), (262, -0.6f), (292, 0), (322, -0.6f), (362, 0));
        b.Keys(ChestRoll, (90, 0), (190, 0.7f), (310, 0.7f), (380, 0));
        return Finish(b);
    }

    /// <summary>A small shrug: both shoulders lift and drop, with a tilt of the head and hands slightly out.</summary>
    public static MiiAnimation ShoulderShrug()
    {
        var b = Begin("shoulder shrug", 11);
        b.ArmsRest(120);
        b.Arms(190, new(32, 62, 5), hand: new(0, 0, 4));
        b.Arms(250, new(32, 62, 5), hand: new(0, 0, 4));
        b.ArmsRest(340);
        b.Keys(HeadRoll, (120, 0), (190, -2f), (260, -2f), (350, 0));
        b.Keys(HeadPitch, (120, 0), (190, 0.9f), (260, 0.9f), (350, 0));
        b.Keys(ChestPitch, (120, 0), (190, -0.8f), (260, -0.8f), (350, 0));
        b.Keys(RootY, (120, 0), (190, -0.5f), (260, -0.5f), (350, 0));
        b.Keys(HeadYaw, (400, 0), (470, -3f), (520, -3f), (600, 0));
        return Finish(b);
    }

    /// <summary>Hands hang loose and the wrists give a few relaxed little shakes.</summary>
    public static MiiAnimation LooseWrists()
    {
        var b = Begin("loose wrists", 10);
        b.ArmsRest(80);
        b.Arms(150, new(30, 62, 9), hand: new(0, 0, 0));
        var shakes = new[] { (190, 7f), (220, -7f), (250, 6f), (280, -6f), (310, 4f), (340, -4f), (370, 0f) };
        foreach (var (frame, bend) in shakes)
            b.Arms(frame, new(30, 62, 9), hand: new(bend, 0, 0));
        b.Arms(400, new(30, 62, 9), hand: new(0, 0, 0));
        b.ArmsRest(470);
        b.Keys(HeadPitch, (100, 0), (170, 1.2f), (390, 1.2f), (480, 0));
        b.Keys(ChestPitch, (100, 0), (170, 0.7f), (390, 0.7f), (480, 0));
        return Finish(b);
    }

    /// <summary>Slow weight shift forward onto the toes and back, as if watching something.</summary>
    public static MiiAnimation RockHeelToe()
    {
        var b = Begin("rock heel toe", 14);
        b.Keys(ChestPitch, (90, 0), (220, 2.2f), (330, 2.2f), (470, -1.8f), (570, -1.8f), (700, 0));
        b.Keys(HeadPitch, (90, 0), (230, -1.4f), (330, -1.4f), (480, 1.2f), (570, 1.2f), (710, 0));
        b.Keys(RootY, (90, 0), (220, -0.7f), (330, -0.7f), (470, -0.9f), (570, -0.9f), (700, 0));
        b.ArmsRest(90);
        b.Arm(MiiLimb.ArmL, 230, Swung(3f));
        b.Arm(MiiLimb.ArmR, 230, MirrorX(Swung(3f)));
        b.Arm(MiiLimb.ArmL, 330, Swung(3f));
        b.Arm(MiiLimb.ArmR, 330, MirrorX(Swung(3f)));
        b.Arm(MiiLimb.ArmL, 480, Swung(-4f));
        b.Arm(MiiLimb.ArmR, 480, MirrorX(Swung(-4f)));
        b.Arm(MiiLimb.ArmL, 570, Swung(-4f));
        b.Arm(MiiLimb.ArmR, 570, MirrorX(Swung(-4f)));
        b.ArmsRest(720);
        return Finish(b);
    }

    /// <summary>A long, slow breath in with lifted shoulders and a loose sigh out.</summary>
    public static MiiAnimation DeepSigh()
    {
        var b = Begin("deep sigh", 12);
        b.ArmsRest(80);
        b.Arms(200, new(30, 64, 4), hand: new(0, 0, 3));
        b.Arms(250, new(30, 64, 4), hand: new(0, 0, 3));
        b.ArmsRest(380);
        b.Keys(ChestPitch, (80, 0), (200, -2f), (250, -2f), (380, 1f), (440, 1f), (540, 0));
        b.Keys(HeadPitch, (80, 0), (200, -1.4f), (250, -1.4f), (390, 2.6f), (440, 2.6f), (550, 0));
        b.Keys(RootY, (80, 0), (200, 0), (380, -1.4f), (440, -1.4f), (540, 0));
        return Finish(b);
    }
}
