using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Presence clips for the friends cards, on the same right-facing card baseline as <see cref="FriendsAnimations"/>.
/// Offline friends sleep (eyes closed, slumped, with floating Z's in the pocket of the card right of the head);
/// online friends idle awake and now and then wave at you with a real forearm wave. Doze-off and wake-up clips
/// link the two baselines when a friend's status changes.
/// </summary>
public static class FriendsPresenceAnimations
{
    public const float SleepHeadPitch = 5f;
    public const float SleepHeadRoll = 9f;
    public const float SleepChestPitch = 4f;

    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("friends/online/Friends_online_idle_curious.miianim", OnlineIdleCurious),
        ("friends/online/wave/Friends_wave_hello.miianim", WaveHello),
        ("friends/online/wave/Friends_wave_excited.miianim", WaveExcited),
        ("friends/online/wave/Friends_wave_shy.miianim", WaveShy),
        ("friends/asleep/Friends_asleep_breathing.miianim", AsleepBreathing),
        ("friends/asleep/Friends_asleep_nodding.miianim", AsleepNodding),
        ("friends/asleep/Friends_asleep_bubble.miianim", AsleepBubble),
        ("friends/doze_off/Friends_doze_off.miianim", DozeOff),
        ("friends/wake_up/Friends_wake_up.miianim", WakeUp),
    ];

    /// <summary>Clips that start and end asleep (the others start and end on the awake card baseline).</summary>
    public static bool StartsAsleep(string path) => path.Contains("/asleep/") || path.Contains("/wake_up/");

    public static bool EndsAsleep(string path) => path.Contains("/asleep/") || path.Contains("/doze_off/");

    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestYaw = Bone(MiiBone.Chest, Channel.RotY);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId ChestBreath = Bone(MiiBone.Chest, Channel.ScaleY);

    private const float AwakePitch = FriendsAnimations.BaselineHeadPitch;
    private const float AwakeRoll = FriendsAnimations.BaselineHeadRoll;

    /// <summary>A bold rounded "Z" for sleeping.</summary>
    public static readonly ParticleShape SleepZ = new(
        "Sleep Z",
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40"><path fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" stroke-linejoin="round" d="M9 9 H31 L9 31 H31"/></svg>"""
    );

    /// <summary>A see-through soap-film bubble with a highlight.</summary>
    public static readonly ParticleShape SnoreBubble = new(
        "Snore bubble",
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40"><circle cx="20" cy="20" r="17" fill="#fff" fill-opacity="0.3" stroke="#fff" stroke-width="2.5"/><ellipse cx="13" cy="12.5" rx="4.5" ry="2.8" fill="#fff" transform="rotate(-38 13 12.5)"/></svg>"""
    );

    // ---- Awake ----

    /// <summary>Loop: notices you, turns to look at you with a small smile and a curious tilt, then back to the card.</summary>
    public static MiiAnimation OnlineIdleCurious()
    {
        var b = FriendsAnimations.Card("Friends online idle curious", 420);
        b.Keys(ChestBreath, (0, 1), (70, 1.006f), (140, 1), (250, 1.006f), (330, 1), (420, 1));
        // The chest follows the head a little, so the turn reads as the whole Mii and not just a neck.
        b.Keys(ChestYaw, (0, 0), (96, 0), (132, -3), (236, -3), (276, 0), (420, 0));
        b.Keys(HeadYaw, (0, 0), (90, 0), (106, -6), (124, -13), (236, -13), (262, -3), (276, 0), (420, 0));
        b.Keys(HeadRoll, (0, AwakeRoll), (120, AwakeRoll), (152, 3), (214, 3), (250, AwakeRoll), (420, AwakeRoll));
        b.Keys(HeadPitch, (0, AwakePitch), (124, AwakePitch), (148, -10), (210, -10), (256, AwakePitch), (420, AwakePitch));
        FriendsAnimations.Blink(b, 60, 6);
        b.Face(140, MiiExpression.Smile).Face(226, MiiExpression.Normal);
        FriendsAnimations.Blink(b, 186, 6);
        FriendsAnimations.Blink(b, 340, 6);
        return b.Animation;
    }

    /// <summary>One shot: turns to you, raises a hand beside the chin and waves from the elbow three times.</summary>
    public static MiiAnimation WaveHello()
    {
        var b = FriendsAnimations.Card("Friends wave hello", 156);
        TurnToViewer(b, 0, 16, 112, 140, headYaw: -13, chestYaw: -6);
        b.Keys(HeadRoll, (0, AwakeRoll), (22, AwakeRoll), (40, 5), (96, 5), (126, AwakeRoll));
        b.Keys(ChestBreath, (0, 1), (14, 1.012f), (30, 1.004f), (100, 1), (156, 1));
        Wave(b, raise: 10, up: 26, swings: [(36, 28), (47, -5), (58, 28), (69, -5), (80, 24)], lower: 108, rest: 132, size: 1f);
        b.Face(12, MiiExpression.Smile).Face(30, MiiExpression.SmileOpenMouth).Face(84, MiiExpression.Smile).Face(130, MiiExpression.Normal);
        b.Animation.AddEvent(26, "wave");
        b.Animation.AddEvent(156, "done");
        return b.Animation;
    }

    /// <summary>One shot: a bouncy, quicker wave with a wink and a few sparkles.</summary>
    public static MiiAnimation WaveExcited()
    {
        var b = FriendsAnimations.Card("Friends wave excited", 138);
        TurnToViewer(b, 0, 12, 96, 122, headYaw: -14, chestYaw: -7);
        b.Keys(ChestPitch, (0, 0), (10, 1.5f), (20, -1.5f), (38, 0), (48, -1), (58, 0), (68, -1), (78, 0), (138, 0));
        b.Keys(HeadRoll, (0, AwakeRoll), (18, AwakeRoll), (30, 6), (86, 6), (112, AwakeRoll));
        b.Keys(ChestBreath, (0, 1), (10, 0.99f), (20, 1.016f), (40, 1.004f), (90, 1), (138, 1));
        Wave(b, raise: 6, up: 20, swings: [(28, 30), (36, -4), (44, 30), (52, -4), (60, 30), (68, -4), (76, 24)], lower: 96, rest: 116, size: 1.05f);
        b.Face(8, MiiExpression.SmileOpenMouth).Face(80, MiiExpression.LikeWinkLeft).Face(104, MiiExpression.Smile).Face(124, MiiExpression.Normal);
        b.Animation.Particles.Add(new ParticleEffect
        {
            Name = "Wave sparkles", StartFrame = 24, Duration = 46, Count = 5, Seed = 4417,
            AttachBone = (byte)MiiBone.HandL, Offset = new Vector3(6, 10, 10), SpawnRadius = new Vector3(8, 8, 2),
            Direction = new Vector3(0.6f, 1, 0), Spread = 40, SpeedMin = 10, SpeedMax = 16, Drag = 1.5f,
            Shape = BuiltInShapes.Star, Orientation = ParticleOrientation.Spin, SpinMin = -120, SpinMax = 120,
            LifeMin = 0.45f, LifeMax = 0.6f, SizeStart = 9, SizeEnd = 2,
            ColorStart = new Vector4(1, 0.97f, 0.8f, 1), ColorEnd = new Vector4(1, 0.97f, 0.8f, 0), Additive = true,
        });
        b.Animation.AddEvent(20, "wave");
        b.Animation.AddEvent(138, "done");
        return b.Animation;
    }

    /// <summary>One shot: a small, slow, bashful wave with the head tilted and a shy smile.</summary>
    public static MiiAnimation WaveShy()
    {
        var b = FriendsAnimations.Card("Friends wave shy", 174);
        TurnToViewer(b, 0, 22, 128, 158, headYaw: -9, chestYaw: -4);
        b.Keys(HeadRoll, (0, AwakeRoll), (24, AwakeRoll), (46, 8), (118, 8), (148, AwakeRoll));
        b.Keys(HeadPitch, (0, AwakePitch), (28, AwakePitch), (46, -4), (118, -4), (148, AwakePitch));
        Wave(b, raise: 14, up: 34, swings: [(46, 22), (60, 2), (74, 22), (88, 2), (102, 16)], lower: 126, rest: 150, size: 0.9f);
        b.Face(18, MiiExpression.Smile).Face(108, MiiExpression.Blink).Face(114, MiiExpression.Smile).Face(150, MiiExpression.Normal);
        b.Animation.AddEvent(34, "wave");
        b.Animation.AddEvent(174, "done");
        return b.Animation;
    }

    /// <summary>Turns the head (and a little of the chest) from the card text towards the viewer and back.</summary>
    private static void TurnToViewer(AnimBuilder b, int from, int turned, int hold, int back, float headYaw, float chestYaw)
    {
        b.Keys(HeadYaw, (from, 0), (turned, headYaw), (hold, headYaw), (back, 0), (b.Animation.Length, 0));
        b.Keys(ChestYaw, (from, 0), (turned + 4, chestYaw), (hold, chestYaw), (back, 0), (b.Animation.Length, 0));
    }

    /// <summary>
    /// Raises the left arm (screen right, towards the card text) beside the chin and swings the forearm from a still
    /// elbow: each swing is (frame, degrees from upright, positive outwards). The hand ends back at rest.
    /// </summary>
    private static void Wave(AnimBuilder b, int raise, int up, (int Frame, float Degrees)[] swings, int lower, int rest, float size)
    {
        b.ArmRest(MiiLimb.ArmL, raise);
        // Elbow out to the side at shoulder height, forearm up beside the jaw and a little forward.
        Vector3 ElbowAt(int frame) => Shoulder(b, frame) + new Vector3(23f, 2.6f, 3.5f) * size;
        Vector3 Forearm(float degrees) => Vector3.Transform(new Vector3(6.5f, 21.9f, 6.5f) * size, Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, degrees * MathF.PI / 180f));

        b.ArmWorldElbow(MiiLimb.ArmL, up, ElbowAt(up) + Forearm(8), ElbowAt(up), interpolation: Interpolation.Overshoot);
        foreach (var (frame, degrees) in swings)
            b.ArmWorldElbow(MiiLimb.ArmL, frame, ElbowAt(frame) + Forearm(degrees), ElbowAt(frame));
        var last = swings[^1].Frame;
        b.ArmWorldElbow(MiiLimb.ArmL, (last + lower) / 2, ElbowAt(last) + Forearm(6), ElbowAt(last));
        // Down along the side of the body, then to rest (not straight through the chest).
        b.ArmWorld(MiiLimb.ArmL, lower, Shoulder(b, lower) + new Vector3(25f, -23f, 9f));
        b.ArmRest(MiiLimb.ArmL, rest);
    }

    private static Vector3 Shoulder(AnimBuilder b, int frame) => b.Rig.Evaluate(b.Animation, frame).WorldPosition[(int)MiiBone.ArmL1];

    // ---- Asleep ----

    /// <summary>Loop: two slow, deep breaths with a soft snore on the out-breath, Z's drifting up all along.</summary>
    public static MiiAnimation AsleepBreathing()
    {
        var b = SleepCard("Friends asleep breathing", 480);
        Breaths(b, [(0, 240), (240, 480)], depth: 1f);
        b.Face(150, MiiExpression.BlinkOpenMouth).Face(196, MiiExpression.Blink);
        b.Face(390, MiiExpression.BlinkOpenMouth).Face(436, MiiExpression.Blink);
        Zzz(b, 10, 470, 6, 2101);
        return b.Animation;
    }

    /// <summary>Loop: the head slowly sinks lower and lower, bobs back up with a little start, and dozes on.</summary>
    public static MiiAnimation AsleepNodding()
    {
        var b = SleepCard("Friends asleep nodding", 540);
        b.Keys(ChestBreath, (0, 1), (90, 1.01f), (190, 1), (380, 1), (450, 1.008f), (540, 1));
        b.Keys(ChestPitch, (0, SleepChestPitch), (230, SleepChestPitch + 2.5f), (262, SleepChestPitch + 3), (272, SleepChestPitch - 1),
            (300, SleepChestPitch), (540, SleepChestPitch));
        // Sinking is slow and heavy; the catch is quick; the settle back is slow again.
        b.Key(HeadPitch, 0, SleepHeadPitch).Key(HeadPitch, 40, SleepHeadPitch, Interpolation.EaseIn).Key(HeadPitch, 258, SleepHeadPitch + 11);
        b.Key(HeadPitch, 258, SleepHeadPitch + 11, Interpolation.Snappy).Key(HeadPitch, 268, SleepHeadPitch - 5);
        b.Keys(HeadPitch, (290, SleepHeadPitch - 3), (380, SleepHeadPitch), (540, SleepHeadPitch));
        b.Keys(HeadRoll, (0, SleepHeadRoll), (250, SleepHeadRoll + 4), (270, SleepHeadRoll - 2), (330, SleepHeadRoll), (540, SleepHeadRoll));
        b.Keys(HeadYaw, (0, 0), (260, 0), (270, -3), (330, 0), (540, 0));
        b.Face(266, MiiExpression.BlinkOpenMouth).Face(282, MiiExpression.Blink);
        Zzz(b, 10, 220, 3, 2201);
        Zzz(b, 330, 190, 3, 2202);
        b.Animation.AddEvent(266, "startle");
        return b.Animation;
    }

    /// <summary>Loop: a snore bubble swells from the nose on a long in-breath, pops with a snort, then the Z's return.</summary>
    public static MiiAnimation AsleepBubble()
    {
        var b = SleepCard("Friends asleep bubble", 600);
        // Very still while the bubble grows (it stays where it was blown), the snort jolts after the pop.
        b.Keys(ChestBreath, (0, 1), (40, 1), (200, 1.012f), (214, 1.003f), (240, 1), (400, 1.008f), (520, 1), (600, 1));
        b.Keys(ChestPitch, (0, SleepChestPitch), (40, SleepChestPitch), (200, SleepChestPitch - 0.8f), (206, SleepChestPitch + 1.6f),
            (230, SleepChestPitch), (600, SleepChestPitch));
        b.Keys(HeadPitch, (0, SleepHeadPitch), (40, SleepHeadPitch), (200, SleepHeadPitch - 0.5f), (205, SleepHeadPitch - 4),
            (224, SleepHeadPitch + 1), (260, SleepHeadPitch), (600, SleepHeadPitch));
        b.Keys(HeadRoll, (0, SleepHeadRoll), (200, SleepHeadRoll), (210, SleepHeadRoll - 3), (250, SleepHeadRoll), (600, SleepHeadRoll));
        b.Face(204, MiiExpression.BlinkOpenMouth).Face(222, MiiExpression.Blink);

        const int blow = 46, pop = 202;
        var nose = NoseOffset(b, blow);
        b.Animation.Particles.Add(new ParticleEffect
        {
            Name = "Snore bubble", StartFrame = blow, Count = 1, Seed = 2301,
            AttachBone = (byte)MiiBone.Head, Offset = nose, SpawnRadius = Vector3.Zero,
            Direction = Vector3.UnitY, Spread = 0, SpeedMin = 0, SpeedMax = 0, Drag = 0,
            Shape = SnoreBubble, Orientation = ParticleOrientation.Upright,
            LifeMin = (pop - blow) / 60f, LifeMax = (pop - blow) / 60f, SizeStart = 3, SizeEnd = 21,
            ColorStart = new Vector4(0.8f, 0.92f, 1f, 0.95f), ColorEnd = new Vector4(0.86f, 0.95f, 1f, 0.95f),
        });
        b.Animation.Particles.Add(new ParticleEffect
        {
            Name = "Bubble pop", StartFrame = pop, Count = 8, Seed = 2302,
            AttachBone = (byte)MiiBone.Head, Offset = nose, SpawnRadius = new Vector3(5, 5, 2), SpawnShell = 0.2f,
            Direction = Vector3.Zero, SpeedMin = 30, SpeedMax = 46, Drag = 4, Gravity = -40,
            Shape = BuiltInShapes.Dot, LifeMin = 0.2f, LifeMax = 0.32f, SizeStart = 3.5f, SizeEnd = 1.5f,
            ColorStart = new Vector4(0.86f, 0.95f, 1f, 0.9f), ColorEnd = new Vector4(0.86f, 0.95f, 1f, 0),
        });
        Zzz(b, 250, 330, 4, 2303);
        b.Animation.AddEvent(pop, "pop");
        return b.Animation;
    }

    /// <summary>One shot (awake → asleep): heavy blinks, a nod that catches itself, then sinks into sleep.</summary>
    public static MiiAnimation DozeOff()
    {
        var b = FriendsAnimations.Card("Friends doze off", 168);
        b.Keys(ChestBreath, (0, 1), (24, 1.01f), (60, 1), (110, 1.008f), (168, 1));
        b.Keys(HeadPitch, (0, AwakePitch), (40, AwakePitch), (62, 2), (70, -6), (92, -4), (138, SleepHeadPitch), (168, SleepHeadPitch));
        b.Keys(HeadRoll, (0, AwakeRoll), (60, AwakeRoll), (72, AwakeRoll - 1), (140, SleepHeadRoll), (168, SleepHeadRoll));
        b.Keys(ChestPitch, (0, 0), (60, 0.5f), (70, -0.5f), (140, SleepChestPitch), (168, SleepChestPitch));
        // Long, heavy blinks: the eyes stay closed a little longer each time.
        b.Face(18, MiiExpression.Blink).Face(28, MiiExpression.Normal);
        b.Face(46, MiiExpression.Blink).Face(66, MiiExpression.Normal);
        b.Face(84, MiiExpression.Blink);
        b.Face(168, MiiExpression.Blink);
        Zzz(b, 120, 30, 1, 2401);
        b.Animation.AddEvent(84, "asleep");
        b.Animation.AddEvent(168, "done");
        return b.Animation;
    }

    /// <summary>One shot (asleep → awake): stirs, startles awake, blinks the sleep away and brightens.</summary>
    public static MiiAnimation WakeUp()
    {
        var b = SleepCard("Friends wake up", 120);
        foreach (var frame in new[] { 0, 120 })
        {
            b.Key(HeadPitch, frame, frame == 0 ? SleepHeadPitch : AwakePitch).Key(HeadRoll, frame, frame == 0 ? SleepHeadRoll : AwakeRoll);
            b.Key(ChestPitch, frame, frame == 0 ? SleepChestPitch : 0);
        }
        b.Keys(HeadPitch, (14, SleepHeadPitch + 1.5f), (30, SleepHeadPitch - 1));
        b.Key(HeadPitch, 34, SleepHeadPitch - 1, Interpolation.Snappy).Key(HeadPitch, 42, -13);
        b.Keys(HeadPitch, (58, -10), (80, AwakePitch));
        b.Keys(HeadRoll, (30, SleepHeadRoll), (44, -1), (62, -6), (84, AwakeRoll));
        b.Keys(HeadYaw, (0, 0), (34, 0), (44, 4), (56, -4), (70, 0), (120, 0));
        b.Keys(ChestPitch, (30, SleepChestPitch), (42, -2.5f), (64, 0));
        b.Keys(ChestBreath, (0, 1), (34, 1), (42, 1.018f), (62, 1.004f), (84, 1), (120, 1));
        b.Face(0, MiiExpression.Blink).Face(38, MiiExpression.Surprise).Face(54, MiiExpression.Blink).Face(59, MiiExpression.Normal);
        b.Face(66, MiiExpression.Blink).Face(70, MiiExpression.Smile).Face(104, MiiExpression.Normal).Face(120, MiiExpression.Normal);
        b.Animation.AddEvent(38, "awake");
        b.Animation.AddEvent(120, "done");
        return b.Animation;
    }

    /// <summary>The card baseline, slumped with the head dropped and tilted and the eyes closed.</summary>
    private static AnimBuilder SleepCard(string name, int length)
    {
        var b = FriendsAnimations.Card(name, length);
        foreach (var frame in new[] { 0, length })
        {
            b.Rot(MiiBone.Head, frame, SleepHeadPitch, 0, SleepHeadRoll);
            b.Rot(MiiBone.Chest, frame, SleepChestPitch, 0, 0);
            b.Face(frame, MiiExpression.Blink);
        }
        return b;
    }

    /// <summary>Slow sleeping breaths: the chest swells and lifts the head a touch, then sinks back.</summary>
    private static void Breaths(AnimBuilder b, (int From, int To)[] breaths, float depth)
    {
        foreach (var (from, to) in breaths)
        {
            var peak = from + (to - from) * 2 / 5;
            b.Keys(ChestBreath, (from, 1), (peak, 1 + 0.013f * depth), (to, 1));
            b.Keys(ChestPitch, (from, SleepChestPitch), (peak, SleepChestPitch - 1.4f * depth), (to, SleepChestPitch));
            b.Keys(HeadPitch, (from, SleepHeadPitch), (peak + 12, SleepHeadPitch - 2f * depth), (to, SleepHeadPitch));
            b.Keys(HeadRoll, (from, SleepHeadRoll), (peak + 20, SleepHeadRoll - 1.2f * depth), (to, SleepHeadRoll));
        }
    }

    /// <summary>
    /// A stream of Z's growing as they drift up and to the right, in the gap between the top of the head and the
    /// card text (the card has no room above the head).
    /// </summary>
    private static void Zzz(AnimBuilder b, int start, int duration, int count, uint seed) =>
        b.Animation.Particles.Add(new ParticleEffect
        {
            Name = "Zzz", StartFrame = start, Duration = duration, Count = count, Seed = seed,
            AttachBone = (byte)MiiBone.Head, Offset = ZzzOffset, SpawnRadius = new Vector3(2.5f, 2.5f, 0),
            Direction = new Vector3(0.6f, 1, 0), Spread = 9, SpeedMin = 12, SpeedMax = 15, Gravity = 2, Drag = 0.25f,
            Shape = SleepZ, Orientation = ParticleOrientation.Upright,
            LifeMin = 1.9f, LifeMax = 2.2f, SizeStart = 8, SizeEnd = 17,
            ColorStart = new Vector4(0.86f, 0.93f, 1f, 1f), ColorEnd = new Vector4(0.86f, 0.93f, 1f, 0f),
        });

    /// <summary>Where the Z's start, from the neck (canonical): beside the temple on the face's side, a little in front.</summary>
    public static readonly Vector3 ZzzOffset = new(39, 90, 22);

    /// <summary>Just in front of the nose on <paramref name="frame"/>, as a particle offset from the neck.</summary>
    private static Vector3 NoseOffset(AnimBuilder b, int frame)
    {
        var scale = MiiStage.DefaultBodyScale;
        var pose = b.Rig.Evaluate(b.Animation, frame);
        var nose = Vector3.Transform(new Vector3(1.5f, 35f, 32f), MiiStage.HeadToStage(pose, scale));
        return (nose - MiiStage.PointOf(pose, scale, MiiBone.Head)) / MiiStage.ParticleScale;
    }
}
