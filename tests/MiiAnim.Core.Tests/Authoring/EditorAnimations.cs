using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using static MiiAnim.Core.Tests.Authoring.AnimBuilder;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Editor action reactions. Paths are relative to samples/wheelwizard. Facial actions have a second,
/// deliberately quiet upper-body shot: the root and legs never move, hands stay below the face, and
/// the edited feature is shown with the Mii's own Normal expression for most of the clip.
/// </summary>
public static class EditorAnimations
{
    public static readonly (string Path, Func<MiiAnimation> Create)[] All =
    [
        ("editor/enter/Editor_enter_wave.miianim", EnterWave),
        ("editor/enter/Editor_enter_ready.miianim", EnterReady),
        ("editor/body_shape/Body_shape_stretch.miianim", BodyStretch),
        ("editor/body_shape/Body_shape_width_check.miianim", BodyWidthCheck),
        ("editor/eyes_eyebrows/Eyes_eyebrows_blink_test.miianim", EyesBlinkTest),
        ("editor/eyes_eyebrows/Eyes_eyebrows_wink_upper.miianim", EyesWinkUpper),
        ("editor/face/Face_angle_check.miianim", FaceAngleCheck),
        ("editor/face/Face_portrait_upper.miianim", FacePortraitUpper),
        ("editor/facial_hair/Facial_hair_proud_chin.miianim", FacialHairProudChin),
        ("editor/facial_hair/Facial_hair_inspect_upper.miianim", FacialHairInspectUpper),
        ("editor/favorite/Favorite_selected.miianim", FavoriteSelected),
        ("editor/favorite/Favorite_removed.miianim", FavoriteRemoved),
        ("editor/favorite/Favorite_removed_stomp.miianim", FavoriteRemovedStomp),
        ("editor/favoritecolor/Shirt_color_present.miianim", ShirtColorPresent),
        ("editor/favoritecolor/Shirt_color_compare.miianim", ShirtColorCompare),
        ("editor/glasses/Glasses_look_over.miianim", GlassesLookOver),
        ("editor/glasses/Glasses_focus_upper.miianim", GlassesFocusUpper),
        ("editor/hair/Hair_swish.miianim", HairSwish),
        ("editor/hair/Hair_inspect_upper.miianim", HairInspectUpper),
        ("editor/mole/Mole_cheek_reveal.miianim", MoleCheekReveal),
        ("editor/mole/Mole_find_upper.miianim", MoleFindUpper),
        ("editor/mouth/Mouth_say_hello.miianim", MouthSayHello),
        ("editor/mouth/Mouth_smile_test_upper.miianim", MouthSmileTestUpper),
        ("editor/name/Name_introduction.miianim", NameIntroduction),
        ("editor/name/Name_thinking.miianim", NameThinking),
        ("editor/nose/Nose_profile_check.miianim", NoseProfileCheck),
        ("editor/nose/Nose_sniff_upper.miianim", NoseSniffUpper),
        ("editor/randomize/Randomize_shuffle.miianim", RandomizeShuffle),
        ("editor/randomize/Randomize_reveal.miianim", RandomizeReveal),
        ("editor/save/Save_proud_bow.miianim", SaveProudBow),
        ("editor/save/Save_relief.miianim", SaveRelief),
    ];

    // Existing authored clips are part of the action catalog, but are deliberately not regenerated here.
    public static readonly string[] PreservedPaths =
    [
        "editor/favoritecolor/Shirt_color_surprise.miianim",
        "editor/gender/Become_boy.miianim",
        "editor/gender/Become_girl.miianim",
    ];

    public static readonly string[] CloseUpActions =
        ["eyes_eyebrows", "face", "facial_hair", "glasses", "hair", "mole", "mouth", "nose"];

    /// <summary>Only these outputs should be regenerated for the editor feedback revision.</summary>
    public static readonly string[] RevisedPaths =
    [
        "editor/enter/Editor_enter_wave.miianim",
        "editor/favorite/Favorite_selected.miianim",
        "editor/favorite/Favorite_removed.miianim",
        "editor/favorite/Favorite_removed_stomp.miianim",
        "editor/favoritecolor/Shirt_color_present.miianim",
        "editor/favoritecolor/Shirt_color_compare.miianim",
        "editor/glasses/Glasses_look_over.miianim",
        "editor/glasses/Glasses_focus_upper.miianim",
        "editor/hair/Hair_swish.miianim",
        "editor/mouth/Mouth_say_hello.miianim",
        "editor/mouth/Mouth_smile_test_upper.miianim",
        "editor/randomize/Randomize_reveal.miianim",
    ];

    private static readonly TrackId RootX = Bone(MiiBone.Root, Channel.PosX);
    private static readonly TrackId RootY = Bone(MiiBone.Root, Channel.PosY);
    private static readonly TrackId RootYaw = Bone(MiiBone.Root, Channel.RotY);
    private static readonly TrackId ChestPitch = Bone(MiiBone.Chest, Channel.RotX);
    private static readonly TrackId ChestYaw = Bone(MiiBone.Chest, Channel.RotY);
    private static readonly TrackId ChestRoll = Bone(MiiBone.Chest, Channel.RotZ);
    private static readonly TrackId HeadPitch = Bone(MiiBone.Head, Channel.RotX);
    private static readonly TrackId HeadYaw = Bone(MiiBone.Head, Channel.RotY);
    private static readonly TrackId HeadRoll = Bone(MiiBone.Head, Channel.RotZ);

    private static AnimBuilder Begin(string name, int length, bool upper = false)
    {
        var b = new AnimBuilder(name, length, female: false);
        b.ArmsRest(0).Face(0, MiiExpression.Normal);
        if (upper)
        {
            // Explicit FK legs make the close-up independent of any IK settings in the player.
            b.FkLegs();
            foreach (var channel in ChannelInfo.BoneChannels)
                b.Key(Bone(MiiBone.Root, channel), 0, ChannelInfo.DefaultValue(Bone(MiiBone.Root, channel)));
            foreach (var bone in new[] { MiiBone.Hip, MiiBone.LegL1, MiiBone.LegL2, MiiBone.FootL, MiiBone.LegR1, MiiBone.LegR2, MiiBone.FootR })
                b.Rot(bone, 0, 0, 0, 0);
        }
        return b;
    }

    private static MiiAnimation Finish(AnimBuilder b, int featureVisibleFrame = -1)
    {
        // Every reaction settles for twelve frames before its done marker. Don't reset the close-up's
        // FK leg blend: it already matches rest and stays explicitly fixed for the entire shot.
        var settle = b.Animation.Length - 12;
        foreach (var id in b.Animation.Tracks.Keys.ToArray())
        {
            var value = id.Target.Kind == TrackTargetKind.Limb && MiiSkeletonInfo.IsLeg(id.Target.AsLimb) && id.Channel == Channel.IkBlend
                ? b.Animation.Evaluate(id, 0)
                : ChannelInfo.DefaultValue(id);
            b.Key(id, settle, value).Key(id, b.Animation.Length, value);
        }
        b.ArmsRest(settle).ArmsRest(b.Animation.Length);
        b.Face(settle, MiiExpression.Normal).Face(b.Animation.Length, MiiExpression.Normal);
        if (featureVisibleFrame >= 0)
            b.Animation.AddEvent(featureVisibleFrame, "feature_visible");
        b.Animation.AddEvent(b.Animation.Length, "done");
        return b.Animation;
    }

    private static void AddFrustrationLines(MiiAnimation animation, int startFrame, float life)
    {
        // Three stationary marks sit outside/above the hair silhouette rather than covering the face.
        // Upright squiggle sprites; a fixed single particle preserves placement.
        var purple = new Vector3(191f / 255f, 65f / 255f, 240f / 255f);
        var offsets = new[] { new Vector3(-43, 116, 4), new Vector3(0, 133, 4), new Vector3(43, 116, 4) };
        for (var i = 0; i < offsets.Length; i++)
            animation.Particles.Add(new ParticleEffect
            {
                Name = $"Frustration line {i + 1}",
                Shape = BuiltInShapes.Squiggle,
                Orientation = ParticleOrientation.Upright,
                StartFrame = startFrame,
                Count = 1,
                Duration = 0,
                Seed = (uint)(200 + i),
                AttachBone = (byte)MiiBone.Head,
                Offset = offsets[i],
                SpawnRadius = Vector3.Zero,
                Direction = Vector3.Zero,
                Spread = 0,
                SpeedMin = 0,
                SpeedMax = 0,
                Gravity = 0,
                Drag = 0,
                SpinMin = 0,
                SpinMax = 0,
                LifeMin = life,
                LifeMax = life,
                SizeStart = 25,
                SizeEnd = 25,
                ColorStart = new Vector4(purple, 1),
                ColorEnd = new Vector4(purple, 0),
                Tint = ParticleTint.None,
            });
    }

    /// <summary>Walk into the editor from the right, step to face the camera, and wave.</summary>
    public static MiiAnimation EnterWave()
    {
        var b = Begin("Editor enter wave", 174);
        b.WalkX(0, 48, 86, 0, -90, stepFrames: 16, lift: 6, armSwing: 0, hipDrop: 4);
        b.TurnInPlace(48, 70, 0, -90, 0, lift: 4);
        b.Keys(RootY, (48, -2), (70, 0), (174, 0));
        b.Keys(HeadYaw, (0, 0), (38, -14), (60, -8), (76, 0));
        // A relaxed, slightly wide wrist path clears the female hip mesh during the dropped-hip
        // walking pose. The arms alternate front/back instead of twisting the hanging hands inward.
        b.Arms(0, new(36, 64, 0));
        foreach (var (frame, depth) in new[] { (12, 8f), (24, -8f), (36, 8f) })
        {
            b.Arm(MiiLimb.ArmL, frame, new(36, 65, depth));
            b.Arm(MiiLimb.ArmR, frame, new(-36, 65, -depth));
        }
        b.Arms(48, new(36, 64, 0));
        b.ArmsRest(70);
        b.Arm(MiiLimb.ArmR, 84, new(-54, 119, 10), hand: new(0, 0, -12));
        b.Arm(MiiLimb.ArmR, 128, new(-54, 119, 10), hand: new(0, 0, -12));
        b.Keys(Bone(MiiBone.HandR, Channel.RotX), (84, 0), (92, -23), (100, 20), (108, -23), (116, 20), (128, 0));
        b.Keys(HeadRoll, (0, 0), (84, -4), (126, -4), (146, 0));
        b.Face(80, MiiExpression.OpenMouth).Face(100, MiiExpression.Normal);
        b.ArmsRest(146);
        b.Animation.AddEvent(70, "arrived");
        return Finish(b);
    }

    /// <summary>A settled welcome: open the hands low, bow lightly, then look attentively at the editor.</summary>
    public static MiiAnimation EnterReady()
    {
        var b = Begin("Editor enter ready", 144);
        b.Arms(24, new(43, 89, 19), hand: new(-14, 0, 12));
        b.Arms(44, new(43, 89, 19), hand: new(-14, 0, 12));
        b.Keys(ChestPitch, (0, 0), (46, 0), (62, 10), (78, 0));
        b.Keys(HeadPitch, (0, 0), (48, 0), (64, 8), (80, -2), (106, 0));
        b.Arms(64, new(34, 69, 18));
        b.ArmsRest(92);
        b.Face(58, MiiExpression.Blink).Face(65, MiiExpression.Normal);
        b.Animation.AddEvent(80, "ready");
        return Finish(b);
    }

    /// <summary>A planted-foot stretch reads as checking the height without replacing the selected body scale.</summary>
    public static MiiAnimation BodyStretch()
    {
        var b = Begin("Body shape stretch", 166);
        b.Keys(RootY, (0, 0), (18, -5), (42, -1), (88, -1), (112, -3), (140, 0));
        b.Keys(ChestPitch, (0, 0), (18, 7), (42, -5), (88, -5), (120, 0));
        b.Keys(HeadPitch, (0, 0), (18, 12), (42, -8), (88, -8), (112, 8), (138, 0));
        b.Arms(18, new(34, 72, 10));
        b.Arms(42, new(56, 109, 3), hand: new(0, 0, 10));
        b.Arms(88, new(56, 109, 3), hand: new(0, 0, 10));
        b.Arms(112, new(43, 81, 10));
        b.ArmsRest(138);
        b.Face(22, MiiExpression.OpenMouth).Face(40, MiiExpression.Normal);
        b.Face(94, MiiExpression.Blink).Face(100, MiiExpression.Normal);
        b.Animation.AddEvent(64, "body_visible");
        return Finish(b);
    }

    /// <summary>Look down and frame the waist with both hands, then extend the arms to compare the width.</summary>
    public static MiiAnimation BodyWidthCheck()
    {
        var b = Begin("Body shape width check", 174);
        b.Keys(RootY, (0, 0), (30, -2), (66, -2), (98, 0));
        b.Keys(HeadPitch, (0, 0), (26, 28), (56, 28), (84, 12), (118, 0));
        b.Keys(HeadYaw, (0, 0), (36, 14), (60, -14), (88, 0));
        b.Arms(28, new(39, 72, 14), swivel: -18, hand: new(0, 0, 12));
        b.Arms(62, new(39, 72, 14), swivel: -18, hand: new(0, 0, 12));
        b.Arms(94, new(55, 96, 6), hand: new(0, 0, 16));
        b.Arms(116, new(55, 96, 6), hand: new(0, 0, 16));
        b.ArmsRest(146);
        b.Face(30, MiiExpression.OpenMouth).Face(52, MiiExpression.Normal);
        b.Animation.AddEvent(94, "body_visible");
        return Finish(b);
    }

    /// <summary>Test the new eyes with one slow blink and a double blink, punctuated by a curious head tilt.</summary>
    public static MiiAnimation EyesBlinkTest()
    {
        var b = Begin("Eyes eyebrows blink test", 144);
        b.Keys(HeadRoll, (0, 0), (26, 7), (54, 7), (72, -5), (104, -5), (124, 0));
        b.Keys(HeadYaw, (0, 0), (26, 10), (54, 10), (82, -8), (112, 0));
        b.Keys(ChestRoll, (0, 0), (28, 2), (76, -2), (114, 0));
        b.Arm(MiiLimb.ArmL, 30, new(40, 79, 16), hand: new(-10, 0, 12));
        b.ArmRest(MiiLimb.ArmL, 112);
        b.Face(35, MiiExpression.Blink).Face(45, MiiExpression.Normal);
        b.Face(79, MiiExpression.Blink).Face(84, MiiExpression.Normal);
        b.Face(91, MiiExpression.Blink).Face(96, MiiExpression.Normal);
        return Finish(b, 104);
    }

    /// <summary>A close-up wink left, wink right, then a short blink. No arm or chest motion.</summary>
    public static MiiAnimation EyesWinkUpper()
    {
        var b = Begin("Eyes eyebrows wink upper", 132, upper: true);
        b.Keys(HeadRoll, (0, 0), (20, 3), (36, 3), (54, -3), (72, -3), (100, 0));
        b.Keys(HeadYaw, (0, 0), (22, 4), (54, -4), (98, 0));
        b.Face(24, MiiExpression.WinkLeft).Face(33, MiiExpression.Normal);
        b.Face(60, MiiExpression.WinkRight).Face(69, MiiExpression.Normal);
        b.Face(88, MiiExpression.Blink).Face(93, MiiExpression.Normal);
        return Finish(b, 98);
    }

    /// <summary>Check the face silhouette in two three-quarter angles, palms low as if presenting the result.</summary>
    public static MiiAnimation FaceAngleCheck()
    {
        var b = Begin("Face angle check", 174);
        b.Keys(ChestYaw, (0, 0), (30, 5), (62, 5), (94, -5), (124, -5), (148, 0));
        b.Keys(HeadYaw, (0, 0), (30, 24), (62, 24), (94, -24), (124, -24), (148, 0));
        b.Keys(HeadPitch, (0, 0), (30, -3), (62, -3), (94, 4), (124, 4), (148, 0));
        b.Arms(26, new(44, 83, 20), hand: new(-12, 0, 12));
        b.Arms(126, new(44, 83, 20), hand: new(-12, 0, 12));
        b.ArmsRest(150);
        b.Face(74, MiiExpression.Blink).Face(79, MiiExpression.Normal);
        return Finish(b, 148);
    }

    /// <summary>The close portrait holds the edited face in Normal while slowly inspecting two small angles.</summary>
    public static MiiAnimation FacePortraitUpper()
    {
        var b = Begin("Face portrait upper", 156, upper: true);
        b.Keys(HeadYaw, (0, 0), (30, 12), (54, 12), (90, -12), (114, -12), (138, 0));
        b.Keys(HeadPitch, (0, 0), (30, -2), (54, -2), (90, 2), (114, 2), (138, 0));
        b.Keys(HeadRoll, (0, 0), (54, 1.5f), (90, -1.5f), (138, 0));
        b.Face(68, MiiExpression.Blink).Face(73, MiiExpression.Normal);
        return Finish(b, 138);
    }

    /// <summary>Lift the chin proudly and turn it to either side, keeping both hands well away from the beard.</summary>
    public static MiiAnimation FacialHairProudChin()
    {
        var b = Begin("Facial hair proud chin", 174);
        b.Keys(ChestPitch, (0, 0), (30, -4), (110, -4), (148, 0));
        b.Keys(HeadPitch, (0, 0), (28, -11), (108, -11), (148, 0));
        b.Keys(HeadYaw, (0, 0), (44, -21), (66, -21), (94, 21), (116, 21), (146, 0));
        b.Arms(28, new(38, 73, -8), swivel: -14);
        b.Arms(116, new(38, 73, -8), swivel: -14);
        b.ArmsRest(148);
        b.Face(128, MiiExpression.OpenMouth).Face(139, MiiExpression.Normal);
        return Finish(b, 116);
    }

    /// <summary>Hold a modest chin lift in close-up, with the neck doing all the motion.</summary>
    public static MiiAnimation FacialHairInspectUpper()
    {
        var b = Begin("Facial hair inspect upper", 144, upper: true);
        b.Keys(HeadPitch, (0, 0), (24, -6), (100, -6), (124, 0));
        b.Keys(HeadYaw, (0, 0), (36, 10), (60, 10), (84, -10), (108, -10), (126, 0));
        b.Keys(HeadRoll, (0, 0), (84, -1.5f), (108, -1.5f), (126, 0));
        b.Face(70, MiiExpression.Blink).Face(75, MiiExpression.Normal);
        return Finish(b, 108);
    }

    /// <summary>Being selected as a favorite: an excited gather and a low, open-armed thank-you.</summary>
    public static MiiAnimation FavoriteSelected()
    {
        var b = Begin("Favorite selected", 168);
        b.Keys(RootY, (0, 0), (20, -5), (40, 0), (56, -2), (74, 0));
        b.Keys(ChestPitch, (0, 0), (20, 8), (40, -6), (96, -6), (132, 0));
        b.Keys(HeadPitch, (0, 0), (20, 8), (40, -5), (64, 3), (78, -5), (124, 0));
        b.Arms(20, new(23, 78, 23), swivel: 16);
        b.Arms(42, new(53, 104, 13), hand: new(-12, 0, 15));
        b.Arms(96, new(53, 104, 13), hand: new(-12, 0, 15));
        b.ArmsRest(134);
        b.Face(22, MiiExpression.SurpriseOpenMouth).Face(42, MiiExpression.SmileOpenMouth);
        b.Face(76, MiiExpression.Smile).Face(84, MiiExpression.Smile);
        b.Face(130, MiiExpression.Normal);
        b.Animation.AddEvent(42, "favorite_selected");
        return Finish(b);
    }

    /// <summary>A frustrated droop with three purple overhead lines, then a small accepting shrug.</summary>
    public static MiiAnimation FavoriteRemoved()
    {
        var b = Begin("Favorite removed", 150);
        b.Keys(ChestPitch, (0, 0), (24, 6), (56, 6), (80, 0));
        b.Keys(HeadPitch, (0, 0), (24, 13), (56, 13), (78, 0), (94, 5), (110, 0));
        b.Keys(HeadRoll, (0, 0), (24, 5), (56, 5), (82, -3), (110, 0));
        b.Arms(76, new(41, 86, 17), hand: new(-20, 0, 15));
        b.Arms(98, new(41, 86, 17), hand: new(-20, 0, 15));
        b.ArmsRest(124);
        b.Face(20, MiiExpression.Frustrated).Face(66, MiiExpression.Normal);
        b.Face(85, MiiExpression.Blink).Face(90, MiiExpression.Normal);
        AddFrustrationLines(b.Animation, 20, 0.9f);
        b.Animation.AddEvent(20, "favorite_removed");
        return Finish(b);
    }

    /// <summary>Gather onto the left leg, lift the right foot, stomp sharply, then let the frustration settle.</summary>
    public static MiiAnimation FavoriteRemovedStomp()
    {
        var b = Begin("Favorite removed stomp", 162);
        b.Keys(RootX, (0, 0), (18, 3), (50, 3), (78, 0));
        b.Keys(RootY, (0, 0), (18, -4), (36, -3), (42, -6), (48, -5), (66, -1), (88, 0));
        b.Keys(ChestPitch, (0, 0), (18, 6), (36, 6), (44, 10), (62, 3), (108, 0));
        b.Keys(HeadPitch, (0, 0), (18, 7), (36, 7), (44, 12), (62, 3), (82, 6), (108, 0));
        b.Keys(HeadRoll, (0, 0), (18, -3), (50, -3), (88, 2), (116, 0));

        // Default leg IK fixes the supporting foot throughout. The stomping foot stays level,
        // holds its lift for four frames, then accelerates into a six-frame impact.
        b.Keys(Limb(MiiLimb.LegR, Channel.TargetY), (0, 0), (14, 0), (32, 15), (36, 15), (42, 0), (162, 0));
        b.Keys(Limb(MiiLimb.LegR, Channel.TargetZ), (0, 0), (14, 0), (32, 5), (36, 5), (42, 0), (162, 0));
        b.Ease(Limb(MiiLimb.LegR, Channel.TargetY), 32, Interpolation.Hold);
        b.Ease(Limb(MiiLimb.LegR, Channel.TargetZ), 32, Interpolation.Hold);
        b.Ease(Limb(MiiLimb.LegR, Channel.TargetY), 36, Interpolation.EaseIn);
        b.Ease(Limb(MiiLimb.LegR, Channel.TargetZ), 36, Interpolation.EaseIn);
        b.FeetAt(0, 0, 0);

        b.Arms(18, new(35, 75, 14), hand: new(0, 0, 8));
        b.Arms(48, new(35, 75, 14), hand: new(0, 0, 8));
        b.Arms(86, new(41, 84, 14), hand: new(-12, 0, 12));
        b.ArmsRest(124);
        b.Face(12, MiiExpression.Anger).Face(42, MiiExpression.Frustrated).Face(108, MiiExpression.Normal);
        AddFrustrationLines(b.Animation, 42, 1f);
        var dust = ParticlePresets.DustRing();
        dust.Name = "Stomp dust";
        dust.StartFrame = 42;
        dust.Count = 10;
        dust.Offset = new(-8, 0, 0);
        dust.SpawnRadius = new Vector3(9, 0, 9);
        dust.SpeedMin = 20;
        dust.SpeedMax = 45;
        dust.LifeMin = 0.4f;
        dust.LifeMax = 0.55f;
        dust.SizeStart = 9;
        dust.SizeEnd = 15;
        dust.Seed = 42;
        b.Animation.Particles.Add(dust);
        b.Animation.AddEvent(12, "favorite_removed");
        b.Animation.AddEvent(18, "anticipate");
        b.Animation.AddEvent(36, "stomp_lift");
        b.Animation.AddEvent(42, "stomp");
        return Finish(b);
    }

    /// <summary>Look at the new shirt, spread the arms to make the color readable, and present it proudly.</summary>
    public static MiiAnimation ShirtColorPresent()
    {
        var b = Begin("Shirt color present", 168);
        b.Keys(HeadPitch, (0, 0), (24, 31), (48, 31), (72, -3), (116, -3), (140, 0));
        b.Keys(ChestPitch, (0, 0), (24, 3), (48, 3), (74, -3), (116, -3), (140, 0));
        b.Arms(24, new(39, 75, 13));
        b.Arms(72, new(53, 97, 9), hand: new(-14, 0, 16));
        b.Arms(116, new(53, 97, 9), hand: new(-14, 0, 16));
        b.ArmsRest(142);
        b.Face(32, MiiExpression.OpenMouth).Face(55, MiiExpression.Normal);
        b.Face(77, MiiExpression.Smile).Face(127, MiiExpression.Normal);
        var poof = ParticlePresets.Poof();
        poof.StartFrame = 1;
        poof.Seed = 168;
        b.Animation.Particles.Add(poof);
        b.Animation.AddEvent(72, "color_visible");
        return Finish(b);
    }

    /// <summary>Compare the two sleeves with alternating downward glances before a satisfied nod.</summary>
    public static MiiAnimation ShirtColorCompare()
    {
        var b = Begin("Shirt color compare", 222);
        b.Keys(HeadPitch, (0, 0), (28, 26), (144, 26), (168, 0), (184, 7), (200, 0));
        b.Keys(HeadYaw, (0, 0), (32, 27), (64, 27), (108, -27), (144, -27), (168, 0));
        b.Keys(ChestYaw, (0, 0), (30, 4), (66, 4), (108, -4), (146, -4), (168, 0));
        b.Arm(MiiLimb.ArmL, 30, new(46, 83, 16), hand: new(0, 0, 15), interpolation: Interpolation.Hold);
        b.Arm(MiiLimb.ArmL, 66, new(46, 83, 16), hand: new(0, 0, 15));
        b.ArmRest(MiiLimb.ArmR, 30);
        b.ArmRest(MiiLimb.ArmR, 86);
        b.ArmRest(MiiLimb.ArmL, 86);
        b.Arm(MiiLimb.ArmR, 108, new(-46, 83, 16), hand: new(0, 0, -15), interpolation: Interpolation.Hold);
        b.Arm(MiiLimb.ArmR, 144, new(-46, 83, 16), hand: new(0, 0, -15));
        b.ArmsRest(168);
        b.Face(172, MiiExpression.Smile).Face(204, MiiExpression.Normal);
        b.Animation.AddEvent(30, "left_sleeve");
        b.Animation.AddEvent(66, "left_sleeve_end");
        b.Animation.AddEvent(108, "right_sleeve");
        b.Animation.AddEvent(144, "right_sleeve_end");
        b.Animation.AddEvent(168, "color_visible");
        return Finish(b);
    }

    /// <summary>A studious glance at each side, chin down and eyes returned to Normal to show the chosen frames.</summary>
    public static MiiAnimation GlassesLookOver()
    {
        var b = Begin("Glasses look over", 164);
        b.Keys(HeadPitch, (0, 0), (22, 9), (62, 9), (82, -4), (120, -4), (142, 0));
        b.Keys(HeadYaw, (0, 0), (34, 19), (58, 19), (90, -19), (116, -19), (142, 0));
        b.Face(70, MiiExpression.Blink).Face(76, MiiExpression.Normal);
        return Finish(b, 142);
    }

    /// <summary>Two tiny focus nods and a deliberate blink for a close shot. Hands stay entirely at rest.</summary>
    public static MiiAnimation GlassesFocusUpper()
    {
        var b = Begin("Glasses focus upper", 212, upper: true);
        b.Keys(HeadPitch, (0, 0), (38, 4), (61, 0), (86, 4), (109, 0), (144, -2), (176, 0));
        b.Keys(HeadYaw, (0, 0), (42, 6), (86, -6), (138, 0));
        // Stretch the inspection timing, while keeping the blink itself to eight frames.
        b.Face(118, MiiExpression.Blink).Face(126, MiiExpression.Normal);
        return Finish(b, 148);
    }

    /// <summary>Controlled haircut inspection with slow turns and held side poses. The legacy filename is retained.</summary>
    public static MiiAnimation HairSwish()
    {
        var b = Begin("Haircut inspection", 216);
        b.Keys(ChestYaw, (0, 0), (42, -4), (78, -4), (132, 4), (168, 4), (198, 0));
        b.Keys(HeadYaw, (0, 0), (42, -23), (78, -23), (132, 23), (168, 23), (198, 0));
        b.Keys(HeadPitch, (0, 0), (42, 2), (78, 2), (132, -2), (168, -2), (198, 0));
        foreach (var id in new[] { ChestYaw, HeadYaw, HeadPitch })
        {
            b.Ease(id, 42, Interpolation.Hold);
            b.Ease(id, 132, Interpolation.Hold);
        }
        b.Face(96, MiiExpression.Blink).Face(102, MiiExpression.Normal);
        return Finish(b, 198);
    }

    /// <summary>Quiet quarter turns and a slight chin dip expose the hairstyle's sides in a cropped shot.</summary>
    public static MiiAnimation HairInspectUpper()
    {
        var b = Begin("Hair inspect upper", 156, upper: true);
        b.Keys(HeadYaw, (0, 0), (30, -16), (56, -16), (90, 16), (116, 16), (136, 0));
        b.Keys(HeadPitch, (0, 0), (30, 3), (56, 3), (90, -3), (116, -3), (136, 0));
        b.Keys(HeadRoll, (0, 0), (30, -2), (56, -2), (90, 2), (116, 2), (136, 0));
        b.Face(70, MiiExpression.Blink).Face(75, MiiExpression.Normal);
        return Finish(b, 136);
    }

    /// <summary>Offer both cheeks to the camera, then settle in a small tilted pose to make the mole visible.</summary>
    public static MiiAnimation MoleCheekReveal()
    {
        var b = Begin("Mole cheek reveal", 180);
        b.Keys(HeadYaw, (0, 0), (28, 25), (54, 25), (88, -25), (114, -25), (142, 0));
        b.Keys(HeadRoll, (0, 0), (28, -4), (54, -4), (88, 4), (114, 4), (142, -3), (160, 0));
        b.Keys(ChestRoll, (0, 0), (28, -2), (88, 2), (138, 0));
        b.Arms(26, new(41, 78, 16), hand: new(-10, 0, 10));
        b.Arms(118, new(41, 78, 16), hand: new(-10, 0, 10));
        b.ArmsRest(150);
        b.Face(66, MiiExpression.Blink).Face(72, MiiExpression.Normal);
        return Finish(b, 142);
    }

    /// <summary>A searching micro-tilt goes left, right, then pauses front-on. No expression masks the feature.</summary>
    public static MiiAnimation MoleFindUpper()
    {
        var b = Begin("Mole find upper", 144, upper: true);
        b.Keys(HeadYaw, (0, 0), (26, 9), (44, 9), (70, -9), (88, -9), (112, 0));
        b.Keys(HeadRoll, (0, 0), (26, -3), (44, -3), (70, 3), (88, 3), (112, -2), (126, 0));
        b.Keys(HeadPitch, (0, 0), (26, -2), (70, 2), (112, 0));
        return Finish(b, 112);
    }

    /// <summary>Understated mouth inspection with a low presenting gesture; the edited mouth stays visible throughout.</summary>
    public static MiiAnimation MouthSayHello()
    {
        var b = Begin("Mouth angle inspection", 144);
        b.Keys(HeadYaw, (0, 0), (26, 10), (48, 10), (80, -10), (104, -10), (124, 0));
        b.Keys(HeadPitch, (0, 0), (26, -2), (48, -2), (80, 2), (104, 2), (124, 0));
        b.Keys(HeadRoll, (0, 0), (26, -2), (48, -2), (80, 2), (104, 2), (124, 0));
        b.Arm(MiiLimb.ArmR, 26, new(-43, 82, 20), hand: new(-18, 0, -12), interpolation: Interpolation.Hold);
        b.Arm(MiiLimb.ArmR, 104, new(-43, 82, 20), hand: new(-18, 0, -12));
        b.ArmRest(MiiLimb.ArmR, 124);
        return Finish(b, 124);
    }

    /// <summary>Close mouth inspection using only small neck angles, with Normal expression on every frame.</summary>
    public static MiiAnimation MouthSmileTestUpper()
    {
        var b = Begin("Mouth close inspection", 132, upper: true);
        b.Keys(HeadYaw, (0, 0), (26, 6), (44, 6), (70, -6), (88, -6), (110, 0));
        b.Keys(HeadPitch, (0, 0), (26, -2), (44, -2), (70, 2), (88, 2), (110, 0));
        b.Keys(HeadRoll, (0, 0), (26, -1.5f), (44, -1.5f), (70, 1.5f), (88, 1.5f), (110, 0));
        return Finish(b, 110);
    }

    /// <summary>Introduce the new name with a low palm-up presentation and a greeting nod.</summary>
    public static MiiAnimation NameIntroduction()
    {
        var b = Begin("Name introduction", 150);
        b.Keys(ChestYaw, (0, 0), (30, -4), (76, -4), (112, 0));
        b.Keys(HeadRoll, (0, 0), (30, -4), (76, -4), (112, 0));
        b.Keys(HeadPitch, (0, 0), (36, -3), (56, 6), (76, 0));
        b.Arm(MiiLimb.ArmR, 28, new(-44, 85, 24), hand: new(-20, 0, -12));
        b.Arm(MiiLimb.ArmR, 84, new(-44, 85, 24), hand: new(-20, 0, -12));
        b.ArmRest(MiiLimb.ArmR, 118);
        b.Face(30, MiiExpression.OpenMouth).Face(44, MiiExpression.Normal);
        b.Face(52, MiiExpression.OpenMouth).Face(64, MiiExpression.Normal);
        b.Animation.AddEvent(28, "name_visible");
        return Finish(b);
    }

    /// <summary>Think up a name: glance upward, consider the other side, then settle with a deciding nod.</summary>
    public static MiiAnimation NameThinking()
    {
        var b = Begin("Name thinking", 180);
        b.Keys(HeadPitch, (0, 0), (28, -8), (86, -8), (120, 0), (134, 8), (150, 0));
        b.Keys(HeadYaw, (0, 0), (28, 16), (54, 16), (80, -14), (104, -14), (124, 0));
        b.Keys(HeadRoll, (0, 0), (28, 4), (80, -4), (124, 0));
        b.Arm(MiiLimb.ArmL, 24, new(34, 76, 24), swivel: 14, hand: new(0, 0, 12));
        b.Arm(MiiLimb.ArmL, 98, new(34, 76, 24), swivel: 14, hand: new(0, 0, 12));
        b.ArmRest(MiiLimb.ArmL, 126);
        b.Face(63, MiiExpression.Blink).Face(69, MiiExpression.Normal);
        b.Face(128, MiiExpression.OpenMouth).Face(143, MiiExpression.Normal);
        b.Animation.AddEvent(128, "name_decided");
        return Finish(b);
    }

    /// <summary>Two pronounced profile holds reveal nose depth while the hands frame the body below the chin.</summary>
    public static MiiAnimation NoseProfileCheck()
    {
        var b = Begin("Nose profile check", 180);
        b.Keys(ChestYaw, (0, 0), (30, 7), (58, 7), (92, -7), (120, -7), (150, 0));
        b.Keys(HeadYaw, (0, 0), (30, 35), (58, 35), (92, -35), (120, -35), (150, 0));
        b.Keys(HeadPitch, (0, 0), (30, -2), (58, -2), (92, 3), (120, 3), (150, 0));
        b.Arms(26, new(40, 76, 17), hand: new(-8, 0, 12));
        b.Arms(124, new(40, 76, 17), hand: new(-8, 0, 12));
        b.ArmsRest(152);
        b.Face(70, MiiExpression.Blink).Face(76, MiiExpression.Normal);
        return Finish(b, 150);
    }

    /// <summary>A close-up sniff uses two small neck pulses, followed by a tiny satisfied nod.</summary>
    public static MiiAnimation NoseSniffUpper()
    {
        var b = Begin("Nose sniff upper", 132, upper: true);
        b.Keys(HeadPitch, (0, 0), (22, 3), (30, -3), (38, 2), (46, -3), (56, 0), (84, 4), (100, 0));
        b.Keys(HeadYaw, (0, 0), (24, 5), (56, 5), (82, 0));
        b.Keys(HeadRoll, (0, 0), (24, -2), (56, -2), (82, 0));
        b.Face(28, MiiExpression.Blink).Face(34, MiiExpression.Normal);
        b.Face(44, MiiExpression.Blink).Face(50, MiiExpression.Normal);
        return Finish(b, 100);
    }

    /// <summary>Shuffle left and right on two short walks, hide the face in a blink for the swap, then react.</summary>
    public static MiiAnimation RandomizeShuffle()
    {
        var b = Begin("Randomize shuffle", 192);
        b.WalkX(10, 34, 0, 18, 0, stepFrames: 12, lift: 5, armSwing: 0, hipDrop: 3);
        b.WalkX(34, 70, 18, -18, 0, stepFrames: 12, lift: 5, armSwing: 0, hipDrop: 3);
        b.WalkX(70, 94, -18, 0, 0, stepFrames: 12, lift: 5, armSwing: 0, hipDrop: 3);
        b.Keys(RootX, (0, 0), (10, 0));
        b.Keys(RootY, (0, 0), (94, -1.5f), (108, 0));
        b.Keys(ChestRoll, (0, 0), (24, -4), (52, 4), (82, -4), (108, 0));
        b.Keys(HeadRoll, (0, 0), (24, 5), (52, -5), (82, 5), (110, 0));
        b.Arms(16, new(42, 82, 12));
        b.Arms(86, new(42, 82, 12));
        b.Arms(110, new(51, 98, 16), hand: new(-10, 0, 15));
        b.Arms(136, new(51, 98, 16), hand: new(-10, 0, 15));
        b.ArmsRest(164);
        b.Face(47, MiiExpression.BlinkOpenMouth).Face(59, MiiExpression.Normal);
        b.Face(109, MiiExpression.SurpriseOpenMouth).Face(126, MiiExpression.Smile);
        b.Face(156, MiiExpression.Normal);
        b.Animation.AddEvent(52, "randomize");
        b.Animation.AddEvent(110, "reveal");
        return Finish(b);
    }

    /// <summary>Compress into a shy huddle, swap at the closed-eye beat, then open out into a confident reveal.</summary>
    public static MiiAnimation RandomizeReveal()
    {
        var b = Begin("Randomize reveal", 162);
        b.Keys(RootY, (0, 0), (24, -7), (40, -7), (58, -1), (78, 0));
        b.Keys(ChestPitch, (0, 0), (24, 13), (40, 13), (58, -5), (98, -5), (136, 0));
        b.Keys(HeadPitch, (0, 0), (24, 15), (40, 15), (58, -7), (94, -7), (130, 0));
        b.Keys(HeadRoll, (0, 0), (64, -5), (94, -5), (130, 0));
        b.Arms(24, new(24, 74, 22), swivel: 14);
        b.Arms(40, new(24, 74, 22), swivel: 14);
        b.Arms(60, new(54, 106, 10), hand: new(-12, 0, 16));
        b.Arms(98, new(54, 106, 10), hand: new(-12, 0, 16));
        b.ArmsRest(136);
        b.Face(24, MiiExpression.Blink).Face(48, MiiExpression.SurpriseOpenMouth);
        b.Face(65, MiiExpression.Normal);
        var bodyPoof = ParticlePresets.Poof();
        bodyPoof.Name = "Randomize big poof";
        bodyPoof.StartFrame = 32;
        bodyPoof.Count = 48;
        bodyPoof.Offset = new(0, 35, 0);
        bodyPoof.SpawnRadius = new Vector3(36);
        bodyPoof.SpeedMin = 70;
        bodyPoof.SpeedMax = 145;
        bodyPoof.LifeMin = 0.75f;
        bodyPoof.LifeMax = 1.1f;
        bodyPoof.SizeStart = 46;
        bodyPoof.SizeEnd = 72;
        bodyPoof.Seed = 36;
        b.Animation.Particles.Add(bodyPoof);
        var headPoof = ParticlePresets.Poof();
        headPoof.Name = "Randomize head poof";
        headPoof.StartFrame = 36;
        headPoof.Count = 26;
        headPoof.AttachBone = (byte)MiiBone.Head;
        headPoof.Offset = new(0, 48, 0);
        headPoof.SpawnRadius = new Vector3(36);
        headPoof.LifeMin = 0.7f;
        headPoof.LifeMax = 0.95f;
        headPoof.SizeStart = 44;
        headPoof.SizeEnd = 66;
        headPoof.Seed = 60;
        b.Animation.Particles.Add(headPoof);
        b.Animation.AddEvent(36, "randomize");
        b.Animation.AddEvent(60, "reveal");
        return Finish(b);
    }

    /// <summary>The finished Mii gives a small bow, then a low palm-up presentation before returning to rest.</summary>
    public static MiiAnimation SaveProudBow()
    {
        var b = Begin("Save proud bow", 174);
        b.Keys(RootY, (0, 0), (40, -2), (74, 0));
        b.Keys(ChestPitch, (0, 0), (22, 0), (42, 16), (58, 16), (82, -3), (124, -3), (146, 0));
        b.Keys(HeadPitch, (0, 0), (22, 0), (42, 11), (58, 11), (82, -4), (124, -4), (146, 0));
        b.Arms(28, new(35, 70, -8), swivel: -10);
        b.Arms(58, new(35, 70, -8), swivel: -10);
        b.Arms(84, new(49, 92, 19), hand: new(-18, 0, 15));
        b.Arms(120, new(49, 92, 19), hand: new(-18, 0, 15));
        b.ArmsRest(146);
        b.Face(38, MiiExpression.Blink).Face(60, MiiExpression.Normal);
        b.Face(84, MiiExpression.Smile).Face(133, MiiExpression.Normal);
        b.Animation.AddEvent(84, "saved");
        return Finish(b);
    }

    /// <summary>A relieved exhale lowers the shoulders, then a quick nod confirms the save.</summary>
    public static MiiAnimation SaveRelief()
    {
        var b = Begin("Save relief", 144);
        b.Keys(ChestPitch, (0, 0), (24, -4), (52, 4), (78, 0));
        b.Keys(HeadPitch, (0, 0), (24, -6), (52, 7), (78, 0), (94, 7), (110, 0));
        b.Keys(HeadRoll, (0, 0), (52, 3), (76, 3), (110, 0));
        b.Arms(24, new(35, 76, 13), hand: new(-10, 0, 8));
        b.Arms(52, new(39, 65, 3));
        b.ArmsRest(80);
        b.Face(26, MiiExpression.BlinkOpenMouth).Face(44, MiiExpression.Normal);
        b.Face(56, MiiExpression.Smile).Face(84, MiiExpression.Normal);
        b.Animation.AddEvent(94, "saved");
        return Finish(b);
    }
}
