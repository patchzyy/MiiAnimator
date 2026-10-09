using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// The podium's grand entrance, one single-Mii clip per step, all the same length so they're started together:
/// bronze drops onto its step first, then silver, then gold lands with a bigger thump and throws its arms up
/// in a burst of confetti while the other two turn and clap. Every clip ends exactly on its actor's first frame
/// of the <see cref="LeaderboardAnimations"/> scenes, so a scene can take over without a seam.
/// </summary>
public static class PodiumEntranceAnimations
{
    public const int Length = 252;

    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("leaderboard/enter/first/Podium_enter_first.miianim", () => Enter(0)),
        ("leaderboard/enter/second/Podium_enter_second.miianim", () => Enter(1)),
        ("leaderboard/enter/third/Podium_enter_third.miianim", () => Enter(2)),
    ];

    /// <summary>When each place drops in (third first): the frame it shows up above the stage, and the frame it lands.</summary>
    public static readonly (int Drop, int Land)[] Timing = [(58, 100), (28, 70), (4, 46)];

    /// <summary>Start this far above the step (canonical), above the top of the stage even for tall Miis.</summary>
    private const float DropHeight = 760f;

    private const int Cheer = 136;

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootZ = Bone(MiiBone.Root, Channel.PosZ);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestStretch = Bone(MiiBone.Chest, Channel.Stretch);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    public static MiiAnimation Enter(int place)
    {
        var name = place switch
        {
            0 => "Podium enter first",
            1 => "Podium enter second",
            _ => "Podium enter third",
        };
        var b = new AnimBuilder(name, Length, female: false);
        var platform = LeaderboardAnimations.Platforms[place];
        var stand = platform.Y - 2;
        var (drop, land) = Timing[place];

        // Out of sight until it drops.
        b.Keys(TrackId.Visible, Interpolation.Hold, (0, 0), (drop, 1));
        b.Keys(RootX, (0, platform.X), (Length, platform.X));
        b.Keys(RootZ, (0, platform.Z), (Length, platform.Z));

        // A gravity fall onto the step, a squash into the knees, a little rebound and settle.
        b.Key(RootY, 0, stand + DropHeight).Key(RootY, drop, stand + DropHeight);
        b.Arc(RootY, drop, stand + DropHeight, land, stand - (place == 0 ? 13 : 10), rising: false);
        b.Keys(RootY, (land + 7, stand + 2), (land + 14, stand - 1), (land + 22, stand), (Length, stand));
        b.Keys(ChestPitch, (0, 0), (drop, -6), (land - 4, -4), (land + 4, place == 0 ? 12 : 9), (land + 14, -2), (land + 24, 0));
        b.Keys(ChestStretch, (0, 1), (drop, 1.06f), (land - 2, 1.08f), (land + 3, 0.9f), (land + 10, 1.04f), (land + 18, 1), (Length, 1));
        b.Keys(HeadPitch, (0, 0), (drop, -12), (land - 2, -8), (land + 6, 8), (land + 16, -3), (land + 26, 0));

        // FK legs dangle and tuck in the air; IK plants the feet on the step from touchdown.
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
        {
            var foot = b.NaturalFoot(leg, platform.X, 0, platform.Z) + new Vector3(0, platform.Y, 0);
            b.Keys(Limb(leg, Channel.IkBlend), Interpolation.Hold, (0, 0), (land, 1));
            b.Key(Limb(leg, Channel.IkStretch), 0, 0);
            b.Foot(leg, 0, foot, 0).Foot(leg, Length, foot, 0);
        }
        foreach (var (upper, lower, kick) in new[] { (MiiBone.LegL1, MiiBone.LegL2, 1f), (MiiBone.LegR1, MiiBone.LegR2, -1f) })
        {
            b.Keys(Bone(upper, Channel.RotX), (0, -10), (drop, -10), (drop + 10, -26 * (kick > 0 ? 1 : 0.7f)), (land - 4, -14), (land, 0));
            b.Keys(Bone(lower, Channel.RotX), (0, 30), (drop, 30), (drop + 10, 48), (land - 4, 26), (land, 0));
            b.Keys(Bone(upper, Channel.RotZ), (0, kick * 6), (land - 4, kick * 3), (land, 0));
        }

        // Arms up for balance on the way down, out wide on the impact, then down.
        b.ArmsRest(0);
        b.Arms(drop, new Vector3(40, 124, 6), hand: new Vector3(0, 0, 20));
        b.Arms(land - 4, new Vector3(46, 116, 8), hand: new Vector3(0, 0, 16));
        b.Arms(land + 6, new Vector3(44, 82, 14));
        b.Arms(land + 16, new Vector3(34, 70, 18));
        b.Face(0, MiiExpression.Normal).Face(drop, MiiExpression.SurpriseOpenMouth).Face(land, MiiExpression.Blink).Face(land + 8, MiiExpression.Smile);

        Dust(b, platform, land, place == 0 ? 18 : 12, (uint)(5100 + place));
        if (place == 0)
            Celebrate(b, land);
        else
            Applaud(b, place, land);

        // End exactly on the scenes' first frame for this actor.
        var scene = LeaderboardAnimations.FirstPlaceHighFives().ForActor(place);
        foreach (var (id, curve) in scene.Tracks)
            if (curve.Count > 0)
                b.Animation.GetOrCreateCurve(id).SetKey(Length, scene.Evaluate(id, 0), ChannelInfo.IsDiscrete(id.Channel) ? Interpolation.Hold : Interpolation.Smooth);
        b.Face(Length, MiiExpression.Normal);
        b.Animation.AddEvent(drop, "drop");
        b.Animation.AddEvent(land, "landed");
        if (place == 0)
            b.Animation.AddEvent(Cheer, "cheer");
        b.Animation.AddEvent(Length, "done");
        return b.Animation;
    }

    /// <summary>Gold: settles from the landing, then throws both arms up in a V while the confetti flies.</summary>
    private static void Celebrate(AnimBuilder b, int land)
    {
        var up = Cheer;
        b.Arms(up - 6, new Vector3(30, 84, 20));
        b.Arms(up, new Vector3(50, 124, 12), hand: new Vector3(0, 0, 24), interpolation: Interpolation.Overshoot);
        b.Arms(up + 14, new Vector3(48, 128, 10), hand: new Vector3(0, 0, 20));
        b.Arms(up + 30, new Vector3(50, 124, 12), hand: new Vector3(0, 0, 24));
        b.Arms(up + 44, new Vector3(48, 128, 10), hand: new Vector3(0, 0, 20));
        b.Arms(up + 64, new Vector3(50, 122, 12), hand: new Vector3(0, 0, 22));
        b.Arms(up + 84, new Vector3(36, 92, 20));
        b.ArmsRest(up + 100);
        b.Keys(ChestPitch, (up - 8, 2), (up, -7), (up + 64, -6), (up + 92, 0));
        b.Keys(HeadPitch, (up - 8, 2), (up + 2, -12), (up + 64, -10), (up + 92, 0));
        b.Keys(HeadRoll, (up, 0), (up + 18, 6), (up + 36, -6), (up + 54, 5), (up + 76, 0));
        // A little bounce on the knees with the cheer.
        var stand = LeaderboardAnimations.Platforms[0].Y - 2;
        b.Keys(RootY, (up - 8, stand - 4), (up, stand + 2), (up + 16, stand - 2), (up + 32, stand + 1), (up + 48, stand - 2), (up + 64, stand), (up + 100, stand));
        b.Face(up - 4, MiiExpression.SmileOpenMouth).Face(up + 70, MiiExpression.LikeWinkLeft).Face(up + 96, MiiExpression.Smile);

        // Confetti bursts up from above gold's head and rains down over the whole podium.
        (Vector4 Color, uint Seed)[] colors =
        [
            (new Vector4(1f, 0.8f, 0.22f, 1), 5201),
            (new Vector4(0.33f, 0.91f, 0.74f, 1), 5202),
            (new Vector4(1f, 0.45f, 0.62f, 1), 5203),
            (new Vector4(0.45f, 0.66f, 1f, 1), 5204),
            (new Vector4(1f, 1f, 1f, 1), 5205),
        ];
        foreach (var (color, seed) in colors)
            b.Animation.Particles.Add(new ParticleEffect
            {
                Name = "Confetti", StartFrame = up, Duration = 8, Count = 18, Seed = seed,
                AttachBone = (byte)MiiBone.Head, Offset = new Vector3(0, 130, 10), SpawnRadius = new Vector3(20, 8, 10),
                Direction = Vector3.UnitY, Spread = 75, SpeedMin = 160, SpeedMax = 300, Gravity = -150, Drag = 2.4f,
                Shape = null, Orientation = ParticleOrientation.Spin, Aspect = 0.55f, SpinMin = -540, SpinMax = 540,
                LifeMin = 1.9f, LifeMax = 2.6f, SizeStart = 7, SizeEnd = 6,
                ColorStart = color, ColorEnd = color with { W = 0 },
            });
        var shine = ParticlePresets.Twinkles();
        shine.Name = "Winner shine";
        shine.StartFrame = up;
        shine.Duration = 40;
        shine.Count = 14;
        shine.Seed = 5206;
        shine.AttachBone = (byte)MiiBone.Chest;
        shine.Offset = new Vector3(0, 40, 14);
        shine.SpawnRadius = new Vector3(70, 60, 10);
        shine.SizeStart = 12;
        shine.SizeEnd = 2;
        shine.LifeMin = 0.4f;
        shine.LifeMax = 0.7f;
        shine.ColorStart = new Vector4(1f, 0.9f, 0.5f, 1);
        shine.ColorEnd = new Vector4(1f, 0.9f, 0.5f, 0);
        shine.Additive = true;
        b.Animation.Particles.Add(shine);
    }

    /// <summary>Silver and bronze: turn to the winner when it lands and clap for it.</summary>
    private static void Applaud(AnimBuilder b, int place, int land)
    {
        var goldLand = Timing[0].Land;
        // Silver stands left of gold and bronze right of it, so they look in opposite directions.
        var towardsGold = place == 1 ? 26f : -26f;
        b.ArmsRest(land + 26);
        b.Keys(HeadYaw, (0, 0), (land + 26, 0), (goldLand - 14, towardsGold * 0.4f), (goldLand + 4, towardsGold), (Cheer + 80, towardsGold), (Cheer + 104, 0));
        b.Keys(HeadPitch, (land + 26, 0), (goldLand + 4, -5), (Cheer + 80, -5), (Cheer + 104, 0));
        b.Face(goldLand, MiiExpression.Surprise).Face(goldLand + 12, MiiExpression.SmileOpenMouth).Face(Cheer + 84, MiiExpression.Smile);

        // Same clap as the scenes: gather outside the hips, clap at chest height, back out the safe way.
        var from = Cheer - 10 + place * 4;
        var end = Cheer + 66;
        b.ArmsRest(from).Arms(from + 8, new Vector3(34, 70, 22));
        for (var frame = from + 16; frame < end; frame += 12)
            b.Arms(frame, new Vector3((frame - from - 16) / 12 % 2 == 0 ? 17 : 9, 80, 24));
        b.Arms(end + 4, new Vector3(28, 78, 28)).Arms(end + 12, new Vector3(34, 70, 22)).ArmsRest(end + 28);
    }

    /// <summary>A puff of dust spreading over the step on touchdown.</summary>
    private static void Dust(AnimBuilder b, Vector3 platform, int land, int count, uint seed)
    {
        var dust = ParticlePresets.DustRing();
        dust.Name = "Step dust";
        dust.StartFrame = land;
        dust.Count = count;
        dust.Seed = seed;
        // Ground particles start at the floor under the Mii; lift them onto the step (particle units use the X scale).
        var scale = MiiAnim.Core.Evaluation.MiiStage.DefaultBodyScale;
        dust.Offset = new Vector3(0, platform.Y * scale.Y / scale.X + 2, 0);
        dust.SpawnRadius = new Vector3(18, 0, 14);
        dust.SpeedMin = 30;
        dust.SpeedMax = 55;
        dust.SizeStart = 10;
        dust.SizeEnd = 20;
        dust.LifeMin = 0.35f;
        dust.LifeMax = 0.55f;
        b.Animation.Particles.Add(dust);
    }
}
