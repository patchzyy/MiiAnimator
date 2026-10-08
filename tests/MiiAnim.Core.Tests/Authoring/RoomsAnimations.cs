using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Head-only motion for the small circular portraits in WheelWizard's room/player lists.
/// Every clip has neutral lead-in and tail frames; the idle clips are quiet, seamless loops.
/// </summary>
public static class RoomsAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("rooms/lobby/Lobby_idle_blink.miianim", IdleBlink),
        ("rooms/lobby/Lobby_idle_glance_left.miianim", IdleGlanceLeft),
        ("rooms/lobby/Lobby_idle_glance_right.miianim", IdleGlanceRight),
        ("rooms/lobby/Lobby_idle_listen.miianim", IdleListen),
        ("rooms/lobby/Lobby_host_nod.miianim", HostNod),
        ("rooms/lobby/Lobby_greeting.miianim", Greeting),
        ("rooms/lobby/Lobby_ready_nod.miianim", ReadyNod),
        ("rooms/lobby/Lobby_state_notice.miianim", StateNotice),
    ];

    private static readonly TrackId Pitch = TrackId.Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId Yaw = TrackId.Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId Roll = TrackId.Bone(MiiBone.Head, Channel.RotZ);

    /// <summary>Eight-second default idle: two brief blinks and a barely perceptible head settle.</summary>
    public static MiiAnimation IdleBlink()
    {
        var animation = Clip("Lobby idle blink", 480);
        Keys(animation, Pitch, (92, 0), (130, 1), (182, 0), (300, 0), (342, -0.7f), (384, 0));
        Keys(animation, Roll, (180, 0), (224, 0.7f), (278, 0));
        Blink(animation, 96);
        Blink(animation, 326);
        return animation;
    }

    /// <summary>Ten-second loop with one small glance toward the portrait's left, then a long neutral pause.</summary>
    public static MiiAnimation IdleGlanceLeft() => IdleGlance("Lobby idle glance left", -1);

    /// <summary>Mirrored version of the left glance; choose by portrait position or mix among players.</summary>
    public static MiiAnimation IdleGlanceRight() => IdleGlance("Lobby idle glance right", 1);

    private static MiiAnimation IdleGlance(string name, float direction)
    {
        var animation = Clip(name, 600);
        Keys(animation, Yaw, (80, 0), (130, direction * 6.5f), (155, direction * 6.5f), (214, 0));
        Keys(animation, Pitch, (80, 0), (130, -0.8f), (155, -0.8f), (214, 0));
        Keys(animation, Roll, (94, 0), (140, direction * -1.5f), (155, direction * -1.5f), (228, 0));
        Blink(animation, 116);
        Blink(animation, 442);
        return animation;
    }

    /// <summary>Nine-second idle: a mild listening tilt, relaxed hold and slow return.</summary>
    public static MiiAnimation IdleListen()
    {
        var animation = Clip("Lobby idle listen", 540);
        Keys(animation, Roll, (116, 0), (178, 2), (226, 2), (300, 0));
        Keys(animation, Yaw, (116, 0), (178, 2.5f), (226, 2.5f), (300, 0));
        Keys(animation, Pitch, (146, 0), (202, 0.8f), (226, 0.8f), (300, 0));
        Blink(animation, 88);
        Blink(animation, 366);
        return animation;
    }

    /// <summary>A single modest host acknowledgement; no recurring grin or body bow.</summary>
    public static MiiAnimation HostNod()
    {
        var animation = Clip("Lobby host nod", 168);
        Keys(animation, Pitch, (24, 0), (44, 4.5f), (76, 0), (94, -0.5f), (116, 0));
        Blink(animation, 40);
        animation.AddEvent(44, "nod");
        return Finish(animation);
    }

    /// <summary>A small turn and two soft nods greet a joining player, with a brief closed-mouth smile.</summary>
    public static MiiAnimation Greeting()
    {
        var animation = Clip("Lobby greeting", 192);
        Keys(animation, Yaw, (22, 0), (48, -3), (80, 0));
        Keys(animation, Roll, (22, 0), (48, -2), (88, 0));
        Keys(animation, Pitch, (22, 0), (48, 3), (70, 0), (94, 2), (124, 0));
        Face(animation, 42, MiiExpression.Smile);
        Face(animation, 66, MiiExpression.Normal);
        Blink(animation, 112);
        animation.AddEvent(48, "hello");
        return Finish(animation);
    }

    /// <summary>One clear, gentle confirmation nod, useful when a player becomes ready.</summary>
    public static MiiAnimation ReadyNod()
    {
        var animation = Clip("Lobby ready nod", 156);
        Keys(animation, Pitch, (24, 0), (44, 4.5f), (78, 0));
        Keys(animation, Roll, (24, 0), (44, 1.2f), (82, 0));
        Face(animation, 36, MiiExpression.Smile);
        Face(animation, 58, MiiExpression.Normal);
        Blink(animation, 96);
        animation.AddEvent(44, "ready");
        return Finish(animation);
    }

    /// <summary>A moment of attention for a changed room state: slight lift, short glance and immediate calm.</summary>
    public static MiiAnimation StateNotice()
    {
        var animation = Clip("Lobby state notice", 180);
        Keys(animation, Pitch, (20, 0), (38, -2.5f), (76, 0));
        Keys(animation, Yaw, (22, 0), (54, 4), (72, 4), (110, 0));
        Keys(animation, Roll, (22, 0), (54, 1), (72, 1), (110, 0));
        Face(animation, 28, MiiExpression.Surprise);
        Face(animation, 40, MiiExpression.Normal);
        Blink(animation, 98);
        animation.AddEvent(28, "noticed");
        return Finish(animation);
    }

    private static MiiAnimation Clip(string name, int length)
    {
        var animation = new MiiAnimation { Name = name, Fps = 60, Length = length };
        foreach (var id in new[] { Pitch, Yaw, Roll })
            Keys(animation, id, (0, 0), (12, 0), (length - 12, 0), (length, 0));
        Face(animation, 0, MiiExpression.Normal);
        Face(animation, length, MiiExpression.Normal);
        return animation;
    }

    private static void Keys(MiiAnimation animation, TrackId id, params (int Frame, float Value)[] keys)
    {
        var curve = animation.GetOrCreateCurve(id);
        foreach (var (frame, value) in keys)
            curve.SetKey(frame, value, Interpolation.Smooth);
    }

    private static void Face(MiiAnimation animation, int frame, MiiExpression expression) =>
        animation.GetOrCreateCurve(TrackId.Expression).SetKey(frame, (float)expression, Interpolation.Hold);

    private static void Blink(MiiAnimation animation, int frame)
    {
        Face(animation, frame, MiiExpression.Blink);
        Face(animation, frame + 8, MiiExpression.Normal);
    }

    private static MiiAnimation Finish(MiiAnimation animation)
    {
        animation.AddEvent(animation.Length, "done");
        return animation;
    }
}
