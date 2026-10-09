using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Snappy entrances for the profile card window (see <see cref="WindowAnimations"/>), for switching between licenses:
/// the new Mii is in place within about half a second, so clicking through licenses never feels slow. Each starts
/// out of sight (below the sill or beside the card) and ends in the rest pose, so a window idle can follow.
/// </summary>
public static class ProfileQuickAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("profile/quick_appear/from_below/Quick_pop_up.miianim", PopUp),
        ("profile/quick_appear/from_below/Quick_pop_cheer.miianim", PopCheer),
        ("profile/quick_appear/from_left/Quick_slide_from_left.miianim", () => Slide(fromLeft: true)),
        ("profile/quick_appear/from_right/Quick_slide_from_right.miianim", () => Slide(fromLeft: false)),
        ("profile/quick_exit/to_below/Quick_drop_down.miianim", DropDown),
        ("profile/quick_exit/to_left/Quick_slide_to_left.miianim", () => SlideOut(toLeft: true)),
        ("profile/quick_exit/to_right/Quick_slide_to_right.miianim", () => SlideOut(toLeft: false)),
    ];

    /// <summary>Exits end out of sight (the others end at rest).</summary>
    public static bool IsExit(string path) => path.Contains("/quick_exit/");

    /// <summary>Below the sill, also for the shortest Miis (root travel shrinks with the body).</summary>
    public const float Hidden = -330f;

    /// <summary>Beside the card, out of sight on either side (the card is about ±190 canonical units wide).</summary>
    public const float Aside = 330f;

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootLean = Bone(MiiBone.Root, Channel.RotZ);
    private static readonly TrackId RootStretch = Bone(MiiBone.Root, Channel.Stretch);
    private static readonly TrackId ChestTilt = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId HeadTilt = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    /// <summary>Springs up from below the sill, overshoots a touch, squashes and settles with a blink.</summary>
    public static MiiAnimation PopUp()
    {
        var b = New("Quick pop up", 48);
        b.Key(RootY, 0, Hidden, Interpolation.EaseOut).Key(RootY, 13, 9);
        b.Keys(RootY, (19, -3), (25, 1), (31, 0), (48, 0));
        b.Keys(RootStretch, (0, 1.14f), (9, 1.1f), (14, 0.93f), (19, 1.04f), (25, 0.99f), (31, 1), (48, 1));
        // The head trails the body: looks up on the way, nods down as the body stops.
        b.Keys(HeadTilt, (0, -9), (11, -6), (17, 5), (24, -2), (31, 0), (48, 0));
        b.Keys(ChestTilt, (0, -3), (13, 2), (21, -1), (29, 0), (48, 0));
        b.Face(0, MiiExpression.Normal).Face(17, MiiExpression.Blink).Face(22, MiiExpression.Normal);
        b.Animation.AddEvent(13, "up");
        return Done(b);
    }

    /// <summary>Pops up a little higher and flings both arms up for a quick "ta-da", then drops them.</summary>
    public static MiiAnimation PopCheer()
    {
        var b = New("Quick pop cheer", 66);
        b.Key(RootY, 0, Hidden, Interpolation.EaseOut).Key(RootY, 14, 13);
        b.Keys(RootY, (21, -4), (28, 1), (34, 0), (66, 0));
        b.Keys(RootStretch, (0, 1.16f), (10, 1.12f), (15, 0.92f), (21, 1.05f), (28, 0.99f), (34, 1), (66, 1));
        b.Keys(HeadTilt, (0, -10), (12, -8), (19, 3), (26, -5), (44, -5), (56, 0), (66, 0));
        b.Keys(ChestTilt, (0, -3), (14, -5), (24, -3), (44, -3), (56, 0), (66, 0));

        b.ArmsRest(0).ArmsRest(8);
        b.Arms(17, new Vector3(50, 124, 12), hand: new Vector3(0, 0, 22), interpolation: Interpolation.Overshoot);
        b.Arms(38, new Vector3(48, 120, 13), hand: new Vector3(0, 0, 18));
        b.Arms(48, new Vector3(36, 88, 20));
        b.ArmsRest(58).ArmsRest(66);

        b.Face(0, MiiExpression.OpenMouth).Face(14, MiiExpression.SmileOpenMouth).Face(42, MiiExpression.Smile).Face(58, MiiExpression.Normal);
        b.Animation.AddEvent(14, "up");
        b.Animation.AddEvent(17, "cheer");
        return Done(b);
    }

    /// <summary>
    /// Zips in from the side with a little hop, leaning back against the speed; when it brakes the top half
    /// swings past and rocks back into place.
    /// </summary>
    public static MiiAnimation Slide(bool fromLeft)
    {
        // The Mii comes from -X (screen left) when s = 1. A negative root roll leans its top towards +X.
        var s = fromLeft ? 1f : -1f;
        var b = New(fromLeft ? "Quick slide from left" : "Quick slide from right", 54);
        b.Key(RootX, 0, -s * Aside, Interpolation.EaseOut).Key(RootX, 15, s * 12);
        b.Keys(RootX, (22, -s * 4), (29, s * 1), (35, 0), (54, 0));
        b.Keys(RootY, (0, 0), (6, 7), (13, 0), (19, -2), (26, 0), (54, 0));
        b.Keys(RootLean, (0, s * 9), (12, s * 7), (18, -s * 10), (25, s * 4), (31, -s * 1.5f), (37, 0), (54, 0));
        b.Keys(ChestRoll, (0, 0), (14, s * 2), (20, -s * 4), (27, s * 2), (34, 0), (54, 0));
        b.Keys(HeadRoll, (0, s * 6), (14, s * 6), (21, -s * 9), (28, s * 4), (36, -s * 1), (42, 0), (54, 0));
        b.Keys(HeadTilt, (0, -4), (14, -2), (22, 3), (30, 0), (54, 0));

        // The arms trail behind while zipping in (the back one swings out), then flop down to the sides.
        var (left, right) = fromLeft ? (new Vector3(30, 66, -6), new Vector3(-36, 76, -6)) : (new Vector3(36, 76, -6), new Vector3(-30, 66, -6));
        foreach (var frame in new[] { 0, 12 })
            b.Arm(MiiLimb.ArmL, frame, left).Arm(MiiLimb.ArmR, frame, right);
        b.ArmsRest(26).ArmsRest(54);

        b.Face(0, MiiExpression.OpenMouth).Face(17, MiiExpression.Blink).Face(22, MiiExpression.Smile).Face(44, MiiExpression.Normal);
        b.Animation.AddEvent(15, "arrive");
        return Done(b);
    }

    /// <summary>Exit: a tiny hop of anticipation, then drops straight down below the sill.</summary>
    public static MiiAnimation DropDown()
    {
        var b = New("Quick drop down", 16);
        b.Keys(RootY, (0, 0), (4, 5));
        b.Key(RootY, 4, 5, Interpolation.EaseIn).Key(RootY, 16, Hidden);
        b.Keys(RootStretch, (0, 1), (4, 0.95f), (9, 1.08f), (16, 1.14f));
        b.Keys(HeadTilt, (0, 0), (4, 3), (12, -8), (16, -10));
        b.Face(2, MiiExpression.Blink).Face(16, MiiExpression.Blink);
        return Done(b);
    }

    /// <summary>Exit: leans back the other way for a beat, then zips off the side of the card.</summary>
    public static MiiAnimation SlideOut(bool toLeft)
    {
        // Leaves towards -X (screen left) when s = 1.
        var s = toLeft ? 1f : -1f;
        var b = New(toLeft ? "Quick slide to left" : "Quick slide to right", 18);
        b.Keys(RootX, (0, 0), (5, s * 6));
        b.Key(RootX, 5, s * 6, Interpolation.EaseIn).Key(RootX, 18, -s * Aside);
        b.Keys(RootLean, (0, 0), (5, s * 5), (12, -s * 9), (18, -s * 10));
        b.Keys(HeadRoll, (0, 0), (5, s * 4), (13, -s * 6), (18, -s * 8));
        var (left, right) = toLeft ? (new Vector3(36, 76, -6), new Vector3(-30, 66, -6)) : (new Vector3(30, 66, -6), new Vector3(-36, 76, -6));
        b.Arm(MiiLimb.ArmL, 14, left).Arm(MiiLimb.ArmR, 14, right).Arm(MiiLimb.ArmL, 18, left).Arm(MiiLimb.ArmR, 18, right);
        b.Face(4, MiiExpression.Blink).Face(18, MiiExpression.Blink);
        return Done(b);
    }

    private static AnimBuilder New(string name, int length)
    {
        var b = new AnimBuilder(name, length, female: false).FkLegs();
        foreach (var frame in new[] { 0, length })
        {
            b.Rot(MiiBone.Root, frame, 0, 0, 0).Rot(MiiBone.Chest, frame, 0, 0, 0).Rot(MiiBone.Head, frame, 0, 0, 0);
            b.Key(RootX, frame, 0).Key(RootY, frame, 0).Key(RootStretch, frame, 1);
        }
        b.ArmsRest(0).ArmsRest(length).Face(0, MiiExpression.Normal).Face(length, MiiExpression.Normal);
        return b;
    }

    private static MiiAnimation Done(AnimBuilder b)
    {
        b.Animation.AddEvent(b.Animation.Length, "done");
        return b.Animation;
    }
}
