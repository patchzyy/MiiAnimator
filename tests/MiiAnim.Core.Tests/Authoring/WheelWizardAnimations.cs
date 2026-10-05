using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Hand-authored animations for WheelWizard's Mii screens. All units are canonical rig units (rest shoulders at
/// y ≈ 103, wrists at ±27.6/58.5, head origin at y ≈ 108; the FFL head is ~45 wide either side and ~110 tall).
/// </summary>
public static class WheelWizardAnimations
{
    public static readonly (string Name, bool Female, Func<MiiAnimation> Create)[] All =
    [
        ("Appear", false, Appear),
        ("Become_girl", true, BecomeGirl),
        ("Become_boy", false, BecomeBoy),
    ];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootTurn = Bone(MiiBone.Root, Channel.RotY);
    private static readonly TrackId RootLean = Bone(MiiBone.Root, Channel.RotZ);
    private static readonly TrackId RootStretch = Bone(MiiBone.Root, Channel.Stretch);

    /// <summary>
    /// Shown when switching Miis: launches out of a hole in the ground, does a tucked 360 in the air,
    /// lands with a squash and presents itself with a big "ta-da".
    /// </summary>
    public static MiiAnimation Appear()
    {
        var b = new AnimBuilder("Appear", 150, female: false);

        // Fly with FK legs (IK feet are pinned to the ground); hand back to IK on touchdown, where both poses match.
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            b.Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 0), (60, 1));

        // Body: hidden underground (even short Miis with tall hair), launch, gravity arc, landing squash.
        b.Key(RootY, 0, -340, Interpolation.Linear);
        b.Arc(RootY, 10, -340, 46, 60, rising: true);
        b.Arc(RootY, 46, 60, 60, 0, rising: false);
        b.Key(RootY, 60, 0, Interpolation.EaseOut).Keys(RootY, (65, -11), (76, 1.5f), (86, 0), (150, 0));

        b.Keys(RootStretch, (0, 1.22f), (12, 1.25f), (32, 1.12f), (42, 1f), (47, 0.94f), (55, 1.08f), (60, 1.04f));
        b.Key(RootStretch, 60, 1.04f, Interpolation.EaseOut).Keys(RootStretch, (64, 0.78f), (73, 1.07f), (82, 0.98f), (90, 1f), (150, 1f));

        // The 360: starts once the whole body is out of the hole, done just before touchdown.
        b.Keys(RootTurn, (0, 0), (29, 0), (58, 360), (150, 360));
        b.Keys(Bone(MiiBone.Root, Channel.RotX), (0, 0), (36, 0), (46, 12), (56, 0), (150, 0));

        // Spine and head: look up while rising, curl into the tuck, absorb the landing, then stand proud.
        b.Keys(Bone(MiiBone.Chest, Channel.RotX), (0, -6), (32, -5), (46, 12), (56, 0), (61, 0), (65, 14), (76, -8), (112, -7), (132, 0), (150, 0));
        b.Keys(Bone(MiiBone.Head, Channel.RotX), (0, -14), (32, -10), (46, 8), (56, 0), (62, 0), (67, 10), (78, -7), (112, -6), (130, 0), (150, 0));
        b.Keys(Bone(MiiBone.Head, Channel.RotZ), (0, 0), (74, 0), (86, 8), (112, 7), (130, 0), (150, 0));

        // Legs: straight and streamlined, cannonball tuck at the top, straighten for the landing.
        foreach (var (thigh, shin) in new[] { (MiiBone.LegL1, MiiBone.LegL2), (MiiBone.LegR1, MiiBone.LegR2) })
        {
            b.Keys(Bone(thigh, Channel.RotX), (0, 0), (32, 0), (43, -75), (50, -72), (57, 0), (150, 0));
            b.Keys(Bone(shin, Channel.RotX), (0, 0), (32, 0), (43, 105), (50, 100), (57, 0), (150, 0));
        }

        // Arms: tucked back for the launch, fling out, hug the shins in the tuck, open for balance,
        // follow through on impact and snap up into the presentation pose.
        b.Arms(0, new Vector3(19, 60, -10));
        b.Arms(24, new Vector3(20, 60, -9));
        b.Arms(35, new Vector3(56, 102, 6));
        b.Arms(44, new Vector3(13, 64, 30), swivel: -20);
        b.Arms(50, new Vector3(13, 65, 29), swivel: -20);
        b.Arms(55, new Vector3(56, 104, 4));
        b.Arms(60, new Vector3(52, 92, 6));
        b.Arms(66, new Vector3(40, 76, 10), interpolation: Interpolation.Overshoot);
        b.Arms(80, new Vector3(52, 118, 14), hand: new Vector3(0, 0, 20));
        b.Arms(112, new Vector3(51, 116, 15), hand: new Vector3(0, 0, 16));
        b.ArmsRest(136);
        b.ArmsRest(150);

        // Faces: the Mii's own eyes throughout, only swapped for the spin, the impact and the "ta-da".
        b.Face(0, MiiExpression.OpenMouth);
        b.Face(42, MiiExpression.BlinkOpenMouth);
        b.Face(55, MiiExpression.OpenMouth);
        b.Face(61, MiiExpression.Frustrated);
        b.Face(67, MiiExpression.SmileOpenMouth);
        b.Face(98, MiiExpression.LikeWinkLeft);
        b.Face(112, MiiExpression.OpenMouth);
        b.Face(124, MiiExpression.Normal);

        b.Animation.AddEvent(10, "launch");
        b.Animation.AddEvent(32, "clear_of_hole");
        b.Animation.AddEvent(60, "landed");
        b.Animation.AddEvent(72, "present");
        b.Animation.AddEvent(150, "done");
        return b.Animation;
    }

    /// <summary>
    /// Switching to a girl: a hop twirl is the transformation (swap_gender fires mid-air, back to the camera),
    /// then a shy hands-behind-the-back twist with a heel pop, and a hand-on-hip wave with a cheeky wink.
    /// </summary>
    public static MiiAnimation BecomeGirl()
    {
        var b = new AnimBuilder("Become girl", 190, female: true);
        var footR = MiiLimb.LegR;

        // Hop twirl: crouch, push off, FK legs in the air with both heels kicked up, land on IK again.
        b.Keys(RootY, (0, 0), (8, -7));
        b.Key(RootY, 8, -7, Interpolation.EaseIn).Key(RootY, 12, 0);
        b.Arc(RootY, 12, 0, 24, 16, rising: true);
        b.Arc(RootY, 24, 16, 36, 0, rising: false);
        b.Key(RootY, 36, 0, Interpolation.EaseOut).Keys(RootY, (40, -5), (48, 0.5f), (56, 0), (190, 0));
        b.Keys(RootStretch, (0, 1), (8, 0.94f), (14, 1.06f), (24, 1), (34, 1.04f), (39, 0.92f), (48, 1), (190, 1));
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            b.Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 1), (12, 0), (36, 1));
        foreach (var (thigh, shin) in new[] { (MiiBone.LegL1, MiiBone.LegL2), (MiiBone.LegR1, MiiBone.LegR2) })
        {
            b.Keys(Bone(thigh, Channel.RotX), (0, 0), (12, 0), (19, -8), (28, -6), (35, 0), (190, 0));
            b.Keys(Bone(shin, Channel.RotX), (0, 0), (12, 0), (19, 80), (27, 80), (35, 0), (190, 0));
        }

        // The spin, then a shy side-to-side twist around the new front (360°).
        b.Keys(RootTurn, (0, 0), (12, 0), (36, 360), (52, 360), (62, 371), (74, 349), (86, 371), (98, 349), (108, 360), (190, 360));

        // Ballerina arms for the twirl, then hands clasped behind the back.
        b.ArmsRest(0);
        b.Arms(6, new Vector3(30, 80, 12));
        b.Arm(MiiLimb.ArmL, 16, new Vector3(50, 116, 12), hand: new Vector3(0, 0, 15));
        b.Arm(MiiLimb.ArmR, 16, new Vector3(-52, 92, 10));
        b.Arm(MiiLimb.ArmL, 32, new Vector3(50, 116, 12), hand: new Vector3(0, 0, 15));
        b.Arm(MiiLimb.ArmR, 32, new Vector3(-52, 92, 10));
        b.Arms(48, new Vector3(8, 72, -22), interpolation: Interpolation.Overshoot);
        b.Arms(100, new Vector3(8, 72, -22));
        b.Arm(MiiLimb.ArmL, 105, new Vector3(38, 68, -6), swivel: -20);
        b.Arm(MiiLimb.ArmR, 106, new Vector3(-38, 70, -4), swivel: 20);

        // Shy: heel pop and knees in, head tilted and looking down a little, then up with a smile.
        b.Keys(Limb(footR, Channel.TargetY), (0, 0), (44, 0), (54, 7), (100, 7), (108, 0), (112, 0), (120, 7), (150, 7), (160, 0), (190, 0));
        b.Keys(Limb(footR, Channel.TargetZ), (0, 0), (44, 0), (54, -7), (100, -7), (108, 0), (112, 0), (120, -7), (150, -7), (160, 0), (190, 0));
        b.Keys(Limb(footR, Channel.TargetRotX), (0, 0), (44, 0), (54, 32), (100, 32), (108, 0), (112, 0), (120, 32), (150, 32), (160, 0), (190, 0));
        b.Keys(Limb(footR, Channel.Swivel), (0, 0), (44, 0), (54, 25), (100, 25), (108, 0), (112, 0), (120, 25), (150, 25), (160, 0), (190, 0));
        b.Keys(Limb(MiiLimb.LegL, Channel.Swivel), (0, 0), (44, 0), (54, -12), (100, -12), (108, 0), (190, 0));
        b.Keys(Bone(MiiBone.Head, Channel.RotZ), (0, 0), (44, 0), (56, 12), (68, 7), (80, 12), (92, 7), (104, 10), (112, 0), (120, -12), (150, -12), (168, 0), (190, 0));
        b.Keys(Bone(MiiBone.Head, Channel.RotX), (0, 0), (8, 6), (16, -8), (36, 0), (48, 0), (56, 9), (80, 9), (90, -3), (104, -3), (112, 0), (190, 0));
        b.Keys(Bone(MiiBone.Chest, Channel.RotX), (0, 0), (8, 6), (16, -6), (36, 0), (40, 6), (48, 4), (100, 4), (112, 0), (120, -4), (150, -4), (168, 0), (190, 0));

        // Finish: left hand on the hip with a hip pop, right hand waving by the face, cheeky wink.
        b.Keys(RootX, (0, 0), (108, 0), (118, 3.5f), (150, 3.5f), (168, 0), (190, 0));
        b.Keys(RootLean, (0, 0), (108, 0), (118, -3), (150, -3), (168, 0), (190, 0));
        b.Keys(Bone(MiiBone.Hip, Channel.RotZ), (0, 0), (108, 0), (118, 7), (150, 7), (168, 0), (190, 0));
        b.Arm(MiiLimb.ArmL, 110, new Vector3(37, 72, 6), swivel: -30);
        b.Arm(MiiLimb.ArmL, 116, new Vector3(24, 76, -2), swivel: -60, hand: new Vector3(0, 40, -50));
        b.Arm(MiiLimb.ArmL, 150, new Vector3(24, 76, -2), swivel: -60, hand: new Vector3(0, 40, -50));
        b.Arm(MiiLimb.ArmL, 160, new Vector3(37, 72, 8), swivel: -30);
        b.Arm(MiiLimb.ArmR, 116, new Vector3(-46, 122, 16), hand: new Vector3(0, 0, -15), interpolation: Interpolation.Overshoot);
        b.Arm(MiiLimb.ArmR, 150, new Vector3(-46, 122, 16), hand: new Vector3(0, 0, -15));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (116, 0), (123, -25), (130, 25), (137, -25), (144, 25), (150, 0));
        b.ArmsRest(172);
        b.ArmsRest(190);

        // Faces: eyes closed and mouth open for the twirl, the Mii's own face for the shy part with a quick
        // blink and a shy smile, a cheeky wink to finish.
        b.Face(0, MiiExpression.Normal);
        b.Face(14, MiiExpression.BlinkOpenMouth);
        b.Face(36, MiiExpression.OpenMouth);
        b.Face(46, MiiExpression.Normal);
        b.Face(66, MiiExpression.Blink);
        b.Face(71, MiiExpression.Normal);
        b.Face(88, MiiExpression.Smile);
        b.Face(104, MiiExpression.Normal);
        b.Face(118, MiiExpression.LikeWinkLeft);
        b.Face(150, MiiExpression.Normal);

        // Swap while she's in the air with her back to the camera (the spin is at 180° on frame 24).
        b.Animation.AddEvent(12, "jump");
        b.Animation.AddEvent(24, "swap_gender");
        b.Animation.AddEvent(36, "landed");
        b.Animation.AddEvent(190, "done");
        return b.Animation;
    }

    /// <summary>
    /// Switching to a boy: gathers strength, stomps into a wide stance, hits a double biceps pose with a few
    /// pumps, shows off the right bicep, then stands like a superhero.
    /// </summary>
    public static MiiAnimation BecomeBoy()
    {
        var b = new AnimBuilder("Become boy", 230, female: false);

        // Gather: crouch and pull the fists in while the right foot lifts for the stomp.
        b.Keys(RootY, (0, 0), (16, -7), (22, -8), (25, -2), (30, 1), (36, 0), (44, -2.5f), (52, 0), (60, -2.5f), (70, 0), (176, 0), (196, -2), (206, 0), (230, 0));
        b.Key(RootY, 22, -8, Interpolation.EaseIn);
        b.Keys(RootStretch, (0, 1), (16, 0.95f), (22, 0.94f), (28, 1.06f), (38, 1), (230, 1));
        b.Keys(Bone(MiiBone.Chest, Channel.RotX), (0, 0), (16, 14), (22, 12), (30, -8), (80, -8), (96, -4), (130, -4), (142, -9), (176, -9), (206, 0), (230, 0));
        b.Keys(Bone(MiiBone.Head, Channel.RotX), (0, 0), (16, 8), (24, 6), (32, -6), (80, -4), (130, -2), (142, -8), (150, 4), (158, -8), (176, -8), (206, 0), (230, 0));

        // Chest puff.
        foreach (var channel in new[] { Channel.ScaleX, Channel.ScaleZ })
            b.Keys(Bone(MiiBone.Chest, channel), (0, 1), (22, 0.96f), (32, 1.14f), (44, 1.18f), (52, 1.12f), (60, 1.18f), (70, 1.12f), (176, 1.12f), (206, 1), (230, 1));

        // Stomp out into a wide stance (right foot), then step back in at the end.
        var footR = MiiLimb.LegR;
        b.Keys(Limb(footR, Channel.TargetY), (0, 0), (6, 0), (16, 11), (21, 0), (186, 0), (194, 9), (202, 0), (230, 0));
        b.Key(Limb(footR, Channel.TargetY), 16, 11, Interpolation.EaseIn);
        b.Key(Limb(footR, Channel.TargetY), 194, 9, Interpolation.EaseIn);
        b.Keys(Limb(footR, Channel.TargetX), (0, 0), (6, 0), (19, -7), (188, -7), (200, 0), (230, 0));
        b.Keys(Limb(MiiLimb.LegL, Channel.TargetX), (0, 0), (21, 0), (25, 1.5f), (188, 1.5f), (200, 0), (230, 0));
        b.Keys(Limb(MiiLimb.LegL, Channel.Swivel), (0, 0), (20, 0), (28, -10), (200, -10), (210, 0), (230, 0));
        b.Keys(Limb(footR, Channel.Swivel), (0, 0), (20, 0), (28, 10), (200, 10), (210, 0), (230, 0));

        // Fists.
        foreach (var hand in new[] { MiiBone.HandL, MiiBone.HandR })
            b.Keys(Bone(hand, Channel.Stretch), (0, 1), (12, 0.7f), (200, 0.7f), (212, 1), (230, 1));

        // Double biceps with two pumps (the upper arms squash = the muscles bulge).
        b.ArmsRest(0);
        b.Arms(16, new Vector3(16, 74, 16), swivel: 30);
        b.Arms(22, new Vector3(17, 73, 15), swivel: 30, interpolation: Interpolation.Snappy);
        b.Arms(34, new Vector3(47, 124, 9), hand: new Vector3(0, 0, 25));
        b.Arms(80, new Vector3(47, 124, 9), hand: new Vector3(0, 0, 25));
        foreach (var upper in new[] { MiiBone.ArmL1, MiiBone.ArmR1 })
            b.Keys(Bone(upper, Channel.Stretch), (0, 1), (36, 1), (44, 0.76f), (52, 1), (60, 0.72f), (70, 1), (96, 1), (230, 1));

        // Show off the right bicep: turn it to the camera, admire it, wink.
        b.Keys(RootTurn, (0, 0), (84, 0), (96, 24), (130, 24), (142, 0), (230, 0));
        b.Keys(Bone(MiiBone.Head, Channel.RotY), (0, 0), (84, 0), (96, -48), (112, -48), (118, -20), (130, -20), (142, 0), (230, 0));
        b.Arm(MiiLimb.ArmR, 96, new Vector3(-45, 128, 14), hand: new Vector3(0, 0, -25));
        b.Arm(MiiLimb.ArmR, 130, new Vector3(-45, 128, 14), hand: new Vector3(0, 0, -25));
        b.Keys(Bone(MiiBone.ArmR1, Channel.Stretch), (96, 1), (102, 0.72f), (108, 1), (114, 0.72f), (122, 1));

        // Fists on the hips, superhero stance.
        b.Arm(MiiLimb.ArmL, 96, new Vector3(22, 76, 4), swivel: -60, hand: new Vector3(0, 35, -40));
        b.Arms(144, new Vector3(22, 76, 4), swivel: -60, hand: new Vector3(0, 35, -40));
        b.Arms(178, new Vector3(22, 76, 4), swivel: -60, hand: new Vector3(0, 35, -40));
        b.Arms(192, new Vector3(37, 72, 9), swivel: -30);
        b.ArmsRest(206);
        b.ArmsRest(230);

        // Faces: the Mii's own face is the base; strain on the gather and the pumps, a roar on the transformation.
        b.Face(0, MiiExpression.Normal);
        b.Face(8, MiiExpression.Frustrated);
        b.Face(25, MiiExpression.AngerOpenMouth);
        b.Face(36, MiiExpression.Normal);
        b.Face(42, MiiExpression.Frustrated);
        b.Face(50, MiiExpression.Normal);
        b.Face(58, MiiExpression.Frustrated);
        b.Face(68, MiiExpression.OpenMouth);
        b.Face(80, MiiExpression.Normal);
        b.Face(116, MiiExpression.LikeWinkRight);
        b.Face(132, MiiExpression.Normal);
        b.Face(146, MiiExpression.OpenMouth);
        b.Face(156, MiiExpression.Normal);

        // The swap happens on the "POW" into the double biceps, hidden by the burst of motion.
        b.Animation.AddEvent(16, "stomp");
        b.Animation.AddEvent(25, "swap_gender");
        b.Animation.AddEvent(230, "done");
        return b.Animation;
    }
}
