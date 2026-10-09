using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;

namespace MiiAnim.Core.Tests;

/// <summary>Friends presence, profile quick entrances and podium entrances (see the matching Authoring classes).</summary>
public class WheelWizardPageAnimationTests
{
    private static readonly MiiBone[] LowerBody =
        [MiiBone.Root, MiiBone.Hip, MiiBone.LegL1, MiiBone.LegL2, MiiBone.FootL, MiiBone.LegR1, MiiBone.LegR2, MiiBone.FootR];

    private static readonly Vector3[] Scales =
        [MiiBodyModel.CalculateBodyScale(0, 0), MiiStage.DefaultBodyScale, MiiBodyModel.CalculateBodyScale(127, 127)];

    public static IEnumerable<object[]> FriendClips =>
        FriendsPresenceAnimations.All.SelectMany(a => new[] { new object[] { a.Path, false }, new object[] { a.Path, true } });

    public static IEnumerable<object[]> QuickClips => ProfileQuickAnimations.All.Where(a => !ProfileQuickAnimations.IsExit(a.Path)).Select(a => new object[] { a.Path });

    public static IEnumerable<object[]> QuickExits => ProfileQuickAnimations.All.Where(a => ProfileQuickAnimations.IsExit(a.Path)).Select(a => new object[] { a.Path });

    private static MiiAnimation Friend(string path) => FriendsPresenceAnimations.All.Single(a => a.Path == path).Create();

    // ---- Friends ----

    [Theory]
    [MemberData(nameof(FriendClips))]
    public void Friends_StartAndEndOnTheAwakeOrAsleepBaseline(string path, bool female)
    {
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var awake = rig.Evaluate(FriendsAnimations.OnlineIdleAttentive(), 0);
        var asleep = rig.Evaluate(FriendsPresenceAnimations.AsleepBreathing(), 0);
        var clip = Friend(path);
        AssertSamePose(FriendsPresenceAnimations.StartsAsleep(path) ? asleep : awake, rig.Evaluate(clip, 0), $"{path} start");
        AssertSamePose(FriendsPresenceAnimations.EndsAsleep(path) ? asleep : awake, rig.Evaluate(clip, clip.Length), $"{path} end");
    }

    [Theory]
    [MemberData(nameof(FriendClips))]
    public void Friends_LowerBodyStaysHiddenAndStill(string path, bool female)
    {
        var clip = Friend(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var baseline = rig.Evaluate(clip, 0);
        for (var frame = 0; frame <= clip.Length; frame++)
        {
            var pose = rig.Evaluate(clip, frame);
            Assert.Equal(Vector3.Zero, pose.RootTravel);
            foreach (var bone in LowerBody)
                Assert.True(Vector3.Distance(baseline.WorldPosition[(int)bone], pose.WorldPosition[(int)bone]) < 0.001f, $"{path} {frame}: {bone} moved");
        }
    }

    [Theory]
    [MemberData(nameof(FriendClips))]
    public void Friends_HandsStayOutOfTheHeadAndTorso(string path, bool female)
    {
        // The wave hand passes beside the jaw; the fitted head ellipsoid is wider there than a real FFL jaw.
        var headLimit = path.Contains("/wave/") ? 0.95f : 1f;
        var report = ClipCheck.Describe(ClipCheck.Run(Friend(path), female), headLimit, torsoLimit: 0.8f);
        Assert.True(report.Length == 0, $"{path}\n{report}");
    }

    [Theory]
    [MemberData(nameof(FriendClips))]
    public void Friends_LoopsHaveQuietSeams(string path, bool female)
    {
        if (!path.Contains("/asleep/") && !path.Contains("_idle_"))
            return;
        var clip = Friend(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var before = rig.Evaluate(clip, clip.Length - 1);
        var after = rig.Evaluate(clip, 1);
        Assert.True(Vector3.Distance(before.WorldPosition[(int)MiiBone.Head], after.WorldPosition[(int)MiiBone.Head]) < 0.05f, $"{path} jumps on its seam");
        Assert.Equal(before.Expression, after.Expression);
    }

    [Fact]
    public void Friends_ZsDriftUpInTheGapBesideTheHead()
    {
        var shot = WheelWizardShot.FriendsCard;
        foreach (var scale in Scales)
        foreach (var (path, create) in FriendsPresenceAnimations.All.Where(a => a.Path.Contains("/asleep/")))
        {
            var clip = create();
            var rig = new MiiRig(MiiBodyModel.Get(false));
            var stage = new RigParticleStage(clip, _ => rig, _ => scale, _ => Vector3.One);
            var zs = 0;
            for (var frame = 0; frame <= clip.Length; frame += 4)
            {
                var particles = new List<Particle>();
                ParticleEvaluator.Evaluate(clip, frame, stage, particles);
                foreach (var particle in particles.Where(p => p.Effect.Name == "Zzz"))
                {
                    zs++;
                    var px = shot.ToPixels(particle.Position, scale);
                    // Right of the face, below the card's top edge, and out of the way of the name text.
                    Assert.InRange(px.X, 62f, 90f);
                    Assert.InRange(px.Y, 4f, 46f);
                }
            }
            Assert.True(zs > 0, $"{path} has no Z's");
        }
    }

    [Fact]
    public void Friends_WavesRaiseTheHandBesideTheChin()
    {
        var shot = WheelWizardShot.FriendsCard;
        foreach (var (path, create) in FriendsPresenceAnimations.All.Where(a => a.Path.Contains("/wave/")))
        {
            var clip = create();
            var rig = new MiiRig(MiiBodyModel.Get(false));
            var wave = clip.Events.Single(e => e.Name == "wave").Frame;
            var xs = new List<float>();
            for (var frame = wave + 6; frame < wave + 50; frame++)
            {
                var pose = rig.Evaluate(clip, frame);
                var hand = shot.ToPixels(pose, pose.WorldPosition[(int)MiiBone.HandL], MiiStage.DefaultBodyScale);
                Assert.InRange(hand.Y, 70f, 95f);
                Assert.InRange(hand.X, 62f, 86f);
                xs.Add(hand.X);
            }
            // It's a wave: the hand travels side to side, not just a wobble.
            Assert.True(xs.Max() - xs.Min() > 5f, $"{path} barely waves");
        }
    }

    [Fact]
    public void Friends_SnoreBubbleGrowsInFrontOfTheFaceAndPops()
    {
        var clip = FriendsPresenceAnimations.AsleepBubble();
        var bubble = clip.Particles.Single(p => p.Name == "Snore bubble");
        var pop = clip.Particles.Single(p => p.Name == "Bubble pop");
        Assert.Equal(bubble.EndFrame(clip.Fps), pop.StartFrame);
        Assert.Contains(clip.Events, e => e.Name == "pop" && e.Frame == pop.StartFrame);
    }

    // ---- Profile quick entrances ----

    [Theory]
    [MemberData(nameof(QuickClips))]
    public void Quick_StartsOutOfTheCardAndEndsAtRestFast(string path)
    {
        var clip = ProfileQuickAnimations.All.Single(a => a.Path == path).Create();
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var shot = WheelWizardShot.ProfileWindow;
        Assert.True(clip.DurationSeconds <= 1.15f, $"{path} isn't quick");
        var arrive = clip.Events.Single(e => e.Name is "up" or "arrive").Frame;
        Assert.True(arrive <= 16, $"{path} takes too long to show up");

        foreach (var scale in Scales)
        {
            var start = rig.Evaluate(clip, 0);
            var top = shot.HeadToPixels(start, new Vector3(0, 80, 0), scale);
            var right = shot.HeadToPixels(start, new Vector3(40, 35, 0), scale);
            var left = shot.HeadToPixels(start, new Vector3(-40, 35, 0), scale);
            var hidden = top.Y > shot.Size.Y || right.X < 0 || left.X > shot.Size.X;
            Assert.True(hidden, $"{path} is visible on its first frame ({top}, {left}, {right})");
        }
        AssertSamePose(rig.RestPose, rig.Evaluate(clip, clip.Length), path);
        Assert.Equal(MiiExpression.Normal, rig.Evaluate(clip, clip.Length).Expression);
    }

    [Theory]
    [MemberData(nameof(QuickExits))]
    public void QuickExit_LeavesTheCardFromRestInAFlash(string path)
    {
        var clip = ProfileQuickAnimations.All.Single(a => a.Path == path).Create();
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var shot = WheelWizardShot.ProfileWindow;
        Assert.True(clip.DurationSeconds <= 0.35f, $"{path} isn't quick");
        AssertSamePose(rig.RestPose, rig.Evaluate(clip, 0), path);
        foreach (var scale in Scales)
        {
            var end = rig.Evaluate(clip, clip.Length);
            var top = shot.HeadToPixels(end, new Vector3(0, 80, 0), scale);
            var right = shot.HeadToPixels(end, new Vector3(40, 35, 0), scale);
            var left = shot.HeadToPixels(end, new Vector3(-40, 35, 0), scale);
            Assert.True(top.Y > shot.Size.Y || right.X < 0 || left.X > shot.Size.X, $"{path} is still visible at its end");
        }
    }

    // ---- Podium ----

    [Fact]
    public void Podium_ScenesAllStartTheSame_SoAnyCanFollowTheEntrance()
    {
        var scenes = LeaderboardAnimations.All.Select(a => a.Create()).ToList();
        var rig = new MiiRig(MiiBodyModel.Get(false));
        for (var actor = 0; actor < 3; actor++)
        {
            var first = rig.Evaluate(scenes[0].ForActor(actor), 0);
            foreach (var scene in scenes.Skip(1))
                AssertSamePose(first, rig.Evaluate(scene.ForActor(actor), 0), $"{scene.Name} actor {actor}");
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Podium_EntranceDropsInAndEndsOnTheScenesFirstFrame(int place, bool female)
    {
        var clip = PodiumEntranceAnimations.Enter(place);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var scene = LeaderboardAnimations.FirstPlaceHighFives().ForActor(place);
        Assert.Equal(PodiumEntranceAnimations.Length, clip.Length);
        AssertSamePose(rig.Evaluate(scene, 0), rig.Evaluate(clip, clip.Length), $"place {place}");

        var (drop, land) = PodiumEntranceAnimations.Timing[place];
        Assert.False(rig.Evaluate(clip, drop - 1).Visible);
        Assert.True(rig.Evaluate(clip, drop).Visible);
        // Starts above the top of the stage, even for the shortest Mii.
        var shot = WheelWizardShot.Podium;
        var dropping = rig.Evaluate(clip, drop);
        foreach (var scale in Scales)
            Assert.True(shot.ToPixels(dropping, dropping.WorldPosition[(int)MiiBone.FootL], scale).Y < 0, $"place {place} shows up inside the stage");

        // Feet stay on the step from touchdown on.
        var platform = LeaderboardAnimations.Platforms[place];
        for (var frame = land; frame <= clip.Length; frame++)
        {
            var pose = rig.Evaluate(clip, frame);
            foreach (var foot in new[] { MiiBone.FootL, MiiBone.FootR })
                Assert.True(MathF.Abs(pose.WorldPosition[(int)foot].Y - (platform.Y + 10f)) < 1.5f, $"place {place} {frame}: {foot} off the step");
        }

        var report = ClipCheck.Describe(ClipCheck.Run(clip, female).Where(r => r.Frame >= land).ToList(), headLimit: 1f, torsoLimit: 0.75f);
        Assert.True(report.Length == 0, $"place {place}\n{report}");
    }

    // ---- Files ----

    [Fact]
    public void AllRoundTrip()
    {
        foreach (var (_, create) in Catalog)
        {
            var source = create();
            var loaded = MiiAnimFormat.Read(MiiAnimFormat.Write(source));
            Assert.Equal(source.Length, loaded.Length);
            Assert.Equal(source.Events, loaded.Events);
            Assert.Equal(source.Particles.Count, loaded.Particles.Count);
        }
    }

    [Fact]
    public void WritePageAnimations()
    {
        foreach (var (path, create) in Catalog)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }

    private static IEnumerable<(string Path, Func<MiiAnimation> Create)> Catalog =>
        FriendsPresenceAnimations.All.Concat(ProfileQuickAnimations.All).Concat(PodiumEntranceAnimations.All);

    private static void AssertSamePose(MiiPose expected, MiiPose actual, string what)
    {
        foreach (var bone in MiiSkeletonInfo.AllBones)
        {
            Assert.True(Vector3.Distance(expected.WorldPosition[(int)bone], actual.WorldPosition[(int)bone]) < 0.01f,
                $"{what}: {bone} at {actual.WorldPosition[(int)bone]}, expected {expected.WorldPosition[(int)bone]}");
            Assert.True(Vector3.Distance(expected.LocalScale[(int)bone], actual.LocalScale[(int)bone]) < 0.001f, $"{what}: {bone} scale");
        }
        Assert.Equal(expected.Expression, actual.Expression);
    }
}
