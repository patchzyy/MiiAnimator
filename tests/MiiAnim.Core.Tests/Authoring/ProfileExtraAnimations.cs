using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// New profile action clips. The action reactions are visible from head to toe, with planted IK feet.
/// The extra window reaction uses the existing card sill and keeps its gesture above the card edge.
/// Existing window clips and the manually edited Change_mii sample are deliberately not regenerated here.
/// </summary>
public static class ProfileExtraAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("profile/make_primary/Make_primary_proud.miianim", MakePrimaryProud),
        ("profile/make_primary/Make_primary_captain.miianim", MakePrimaryCaptain),
        ("profile/rename/Rename_introduction.miianim", RenameIntroduction),
        ("profile/rename/Rename_delighted.miianim", RenameDelighted),
        ("profile/window/funny/Window_double_take.miianim", WindowDoubleTake),
    ];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    /// <summary>Gather, stand proud, open both palms, acknowledge the choice with a nod, then relax.</summary>
    public static MiiAnimation MakePrimaryProud()
    {
        var b = New("Make primary proud", 192);
        b.Keys(RootY, (0, 0), (12, 0), (25, -4), (43, 0), (102, 0), (117, -2), (138, 0), (192, 0));
        b.Keys(ChestPitch, (0, 0), (12, 0), (25, 7), (43, -6), (102, -6), (118, 7), (138, -2), (165, 0), (192, 0));
        b.Keys(HeadPitch, (0, 0), (25, 7), (43, -6), (84, -6), (97, 4), (108, -4), (118, 7), (138, -2), (165, 0), (192, 0));
        b.Keys(HeadRoll, (0, 0), (43, 0), (59, 5), (91, 5), (108, 0), (192, 0));

        b.ArmsRest(12);
        b.Arms(25, new Vector3(30, 75, 18));
        b.Arms(43, new Vector3(52, 118, 14), hand: new Vector3(0, 0, 18));
        b.Arms(58, new Vector3(51, 116, 15), hand: new Vector3(0, 0, 16));
        b.Arms(102, new Vector3(51, 116, 15), hand: new Vector3(0, 0, 16));
        b.Arms(119, new Vector3(40, 91, 22));
        b.Arms(139, new Vector3(31, 72, 15));
        b.ArmsRest(165);
        b.ArmsRest(192);

        b.Face(25, MiiExpression.Blink);
        b.Face(36, MiiExpression.Normal);
        b.Face(43, MiiExpression.SmileOpenMouth);
        b.Face(75, MiiExpression.Smile);
        b.Face(117, MiiExpression.Blink);
        b.Face(126, MiiExpression.Smile);
        b.Face(165, MiiExpression.Normal);
        b.Animation.AddEvent(43, "primary");
        b.Animation.AddEvent(97, "acknowledge");
        return Done(b);
    }

    /// <summary>A confident single-palm announcement, an easy wrist wave, and a small appreciative bow.</summary>
    public static MiiAnimation MakePrimaryCaptain()
    {
        var b = New("Make primary captain", 216);
        b.Keys(RootY, (0, 0), (12, 0), (28, -2.5f), (45, 0), (122, 0), (144, -3), (173, 0), (216, 0));
        b.Keys(ChestPitch, (0, 0), (28, 5), (45, -4), (122, -4), (144, 12), (156, 12), (173, 0), (216, 0));
        b.Keys(ChestRoll, (0, 0), (45, -2), (122, -2), (144, 0), (216, 0));
        b.Keys(HeadPitch, (0, 0), (22, 7), (34, 0), (48, -5), (103, -5), (115, 6), (126, -1), (144, 8), (156, 8), (173, 0), (216, 0));
        b.Keys(HeadRoll, (0, 0), (50, -4), (106, -4), (133, 0), (216, 0));

        b.ArmsRest(12);
        b.Arm(MiiLimb.ArmL, 30, new Vector3(33, 85, 22));
        b.Arm(MiiLimb.ArmL, 48, new Vector3(52, 113, 14), hand: new Vector3(0, 0, 20));
        b.Arm(MiiLimb.ArmL, 112, new Vector3(52, 113, 14), hand: new Vector3(0, 0, 20));
        b.Keys(Bone(MiiBone.HandL, Channel.RotX), (48, 0), (62, -18), (76, 18), (90, -12), (105, 0));
        b.Arm(MiiLimb.ArmR, 30, new Vector3(-31, 73, 18));
        b.Arm(MiiLimb.ArmR, 112, new Vector3(-31, 73, 18));
        b.Arms(134, new Vector3(29, 79, 25));
        b.Arms(156, new Vector3(29, 79, 25));
        b.ArmsRest(177);
        b.ArmsRest(216);

        b.Face(23, MiiExpression.Blink);
        b.Face(30, MiiExpression.Normal);
        b.Face(48, MiiExpression.Smile);
        b.Face(87, MiiExpression.LikeWinkLeft);
        b.Face(97, MiiExpression.Smile);
        b.Face(144, MiiExpression.Blink);
        b.Face(163, MiiExpression.Smile);
        b.Face(184, MiiExpression.Normal);
        b.Animation.AddEvent(48, "primary");
        b.Animation.AddEvent(144, "bow");
        return Done(b);
    }

    /// <summary>Politely bows, introduces the new name with an open palm, then gives a friendly nod.</summary>
    public static MiiAnimation RenameIntroduction()
    {
        var b = New("Rename introduction", 222);
        b.Keys(RootY, (0, 0), (12, 0), (31, -4), (45, -4), (64, 0), (147, 0), (168, -1.5f), (188, 0), (222, 0));
        b.Keys(ChestPitch, (0, 0), (12, 0), (31, 16), (45, 16), (64, -2), (147, -2), (168, 5), (188, 0), (222, 0));
        b.Keys(HeadPitch, (0, 0), (31, 9), (45, 9), (64, -3), (132, -3), (148, 7), (162, -2), (180, 0), (222, 0));
        b.Keys(HeadRoll, (0, 0), (76, 4), (130, 4), (148, 0), (222, 0));

        b.ArmsRest(12);
        b.Arms(31, new Vector3(23, 78, 26));
        b.Arms(45, new Vector3(23, 78, 26));
        b.Arm(MiiLimb.ArmL, 76, new Vector3(51, 108, 16), hand: new Vector3(0, 0, 20));
        b.Arm(MiiLimb.ArmL, 130, new Vector3(51, 108, 16), hand: new Vector3(0, 0, 20));
        b.Arm(MiiLimb.ArmR, 76, new Vector3(-31, 73, 18));
        b.Arm(MiiLimb.ArmR, 130, new Vector3(-31, 73, 18));
        b.Arms(159, new Vector3(34, 83, 23));
        b.ArmsRest(189);
        b.ArmsRest(222);

        b.Face(26, MiiExpression.Blink);
        b.Face(51, MiiExpression.Normal);
        b.Face(76, MiiExpression.Smile);
        b.Face(94, MiiExpression.OpenMouth);
        b.Face(105, MiiExpression.Smile);
        b.Face(142, MiiExpression.Blink);
        b.Face(151, MiiExpression.Smile);
        b.Face(190, MiiExpression.Normal);
        b.Animation.AddEvent(31, "bow");
        b.Animation.AddEvent(76, "introduce");
        return Done(b);
    }

    /// <summary>Notices the new name, gives a delighted little double nod, and presents it with both palms.</summary>
    public static MiiAnimation RenameDelighted()
    {
        var b = New("Rename delighted", 186);
        b.Keys(RootY, (0, 0), (12, 0), (30, -2), (48, 0), (88, 0), (105, -2.5f), (127, 0), (186, 0));
        b.Keys(ChestPitch, (0, 0), (30, 5), (48, -3), (88, -3), (105, 5), (127, 0), (186, 0));
        b.Keys(HeadPitch, (0, 0), (26, 9), (37, -3), (48, 7), (60, -4), (86, -4), (105, 5), (127, 0), (186, 0));
        b.Keys(HeadRoll, (0, 0), (63, -6), (85, -6), (104, 0), (186, 0));

        b.ArmsRest(12);
        b.Arms(30, new Vector3(32, 78, 23));
        b.Arms(62, new Vector3(50, 105, 20), hand: new Vector3(0, 0, 18));
        b.Arms(88, new Vector3(50, 105, 20), hand: new Vector3(0, 0, 18));
        b.Arms(111, new Vector3(35, 85, 23));
        b.ArmsRest(143);
        b.ArmsRest(186);

        b.Face(25, MiiExpression.Surprise);
        b.Face(37, MiiExpression.Smile);
        b.Face(62, MiiExpression.SmileOpenMouth);
        b.Face(82, MiiExpression.Smile);
        b.Face(101, MiiExpression.Blink);
        b.Face(109, MiiExpression.Smile);
        b.Face(145, MiiExpression.Normal);
        b.Animation.AddEvent(25, "noticed");
        b.Animation.AddEvent(62, "introduce");
        return Done(b);
    }

    /// <summary>Two curious glances, a startled double take, and a playful shrug visible above the sill.</summary>
    public static MiiAnimation WindowDoubleTake()
    {
        var b = New("Window double take", 210);
        b.Keys(RootY, (0, 0), (12, 0), (27, -1), (49, -1), (63, 0), (91, -2), (128, -2), (151, 0), (210, 0));
        b.Keys(ChestPitch, (0, 0), (27, 2), (49, 2), (63, -2), (83, 0), (128, 0), (151, 0), (210, 0));
        b.Keys(HeadYaw, (0, 0), (12, 0), (27, 23), (36, 23), (48, -24), (54, -24), (63, 0), (210, 0));
        b.Keys(HeadPitch, (0, 0), (27, 3), (49, 3), (63, -7), (83, 0), (97, -3), (128, -3), (151, 0), (210, 0));
        b.Keys(HeadRoll, (0, 0), (27, 5), (49, -5), (63, 0), (97, 5), (128, 5), (151, 0), (210, 0));

        // The gesture never crosses the sill's front plane (z = 58), and the palms rise above y = 88.
        b.ArmsRest(69);
        b.Arms(83, new Vector3(36, 90, 22));
        b.Arms(97, new Vector3(52, 105, 14), hand: new Vector3(0, 0, 18));
        b.Arms(128, new Vector3(52, 105, 14), hand: new Vector3(0, 0, 18));
        b.Arms(151, new Vector3(36, 90, 22));
        b.ArmsRest(173);
        b.ArmsRest(210);

        b.Face(34, MiiExpression.Blink);
        b.Face(40, MiiExpression.Normal);
        b.Face(63, MiiExpression.Surprise);
        b.Face(79, MiiExpression.Normal);
        b.Face(97, MiiExpression.Smile);
        b.Face(112, MiiExpression.LikeWinkRight);
        b.Face(123, MiiExpression.Smile);
        b.Face(171, MiiExpression.Normal);
        b.Animation.AddEvent(63, "double_take");
        b.Animation.AddEvent(97, "shrug");
        return Done(b);
    }

    private static AnimBuilder New(string name, int length)
    {
        var b = new AnimBuilder(name, length, female: false);
        // Leave legs at their normal IK blend and their normal rest targets. Root offsets shift the hips
        // while both feet remain planted, including when a clip is played on the female body model.
        b.Key(RootX, 0, 0).Key(RootX, length, 0);
        b.ArmsRest(0);
        b.Face(0, MiiExpression.Normal);
        return b;
    }

    private static MiiAnimation Done(AnimBuilder b)
    {
        // Every clip begins with a twelve-frame rest hold. Apply it to head/chest channels too,
        // rather than allowing their first smooth segment to start moving immediately at frame 0.
        foreach (var curve in b.Animation.Tracks.Values)
            if (curve.Count > 0 && curve.Keys[0].Frame == 0 && !curve.HasKeyAt(12))
                curve.SetKey(12, curve.Keys[0].Value, curve.Keys[0].Interpolation);
        b.Face(b.Animation.Length, MiiExpression.Normal);
        b.Animation.AddEvent(b.Animation.Length, "done");
        return b.Animation;
    }
}
