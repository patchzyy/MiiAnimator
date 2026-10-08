using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Reusable full-body reactions for any WheelWizard screen. Standing reactions keep their feet in IK;
/// the balloon flights and happy hop deliberately use FK in the air. No caller-specific Mii or actors.
/// </summary>
public static class SharedAnimations
{
    // Only additions belong here: the original Appear and the two original idles remain untouched.
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("shared/appear/Appear_balloon.miianim", AppearBalloon),
        ("shared/disappear/Disappear_goodbye_poof.miianim", DisappearGoodbyePoof),
        ("shared/disappear/Disappear_balloon_away.miianim", DisappearBalloonAway),
        ("shared/error/Error_puzzled_shrug.miianim", ErrorPuzzledShrug),
        ("shared/error/Error_startled_recovery.miianim", ErrorStartledRecovery),
        ("shared/hover/Hover_perk_up.miianim", HoverPerkUp),
        ("shared/hover/Hover_little_wave.miianim", HoverLittleWave),
        ("shared/idle/Idle_curious_look.miianim", IdleCuriousLook),
        ("shared/idle/Idle_tiny_groove.miianim", IdleTinyGroove),
        ("shared/loading/Loading_foot_tap.miianim", LoadingFootTap),
        ("shared/loading/Loading_ponder.miianim", LoadingPonder),
        ("shared/selected/Selected_ta_da.miianim", SelectedTaDa),
        ("shared/selected/Selected_yes_nod.miianim", SelectedYesNod),
        ("shared/success/Success_fist_pump.miianim", SuccessFistPump),
        ("shared/success/Success_happy_hop.miianim", SuccessHappyHop),
    ];

    public static bool IsLoop(string path) => path.StartsWith("shared/idle/", StringComparison.Ordinal)
        || path.StartsWith("shared/loading/", StringComparison.Ordinal);

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootRoll = Bone(MiiBone.Root, Channel.RotZ);
    private static readonly TrackId RootStretch = Bone(MiiBone.Root, Channel.Stretch);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestTurn = Bone(MiiBone.Chest, Channel.RotY);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId HipRoll = Bone(MiiBone.Hip, Channel.RotZ);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadTurn = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    private static AnimBuilder Begin(string name, int length, bool loop = false)
    {
        var b = new AnimBuilder(name, length, female: false);
        foreach (var bone in new[] { MiiBone.Root, MiiBone.Hip, MiiBone.Chest, MiiBone.Head })
            b.Rot(bone, 0, 0, 0, 0).Rot(bone, length, 0, 0, 0);
        foreach (var id in new[] { RootX, RootY, Bone(MiiBone.Root, Channel.PosZ) })
            b.Keys(id, (0, 0), (length, 0));
        b.Keys(RootStretch, (0, 1), (length, 1));
        foreach (var channel in new[] { Channel.ScaleX, Channel.ScaleY, Channel.ScaleZ })
            b.Keys(Bone(MiiBone.Root, channel), (0, 1), (length, 1));
        foreach (var hand in new[] { MiiBone.HandL, MiiBone.HandR })
            b.Keys(Bone(hand, Channel.Stretch), (0, 1), (length, 1));
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
        {
            b.Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 1), (length, 1));
            b.Keys(Limb(leg, Channel.TargetRotX), (0, 0), (length, 0));
            b.Keys(Limb(leg, Channel.Swivel), (0, 0), (length, 0));
            var (upper, lower, _) = MiiSkeletonInfo.Chain(leg);
            b.Rot(upper, 0, 0, 0, 0).Rot(upper, length, 0, 0, 0);
            b.Rot(lower, 0, 0, 0, 0).Rot(lower, length, 0, 0, 0);
        }
        b.FeetAt(0, 0, 0).FeetAt(length, 0, 0);
        b.ArmsRest(0).ArmsRest(length);
        b.Face(0, MiiExpression.Normal).Face(length, MiiExpression.Normal);
        b.Key(TrackId.Visible, 0, 1).Key(TrackId.Visible, length, 1);
        if (!loop)
            b.Animation.AddEvent(length, "done");
        return b;
    }

    /// <summary>A buoyant magical arrival: float down, flutter for balance, settle, and wave hello.</summary>
    public static MiiAnimation AppearBalloon()
    {
        var b = Begin("Appear balloon", 156);
        b.Key(TrackId.Visible, 0, 0).Key(TrackId.Visible, 8, 1);
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            b.Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 0), (84, 1));
        b.Keys(RootY, (0, 115), (8, 115), (24, 87), (38, 94), (58, 38));
        b.Arc(RootY, 58, 38, 84, 0, rising: false);
        b.Keys(RootY, (89, -5), (98, -0.5f), (108, 0), (140, 0));
        b.Keys(RootRoll, (0, -3), (24, 4), (42, -4), (62, 3), (80, 0), (140, 0));
        b.Keys(RootStretch, (0, 0.92f), (24, 1.025f), (44, 0.99f), (70, 1), (84, 1), (89, 0.95f), (100, 1), (140, 1));
        b.Keys(ChestPitch, (0, -5), (40, -3), (72, 4), (84, 0), (90, 5), (102, -3), (130, 0), (140, 0));
        b.Keys(HeadPitch, (0, 8), (24, 4), (44, -5), (72, 4), (84, 0), (94, -4), (130, 0), (140, 0));
        b.Keys(HeadRoll, (0, 0), (28, -5), (48, 6), (76, 0), (108, -7), (124, -5), (140, 0));
        foreach (var leg in new[] { MiiBone.LegL2, MiiBone.LegR2 })
            b.Keys(Bone(leg, Channel.RotX), (0, 12), (48, 12), (70, 4), (80, 0), (140, 0));
        b.Arms(0, new Vector3(52, 101, 12));
        b.Arms(24, new Vector3(53, 112, 12), hand: new Vector3(0, 0, 16));
        b.Arms(42, new Vector3(55, 100, 14), hand: new Vector3(0, 0, -8));
        b.Arms(62, new Vector3(52, 109, 12), hand: new Vector3(0, 0, 12));
        b.Arms(84, new Vector3(45, 90, 12));
        b.Arms(92, new Vector3(38, 73, 8));
        b.ArmRest(MiiLimb.ArmL, 106);
        b.Arm(MiiLimb.ArmR, 106, new Vector3(-49, 117, 14), hand: new Vector3(0, 0, -15));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (106, 0), (112, -18), (118, 18), (124, -12), (130, 0));
        b.ArmsRest(140);
        b.Face(8, MiiExpression.SurpriseOpenMouth).Face(42, MiiExpression.Normal);
        b.Face(85, MiiExpression.Blink).Face(91, MiiExpression.OpenMouth);
        b.Face(108, MiiExpression.LikeWinkRight).Face(122, MiiExpression.Normal);
        Bubbles(b, "Arrival bubbles", 10, 28, 18, 1101);
        LandingDust(b, 84, 1102);
        b.Animation.AddEvent(8, "appeared");
        b.Animation.AddEvent(84, "landed");
        b.Animation.AddEvent(108, "hello");
        return b.Animation;
    }

    /// <summary>A friendly goodbye wave, a small squash, and a shirt-coloured disappearing cloud.</summary>
    public static MiiAnimation DisappearGoodbyePoof()
    {
        var b = Begin("Disappear goodbye poof", 132);
        b.Keys(RootX, (12, 0), (26, 1.5f), (62, 1.5f), (80, 0), (120, 0));
        b.Keys(RootY, (12, 0), (26, -2), (62, -2), (82, -4), (96, 0), (120, 0));
        b.Keys(ChestPitch, (12, 0), (26, -3), (64, -3), (82, 6), (96, 0), (120, 0));
        b.Keys(HeadRoll, (12, 0), (26, -8), (62, -8), (80, 0), (120, 0));
        b.Arm(MiiLimb.ArmR, 24, new Vector3(-49, 117, 14), hand: new Vector3(0, 0, -16));
        b.Arm(MiiLimb.ArmR, 62, new Vector3(-49, 117, 14), hand: new Vector3(0, 0, -16));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (24, 0), (32, -24), (40, 24), (48, -24), (56, 24), (62, 0));
        b.Arms(80, new Vector3(38, 73, 10));
        b.ArmsRest(96).ArmsRest(120);
        b.Keys(RootStretch, (62, 1), (80, 0.94f), (96, 1), (120, 1));
        foreach (var channel in new[] { Channel.ScaleX, Channel.ScaleY, Channel.ScaleZ })
            b.Keys(Bone(MiiBone.Root, channel), (82, 1), (94, 0.6f), (100, 0.48f), (112, 1), (120, 1));
        b.Key(TrackId.Visible, 96, 0).Key(TrackId.Visible, 132, 0);
        b.Face(26, MiiExpression.OpenMouth).Face(46, MiiExpression.LikeWinkRight);
        b.Face(62, MiiExpression.Normal).Face(80, MiiExpression.Blink).Face(96, MiiExpression.Normal);
        var poof = ParticlePresets.Poof();
        poof.Name = "Goodbye cloud";
        poof.StartFrame = 88;
        poof.Count = 22;
        poof.Seed = 1201;
        poof.LifeMin = 0.5f;
        poof.LifeMax = 0.65f;
        poof.SpawnRadius = new Vector3(21);
        poof.SizeStart = 30;
        poof.SizeEnd = 42;
        b.Animation.Particles.Add(poof);
        b.Animation.AddEvent(24, "goodbye");
        b.Animation.AddEvent(96, "vanished");
        return b.Animation;
    }

    /// <summary>Wave goodbye, then drift up as if a balloon has gently carried the Mii away.</summary>
    public static MiiAnimation DisappearBalloonAway()
    {
        var b = Begin("Disappear balloon away", 156);
        b.Keys(RootY, (10, 0), (22, -2), (50, -2), (64, 0));
        b.Key(RootY, 64, 0, Interpolation.EaseIn).Key(RootY, 108, 120, Interpolation.EaseIn);
        b.Keys(RootY, (132, 260), (144, 0));
        b.Keys(RootRoll, (72, 0), (90, 3), (112, -3), (132, 0), (144, 0));
        b.Keys(HeadRoll, (10, 0), (24, -8), (48, -8), (66, 0), (92, 5), (116, -4), (132, 0), (144, 0));
        b.Keys(HeadPitch, (58, 0), (84, 7), (114, 9), (132, 0), (144, 0));
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            b.Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 1), (64, 0), (132, 1));
        foreach (var (upper, lower) in new[] { (MiiBone.LegL1, MiiBone.LegL2), (MiiBone.LegR1, MiiBone.LegR2) })
        {
            b.Keys(Bone(upper, Channel.RotX), (72, 0), (90, -6), (118, -6), (132, 0), (144, 0));
            b.Keys(Bone(lower, Channel.RotX), (72, 0), (90, 24), (118, 24), (132, 0), (144, 0));
        }
        b.Arm(MiiLimb.ArmR, 24, new Vector3(-49, 117, 14), hand: new Vector3(0, 0, -16));
        b.Arm(MiiLimb.ArmR, 48, new Vector3(-49, 117, 14), hand: new Vector3(0, 0, -16));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (24, 0), (32, -20), (40, 20), (48, 0));
        b.Arms(64, new Vector3(52, 103, 14));
        b.Arms(90, new Vector3(54, 114, 12), hand: new Vector3(0, 0, 16));
        b.Arms(116, new Vector3(52, 105, 14));
        b.ArmsRest(144);
        b.Face(24, MiiExpression.OpenMouth).Face(50, MiiExpression.Normal);
        b.Face(76, MiiExpression.SurpriseOpenMouth).Face(108, MiiExpression.SmileOpenMouth).Face(132, MiiExpression.Normal);
        b.Key(TrackId.Visible, 132, 0).Key(TrackId.Visible, 156, 0);
        Bubbles(b, "Departure bubbles", 70, 36, 20, 1202);
        b.Animation.AddEvent(24, "goodbye");
        b.Animation.AddEvent(64, "lift_off");
        b.Animation.AddEvent(132, "vanished");
        return b.Animation;
    }

    /// <summary>A readable, sympathetic "huh?": puzzled look, palms up, and a relaxed recovery.</summary>
    public static MiiAnimation ErrorPuzzledShrug()
    {
        var b = Begin("Error puzzled shrug", 150);
        b.Keys(RootY, (10, 0), (24, -2.5f), (42, -1.5f), (88, -1.5f), (124, 0), (140, 0));
        b.Keys(ChestPitch, (10, 0), (24, 4), (42, 2), (88, 2), (124, 0), (140, 0));
        b.Keys(HeadRoll, (10, 0), (26, 12), (42, 10), (78, 10), (94, -5), (124, 0), (140, 0));
        b.Keys(HeadTurn, (10, 0), (26, -10), (42, 0), (78, 0), (94, 7), (124, 0), (140, 0));
        b.Keys(HeadPitch, (10, 0), (26, 4), (42, -3), (78, -3), (100, 3), (124, 0), (140, 0));
        b.Arms(24, new Vector3(39, 79, 12));
        b.Arms(42, new Vector3(55, 106, 14), hand: new Vector3(0, 0, 24));
        b.Arms(78, new Vector3(55, 106, 14), hand: new Vector3(0, 0, 24));
        b.Arms(102, new Vector3(39, 76, 10));
        b.ArmsRest(124).ArmsRest(140);
        b.Face(12, MiiExpression.Surprise).Face(32, MiiExpression.OpenMouth);
        b.Face(86, MiiExpression.Blink).Face(92, MiiExpression.Normal);
        b.Animation.AddEvent(42, "shrug");
        return b.Animation;
    }

    /// <summary>A small startled squash, two disbelieving head shakes, then composure.</summary>
    public static MiiAnimation ErrorStartledRecovery()
    {
        var b = Begin("Error startled recovery", 138);
        b.Key(RootY, 0, 0, Interpolation.EaseOut).Keys(RootY, (8, -5), (18, -2), (74, -2), (116, 0), (128, 0));
        b.Keys(RootStretch, (0, 1), (8, 0.95f), (18, 1), (116, 1), (128, 1));
        b.Keys(ChestPitch, (0, 0), (8, -7), (20, 0), (76, 5), (96, 4), (116, 0), (128, 0));
        b.Keys(HeadPitch, (0, 0), (8, -9), (20, 2), (74, 6), (96, 3), (116, 0), (128, 0));
        b.Keys(HeadTurn, (20, 0), (30, -16), (42, 16), (54, -12), (66, 10), (78, 0), (116, 0), (128, 0));
        b.Keys(HeadRoll, (20, 0), (32, 4), (44, -4), (56, 3), (76, 0), (116, 0), (128, 0));
        b.Arms(8, new Vector3(52, 101, 14));
        b.Arms(22, new Vector3(48, 93, 16));
        b.Arms(66, new Vector3(48, 93, 16));
        b.Arms(84, new Vector3(37, 72, 9));
        b.ArmsRest(116).ArmsRest(128);
        b.Face(4, MiiExpression.SurpriseOpenMouth).Face(20, MiiExpression.Frustrated);
        b.Face(76, MiiExpression.Sorrow).Face(98, MiiExpression.Blink).Face(104, MiiExpression.Normal);
        b.Animation.AddEvent(8, "startled");
        b.Animation.AddEvent(104, "recovered");
        return b.Animation;
    }

    /// <summary>A quick acknowledgement on pointer entry: perk up, open one hand, wink.</summary>
    public static MiiAnimation HoverPerkUp()
    {
        var b = Begin("Hover perk up", 90);
        b.Keys(RootY, (6, 0), (18, -1.5f), (52, -1.5f), (76, 0), (84, 0));
        b.Keys(ChestPitch, (6, 0), (18, -4), (52, -4), (76, 0), (84, 0));
        b.Keys(HeadPitch, (6, 0), (18, -4), (52, -4), (76, 0), (84, 0));
        b.Keys(HeadRoll, (6, 0), (18, 7), (52, 7), (76, 0), (84, 0));
        b.Arm(MiiLimb.ArmL, 20, new Vector3(48, 96, 14), hand: new Vector3(0, 0, 18));
        b.Arm(MiiLimb.ArmL, 52, new Vector3(48, 96, 14), hand: new Vector3(0, 0, 18));
        b.ArmsRest(76).ArmsRest(84);
        b.Face(20, MiiExpression.OpenMouth).Face(38, MiiExpression.LikeWinkLeft).Face(48, MiiExpression.Normal);
        b.Animation.AddEvent(20, "acknowledged");
        return b.Animation;
    }

    /// <summary>A small side wave with a body lean and a warm glance at the viewer.</summary>
    public static MiiAnimation HoverLittleWave()
    {
        var b = Begin("Hover little wave", 120);
        b.Keys(RootX, (8, 0), (24, 1.5f), (74, 1.5f), (106, 0), (114, 0));
        b.Keys(RootY, (8, 0), (24, -2), (74, -2), (106, 0), (114, 0));
        b.Keys(ChestRoll, (8, 0), (24, -2), (74, -2), (106, 0), (114, 0));
        b.Keys(HeadRoll, (8, 0), (24, -7), (74, -7), (106, 0), (114, 0));
        b.Arm(MiiLimb.ArmR, 24, new Vector3(-48, 114, 14), hand: new Vector3(0, 0, -14));
        b.Arm(MiiLimb.ArmR, 74, new Vector3(-48, 114, 14), hand: new Vector3(0, 0, -14));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (24, 0), (34, -20), (44, 20), (54, -20), (64, 20), (74, 0));
        b.ArmsRest(106).ArmsRest(114);
        b.Face(24, MiiExpression.OpenMouth).Face(58, MiiExpression.Blink).Face(64, MiiExpression.Normal);
        b.Animation.AddEvent(24, "hello");
        return b.Animation;
    }

    /// <summary>A calm six-second loop with natural pauses, glances, breaths, and two quick blinks.</summary>
    public static MiiAnimation IdleCuriousLook()
    {
        var b = Begin("Idle curious look", 360, loop: true);
        b.Keys(RootX, (20, 0), (84, -1.2f), (132, -1.2f), (188, 1.2f), (246, 1.2f), (332, 0), (348, 0));
        b.Keys(RootY, (20, 0), (84, -2), (132, -2), (188, -2), (246, -2), (332, 0), (348, 0));
        b.Keys(ChestTurn, (20, 0), (84, -3), (132, -3), (188, 3), (246, 3), (332, 0), (348, 0));
        b.Keys(HeadTurn, (28, 0), (78, -18), (126, -18), (184, 18), (242, 18), (292, 0), (332, 0), (348, 0));
        b.Keys(HeadRoll, (28, 0), (78, 5), (126, 5), (184, -4), (242, -4), (292, 0), (332, 0), (348, 0));
        b.Keys(HeadPitch, (28, 0), (78, 3), (126, 3), (184, 0), (242, 0), (282, -3), (312, 1), (332, 0), (348, 0));
        b.Keys(Bone(MiiBone.Chest, Channel.Stretch), (0, 1), (60, 1.012f), (126, 1), (190, 1.012f), (250, 1), (312, 1.008f), (332, 1), (360, 1));
        b.Arms(84, new Vector3(32, 62, -2));
        b.Arms(246, new Vector3(32, 62, -2));
        b.ArmsRest(332).ArmsRest(348);
        b.Face(98, MiiExpression.Blink).Face(104, MiiExpression.Normal);
        b.Face(270, MiiExpression.Blink).Face(276, MiiExpression.Normal);
        return CloseLoop(b);
    }

    /// <summary>A tiny in-place groove, with a soft knee bounce and alternating shoulder/hand accents.</summary>
    public static MiiAnimation IdleTinyGroove()
    {
        var b = Begin("Idle tiny groove", 240, loop: true);
        b.Keys(RootX, (12, 0), (40, -2), (76, 2), (112, -2), (148, 2), (184, -1.5f), (222, 0), (232, 0));
        b.Keys(RootY, (12, 0), (28, -3), (40, -2), (64, -3), (76, -2), (100, -3), (112, -2), (136, -3), (148, -2), (172, -3), (184, -2), (222, 0), (232, 0));
        b.Keys(ChestRoll, (12, 0), (40, 4), (76, -4), (112, 4), (148, -4), (184, 3), (222, 0), (232, 0));
        b.Keys(HipRoll, (12, 0), (40, -2), (76, 2), (112, -2), (148, 2), (184, -1.5f), (222, 0), (232, 0));
        b.Keys(HeadRoll, (12, 0), (40, -5), (76, 5), (112, -5), (148, 5), (184, -4), (222, 0), (232, 0));
        b.Keys(HeadPitch, (12, 0), (28, 3), (40, 0), (64, 3), (76, 0), (100, 3), (112, 0), (136, 3), (148, 0), (184, 2), (222, 0), (232, 0));
        b.Arms(24, new Vector3(37, 72, 8));
        foreach (var (frame, leftUp) in new[] { (40, true), (76, false), (112, true), (148, false), (184, true) })
        {
            b.Arm(MiiLimb.ArmL, frame, leftUp ? new Vector3(43, 87, 12) : new Vector3(36, 72, 8));
            b.Arm(MiiLimb.ArmR, frame, leftUp ? new Vector3(-36, 72, 8) : new Vector3(-43, 87, 12));
        }
        b.ArmsRest(222).ArmsRest(232);
        b.Face(80, MiiExpression.Blink).Face(86, MiiExpression.Normal);
        b.Face(154, MiiExpression.OpenMouth).Face(190, MiiExpression.Normal);
        return CloseLoop(b);
    }

    /// <summary>A patient waiting loop. The right shoe really lifts for each tap; the left foot stays planted.</summary>
    public static MiiAnimation LoadingFootTap()
    {
        var b = Begin("Loading foot tap", 180, loop: true);
        b.Keys(RootX, (12, 0), (30, 1.2f), (142, 1.2f), (168, 0), (176, 0));
        b.Keys(RootY, (12, 0), (30, -2.5f), (142, -2.5f), (168, 0), (176, 0));
        b.Keys(HeadPitch, (12, 0), (34, 8), (104, 8), (128, 0), (168, 0), (176, 0));
        b.Keys(HeadTurn, (12, 0), (34, -6), (104, -6), (128, 0), (168, 0), (176, 0));
        b.Keys(HeadRoll, (12, 0), (34, 4), (104, 4), (128, 0), (168, 0), (176, 0));
        b.Keys(ChestPitch, (12, 0), (30, 2), (142, 2), (168, 0), (176, 0));
        b.Arms(30, new Vector3(37, 72, 8));
        b.Arms(142, new Vector3(37, 72, 8));
        b.ArmsRest(168).ArmsRest(176);
        var foot = MiiLimb.LegR;
        foreach (var (start, peak, end) in new[] { (44, 52, 60), (72, 80, 88), (100, 108, 116), (128, 136, 144) })
        {
            b.Keys(Limb(foot, Channel.TargetY), (start, 0), (peak, 3.5f), (end, 0));
            b.Keys(Limb(foot, Channel.TargetZ), (start, 0), (peak, 1.5f), (end, 0));
            b.Keys(Limb(foot, Channel.TargetRotX), (start, 0), (peak, 10), (end, 0));
            b.Animation.AddEvent(end, "tap");
        }
        b.Face(64, MiiExpression.Blink).Face(70, MiiExpression.Normal);
        b.Face(150, MiiExpression.Blink).Face(156, MiiExpression.Normal);
        return CloseLoop(b);
    }

    /// <summary>Patiently weigh two possibilities in the hands, glance between them, and repeat.</summary>
    public static MiiAnimation LoadingPonder()
    {
        var b = Begin("Loading ponder", 240, loop: true);
        b.Keys(RootX, (16, 0), (56, -1.2f), (96, -1.2f), (144, 1.2f), (184, 1.2f), (224, 0), (232, 0));
        b.Keys(RootY, (16, 0), (48, -2.5f), (184, -2.5f), (224, 0), (232, 0));
        b.Keys(ChestTurn, (16, 0), (56, 3), (96, 3), (144, -3), (184, -3), (224, 0), (232, 0));
        b.Keys(HeadTurn, (16, 0), (56, 16), (96, 16), (144, -16), (184, -16), (224, 0), (232, 0));
        b.Keys(HeadRoll, (16, 0), (56, -7), (96, -7), (144, 7), (184, 7), (224, 0), (232, 0));
        b.Keys(HeadPitch, (16, 0), (56, 5), (96, 5), (144, 5), (184, 5), (224, 0), (232, 0));
        b.Arms(36, new Vector3(39, 79, 14));
        foreach (var frame in new[] { 56, 96 })
        {
            b.Arm(MiiLimb.ArmL, frame, new Vector3(50, 102, 18), hand: new Vector3(0, 0, 24));
            b.Arm(MiiLimb.ArmR, frame, new Vector3(-38, 76, 10));
        }
        b.Arms(120, new Vector3(42, 86, 16));
        foreach (var frame in new[] { 144, 184 })
        {
            b.Arm(MiiLimb.ArmL, frame, new Vector3(38, 76, 10));
            b.Arm(MiiLimb.ArmR, frame, new Vector3(-50, 102, 18), hand: new Vector3(0, 0, -24));
        }
        b.ArmsRest(224).ArmsRest(232);
        b.Face(104, MiiExpression.Blink).Face(110, MiiExpression.Normal);
        b.Face(196, MiiExpression.Blink).Face(202, MiiExpression.Normal);
        return CloseLoop(b);
    }

    /// <summary>A proud, grounded presentation pose with a small anticipation and a cheeky wink.</summary>
    public static MiiAnimation SelectedTaDa()
    {
        var b = Begin("Selected ta da", 150);
        b.Keys(RootY, (8, 0), (20, -4), (36, -1), (92, -1), (130, 0), (142, 0));
        b.Keys(RootStretch, (8, 1), (20, 0.97f), (36, 1.025f), (92, 1.025f), (130, 1), (142, 1));
        b.Keys(ChestPitch, (8, 0), (20, 5), (36, -5), (92, -5), (130, 0), (142, 0));
        b.Keys(HeadPitch, (8, 0), (20, 3), (36, -5), (92, -5), (130, 0), (142, 0));
        b.Keys(HeadRoll, (20, 0), (42, 7), (92, 7), (130, 0), (142, 0));
        b.Arms(20, new Vector3(37, 72, 10));
        b.Arms(36, new Vector3(53, 115, 14), hand: new Vector3(0, 0, 18));
        b.Arms(92, new Vector3(53, 115, 14), hand: new Vector3(0, 0, 18));
        b.Arms(112, new Vector3(42, 84, 12));
        b.ArmsRest(130).ArmsRest(142);
        b.Face(20, MiiExpression.Blink).Face(30, MiiExpression.OpenMouth);
        b.Face(70, MiiExpression.LikeWinkLeft).Face(84, MiiExpression.Normal);
        Twinkles(b, "Presentation stars", 36, 12, 14, 1701);
        b.Animation.AddEvent(30, "selected");
        b.Animation.AddEvent(36, "present");
        return b.Animation;
    }

    /// <summary>A compact confirmation: two affirmative nods and a friendly open hand.</summary>
    public static MiiAnimation SelectedYesNod()
    {
        var b = Begin("Selected yes nod", 102);
        b.Keys(RootY, (6, 0), (20, -2), (62, -2), (88, 0), (96, 0));
        b.Keys(ChestPitch, (6, 0), (20, -2), (62, -2), (88, 0), (96, 0));
        b.Keys(HeadPitch, (6, 0), (20, 11), (30, -4), (42, 9), (54, -3), (66, 0), (88, 0), (96, 0));
        b.Keys(HeadRoll, (6, 0), (20, -4), (62, -4), (88, 0), (96, 0));
        b.Arm(MiiLimb.ArmR, 22, new Vector3(-50, 98, 14), hand: new Vector3(0, 0, -20));
        b.Arm(MiiLimb.ArmR, 62, new Vector3(-50, 98, 14), hand: new Vector3(0, 0, -20));
        b.ArmsRest(88).ArmsRest(96);
        b.Face(22, MiiExpression.OpenMouth).Face(60, MiiExpression.Normal);
        Twinkles(b, "Confirmation stars", 28, 0, 7, 1702);
        b.Animation.AddEvent(22, "selected");
        return b.Animation;
    }

    /// <summary>A little "yes!": two fist pumps, a proud chest, and a relaxed smile.</summary>
    public static MiiAnimation SuccessFistPump()
    {
        var b = Begin("Success fist pump", 132);
        b.Keys(RootX, (8, 0), (26, -1.5f), (76, -1.5f), (116, 0), (124, 0));
        b.Keys(RootY, (8, 0), (20, -4), (34, -2), (44, -3.5f), (56, -2), (76, -2), (116, 0), (124, 0));
        b.Keys(ChestPitch, (8, 0), (20, 3), (34, -6), (76, -6), (116, 0), (124, 0));
        b.Keys(ChestTurn, (8, 0), (34, -5), (76, -5), (116, 0), (124, 0));
        b.Keys(HeadPitch, (8, 0), (20, 3), (34, -6), (76, -6), (116, 0), (124, 0));
        b.Keys(HeadRoll, (8, 0), (34, -5), (76, -5), (116, 0), (124, 0));
        b.Arm(MiiLimb.ArmL, 20, new Vector3(37, 72, 9));
        b.Arm(MiiLimb.ArmL, 76, new Vector3(37, 72, 9));
        b.Arm(MiiLimb.ArmR, 20, new Vector3(-38, 79, 14));
        b.Arm(MiiLimb.ArmR, 34, new Vector3(-48, 124, 12), hand: new Vector3(0, 0, -20));
        b.Arm(MiiLimb.ArmR, 44, new Vector3(-49, 110, 14), hand: new Vector3(0, 0, -18));
        b.Arm(MiiLimb.ArmR, 56, new Vector3(-48, 124, 12), hand: new Vector3(0, 0, -20));
        b.Arm(MiiLimb.ArmR, 76, new Vector3(-48, 122, 12), hand: new Vector3(0, 0, -20));
        b.Arms(96, new Vector3(38, 73, 10));
        b.ArmsRest(116).ArmsRest(124);
        b.Keys(Bone(MiiBone.HandR, Channel.Stretch), (8, 1), (20, 0.82f), (90, 0.82f), (108, 1), (124, 1));
        b.Face(22, MiiExpression.OpenMouth).Face(34, MiiExpression.SmileOpenMouth);
        b.Face(78, MiiExpression.LikeWinkRight).Face(94, MiiExpression.Normal);
        Twinkles(b, "Victory stars", 34, 16, 12, 1801);
        b.Animation.AddEvent(34, "celebrate");
        return b.Animation;
    }

    /// <summary>An exuberant but compact happy hop, with a real airborne leg tuck and clean landing.</summary>
    public static MiiAnimation SuccessHappyHop()
    {
        var b = Begin("Success happy hop", 162);
        b.Keys(RootY, (6, 0), (14, -7));
        b.Key(RootY, 14, -7, Interpolation.EaseIn).Key(RootY, 22, 0);
        b.Arc(RootY, 22, 0, 38, 20, rising: true);
        b.Arc(RootY, 38, 20, 54, 0, rising: false);
        b.Key(RootY, 54, 0, Interpolation.EaseOut).Keys(RootY, (59, -5), (70, -0.5f), (82, 0), (144, 0));
        b.Keys(RootStretch, (6, 1), (14, 0.94f), (22, 1), (34, 1.04f), (54, 1), (59, 0.94f), (74, 1), (144, 1));
        b.Keys(RootRoll, (22, 0), (36, -3), (46, 2), (54, 0), (144, 0));
        b.Keys(ChestPitch, (6, 0), (14, 9), (22, 0), (38, -5), (54, 0), (60, 8), (78, -4), (104, -4), (144, 0));
        b.Keys(HeadPitch, (6, 0), (14, 5), (22, -5), (38, -8), (54, 0), (60, 5), (78, -5), (104, -5), (144, 0));
        b.Keys(HeadRoll, (54, 0), (80, 8), (110, 8), (144, 0));
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            b.Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 1), (22, 0), (54, 1));
        foreach (var (upper, lower) in new[] { (MiiBone.LegL1, MiiBone.LegL2), (MiiBone.LegR1, MiiBone.LegR2) })
        {
            b.Keys(Bone(upper, Channel.RotX), (22, 0), (32, -12), (44, -10), (52, 0), (54, 0), (144, 0));
            b.Keys(Bone(lower, Channel.RotX), (22, 0), (32, 48), (44, 40), (52, 0), (54, 0), (144, 0));
        }
        b.Arms(14, new Vector3(37, 72, 10));
        b.Arms(26, new Vector3(55, 106, 12));
        b.Arms(38, new Vector3(53, 118, 14), hand: new Vector3(0, 0, 18));
        b.Arms(54, new Vector3(49, 96, 12));
        b.Arms(60, new Vector3(39, 76, 10));
        b.Arms(80, new Vector3(53, 114, 14), hand: new Vector3(0, 0, 18));
        b.Arms(110, new Vector3(53, 114, 14), hand: new Vector3(0, 0, 18));
        b.ArmsRest(144).ArmsRest(154);
        b.Face(14, MiiExpression.Frustrated).Face(24, MiiExpression.SmileOpenMouth);
        b.Face(55, MiiExpression.Blink).Face(62, MiiExpression.OpenMouth);
        b.Face(92, MiiExpression.LikeWinkLeft).Face(112, MiiExpression.Normal);
        Twinkles(b, "Happy stars", 26, 0, 10, 1802);
        LandingDust(b, 54, 1803);
        b.Animation.AddEvent(22, "jump");
        b.Animation.AddEvent(54, "landed");
        b.Animation.AddEvent(80, "celebrate");
        return b.Animation;
    }

    private static MiiAnimation CloseLoop(AnimBuilder b)
    {
        // Include hands, scale, and every other keyed channel in the neutral pause. Individual body
        // holds alone still let the arms begin drifting immediately toward their first gesture.
        foreach (var (id, curve) in b.Animation.Tracks)
        {
            curve.SetKey(8, b.Animation.Evaluate(id, 0), Interpolation.Smooth);
            curve.SetKey(b.Animation.Length - 8, b.Animation.Evaluate(id, b.Animation.Length), Interpolation.Smooth);
        }
        return b.Animation;
    }

    private static void LandingDust(AnimBuilder b, int frame, uint seed)
    {
        var dust = ParticlePresets.DustRing();
        dust.Name = "Soft landing";
        dust.StartFrame = frame;
        dust.Count = 14;
        dust.Seed = seed;
        dust.SpawnRadius = new Vector3(15, 0, 15);
        dust.SpeedMin = 20;
        dust.SpeedMax = 40;
        dust.SizeStart = 9;
        dust.SizeEnd = 18;
        dust.LifeMin = 0.4f;
        dust.LifeMax = 0.6f;
        b.Animation.Particles.Add(dust);
    }

    private static void Twinkles(AnimBuilder b, string name, int frame, int duration, int count, uint seed)
    {
        var stars = ParticlePresets.Twinkles();
        stars.Name = name;
        stars.StartFrame = frame;
        stars.Duration = duration;
        stars.Count = count;
        stars.Seed = seed;
        stars.AttachBone = (byte)MiiBone.Chest;
        stars.Offset = new Vector3(0, 22, 14);
        stars.SpawnRadius = new Vector3(47);
        stars.SizeStart = 10;
        stars.SizeEnd = 2;
        stars.LifeMin = 0.4f;
        stars.LifeMax = 0.6f;
        stars.Tint = ParticleTint.FavoriteColor;
        b.Animation.Particles.Add(stars);
    }

    private static void Bubbles(AnimBuilder b, string name, int frame, int duration, int count, uint seed) =>
        b.Animation.Particles.Add(new ParticleEffect
        {
            Name = name, StartFrame = frame, Duration = duration, Count = count, Seed = seed,
            AttachBone = (byte)MiiBone.Chest, Offset = new Vector3(0, -10, 8), SpawnRadius = new Vector3(33),
            Direction = Vector3.UnitY, Spread = 38, SpeedMin = 18, SpeedMax = 38,
            Gravity = 8, Drag = 0.7f, Shape = BuiltInShapes.Ring,
            LifeMin = 0.45f, LifeMax = 0.7f, SizeStart = 12, SizeEnd = 20,
            ColorStart = new Vector4(1, 1, 1, 0.65f), ColorEnd = new Vector4(1, 1, 1, 0),
            Tint = ParticleTint.FavoriteColor,
        });
}
