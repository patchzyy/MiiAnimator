using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Quiet head-and-shoulder motion for the tightly cropped FriendsSideProfile card.
/// +Z is the face's front and +X is screen right: a positive Y rotation turns toward the card text.
/// The yaw is included in these files, so the player should not add the static image variant's 15 degrees again.
/// </summary>
public static class FriendsAnimations
{
    public const float BaselineYaw = 15f;
    public const float BaselineHeadPitch = -8f;
    public const float BaselineHeadRoll = -4f;

    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("friends/online/Friends_online_idle_attentive.miianim", OnlineIdleAttentive),
        ("friends/online/Friends_online_idle_amused.miianim", OnlineIdleAmused),
        ("friends/online/Friends_became_online.miianim", BecameOnline),
        ("friends/offline/Friends_offline_idle_restful.miianim", OfflineIdleRestful),
        ("friends/offline/Friends_offline_idle_thoughtful.miianim", OfflineIdleThoughtful),
        ("friends/offline/Friends_became_offline.miianim", BecameOffline),
    ];

    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestYaw = Bone(MiiBone.Chest, Channel.RotY);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId ChestBreath = Bone(MiiBone.Chest, Channel.ScaleY);

    /// <summary>Loop: a light breath, a small acknowledgement toward the text, and two natural blinks.</summary>
    public static MiiAnimation OnlineIdleAttentive()
    {
        var b = Card("Friends online idle attentive", 360);
        b.Keys(ChestBreath, (0, 1), (66, 1.007f), (126, 1), (214, 1.006f), (284, 1), (360, 1));
        b.Keys(ChestPitch, (0, 0), (64, -0.7f), (126, 0), (198, 0.8f), (254, 0), (360, 0));
        // The chest begins the acknowledgement; the head follows and settles a little later.
        b.Keys(HeadYaw, (0, 0), (124, 0), (154, 3.5f), (196, 3.5f), (238, 0), (360, 0));
        b.Keys(HeadPitch, (0, BaselineHeadPitch), (146, BaselineHeadPitch), (168, -3), (188, -6),
            (210, BaselineHeadPitch), (360, BaselineHeadPitch));
        b.Keys(HeadRoll, (0, BaselineHeadRoll), (72, -3), (128, BaselineHeadRoll), (182, -5.5f),
            (236, BaselineHeadRoll), (360, BaselineHeadRoll));
        Blink(b, 82, 6);
        b.Face(168, MiiExpression.Smile).Face(204, MiiExpression.Normal);
        Blink(b, 290, 5);
        return b.Animation;
    }

    /// <summary>Loop: hears something amusing, gives a restrained two-beat chuckle, then settles.</summary>
    public static MiiAnimation OnlineIdleAmused()
    {
        var b = Card("Friends online idle amused", 420);
        b.Keys(ChestBreath, (0, 1), (76, 1.006f), (140, 1), (190, 1), (208, 1.011f),
            (222, 1.002f), (236, 1.008f), (252, 1), (334, 1.005f), (390, 1), (420, 1));
        b.Keys(ChestPitch, (0, 0), (166, 0), (198, -1.2f), (218, 0.8f), (236, -0.8f), (266, 0), (420, 0));
        b.Keys(ChestRoll, (0, 0), (156, 0), (194, -0.8f), (236, -0.8f), (278, 0), (420, 0));
        b.Keys(HeadRoll, (0, BaselineHeadRoll), (154, BaselineHeadRoll), (192, -7.5f),
            (244, -7.5f), (294, BaselineHeadRoll), (420, BaselineHeadRoll));
        b.Keys(HeadPitch, (0, BaselineHeadPitch), (184, BaselineHeadPitch), (204, -10), (220, -5.5f),
            (236, -9), (258, -6), (294, BaselineHeadPitch), (420, BaselineHeadPitch));
        b.Keys(HeadYaw, (0, 0), (158, 0), (190, 2), (256, 2), (296, 0), (420, 0));
        Blink(b, 100, 5);
        b.Face(192, MiiExpression.Smile).Face(212, MiiExpression.SmileOpenMouth)
            .Face(220, MiiExpression.Smile).Face(236, MiiExpression.SmileOpenMouth)
            .Face(244, MiiExpression.Smile).Face(270, MiiExpression.Normal);
        Blink(b, 340, 6);
        return b.Animation;
    }

    /// <summary>One shot: notices the connection, brightens, then gives a friendly two-part nod.</summary>
    public static MiiAnimation BecameOnline()
    {
        var b = Card("Friends became online", 156);
        b.Keys(ChestBreath, (0, 1), (16, 1.013f), (40, 1.005f), (78, 1), (156, 1));
        b.Keys(ChestPitch, (0, 0), (18, -1.6f), (34, 0), (56, 1.5f), (78, 0), (156, 0));
        b.Keys(HeadPitch, (0, BaselineHeadPitch), (16, -11), (30, -7), (48, -0.5f),
            (62, -7), (76, -3), (94, BaselineHeadPitch), (156, BaselineHeadPitch));
        b.Keys(HeadYaw, (0, 0), (24, 3), (80, 3), (112, 0), (156, 0));
        b.Keys(HeadRoll, (0, BaselineHeadRoll), (20, -2), (44, -4), (66, -5.5f),
            (106, BaselineHeadRoll), (156, BaselineHeadRoll));
        b.Face(12, MiiExpression.Surprise).Face(24, MiiExpression.Smile)
            .Face(94, MiiExpression.Normal);
        Blink(b, 116, 5);
        b.Animation.AddEvent(24, "online");
        b.Animation.AddEvent(156, "done");
        return b.Animation;
    }

    /// <summary>Loop: a slower exhale, gentle chin settling, and a soft extended blink.</summary>
    public static MiiAnimation OfflineIdleRestful()
    {
        var b = Card("Friends offline idle restful", 420);
        b.Keys(ChestBreath, (0, 1), (98, 1.004f), (194, 1), (298, 1.0035f), (390, 1), (420, 1));
        b.Keys(ChestPitch, (0, 0), (144, 0.8f), (224, 0), (354, 0.6f), (420, 0));
        b.Keys(HeadPitch, (0, BaselineHeadPitch), (80, -7), (164, -4.5f), (234, BaselineHeadPitch),
            (320, -6), (388, BaselineHeadPitch), (420, BaselineHeadPitch));
        b.Keys(HeadRoll, (0, BaselineHeadRoll), (138, -5.5f), (224, BaselineHeadRoll),
            (324, -3), (396, BaselineHeadRoll), (420, BaselineHeadRoll));
        Blink(b, 160, 14);
        Blink(b, 342, 9);
        return b.Animation;
    }

    /// <summary>Loop: quietly thinks, glancing slightly farther right and down before returning to the card pose.</summary>
    public static MiiAnimation OfflineIdleThoughtful()
    {
        var b = Card("Friends offline idle thoughtful", 480);
        b.Keys(ChestBreath, (0, 1), (92, 1.004f), (188, 1), (306, 1.004f), (420, 1), (480, 1));
        b.Keys(ChestYaw, (0, 0), (116, 0), (170, 1), (238, 1), (294, 0), (480, 0));
        b.Keys(HeadYaw, (0, 0), (106, 0), (158, 5), (230, 5), (292, 0), (480, 0));
        b.Keys(HeadPitch, (0, BaselineHeadPitch), (108, BaselineHeadPitch), (160, -2), (232, -2),
            (300, BaselineHeadPitch), (384, -9), (438, BaselineHeadPitch), (480, BaselineHeadPitch));
        b.Keys(HeadRoll, (0, BaselineHeadRoll), (112, BaselineHeadRoll), (166, -1), (236, -1),
            (300, BaselineHeadRoll), (480, BaselineHeadRoll));
        Blink(b, 86, 8);
        Blink(b, 210, 7);
        Blink(b, 402, 9);
        return b.Animation;
    }

    /// <summary>One shot: a small closing acknowledgement, slow exhale and blink, then the offline baseline.</summary>
    public static MiiAnimation BecameOffline()
    {
        var b = Card("Friends became offline", 192);
        // A held chin dip distinguishes leaving from the online clip's quicker double nod.
        b.Keys(ChestPitch, (0, 0), (24, 0), (58, 2), (80, 2), (130, 0), (192, 0));
        b.Keys(ChestBreath, (0, 1), (28, 1.005f), (88, 1), (192, 1));
        b.Keys(HeadPitch, (0, BaselineHeadPitch), (20, BaselineHeadPitch), (52, 0.5f), (74, 0.5f),
            (124, BaselineHeadPitch), (192, BaselineHeadPitch));
        b.Keys(HeadRoll, (0, BaselineHeadRoll), (44, -5.5f), (84, -5.5f),
            (130, BaselineHeadRoll), (192, BaselineHeadRoll));
        b.Keys(HeadYaw, (0, 0), (40, 1.5f), (92, 1.5f), (132, 0), (192, 0));
        Blink(b, 52, 18);
        Blink(b, 150, 8);
        b.Animation.AddEvent(70, "offline");
        b.Animation.AddEvent(192, "done");
        return b.Animation;
    }

    private static AnimBuilder Card(string name, int length)
    {
        var b = new AnimBuilder(name, length, female: false).FkLegs();
        foreach (var frame in new[] { 0, length })
        {
            // The hidden lower body has no animation. FK makes it remain rigid under this constant yaw.
            b.Rot(MiiBone.Root, frame, 0, BaselineYaw, 0);
            b.Key(Bone(MiiBone.Root, Channel.PosX), frame, 0)
                .Key(Bone(MiiBone.Root, Channel.PosY), frame, 0)
                .Key(Bone(MiiBone.Root, Channel.PosZ), frame, 0);
            b.Rot(MiiBone.Chest, frame, 0, 0, 0);
            b.Rot(MiiBone.Head, frame, BaselineHeadPitch, 0, BaselineHeadRoll);
            b.Key(ChestBreath, frame, 1);
            b.ArmsRest(frame);
            b.Face(frame, MiiExpression.Normal);
        }
        return b;
    }

    private static void Blink(AnimBuilder b, int frame, int duration) =>
        b.Face(frame, MiiExpression.Blink).Face(frame + duration, MiiExpression.Normal);
}
