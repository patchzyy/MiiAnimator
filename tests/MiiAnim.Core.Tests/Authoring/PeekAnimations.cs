using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Miis peeking over the top edge of a card (WheelWizard's MiiPeeker, e.g. the license picker on the profile page).
/// Same geometry as <see cref="WindowAnimations"/>: the card's top edge is the sill (y 88, wall 58 in front), and the
/// view's bottom edge sits on it, so everything below the sill is hidden behind the card.
/// <para>
/// Asleep the Mii dozes with its chin on the edge; hovered it wakes up, rests both hands on the edge and looks around.
/// Every asleep clip starts and ends in <see cref="Asleep"/>, every awake clip in <see cref="Awake"/>; wake up and doze
/// off link the two.
/// </para>
/// </summary>
public static class PeekAnimations
{
    public static readonly ClipCheck.Window Window = WindowAnimations.Window;

    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("peek/asleep/Peek_asleep_breathing.miianim", AsleepBreathing),
        ("peek/asleep/slip/Peek_asleep_slip.miianim", AsleepSlip),
        ("peek/wake_up/Peek_wake_up.miianim", WakeUp),
        ("peek/awake/Peek_awake_look_around.miianim", AwakeLookAround),
        ("peek/awake/Peek_awake_happy.miianim", AwakeHappy),
        ("peek/doze_off/Peek_doze_off.miianim", DozeOff),
    ];

    public static bool StartsAsleep(string path) => path.Contains("/asleep/") || path.Contains("/wake_up/");

    public static bool EndsAsleep(string path) => path.Contains("/asleep/") || path.Contains("/doze_off/");

    // Asleep: sunk down, hunched over the edge with the chin just above it, head dropped and tilted.
    public const float SleepY = -18f;
    public const float SleepZ = 2f;
    public const float SleepChest = 12f;
    public const float SleepHeadPitch = 9f;
    public const float SleepHeadRoll = 10f;

    // Awake: up to the shoulders, leaning on the edge with both hands on it.
    public const float AwakeY = -3f;
    public const float AwakeZ = 4f;
    public const float AwakeChest = 5f;
    public const float AwakeHeadPitch = -3f;

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootZ = Bone(MiiBone.Root, Channel.PosZ);
    private static readonly TrackId RootLean = Bone(MiiBone.Root, Channel.RotZ);
    private static readonly TrackId RootStretch = Bone(MiiBone.Root, Channel.Stretch);
    private static readonly TrackId ChestTilt = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestBreath = Bone(MiiBone.Chest, Channel.ScaleY);
    private static readonly TrackId HeadTilt = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadTurn = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    /// <summary>Wrist resting on the edge (left hand, screen right; mirror for the right).</summary>
    private static readonly Vector3 OnEdge = new(22, 99, 45);

    /// <summary>Above and behind the edge: hands pass here to get onto / off it without going through the card.</summary>
    private static readonly Vector3 OverEdge = new(34, 106, 32);

    /// <summary>On the way up from hanging at the side (rest space).</summary>
    private static readonly Vector3 Raising = new(30, 84, 26);

    // ---- Asleep ----

    /// <summary>Loop: slow, deep breaths with a soft snore on the out-breath and Z's drifting up.</summary>
    public static MiiAnimation AsleepBreathing()
    {
        var b = New("Peek asleep breathing", 480, asleep: true);
        Breaths(b, [(0, 240), (240, 480)]);
        b.Face(150, MiiExpression.BlinkOpenMouth).Face(194, MiiExpression.Blink);
        b.Face(390, MiiExpression.BlinkOpenMouth).Face(434, MiiExpression.Blink);
        Zzz(b, 20, 440, 5, 3101);
        b.Animation.AddEvent(150, "snore");
        return b.Animation;
    }

    /// <summary>Loop: slowly slides down behind the edge until only the eyes show, startles and pulls back up.</summary>
    public static MiiAnimation AsleepSlip()
    {
        var b = New("Peek asleep slip", 480, asleep: true);
        b.Keys(ChestBreath, (0, 1), (70, 1.01f), (150, 1), (360, 1), (420, 1.008f), (480, 1));
        // Sinking is slow and heavy, the catch quick, the settle slow again.
        b.Key(RootY, 0, SleepY).Key(RootY, 30, SleepY, Interpolation.EaseIn).Key(RootY, 250, SleepY - 19);
        b.Key(RootY, 250, SleepY - 19, Interpolation.Snappy).Key(RootY, 262, SleepY + 4);
        b.Keys(RootY, (280, SleepY - 1), (320, SleepY), (480, SleepY));
        // Slides back a little while sinking, so the chin goes down behind the card rather than into it.
        b.Keys(RootZ, (0, SleepZ), (30, SleepZ), (250, SleepZ - 12), (266, SleepZ), (480, SleepZ));
        b.Keys(HeadTilt, (0, SleepHeadPitch), (250, SleepHeadPitch + 4), (260, SleepHeadPitch - 8), (300, SleepHeadPitch - 2), (360, SleepHeadPitch), (480, SleepHeadPitch));
        b.Keys(HeadRoll, (0, SleepHeadRoll), (250, SleepHeadRoll + 5), (262, -2), (320, SleepHeadRoll), (480, SleepHeadRoll));
        b.Keys(HeadTurn, (0, 0), (262, 0), (272, 8), (284, -8), (300, 0), (480, 0));
        b.Keys(RootStretch, (0, 1), (252, 1), (258, 1.06f), (266, 0.97f), (280, 1), (480, 1));
        b.Face(256, MiiExpression.Surprise).Face(286, MiiExpression.Blink).Face(292, MiiExpression.Normal).Face(306, MiiExpression.Blink);
        Zzz(b, 340, 120, 2, 3301);
        b.Animation.AddEvent(256, "startle");
        return b.Animation;
    }

    // ---- Waking up and dozing off ----

    /// <summary>One shot (asleep → awake), quick so hovering feels responsive: startles awake, pops up and grabs the edge.</summary>
    public static MiiAnimation WakeUp()
    {
        var b = new AnimBuilder("Peek wake up", 54, female: false).FkLegs();
        Asleep(b, 0);
        b.Key(RootY, 4, SleepY - 1).Key(RootY, 8, SleepY - 1, Interpolation.Snappy).Key(RootY, 20, AwakeY + 5);
        b.Keys(RootY, (30, AwakeY - 1.5f), (40, AwakeY));
        b.Keys(RootZ, (8, SleepZ), (24, AwakeZ));
        b.Keys(RootStretch, (8, 0.97f), (14, 1.07f), (22, 0.96f), (32, 1.01f), (40, 1));
        b.Keys(ChestTilt, (6, SleepChest + 1), (20, AwakeChest - 2), (34, AwakeChest));
        b.Keys(HeadTilt, (6, SleepHeadPitch + 2), (16, -8), (30, AwakeHeadPitch - 2), (40, AwakeHeadPitch));
        b.Keys(HeadRoll, (8, SleepHeadRoll), (18, -4), (30, 2), (40, 0));
        b.Keys(HeadTurn, (0, 0), (18, 0), (26, 6), (36, 0));
        b.ArmsRest(8);
        b.Arms(16, Raising);
        b.ArmsWorld(24, OverEdge);
        Awake(b, 54);
        b.ArmsWorld(32, OnEdge);
        b.ArmsWorld(40, OnEdge);
        b.Face(10, MiiExpression.Surprise).Face(24, MiiExpression.Blink).Face(28, MiiExpression.Smile).Face(48, MiiExpression.Normal);
        b.Animation.AddEvent(10, "awake");
        b.Animation.AddEvent(54, "done");
        return b.Animation;
    }

    /// <summary>One shot (awake → asleep): heavy blinks, lets go of the edge and sinks back down onto it.</summary>
    public static MiiAnimation DozeOff()
    {
        var b = new AnimBuilder("Peek doze off", 110, female: false).FkLegs();
        Awake(b, 0);
        b.Keys(HeadTilt, (16, AwakeHeadPitch), (36, 2), (44, -2), (90, SleepHeadPitch));
        b.Keys(HeadRoll, (30, 0), (90, SleepHeadRoll));
        b.Keys(ChestTilt, (40, AwakeChest), (86, SleepChest));
        b.Key(RootY, 40, AwakeY, Interpolation.EaseInOut).Key(RootY, 86, SleepY);
        b.Keys(RootZ, (40, AwakeZ), (86, SleepZ));
        b.ArmsWorld(40, OnEdge);
        b.ArmsWorld(54, OverEdge);
        b.Arms(66, Raising);
        Asleep(b, 110);
        b.ArmsRest(80);
        b.Face(14, MiiExpression.Blink).Face(24, MiiExpression.Normal).Face(40, MiiExpression.Blink).Face(54, MiiExpression.Normal);
        b.Face(64, MiiExpression.Blink);
        Zzz(b, 84, 26, 1, 3401);
        b.Animation.AddEvent(64, "asleep");
        b.Animation.AddEvent(110, "done");
        return b.Animation;
    }

    // ---- Awake ----

    /// <summary>Loop: leans on the edge, looks left and right and back at you with a blink and a small smile.</summary>
    public static MiiAnimation AwakeLookAround()
    {
        var b = New("Peek awake look around", 360, asleep: false);
        b.Keys(HeadTurn, (0, 0), (40, 0), (58, 24), (100, 24), (118, 0), (170, 0), (188, -24), (226, -24), (244, 0), (360, 0));
        b.Keys(HeadRoll, (0, 0), (58, 5), (100, 5), (118, 0), (188, -5), (226, -5), (244, 0), (360, 0));
        b.Keys(HeadTilt, (0, AwakeHeadPitch), (260, AwakeHeadPitch), (276, AwakeHeadPitch - 5), (312, AwakeHeadPitch - 5), (330, AwakeHeadPitch), (360, AwakeHeadPitch));
        b.Keys(ChestBreath, (0, 1), (90, 1.008f), (180, 1), (270, 1.008f), (360, 1));
        Blink(b, 84, 6);
        Blink(b, 210, 6);
        b.Face(280, MiiExpression.Smile).Face(320, MiiExpression.Normal);
        b.Animation.AddEvent(280, "smile");
        return b.Animation;
    }

    /// <summary>Loop: sways happily, drums the fingers on the edge and gives a little bounce with a big smile.</summary>
    public static MiiAnimation AwakeHappy()
    {
        var b = New("Peek awake happy", 300, asleep: false);
        b.Keys(RootLean, (0, 0), (60, -2.5f), (120, 0), (180, 2.5f), (240, 0), (300, 0));
        b.Keys(HeadRoll, (0, 0), (60, 6), (120, 0), (180, -6), (240, 0), (300, 0));
        b.Keys(RootY, (0, AwakeY), (196, AwakeY), (204, AwakeY + 4), (212, AwakeY - 1), (220, AwakeY + 3), (230, AwakeY), (300, AwakeY));
        b.Keys(RootStretch, (0, 1), (196, 1), (202, 1.04f), (210, 0.97f), (218, 1.03f), (228, 1), (300, 1));
        foreach (var frame in new[] { 60, 120, 180, 196, 204, 212, 220, 230, 240 })
            b.ArmsWorld(frame, OnEdge);
        b.Keys(Bone(MiiBone.HandL, Channel.RotX), (70, 0), (76, -18), (82, 0), (88, -18), (94, 0), (100, -18), (106, 0));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (73, 0), (79, -18), (85, 0), (91, -18), (97, 0), (103, -18), (109, 0));
        Blink(b, 40, 6);
        b.Face(198, MiiExpression.SmileOpenMouth).Face(236, MiiExpression.Smile).Face(268, MiiExpression.Normal);
        b.Animation.AddEvent(204, "bounce");
        return b.Animation;
    }

    // ---- Baselines ----

    private static AnimBuilder New(string name, int length, bool asleep)
    {
        var b = new AnimBuilder(name, length, female: false).FkLegs();
        foreach (var frame in new[] { 0, length })
        {
            if (asleep)
                Asleep(b, frame);
            else
                Awake(b, frame);
        }
        b.Animation.AddEvent(length, "done");
        return b;
    }

    /// <summary>The asleep baseline: chin on the edge, eyes closed, arms hanging out of sight.</summary>
    private static void Asleep(AnimBuilder b, int frame)
    {
        b.Key(RootX, frame, 0).Key(RootY, frame, SleepY).Key(RootZ, frame, SleepZ).Key(RootLean, frame, 0).Key(RootStretch, frame, 1);
        b.Rot(MiiBone.Chest, frame, SleepChest, 0, 0);
        b.Key(ChestBreath, frame, 1);
        b.Rot(MiiBone.Head, frame, SleepHeadPitch, 0, SleepHeadRoll);
        b.ArmsRest(frame);
        b.Face(frame, MiiExpression.Blink);
    }

    /// <summary>The awake baseline: up to the shoulders, both hands on the edge, looking at you.</summary>
    private static void Awake(AnimBuilder b, int frame)
    {
        b.Key(RootX, frame, 0).Key(RootY, frame, AwakeY).Key(RootZ, frame, AwakeZ).Key(RootLean, frame, 0).Key(RootStretch, frame, 1);
        b.Rot(MiiBone.Chest, frame, AwakeChest, 0, 0);
        b.Key(ChestBreath, frame, 1);
        b.Rot(MiiBone.Head, frame, AwakeHeadPitch, 0, 0);
        b.ArmsWorld(frame, OnEdge);
        b.Keys(Bone(MiiBone.HandL, Channel.RotX), (frame, 0));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (frame, 0));
        b.Face(frame, MiiExpression.Normal);
    }

    private static void Blink(AnimBuilder b, int frame, int duration) =>
        b.Face(frame, MiiExpression.Blink).Face(frame + duration, MiiExpression.Normal);

    /// <summary>Slow sleeping breaths: the chest swells and lifts the head a touch, then sinks back.</summary>
    private static void Breaths(AnimBuilder b, (int From, int To)[] breaths)
    {
        foreach (var (from, to) in breaths)
        {
            var peak = from + (to - from) * 2 / 5;
            b.Keys(ChestBreath, (from, 1), (peak, 1.014f), (to, 1));
            b.Keys(ChestTilt, (from, SleepChest), (peak, SleepChest - 1.6f), (to, SleepChest));
            b.Keys(HeadTilt, (from, SleepHeadPitch), (peak + 12, SleepHeadPitch - 2.5f), (to, SleepHeadPitch));
            b.Keys(HeadRoll, (from, SleepHeadRoll), (peak + 20, SleepHeadRoll - 1.5f), (to, SleepHeadRoll));
        }
    }

    /// <summary>Z's drifting up beside the head (screen right), small enough for a small view.</summary>
    private static void Zzz(AnimBuilder b, int start, int duration, int count, uint seed) =>
        b.Animation.Particles.Add(new ParticleEffect
        {
            Name = "Zzz", StartFrame = start, Duration = duration, Count = count, Seed = seed,
            AttachBone = (byte)MiiBone.Head, Offset = new Vector3(38, 84, 12), SpawnRadius = new Vector3(2.5f, 2.5f, 0),
            Direction = new Vector3(0.5f, 1, 0), Spread = 9, SpeedMin = 10, SpeedMax = 13, Gravity = 2, Drag = 0.25f,
            Shape = FriendsPresenceAnimations.SleepZ, Orientation = ParticleOrientation.Upright,
            LifeMin = 1.6f, LifeMax = 1.9f, SizeStart = 8, SizeEnd = 15,
            ColorStart = new Vector4(0.86f, 0.93f, 1f, 1f), ColorEnd = new Vector4(0.86f, 0.93f, 1f, 0f),
        });
}
