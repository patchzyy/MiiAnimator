using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Upper-body animations for WheelWizard's profile card: the Mii is behind a "window" (the card's bottom edge),
/// so the legs are never seen. Authored against the animator's window prop with its default settings
/// (sill at y 88, wall 56 in front of the Mii, opening 80 wide either side); see <see cref="ClipCheck.Window"/>.
/// All of them start or end in the rest pose so they can be chained: Peek/Hello → Idle (loops).
/// </summary>
public static class WindowAnimations
{
    public static readonly ClipCheck.Window Window = new();

    public static readonly (string Name, Func<MiiAnimation> Create)[] All =
    [
        ("Window_peek", Peek),
        ("Window_hello_pop", HelloPop),
        ("Window_hello_slide", HelloSlide),
        ("Window_idle_lean", IdleLean),
        ("Window_idle_sway", IdleSway),
    ];

    /// <summary>Fully hidden below the sill (also for fairly short Miis with big hair).</summary>
    private const float Hidden = -190f;

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootZ = Bone(MiiBone.Root, Channel.PosZ);
    private static readonly TrackId RootLean = Bone(MiiBone.Root, Channel.RotZ);
    private static readonly TrackId RootStretch = Bone(MiiBone.Root, Channel.Stretch);
    private static readonly TrackId ChestTilt = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId HeadTilt = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadTurn = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    /// <summary>Wrist resting on the sill ledge (left hand; mirror for the right).</summary>
    private static readonly Vector3 OnSill = new(22, 99, 45);

    /// <summary>Above and behind the ledge: hands pass through here to get onto / off the sill without going through it.</summary>
    private static readonly Vector3 OverSill = new(34, 106, 32);

    /// <summary>
    /// Peeks over the window: eyes come up over the sill, looks left and right, notices you, ducks away,
    /// then pops back up with hands on the sill, waves and stands up.
    /// </summary>
    public static MiiAnimation Peek()
    {
        var b = new AnimBuilder("Window peek", 280, female: false).FkLegs();

        b.Key(RootY, 0, Hidden).Key(RootY, 16, Hidden, Interpolation.EaseInOut).Key(RootY, 64, -58);
        b.Keys(RootY, (106, -58));
        b.Key(RootY, 106, -58, Interpolation.Snappy).Key(RootY, 112, -52);
        b.Key(RootY, 118, -54, Interpolation.EaseIn).Key(RootY, 128, -125);
        b.Keys(RootY, (140, -122), (150, -125));
        b.Key(RootY, 158, -125, Interpolation.Overshoot).Key(RootY, 174, -10);
        b.Keys(RootY, (200, -11.5f), (226, -10), (232, -10), (256, 0), (280, 0));
        b.Keys(RootStretch, (0, 1), (118, 1), (122, 0.93f), (130, 1), (158, 1), (164, 1.12f), (174, 0.95f), (184, 1), (280, 1));

        // Curious looking around, then lean on the sill once back up.
        b.Keys(HeadTurn, (0, 0), (64, 0), (74, 32), (86, 32), (96, -32), (106, -32), (112, 0), (280, 0));
        b.Keys(HeadRoll, (0, 0), (64, 0), (74, 7), (86, 7), (96, -7), (106, -7), (112, 0), (176, 0), (188, -9), (226, -9), (244, 0), (280, 0));
        b.Keys(HeadTilt, (0, 0), (112, -4), (118, 0), (280, 0));
        b.Keys(ChestTilt, (0, 0), (174, 0), (186, 7), (226, 7), (244, 0), (280, 0));

        // Arms stay hidden until the pop, then grab the sill; the right hand waves.
        b.ArmsRest(0);
        b.ArmsRest(158);
        b.Arms(166, new Vector3(30, 84, 26));
        b.ArmsWorld(170, OverSill);
        b.ArmsWorld(176, OnSill);
        b.ArmWorld(MiiLimb.ArmL, 226, OnSill);
        b.ArmWorld(MiiLimb.ArmR, 186, MirrorX(OnSill));
        b.ArmWorld(MiiLimb.ArmR, 194, new Vector3(-46, 112, 18), hand: new Vector3(0, 0, -15));
        b.ArmWorld(MiiLimb.ArmR, 218, new Vector3(-46, 112, 18), hand: new Vector3(0, 0, -15));
        b.ArmWorld(MiiLimb.ArmR, 226, MirrorX(OnSill));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (194, 0), (200, -28), (206, 28), (212, -28), (218, 0));
        b.ArmsWorld(234, OverSill);
        b.Arms(242, new Vector3(30, 84, 26));
        b.ArmsRest(256);
        b.ArmsRest(280);

        b.Face(0, MiiExpression.Normal);
        b.Face(112, MiiExpression.Surprise);
        b.Face(119, MiiExpression.Blink);
        b.Face(128, MiiExpression.Normal);
        b.Face(162, MiiExpression.SmileOpenMouth);
        b.Face(182, MiiExpression.Normal);
        b.Face(196, MiiExpression.OpenMouth);
        b.Face(203, MiiExpression.Normal);
        b.Face(209, MiiExpression.OpenMouth);
        b.Face(216, MiiExpression.Normal);
        b.Face(242, MiiExpression.Smile);
        b.Face(262, MiiExpression.Normal);

        b.Animation.AddEvent(64, "peek");
        b.Animation.AddEvent(112, "noticed");
        b.Animation.AddEvent(118, "duck");
        b.Animation.AddEvent(158, "pop");
        b.Animation.AddEvent(194, "wave");
        b.Animation.AddEvent(280, "done");
        return b.Animation;
    }

    /// <summary>First-boot hello #1: springs up into view with a "yay", then rests a hand on the sill and waves.</summary>
    public static MiiAnimation HelloPop()
    {
        var b = new AnimBuilder("Window hello pop", 180, female: false).FkLegs();

        b.Key(RootY, 0, Hidden).Key(RootY, 8, Hidden, Interpolation.EaseOut).Key(RootY, 24, 6);
        b.Keys(RootY, (32, -2), (40, 0), (180, 0));
        b.Keys(RootStretch, (0, 1.15f), (8, 1.18f), (22, 1.05f), (28, 0.94f), (36, 1.02f), (44, 1), (180, 1));
        b.Keys(ChestTilt, (0, 0), (26, -6), (40, 0), (180, 0));

        b.ArmsRest(0);
        b.ArmsRest(8);
        b.Arms(26, new Vector3(52, 118, 14), hand: new Vector3(0, 0, 20), interpolation: Interpolation.Overshoot);
        b.ArmWorld(MiiLimb.ArmL, 48, OnSill);
        b.ArmWorld(MiiLimb.ArmL, 112, OnSill);
        b.Arm(MiiLimb.ArmR, 48, new Vector3(-42, 122, 18), hand: new Vector3(0, 0, -15));
        b.Arm(MiiLimb.ArmR, 104, new Vector3(-42, 122, 18), hand: new Vector3(0, 0, -15));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (52, 0), (60, -28), (68, 28), (76, -28), (84, 28), (92, -28), (100, 0));
        b.ArmWorld(MiiLimb.ArmR, 116, MirrorX(OnSill));
        b.ArmsWorld(126, OverSill);
        b.Arms(136, new Vector3(30, 84, 26));
        b.ArmsRest(148);
        b.ArmsRest(180);

        b.Keys(HeadRoll, (0, 0), (48, 0), (60, -9), (76, -4), (92, -9), (104, 0), (116, 6), (140, 6), (156, 0), (180, 0));
        b.Keys(HeadTilt, (0, -8), (24, -6), (40, 0), (180, 0));

        b.Face(0, MiiExpression.Normal);
        b.Face(12, MiiExpression.OpenMouth);
        b.Face(26, MiiExpression.SmileOpenMouth);
        b.Face(40, MiiExpression.Normal);
        b.Face(54, MiiExpression.OpenMouth);
        b.Face(62, MiiExpression.Normal);
        b.Face(70, MiiExpression.OpenMouth);
        b.Face(80, MiiExpression.Normal);
        b.Face(96, MiiExpression.Smile);
        b.Face(124, MiiExpression.Normal);

        b.Animation.AddEvent(8, "pop");
        b.Animation.AddEvent(48, "wave");
        b.Animation.AddEvent(180, "done");
        return b.Animation;
    }

    /// <summary>
    /// First-boot hello #2: leans in from the side of the window, hops over to the middle, does a polite bow
    /// and waves with both hands.
    /// </summary>
    public static MiiAnimation HelloSlide()
    {
        var b = new AnimBuilder("Window hello slide", 220, female: false).FkLegs();

        b.Key(RootX, 0, -140).Key(RootX, 56, -140, Interpolation.EaseInOut).Key(RootX, 96, 5);
        b.Keys(RootX, (108, 0), (220, 0));
        b.Keys(RootLean, (0, 0), (26, -30), (56, -30), (80, 0), (220, 0));
        b.Keys(HeadRoll, (0, 0), (26, 14), (56, 14), (80, 0), (146, 0), (156, 6), (170, -6), (184, 6), (196, 0), (220, 0));
        b.Keys(RootY, (0, 0), (56, 0), (64, 6), (72, 0), (80, 6), (88, 0), (96, 3), (104, 0), (220, 0));
        b.Keys(RootStretch, (0, 1), (56, 1), (60, 1.05f), (72, 0.96f), (76, 1.05f), (88, 0.96f), (92, 1.03f), (104, 1), (220, 1));

        // Polite bow.
        b.Keys(ChestTilt, (0, 0), (100, 0), (114, 20), (124, 20), (136, -3), (146, 0), (220, 0));
        b.Keys(HeadTilt, (0, 0), (100, 0), (114, 12), (124, 12), (136, -5), (150, 0), (220, 0));

        b.ArmsRest(0);
        b.ArmsRest(100);
        b.Arms(112, new Vector3(8, 80, 24), swivel: 20);
        b.Arms(126, new Vector3(8, 80, 24), swivel: 20);
        b.Arms(146, new Vector3(50, 120, 14), hand: new Vector3(0, 0, 20), interpolation: Interpolation.Overshoot);
        b.Arms(186, new Vector3(50, 120, 14), hand: new Vector3(0, 0, 20));
        b.Keys(Bone(MiiBone.HandL, Channel.RotX), (150, 0), (157, -28), (164, 28), (171, -28), (178, 28), (186, 0));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (150, 0), (157, 28), (164, -28), (171, 28), (178, -28), (186, 0));
        b.Arms(198, new Vector3(30, 84, 26));
        b.ArmsRest(210);
        b.ArmsRest(220);

        b.Face(0, MiiExpression.Normal);
        b.Face(30, MiiExpression.Blink);
        b.Face(35, MiiExpression.Normal);
        b.Face(58, MiiExpression.OpenMouth);
        b.Face(96, MiiExpression.Normal);
        b.Face(108, MiiExpression.Blink);
        b.Face(132, MiiExpression.Normal);
        b.Face(146, MiiExpression.SmileOpenMouth);
        b.Face(160, MiiExpression.OpenMouth);
        b.Face(174, MiiExpression.LikeWinkLeft);
        b.Face(192, MiiExpression.Normal);

        b.Animation.AddEvent(0, "lean_in");
        b.Animation.AddEvent(56, "slide");
        b.Animation.AddEvent(108, "bow");
        b.Animation.AddEvent(146, "wave");
        b.Animation.AddEvent(220, "done");
        return b.Animation;
    }

    /// <summary>Idle #1 (loops): leans forward onto the sill with folded arms, daydreams, looks around, stands back up.</summary>
    public static MiiAnimation IdleLean()
    {
        var b = new AnimBuilder("Window idle lean", 360, female: false).FkLegs();

        b.Keys(RootY, (0, 0), (36, -2), (320, -2), (352, 0), (360, 0));
        b.Keys(RootZ, (0, 0), (36, 6), (320, 6), (352, 0), (360, 0));
        b.Keys(ChestTilt, (0, 0), (36, 3), (80, 4.5f), (130, 3), (180, 4.5f), (230, 3), (280, 4.5f), (320, 3), (352, 0), (360, 0));

        // Folded arms resting on the sill (hands come up and over the ledge, not through it).
        b.ArmsRest(0);
        b.ArmsWorld(18, new Vector3(20, 102, 30), swivel: 25);
        b.ArmsWorld(36, new Vector3(21, 99, 45), swivel: 30);
        b.ArmsWorld(320, new Vector3(21, 99, 45), swivel: 30);
        b.ArmsWorld(336, new Vector3(20, 102, 30), swivel: 25);
        b.ArmsRest(352);
        b.ArmsRest(360);

        b.Keys(HeadRoll, (0, 0), (36, 0), (70, 6), (160, 6), (190, -4), (260, -4), (300, 0), (360, 0));
        b.Keys(HeadTurn, (0, 0), (170, 0), (190, 24), (230, 24), (250, 0), (360, 0));
        b.Keys(HeadTilt, (0, 0), (36, 0), (60, 4), (300, 4), (330, 0), (360, 0));

        b.Face(0, MiiExpression.Normal);
        b.Face(90, MiiExpression.Blink);
        b.Face(96, MiiExpression.Normal);
        b.Face(214, MiiExpression.Blink);
        b.Face(220, MiiExpression.Normal);
        b.Face(262, MiiExpression.Smile);
        b.Face(300, MiiExpression.Normal);
        return b.Animation;
    }

    /// <summary>Idle #2 (loops): gentle side-to-side sway, looks left and right, a glance up with a little shrug and a smile.</summary>
    public static MiiAnimation IdleSway()
    {
        var b = new AnimBuilder("Window idle sway", 300, female: false).FkLegs();

        b.Keys(RootX, (0, 0), (75, 2.5f), (150, 0), (225, -2.5f), (300, 0));
        b.Keys(RootLean, (0, 0), (75, -2), (150, 0), (225, 2), (300, 0));
        b.Keys(RootY, (0, 0), (75, -1), (150, 0), (225, -1), (300, 0));
        b.Keys(HeadRoll, (0, 0), (75, 4), (150, 0), (225, -4), (300, 0));
        b.Keys(HeadTurn, (0, 0), (40, 0), (60, 28), (95, 28), (115, 0), (160, 0), (180, -28), (210, -28), (230, 0), (300, 0));
        b.Keys(HeadTilt, (0, 0), (235, 0), (245, -10), (262, -10), (275, 0), (300, 0));
        b.Keys(Bone(MiiBone.Chest, Channel.Stretch), (0, 1), (235, 1), (245, 1.06f), (258, 1), (300, 1));
        b.Keys(ChestTilt, (0, 0), (75, 1.5f), (150, 0), (225, 1.5f), (300, 0));

        b.Face(0, MiiExpression.Normal);
        b.Face(30, MiiExpression.Blink);
        b.Face(35, MiiExpression.Normal);
        b.Face(135, MiiExpression.Blink);
        b.Face(140, MiiExpression.Normal);
        b.Face(245, MiiExpression.Smile);
        b.Face(266, MiiExpression.Normal);
        return b.Animation;
    }
}
