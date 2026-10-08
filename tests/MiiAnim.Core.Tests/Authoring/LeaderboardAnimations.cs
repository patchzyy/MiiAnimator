using System.Numerics;
using System.Text.Json;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>Three ensemble podium loops. Actor 0 is always first; extra roles are always second and third.</summary>
public static class LeaderboardAnimations
{
    public const string SecondRole = "second";
    public const string ThirdRole = "third";
    public const string PodiumFile = "podium.glb";
    public const int FirstSilverContact = 432;
    public const int FirstBronzeContact = 792;

    public static readonly Vector3[] Platforms = [new(0, 70, 0), new(-132, 46, 0), new(132, 28, 0)];
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("leaderboard/first_place/First_place_high_fives.miianim", FirstPlaceHighFives),
        ("leaderboard/second_place/Second_place_conductor.miianim", SecondPlaceConductor),
        ("leaderboard/third_place/Third_place_copycat.miianim", ThirdPlaceCopycat),
    ];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);

    /// <summary>The winner offers the left neighbour a high five, waits, then offers the right neighbour a low five.</summary>
    public static MiiAnimation FirstPlaceHighFives()
    {
        var (scene, actors) = Ensemble("First place: high fives", 1440, [(0, 360), (540, 720), (900, 1440)]);
        var gold = actors[0];
        var silver = actors[1];
        var bronze = actors[2];

        // Silver is higher than bronze, so the winner deliberately lowers the second offered palm.
        HighFive(gold, silver, MiiLimb.ArmR, MiiLimb.ArmL, 360, FirstSilverContact, 516,
            goldX: -12, neighbourX: -120, new Vector3(-66, 166, 8));
        Look(gold, 360, 414, 486, 540, -24);
        Look(silver, 372, 414, 486, 540, 24);
        Look(bronze, 390, 420, 492, 540, -24);
        bronze.Face(410, MiiExpression.Smile).Face(454, MiiExpression.SmileOpenMouth).Face(528, MiiExpression.Normal);
        Nod(bronze, 432, 446, 462, 6);

        HighFive(gold, bronze, MiiLimb.ArmL, MiiLimb.ArmR, 720, FirstBronzeContact, 876,
            goldX: 12, neighbourX: 120, new Vector3(66, 150, 8));
        Look(gold, 720, 768, 846, 900, 24);
        Look(bronze, 732, 768, 846, 900, -24);
        Look(silver, 738, 776, 850, 900, 28);
        silver.Face(768, MiiExpression.Smile).Face(808, MiiExpression.WinkLeft).Face(884, MiiExpression.Normal);
        Nod(silver, 792, 806, 822, 6);
        scene.AddEvent(FirstSilverContact, "silver_high_five");
        scene.AddEvent(FirstBronzeContact, "bronze_low_five");
        scene.AddEvent(900, "idle");
        return scene;
    }

    /// <summary>Silver mischievously conducts ceremonial bows, then gets caught having to take a bow too.</summary>
    public static MiiAnimation SecondPlaceConductor()
    {
        var (scene, actors) = Ensemble("Second place: conductor", 1560, [(0, 360), (660, 900), (1200, 1560)]);
        var gold = actors[0];
        var silver = actors[1];
        var bronze = actors[2];

        // An open palm cues gold. Bronze sees the joke, copies the cue, and laughs at the over-polite bow.
        Look(silver, 360, 394, 478, 520, 30);
        Gesture(silver, MiiLimb.ArmL, 360, 400, 452, 496, new Vector3(51, 111, 12), hand: new Vector3(0, 0, -12));
        silver.Face(388, MiiExpression.WinkLeft).Face(416, MiiExpression.Smile).Face(512, MiiExpression.Normal);
        Look(gold, 386, 420, 504, 540, -24);
        Bow(gold, 430, 466, 512, 12);
        gold.Face(432, MiiExpression.Smile).Face(480, MiiExpression.Blink).Face(516, MiiExpression.Normal);
        Look(bronze, 394, 424, 512, 548, -27);
        Gesture(bronze, MiiLimb.ArmR, 412, 450, 478, 528, new Vector3(-46, 93, 27));
        bronze.Face(448, MiiExpression.SmileOpenMouth).Face(536, MiiExpression.Normal);

        // Silver cues bronze, who returns a much deeper bow; gold and silver react together.
        Look(silver, 522, 550, 606, 660, 34);
        Gesture(silver, MiiLimb.ArmL, 520, 550, 578, 620, new Vector3(50, 105, 20));
        Look(bronze, 528, 558, 620, 660, -26);
        Bow(bronze, 562, 594, 638, 17);
        bronze.Face(564, MiiExpression.Blink).Face(614, MiiExpression.SmileOpenMouth).Face(656, MiiExpression.Normal);
        Look(gold, 534, 566, 622, 660, 26);
        Nod(gold, 580, 598, 620, 7);
        silver.Face(590, MiiExpression.Surprise).Face(614, MiiExpression.SmileOpenMouth).Face(652, MiiExpression.Normal);
        gold.Face(582, MiiExpression.Smile).Face(654, MiiExpression.Normal);

        // After another quiet stretch, both neighbours gesture back: "your turn". Silver sheepishly obliges.
        Look(gold, 900, 942, 1080, 1200, -24);
        Look(bronze, 914, 952, 1080, 1200, -30);
        Gesture(gold, MiiLimb.ArmR, 912, 958, 1010, 1050, new Vector3(-46, 92, 24));
        Gesture(bronze, MiiLimb.ArmR, 926, 972, 1010, 1058, new Vector3(-46, 94, 24));
        silver.Face(962, MiiExpression.Surprise).Face(1002, MiiExpression.Frustrated).Face(1040, MiiExpression.Smile);
        Look(silver, 910, 956, 994, 1020, 30);
        Bow(silver, 1020, 1070, 1132, 14);
        Clap(gold, 1048, 1112);
        Clap(bronze, 1062, 1126);
        gold.Face(1038, MiiExpression.SmileOpenMouth).Face(1184, MiiExpression.Normal);
        bronze.Face(1048, MiiExpression.SmileOpenMouth).Face(1184, MiiExpression.Normal);
        silver.Face(1140, MiiExpression.WinkRight).Face(1184, MiiExpression.Normal);
        scene.AddEvent(466, "gold_bow");
        scene.AddEvent(594, "bronze_bow");
        scene.AddEvent(1070, "silver_bow");
        scene.AddEvent(1200, "idle");
        return scene;
    }

    /// <summary>Bronze invents a tiny dance; gold and silver copy with delayed reactions, then get caught mid-pose.</summary>
    public static MiiAnimation ThirdPlaceCopycat()
    {
        var (scene, actors) = Ensemble("Third place: copycat", 1680, [(0, 360), (690, 960), (1290, 1680)]);
        var gold = actors[0];
        var silver = actors[1];
        var bronze = actors[2];

        // Bronze checks both neighbours, then sends a shoulder wiggle across the podium.
        Look(bronze, 360, 398, 642, 690, -27);
        Look(gold, 376, 424, 642, 690, 27);
        Look(silver, 396, 450, 642, 690, 30);
        CopyDance(bronze, 390, 1);
        CopyDance(gold, 438, 1);
        CopyDance(silver, 486, 1);
        bronze.Face(394, MiiExpression.WinkRight).Face(430, MiiExpression.SmileOpenMouth).Face(680, MiiExpression.Normal);
        gold.Face(442, MiiExpression.Surprise).Face(472, MiiExpression.Smile).Face(680, MiiExpression.Normal);
        silver.Face(490, MiiExpression.Surprise).Face(520, MiiExpression.SmileOpenMouth).Face(680, MiiExpression.Normal);

        // The return wave reverses. Bronze stops early while the others are still waiting for the next move.
        Look(bronze, 960, 1000, 1140, 1290, -28);
        Look(gold, 974, 1020, 1170, 1290, 27);
        Look(silver, 994, 1040, 1190, 1290, 30);
        CopyDance(bronze, 984, -1);
        CopyDance(gold, 1032, -1);
        CopyDance(silver, 1080, -1);
        bronze.ArmsRest(1100).Face(1102, MiiExpression.Normal).Face(1182, MiiExpression.WinkLeft);
        bronze.Keys(ChestRoll, (1096, 0), (1108, 0), (1680, 0));
        // Hold the copied flourish briefly, then look embarrassed as bronze innocently looks forward.
        gold.Arms(1164, new Vector3(45, 94, 24)).Arms(1200, new Vector3(45, 94, 24)).ArmsRest(1254);
        silver.Arms(1200, new Vector3(45, 94, 24)).Arms(1224, new Vector3(45, 94, 24)).ArmsRest(1272);
        gold.Face(1170, MiiExpression.Surprise).Face(1228, MiiExpression.Smile).Face(1284, MiiExpression.Normal);
        silver.Face(1210, MiiExpression.SurpriseOpenMouth).Face(1254, MiiExpression.Smile).Face(1284, MiiExpression.Normal);
        bronze.Face(1284, MiiExpression.Normal);
        scene.AddEvent(390, "bronze_starts");
        scene.AddEvent(438, "gold_copies");
        scene.AddEvent(486, "silver_copies");
        scene.AddEvent(1100, "bronze_freezes");
        scene.AddEvent(1290, "idle");
        return scene;
    }

    private static (MiiAnimation Scene, AnimBuilder[] Actors) Ensemble(string name, int length, (int From, int To)[] quiet)
    {
        var scene = new MiiAnimation { Name = name, Fps = 60, Length = length };
        scene.AddActor(SecondRole);
        scene.AddActor(ThirdRole);
        var actors = Enumerable.Range(0, 3).Select(i => new AnimBuilder(scene.ForActor(i), female: false)).ToArray();
        for (var actor = 0; actor < actors.Length; actor++)
        {
            var b = actors[actor];
            var place = Platforms[actor];
            b.Keys(RootX, (0, place.X), (length, place.X));
            b.Keys(Bone(MiiBone.Root, Channel.PosZ), (0, place.Z), (length, place.Z));
            // Lower the hips a little: near-straight rest legs otherwise cannot follow a small sideways lean.
            // Do not squash the Root, which would also scale the podium placement and planted feet.
            for (var frame = 0; frame <= length; frame += 120)
                b.Key(RootY, frame, place.Y - 2 - (frame / 120 % 2 == 0 ? 0 : 1.2f));
            b.Key(RootY, length, place.Y - 2);
            foreach (var bone in new[] { MiiBone.Root, MiiBone.Chest, MiiBone.Head })
                b.Rot(bone, 0, 0, 0, 0).Rot(bone, length, 0, 0, 0);
            b.ArmsRest(0).ArmsRest(length).Face(0, MiiExpression.Normal).Face(length, MiiExpression.Normal);
            foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            {
                var foot = b.NaturalFoot(leg, place.X, 0, place.Z) + new Vector3(0, place.Y, 0);
                b.Key(Limb(leg, Channel.IkBlend), 0, 1).Key(Limb(leg, Channel.IkStretch), 0, 0);
                b.Foot(leg, 0, foot, 0).Foot(leg, length, foot, 0);
            }
            foreach (var (from, to) in quiet)
                for (var frame = from + 96 + actor * 29; frame + 7 < to - 24; frame += 186)
                    b.Face(frame, MiiExpression.Blink).Face(frame + 7, MiiExpression.Normal);
        }
        return (scene, actors);
    }

    private static void HighFive(AnimBuilder gold, AnimBuilder neighbour, MiiLimb goldArm, MiiLimb neighbourArm,
        int from, int contact, int end, float goldX, float neighbourX, Vector3 palm)
    {
        var goldRestX = Platforms[0].X;
        var neighbourRestX = neighbourArm == MiiLimb.ArmL ? Platforms[1].X : Platforms[2].X;
        var neighbourTop = neighbourArm == MiiLimb.ArmL ? Platforms[1].Y : Platforms[2].Y;
        gold.Keys(RootX, (from, goldRestX), (contact - 24, goldX), (contact + 12, goldX), (end, goldRestX));
        neighbour.Keys(RootX, (from + 12, neighbourRestX), (contact - 24, neighbourX), (contact + 12, neighbourX), (end, neighbourRestX));
        gold.Keys(RootY, (contact - 24, Platforms[0].Y - 2), (contact + 12, Platforms[0].Y - 2));
        neighbour.Keys(RootY, (contact - 24, neighbourTop - 2), (contact + 12, neighbourTop - 2));
        gold.ArmRest(goldArm, from);
        neighbour.ArmRest(neighbourArm, from + 12);
        var goldSign = goldArm == MiiLimb.ArmL ? 1 : -1;
        gold.Arm(goldArm, from + 30, new Vector3(goldSign * 44, 100, 24));
        neighbour.Arm(neighbourArm, from + 42, new Vector3(-goldSign * 44, 110, 21));
        // World-space authored palms truly meet. Small mirrored wrist rolls make the palms read as a high five.
        gold.ArmWorld(goldArm, contact - 12, palm + new Vector3(goldSign * -3, 0, -5), hand: new Vector3(0, 0, goldSign * 18));
        neighbour.ArmWorld(neighbourArm, contact - 12, palm + new Vector3(goldSign * 3, 0, -5), hand: new Vector3(0, 0, -goldSign * 18));
        gold.ArmWorld(goldArm, contact, palm, hand: new Vector3(0, 0, goldSign * 18));
        neighbour.ArmWorld(neighbourArm, contact, palm, hand: new Vector3(0, 0, -goldSign * 18));
        gold.ArmWorld(goldArm, contact + 6, palm, hand: new Vector3(0, 0, goldSign * 18));
        neighbour.ArmWorld(neighbourArm, contact + 6, palm, hand: new Vector3(0, 0, -goldSign * 18));
        gold.Arm(goldArm, contact + 30, new Vector3(goldSign * 42, 91, 20));
        neighbour.Arm(neighbourArm, contact + 32, new Vector3(-goldSign * 42, 91, 20));
        gold.ArmRest(goldArm, end).Face(from + 26, MiiExpression.Smile).Face(contact, MiiExpression.SmileOpenMouth).Face(end, MiiExpression.Normal);
        neighbour.ArmRest(neighbourArm, end).Face(from + 38, MiiExpression.Surprise).Face(contact, MiiExpression.SmileOpenMouth).Face(end, MiiExpression.Normal);
    }

    private static void Look(AnimBuilder b, int from, int turn, int hold, int end, float yaw) =>
        b.Keys(HeadYaw, (from, 0), (turn, yaw), (hold, yaw), (end, 0));

    private static void Nod(AnimBuilder b, int from, int down, int end, float amount) =>
        b.Keys(HeadPitch, (from, 0), (down, amount), (end, 0));

    private static void Gesture(AnimBuilder b, MiiLimb arm, int from, int up, int hold, int end, Vector3 wrist, Vector3 hand = default)
    {
        b.ArmRest(arm, from).Arm(arm, up, wrist, hand: hand).Arm(arm, hold, wrist, hand: hand).ArmRest(arm, end);
    }

    private static void Bow(AnimBuilder b, int from, int down, int end, float amount)
    {
        b.Keys(ChestPitch, (from, 0), (down, amount), (end, 0));
        b.Keys(HeadPitch, (from, 0), (down, 5), (end, 0));
        // Hands held clear of the torso, below and in front of the large FFL head.
        b.ArmsRest(from).Arms(down, new Vector3(29, 74, 23)).ArmsRest(end);
    }

    private static void Clap(AnimBuilder b, int from, int end)
    {
        // Gather outside the wider female hip before moving inward at chest height; reverse that safe route
        // after the last clap instead of interpolating directly from touching palms to the low rest wrists.
        b.ArmsRest(from).Arms(from + 8, new Vector3(34, 70, 22));
        for (var frame = from + 16; frame < end; frame += 12)
            b.Arms(frame, new Vector3((frame - from - 16) / 12 % 2 == 0 ? 17 : 9, 80, 24));
        b.Arms(end + 4, new Vector3(28, 78, 28)).Arms(end + 12, new Vector3(34, 70, 22)).ArmsRest(end + 28);
    }

    private static void CopyDance(AnimBuilder b, int start, int direction)
    {
        b.ArmsRest(start).Arms(start + 24, new Vector3(46, 94, 23));
        b.Keys(ChestRoll, (start, 0), (start + 24, direction * 3), (start + 54, -direction * 3),
            (start + 84, direction * 3), (start + 110, 0));
        b.Keys(HeadRoll, (start, 0), (start + 24, -direction * 3), (start + 54, direction * 3),
            (start + 84, -direction * 3), (start + 110, 0));
        b.Arms(start + 54, new Vector3(42, 100, 23)).Arms(start + 84, new Vector3(46, 94, 23)).ArmsRest(start + 130);
    }

    /// <summary>Sidecar consumed by an integration: GLB units, actor bindings, floor heights and scale conversion.</summary>
    public static string SceneManifest(string relativePath, MiiAnimation animation)
    {
        var defaultScale = MiiBodyModel.CalculateBodyScale(64, 64);
        var scene = new
        {
            schemaVersion = 1,
            animation = System.IO.Path.GetFileName(relativePath),
            podium = "../" + PodiumFile,
            fps = animation.Fps,
            frames = animation.Length,
            looping = true,
            coordinates = new
            {
                units = "canonical",
                up = "+Y",
                front = "+Z",
                canonicalToRender = MiiBodyModel.CanonicalToRenderUnits,
                defaultBodyScale = new[] { defaultScale.X, defaultScale.Y, defaultScale.Z },
                podiumToStageScale = new[] { defaultScale.X * 0.7f, defaultScale.Y * 0.7f, defaultScale.Z * 0.7f },
            },
            actors = Platforms.Select((p, index) => new
            {
                index,
                role = index == 0 ? "first" : index == 1 ? SecondRole : ThirdRole,
                trackBinding = index == 0 ? "main" : "extra:" + (index == 1 ? SecondRole : ThirdRole),
                canonicalPlacement = new[] { p.X, p.Y, p.Z },
                podiumTop = p.Y,
                rootIdleOffsetY = -2,
                runtimeStageOffsetY = "0.7 * podiumTop * (defaultBodyScale.y - actorBodyScale.y)",
            }).ToArray(),
            events = animation.Events.Select(e => new { frame = e.Frame, name = e.Name }).ToArray(),
            contacts = animation.Name.StartsWith("First place", StringComparison.Ordinal) ? new[]
            {
                new { frame = FirstSilverContact, actors = new[] { 0, 1 }, limbs = new[] { "ArmR", "ArmL" }, bones = new[] { "HandR", "HandL" }, canonicalPoint = new[] { -66, 166, 8 } },
                new { frame = FirstBronzeContact, actors = new[] { 0, 2 }, limbs = new[] { "ArmL", "ArmR" }, bones = new[] { "HandL", "HandR" }, canonicalPoint = new[] { 66, 150, 8 } },
            } : [],
            notes = "Play all three actors together. Miis are supplied by the app. Apply each actor's runtimeStageOffsetY to its body and head if height differs from the default preview. Root X/Z travel already uses the shared stage scale. For different builds/heights, runtime hand IK can retarget the contact markers; authored FK palm contacts are exact for the default body scale.",
        };
        return JsonSerializer.Serialize(scene, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
    }
}
