using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Profile page animations with more than one Mii. Actor 0 is the Mii the page shows; extra actors are named roles
/// the app fills in (here "old" = the previously selected Mii).
/// </summary>
public static class ProfileAnimations
{
    public const string OldMiiRole = "old";

    public static readonly (string Name, Func<MiiAnimation> Create)[] All = [("Change_mii", ChangeMii)];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootPitch = Bone(MiiBone.Root, Channel.RotX);
    private static readonly TrackId RootStretch = Bone(MiiBone.Root, Channel.Stretch);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    /// <summary>Old Mii stands in the middle; the new one walks in from the right, they push palm to palm, and the
    /// new Mii shoves the old one out of the frame, dusts off its hands, walks to the middle and takes a bow.</summary>
    public const int ContactFrame = 90;
    public const int ShoveFrame = 113;
    public const int OldGoneFrame = 198;

    /// <summary>
    /// Change Mii on the profile page. Events: "push" when the shove lands, "old_gone" once the old Mii has left
    /// (the app can stop drawing it), "done" at the end (the new Mii is back in its rest pose).
    /// </summary>
    public static MiiAnimation ChangeMii()
    {
        const float facingLeft = -90f; // toward -X (screen left)
        const float facingRight = 90f;
        const float contactX = 111f; // palms touch: arms straight out from both Miis

        var main = new AnimBuilder("Change Mii", 280, female: false);
        var animation = main.Animation;
        var oldActor = animation.AddActor(OldMiiRole);
        var old = new AnimBuilder(oldActor.Motion, female: false);

        // ---------- New Mii (actor 0) ----------
        // Walk in from off screen, then one more step into the push.
        main.WalkX(0, 72, 262, 126, facingLeft, stepFrames: 18, hipDrop: 6f);
        main.WalkX(72, 90, 126, contactX, facingLeft, stepFrames: 18, armSwing: 0, hipDrop: 3f);
        main.Key(RootY, 0, -6.8f);

        // Hands up and forward for the push, palms out.
        main.Arms(72, new Vector3(30, 62, 6));
        main.Arms(84, new Vector3(14, 99, 44), hand: new Vector3(-35, 0, 0));
        main.Arms(90, new Vector3(14, 99, 45), hand: new Vector3(-40, 0, 0));
        // Struggle: lean into it, inching forward with the feet planted, then the big shove with a step.
        main.Keys(RootX, (90, contactX), (108, contactX - 8));
        main.Keys(RootPitch, (0, 0), (84, 0), (92, 4), (108, 5), (113, 6), (124, 3), (138, 0));
        main.Keys(ChestPitch, (0, 0), (84, 0), (92, 3), (108, 4), (113, 4), (124, 0), (150, 0));
        main.Keys(HeadPitch, (0, 0), (84, 0), (100, -6), (113, -2), (130, 0));
        main.Arms(108, new Vector3(14, 98, 44), hand: new Vector3(-40, 0, 0));
        main.Arms(114, new Vector3(14, 101, 46), hand: new Vector3(-30, 0, 0), interpolation: Interpolation.Overshoot);
        // Feet stayed planted through the struggle; the shove is one big step with each foot.
        main.Key(RootX, 108, contactX - 8, Interpolation.EaseOut).Key(RootX, 122, 86);
        foreach (var (leg, start) in new[] { (MiiLimb.LegL, 108), (MiiLimb.LegR, 113) })
        {
            var from = main.NaturalFoot(leg, contactX, facingLeft);
            var to = main.NaturalFoot(leg, 86, facingLeft);
            main.Foot(leg, start, from, facingLeft);
            main.Foot(leg, start + 5, Vector3.Lerp(from, to, 0.5f) + new Vector3(0, 5, 0), facingLeft);
            main.Foot(leg, start + 9, to, facingLeft);
        }

        main.Keys(RootY, (108, -3), (114, -4.5f), (122, -3), (136, 0));

        // Dust off the hands with a few claps, looking pleased.
        main.Arms(130, new Vector3(28, 66, 10));
        main.Arms(140, new Vector3(16, 80, 20));
        main.Arms(145, new Vector3(8.5f, 80, 22));
        main.Arms(150, new Vector3(16, 81, 20));
        main.Arms(155, new Vector3(8.5f, 81, 22));
        main.Arms(162, new Vector3(22, 70, 14));
        main.ArmsRest(172);
        main.Keys(HeadYaw, (0, 0), (136, 0), (144, 18), (158, 18), (168, 0));
        main.Keys(HeadRoll, (0, 0), (140, 0), (146, -6), (152, 6), (160, 0));

        // Walk to the middle, turn to the camera and take a bow.
        main.WalkX(170, 206, 86, 0, facingLeft, stepFrames: 18, armSwing: 10);
        main.TurnInPlace(206, 222, 0, facingLeft, 0);
        main.Keys(RootY, (206, -2), (222, 0), (236, -3), (248, 0), (280, 0));
        main.Keys(RootPitch, (222, 0), (238, 18), (250, 0), (280, 0));
        main.Keys(ChestPitch, (222, 0), (238, 14), (250, -3), (264, 0), (280, 0));
        main.Keys(HeadPitch, (222, 0), (238, 10), (250, -4), (264, 0), (280, 0));
        main.Arms(230, new Vector3(30, 70, 14));
        main.Arms(238, new Vector3(18, 76, 24));
        main.Arms(252, new Vector3(44, 96, 16), hand: new Vector3(0, 0, 18));
        main.ArmsRest(270);
        main.ArmsRest(280);
        main.Keys(RootStretch, (0, 1), (248, 1), (252, 1.04f), (258, 0.98f), (264, 1));

        main.Face(0, MiiExpression.Normal);
        main.Face(86, MiiExpression.Anger);
        main.Face(110, MiiExpression.AngerOpenMouth);
        main.Face(122, MiiExpression.Smile);
        main.Face(144, MiiExpression.WinkLeft);
        main.Face(160, MiiExpression.Smile);
        main.Face(180, MiiExpression.Normal);
        main.Face(232, MiiExpression.Blink);
        main.Face(240, MiiExpression.SmileOpenMouth);
        main.Face(262, MiiExpression.Normal);
        main.Face(280, MiiExpression.Normal);

        // ---------- Old Mii (actor "old") ----------
        old.FeetAt(0, 0, 0);
        old.Keys(Bone(MiiBone.Chest, Channel.ScaleZ), (0, 1), (20, 1.015f), (40, 1));
        // Hears footsteps, looks over, turns to face the newcomer and puts its hands up.
        old.Keys(HeadYaw, (0, 0), (26, 0), (40, 45), (48, 45), (62, 0));
        old.Keys(HeadPitch, (0, 0), (40, -4), (62, 0));
        old.TurnInPlace(44, 62, 0, 0, facingRight);
        old.Arms(0, new Vector3(27.6f, 58.5f, 0));
        old.Arms(58, new Vector3(30, 66, 10));
        old.Arms(74, new Vector3(14, 99, 44), hand: new Vector3(-40, 0, 0), interpolation: Interpolation.Overshoot);
        old.Arms(108, new Vector3(14, 98, 44), hand: new Vector3(-40, 0, 0));

        // Pushed back a little, leaning against it, feet scraping back.
        old.Keys(RootX, (0, 0), (ContactFrame, 0), (108, -8), (ShoveFrame, -10));
        old.Keys(RootPitch, (0, 0), (ContactFrame, 0), (100, -6), (ShoveFrame, -11));
        old.Keys(RootY, (0, 0), (ContactFrame, 0), (100, -3), (ShoveFrame, -3));
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
        {
            old.Foot(leg, ContactFrame, old.NaturalFoot(leg, 0, facingRight), facingRight);
            old.Foot(leg, ShoveFrame, old.NaturalFoot(leg, -10, facingRight), facingRight);
        }

        // The shove: flies backwards in an arc, arms flailing, and lands with a squash.
        old.Arc(RootX, ShoveFrame, -10, 136, -122, rising: true);
        old.Arc(RootY, ShoveFrame, -3, 124, 24, rising: true);
        old.Arc(RootY, 124, 24, 136, 0, rising: false);
        old.Keys(RootPitch, (124, -24), (136, -6), (146, 0));
        old.Arms(117, new Vector3(46, 104, 24));
        old.Arms(124, new Vector3(54, 116, 4), interpolation: Interpolation.Overshoot);
        old.Arms(130, new Vector3(50, 112, 10));
        old.Arms(138, new Vector3(44, 84, 4));
        old.Keys(RootStretch, (0, 1), (ShoveFrame, 1), (118, 1.06f), (134, 1), (138, 0.86f), (146, 1.04f), (152, 1));
        // Feet ride along under the flying body (legs dangling, one a little higher), landing at -122.
        for (var frame = ShoveFrame + 2; frame < 136; frame += 3)
        {
            var bodyX = old.Animation.Evaluate(RootX, frame);
            var lifted = MathF.Max(0f, old.Animation.Evaluate(RootY, frame) + 3f);
            var t = (frame - ShoveFrame) / (float)(136 - ShoveFrame);
            var tuck = MathF.Sin(t * MathF.PI) * 6f;
            foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            {
                var dangle = new Vector3(0, lifted + tuck * (leg == MiiLimb.LegL ? 1.6f : 0.6f), 0);
                old.Foot(leg, frame, old.NaturalFoot(leg, bodyX, facingRight) + dangle, facingRight);
            }
        }

        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            old.Foot(leg, 136, old.NaturalFoot(leg, -122, facingRight), facingRight);


        // Spins round dizzily and runs off to the left, then is hidden.
        old.TurnInPlace(140, 152, -122, facingRight, facingLeft);
        old.WalkX(152, 196, -122, -340, facingLeft, stepFrames: 7, lift: 10f, armSwing: 40f, hipDrop: 6f);
        old.Key(TrackId.Visible, 0, 1).Key(TrackId.Visible, OldGoneFrame, 0);

        old.Face(0, MiiExpression.Normal);
        old.Face(38, MiiExpression.Surprise);
        old.Face(64, MiiExpression.SurpriseOpenMouth);
        old.Face(92, MiiExpression.Frustrated);
        old.Face(ShoveFrame, MiiExpression.SurpriseOpenMouth);
        old.Face(138, MiiExpression.BlinkOpenMouth);
        old.Face(146, MiiExpression.SorrowOpenMouth);

        // ---------- Particles ----------
        var clash = ParticlePresets.Twinkles();
        clash.Name = "Clash";
        clash.StartFrame = ShoveFrame;
        clash.Duration = 0;
        clash.Count = 14;
        clash.Actor = 0;
        clash.AttachBone = (byte)MiiBone.HandL;
        clash.Offset = new Vector3(-8, 0, 0);
        clash.SpawnRadius = new Vector3(4);
        clash.Direction = Vector3.Zero;
        clash.Spread = 0;
        clash.SpeedMin = 70;
        clash.SpeedMax = 160;
        clash.Drag = 4;
        clash.LifeMin = 0.3f;
        clash.LifeMax = 0.5f;
        clash.SizeStart = 16;
        clash.SizeEnd = 4;
        clash.Seed = 113;
        animation.Particles.Add(clash);

        var landing = ParticlePresets.DustRing();
        landing.Name = "Landing dust";
        landing.StartFrame = 136;
        landing.Actor = 1;
        landing.Seed = 136;
        animation.Particles.Add(landing);

        var trail = ParticlePresets.DustRing();
        trail.Name = "Run-away dust";
        trail.StartFrame = 154;
        trail.Duration = 40;
        trail.Count = 20;
        trail.Actor = 1;
        trail.SpawnShell = 1f;
        trail.SpawnRadius = new Vector3(6);
        trail.Offset = new Vector3(0, 3, 0);
        trail.Direction = new Vector3(1, 0.6f, 0);
        trail.Spread = 35;
        trail.SpeedMin = 30;
        trail.SpeedMax = 70;
        trail.SizeStart = 10;
        trail.SizeEnd = 18;
        trail.LifeMin = 0.35f;
        trail.LifeMax = 0.6f;
        trail.Seed = 154;
        animation.Particles.Add(trail);

        animation.AddEvent(ShoveFrame, "push");
        animation.AddEvent(OldGoneFrame, "old_gone");
        animation.AddEvent(animation.Length, "done");
        return animation;
    }
}
