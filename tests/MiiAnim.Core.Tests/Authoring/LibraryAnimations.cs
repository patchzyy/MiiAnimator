using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Reactions for the My Miis square portrait tiles. The head does the acting; the arms only open a few
/// degrees below the neck. No body travel, scaling, chest movement, legs, actors, or particles.
/// Hover enter/idle/leave clips come in matching sets with an explicit smiling anchor pose.
/// </summary>
public static class LibraryAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("library/favorite/Favorite_grateful_nod.miianim", FavoriteGratefulNod),
        ("library/favorite/Favorite_delighted_wink.miianim", FavoriteDelightedWink),
        ("library/favorite/Favorite_shy_smile.miianim", FavoriteShySmile),
        ("library/favorite/Unfavorite_soft_nod.miianim", UnfavoriteSoftNod),
        ("library/favorite/Unfavorite_curious_blink.miianim", UnfavoriteCuriousBlink),
        ("library/hover/Hover_enter_bright.miianim", HoverEnterBright),
        ("library/hover/Hover_idle_bright.miianim", HoverIdleBright),
        ("library/hover/Hover_leave_bright.miianim", HoverLeaveBright),
        ("library/hover/Hover_enter_curious.miianim", HoverEnterCurious),
        ("library/hover/Hover_idle_curious.miianim", HoverIdleCurious),
        ("library/hover/Hover_leave_curious.miianim", HoverLeaveCurious),
    ];

    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    /// <summary>Two small grateful nods, a quick delighted smile, then the Mii's own face again.</summary>
    public static MiiAnimation FavoriteGratefulNod()
    {
        var b = Portrait("Favorite grateful nod", 90);
        b.Keys(HeadPitch, (0, 0), (10, -3), (26, 6), (38, -2), (50, 4), (66, 0), (90, 0));
        b.Keys(HeadRoll, (0, 0), (13, -3), (26, 0), (50, 1), (68, 0), (90, 0));
        OpenArms(b, (0, 0), (18, 5), (38, 4), (68, 0), (90, 0));
        b.Face(8, MiiExpression.Surprise);
        b.Face(14, MiiExpression.SmileOpenMouth);
        b.Face(30, MiiExpression.Smile);
        b.Face(68, MiiExpression.Normal);
        return Reaction(b, 14, "favorite_ack");
    }

    /// <summary>A pleased head tilt and one wink, with a tiny outward opening of the shoulders.</summary>
    public static MiiAnimation FavoriteDelightedWink()
    {
        var b = Portrait("Favorite delighted wink", 78);
        b.Keys(HeadPitch, (0, 0), (12, -3), (28, -1), (42, 2), (60, 0), (78, 0));
        b.Keys(HeadYaw, (0, 0), (16, 5), (34, 5), (56, 0), (78, 0));
        b.Keys(HeadRoll, (0, 0), (18, -5), (34, -5), (50, -2), (62, 0), (78, 0));
        OpenArms(b, (0, 0), (18, 7), (36, 5), (62, 0), (78, 0));
        b.Face(6, MiiExpression.Smile);
        b.Face(20, MiiExpression.LikeWinkRight);
        b.Face(30, MiiExpression.Smile);
        b.Face(60, MiiExpression.Normal);
        return Reaction(b, 20, "favorite_ack");
    }

    /// <summary>A shy downward glance, eyes closed for a beat, then a warm little nod to the viewer.</summary>
    public static MiiAnimation FavoriteShySmile()
    {
        var b = Portrait("Favorite shy smile", 102);
        b.Keys(HeadPitch, (0, 0), (18, 5), (38, 5), (54, -2), (70, 3), (86, 0), (102, 0));
        b.Keys(HeadYaw, (0, 0), (20, -6), (38, -6), (56, 0), (102, 0));
        b.Keys(HeadRoll, (0, 0), (20, 4), (38, 4), (58, 1), (86, 0), (102, 0));
        OpenArms(b, (0, 0), (20, 2), (54, 4), (86, 0), (102, 0));
        b.Face(10, MiiExpression.Smile);
        b.Face(28, MiiExpression.Blink);
        b.Face(34, MiiExpression.Smile);
        b.Face(86, MiiExpression.Normal);
        return Reaction(b, 18, "favorite_ack");
    }

    /// <summary>A calm acknowledging nod when the star is removed; settles without a sad or angry reaction.</summary>
    public static MiiAnimation UnfavoriteSoftNod()
    {
        var b = Portrait("Unfavorite soft nod", 72);
        b.Keys(HeadPitch, (0, 0), (12, 5), (22, 5), (36, -1), (54, 0), (72, 0));
        b.Keys(HeadRoll, (0, 0), (12, 2), (30, 1), (54, 0), (72, 0));
        OpenArms(b, (0, 0), (14, 2), (46, 0), (72, 0));
        b.Face(13, MiiExpression.Blink);
        b.Face(19, MiiExpression.Normal);
        return Reaction(b, 13, "unfavorite_ack");
    }

    /// <summary>A brief curious sideways look and blink, then an easy return to the tile's neutral portrait.</summary>
    public static MiiAnimation UnfavoriteCuriousBlink()
    {
        var b = Portrait("Unfavorite curious blink", 84);
        b.Keys(HeadPitch, (0, 0), (16, -2), (30, -2), (46, 2), (66, 0), (84, 0));
        b.Keys(HeadYaw, (0, 0), (18, 8), (30, 8), (50, 0), (84, 0));
        b.Keys(HeadRoll, (0, 0), (18, -4), (30, -4), (48, -1), (66, 0), (84, 0));
        OpenArms(b, (0, 0), (18, 3), (54, 0), (84, 0));
        b.Face(10, MiiExpression.Surprise);
        b.Face(30, MiiExpression.Blink);
        b.Face(36, MiiExpression.Normal);
        return Reaction(b, 18, "unfavorite_ack");
    }

    /// <summary>A quick friendly nod as the pointer enters; finishes in the bright hover anchor.</summary>
    public static MiiAnimation HoverEnterBright()
    {
        var b = Portrait("Hover enter bright", 36);
        b.Keys(HeadPitch, (0, 0), (10, -2), (18, 4), (28, -1.5f), (36, -1.5f));
        b.Keys(HeadRoll, (0, 0), (12, -3), (28, -2), (36, -2));
        OpenArms(b, (0, 0), (12, 3), (28, 2), (36, 2));
        b.Face(6, MiiExpression.Smile);
        b.Face(36, MiiExpression.Smile);
        b.Animation.AddEvent(36, "hover_ready");
        return b.Animation;
    }

    /// <summary>Four-second smiling loop: a small glance, one blink, and a quiet head sway.</summary>
    public static MiiAnimation HoverIdleBright()
    {
        var b = Portrait("Hover idle bright", 240);
        BrightAnchor(b, 0);
        b.Keys(HeadPitch, (0, -1.5f), (50, -2.5f), (90, -1.5f), (145, 0.5f), (190, -1.5f), (220, -1.5f), (240, -1.5f));
        b.Keys(HeadYaw, (0, 0), (85, 0), (110, 4), (145, 4), (174, 0), (240, 0));
        b.Keys(HeadRoll, (0, -2), (75, -3), (120, -2), (195, -1), (220, -2), (240, -2));
        OpenArms(b, (0, 2), (60, 3), (120, 2), (180, 2.5f), (220, 2), (240, 2));
        b.Face(143, MiiExpression.Blink);
        b.Face(148, MiiExpression.Smile);
        BrightAnchor(b, 240);
        return b.Animation;
    }

    /// <summary>The bright anchor relaxes back to the original face and rest pose after pointer leave.</summary>
    public static MiiAnimation HoverLeaveBright()
    {
        var b = Portrait("Hover leave bright", 30);
        BrightAnchor(b, 0);
        b.Keys(HeadPitch, (0, -1.5f), (12, 1), (24, 0), (30, 0));
        b.Keys(HeadRoll, (0, -2), (12, -1), (24, 0), (30, 0));
        OpenArms(b, (0, 2), (24, 0), (30, 0));
        b.Face(24, MiiExpression.Normal);
        b.Face(30, MiiExpression.Normal);
        b.Animation.AddEvent(30, "done");
        return b.Animation;
    }

    /// <summary>A small questioning tilt and smile as the pointer enters; finishes in the curious anchor.</summary>
    public static MiiAnimation HoverEnterCurious()
    {
        var b = Portrait("Hover enter curious", 36);
        b.Keys(HeadPitch, (0, 0), (10, 1), (20, -2), (28, 1), (36, 1));
        b.Keys(HeadYaw, (0, 0), (16, -5), (28, -3), (36, -3));
        b.Keys(HeadRoll, (0, 0), (16, 4), (28, 3), (36, 3));
        OpenArms(b, (0, 0), (16, 3), (28, 2), (36, 2));
        b.Face(10, MiiExpression.Smile);
        b.Face(36, MiiExpression.Smile);
        b.Animation.AddEvent(36, "hover_ready");
        return b.Animation;
    }

    /// <summary>Four-second curious loop: a short glance back across the tile, a blink, then the same anchor.</summary>
    public static MiiAnimation HoverIdleCurious()
    {
        var b = Portrait("Hover idle curious", 240);
        CuriousAnchor(b, 0);
        b.Keys(HeadPitch, (0, 1), (58, 0), (120, 1), (155, -1), (200, 1), (220, 1), (240, 1));
        b.Keys(HeadYaw, (0, -3), (44, -3), (94, 2), (128, 2), (180, -3), (220, -3), (240, -3));
        b.Keys(HeadRoll, (0, 3), (72, 2), (145, 4), (200, 3), (220, 3), (240, 3));
        OpenArms(b, (0, 2), (60, 2.5f), (120, 2), (180, 3), (220, 2), (240, 2));
        b.Face(152, MiiExpression.Blink);
        b.Face(157, MiiExpression.Smile);
        CuriousAnchor(b, 240);
        return b.Animation;
    }

    /// <summary>The curious tilt straightens with a tiny nod, returning the Mii's own face.</summary>
    public static MiiAnimation HoverLeaveCurious()
    {
        var b = Portrait("Hover leave curious", 30);
        CuriousAnchor(b, 0);
        b.Keys(HeadPitch, (0, 1), (10, 2.5f), (24, 0), (30, 0));
        b.Keys(HeadYaw, (0, -3), (18, 0), (30, 0));
        b.Keys(HeadRoll, (0, 3), (12, 1), (24, 0), (30, 0));
        OpenArms(b, (0, 2), (24, 0), (30, 0));
        b.Face(24, MiiExpression.Normal);
        b.Face(30, MiiExpression.Normal);
        b.Animation.AddEvent(30, "done");
        return b.Animation;
    }

    private static AnimBuilder Portrait(string name, int length)
    {
        var b = new AnimBuilder(name, length, female: false);
        b.Rot(MiiBone.Head, 0, 0, 0, 0).Rot(MiiBone.Head, length, 0, 0, 0);
        b.ArmsRest(0).ArmsRest(length);
        b.Face(0, MiiExpression.Normal).Face(length, MiiExpression.Normal);
        return b;
    }

    // Positive local Z opens the left upper arm outward; mirrored right Z opens the right arm.
    // The forearms and hands keep their own rest rotations. Even the peak opening is only seven degrees.
    private static void OpenArms(AnimBuilder b, params (int Frame, float Degrees)[] keys)
    {
        foreach (var (frame, degrees) in keys)
        {
            b.Key(Bone(MiiBone.ArmL1, Channel.RotZ), frame, degrees);
            b.Key(Bone(MiiBone.ArmR1, Channel.RotZ), frame, -degrees);
        }
    }

    private static void BrightAnchor(AnimBuilder b, int frame)
    {
        b.Rot(MiiBone.Head, frame, -1.5f, 0, -2);
        OpenArms(b, (frame, 2));
        b.Face(frame, MiiExpression.Smile);
    }

    private static void CuriousAnchor(AnimBuilder b, int frame)
    {
        b.Rot(MiiBone.Head, frame, 1, -3, 3);
        OpenArms(b, (frame, 2));
        b.Face(frame, MiiExpression.Smile);
    }

    private static MiiAnimation Reaction(AnimBuilder b, int accentFrame, string accent)
    {
        b.Animation.AddEvent(accentFrame, accent);
        b.Animation.AddEvent(b.Animation.Length, "done");
        return b.Animation;
    }
}
