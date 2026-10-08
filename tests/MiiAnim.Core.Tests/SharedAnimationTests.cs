using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class SharedAnimationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Clips => SharedAnimations.All.Select(a => new object[] { a.Path });
    public static IEnumerable<object[]> ClipsAndBodies => SharedAnimations.All.SelectMany(a =>
        new[] { new object[] { a.Path, false }, new object[] { a.Path, true } });
    public static IEnumerable<object[]> Loops => SharedAnimations.All.Where(a => SharedAnimations.IsLoop(a.Path))
        .Select(a => new object[] { a.Path });

    private static MiiAnimation Get(string path) => SharedAnimations.All.Single(a => a.Path == path).Create();

    [Fact]
    public void Catalog_CoversEverySharedActionWithoutReplacingOriginalSamples()
    {
        Assert.Equal(SharedAnimations.All.Length, SharedAnimations.All.Select(a => a.Path).Distinct().Count());
        var folders = SharedAnimations.All.Select(a => a.Path.Split('/')[1]).Distinct().Order().ToArray();
        Assert.Equal(new[] { "appear", "disappear", "error", "hover", "idle", "loading", "selected", "success" }, folders);
        Assert.DoesNotContain(SharedAnimations.All, a => a.Path.EndsWith("/Appear.miianim", StringComparison.Ordinal));
        Assert.DoesNotContain(SharedAnimations.All, a => a.Path.EndsWith("/Idle_breathing.miianim", StringComparison.Ordinal));
        Assert.DoesNotContain(SharedAnimations.All, a => a.Path.EndsWith("/Idle_weight_shift.miianim", StringComparison.Ordinal));
        Assert.All(SharedAnimations.All, a => Assert.True(a.Path.StartsWith("shared/", StringComparison.Ordinal)
            && a.Path.EndsWith(MiiAnimFormat.FileExtension, StringComparison.Ordinal)));
    }

    [Theory]
    [MemberData(nameof(ClipsAndBodies))]
    public void Hands_ClearTheHeadAndTorso_AndShoesNeverSink(string path, bool female)
    {
        var animation = Get(path);
        var results = ClipCheck.Run(animation, female)
            .Where(r => animation.Evaluate(TrackId.Visible, r.Frame) >= 0.5f).ToList();
        var report = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
        output.WriteLine($"{path}, {(female ? "female" : "male")}: {(report.Length == 0 ? "clear" : report)}");
        Assert.True(report.Length == 0, report);
        var sunk = results.Where(r => r.FootY < -1.5f).ToList();
        Assert.True(sunk.Count == 0, $"{path}: shoes below floor at {string.Join(", ", sunk.Select(r => $"{r.Frame}:{r.FootY:0.00}"))}");
    }

    [Theory]
    [MemberData(nameof(ClipsAndBodies))]
    public void PlantedFeetReachTheirTargetsWithoutStretching(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        for (var frame = 0; frame <= animation.Length; frame++)
        {
            var pose = rig.Evaluate(animation, frame);
            if (!pose.Visible)
                continue;
            foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            {
                var state = pose.Limbs[(int)leg];
                if (state.IkBlend < 0.99f)
                    continue;
                var foot = MiiSkeletonInfo.Chain(leg).End;
                Assert.False(state.Stretched);
                var miss = Vector3.Distance(pose.WorldPosition[(int)foot], state.Target);
                Assert.True(miss < 0.2f, $"{path} frame {frame}: {leg} misses planted target by {miss:0.00}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(ClipsAndBodies))]
    public void FinishesInNeutralWithNormalFaceAndSafeScale(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var last = rig.Evaluate(animation, animation.Length);
        foreach (var bone in MiiSkeletonInfo.AllBones)
        {
            Assert.True(Vector3.Distance(last.WorldPosition[(int)bone], rig.RestPose.WorldPosition[(int)bone]) < 0.2f, $"{path}: {bone} not neutral");
            var dot = MathF.Abs(Quaternion.Dot(last.WorldRotation[(int)bone], rig.RestPose.WorldRotation[(int)bone]));
            Assert.True(dot > 0.9999f, $"{path}: {bone} rotation not neutral");
        }
        Assert.Equal(MiiExpression.Normal, last.Expression);
        Assert.Equal(!path.StartsWith("shared/disappear/", StringComparison.Ordinal), last.Visible);
        for (var frame = 0; frame <= animation.Length; frame++)
        {
            var pose = rig.Evaluate(animation, frame);
            foreach (var p in pose.WorldPosition)
                Assert.True(float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z), $"{path}: invalid pose at {frame}");
            foreach (var scale in pose.LocalScale)
                Assert.True(scale.X >= 0.4f && scale.Y >= 0.4f && scale.Z >= 0.4f
                    && scale.X <= 1.5f && scale.Y <= 1.5f && scale.Z <= 1.5f, $"{path}: unsafe scale {scale} at {frame}");
        }
    }

    [Theory]
    [MemberData(nameof(Loops))]
    public void LoopsCloseInPoseChannelsFaceAndVelocity(string path)
    {
        var animation = Get(path);
        Assert.Empty(animation.Particles);
        Assert.DoesNotContain(animation.Events, e => e.Name == "done");
        foreach (var id in animation.Tracks.Keys)
        {
            Assert.Equal(animation.Evaluate(id, 0), animation.Evaluate(id, animation.Length));
            // The quiet seam has matching zero velocity; a held pose must not restart with a jerk.
            Assert.Equal(animation.Evaluate(id, 0), animation.Evaluate(id, 1));
            Assert.Equal(animation.Evaluate(id, animation.Length), animation.Evaluate(id, animation.Length - 1));
        }
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var first = rig.Evaluate(animation, 0);
            var last = rig.Evaluate(animation, animation.Length);
            foreach (var bone in MiiSkeletonInfo.AllBones)
                Assert.True(Vector3.Distance(first.WorldPosition[(int)bone], last.WorldPosition[(int)bone]) < 0.01f);
            Assert.Equal(first.Expression, last.Expression);
        }
    }

    [Theory]
    [InlineData("shared/appear/Appear_balloon.miianim", 84)]
    [InlineData("shared/success/Success_happy_hop.miianim", 54)]
    public void AirborneMotionsRejoinGroundedIkExactlyOnLanding(string path, int landing)
    {
        var animation = Get(path);
        Assert.Contains(animation.Events, e => e.Frame == landing && e.Name == "landed");
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var pose = rig.Evaluate(animation, landing);
            Assert.Equal(0, animation.Evaluate(TrackId.Bone(MiiBone.Root, Channel.PosY), landing));
            foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
            {
                Assert.Equal(0, animation.Evaluate(TrackId.Limb(leg, Channel.IkBlend), landing - 1));
                Assert.Equal(1, pose.Limbs[(int)leg].IkBlend);
                var foot = MiiSkeletonInfo.Chain(leg).End;
                Assert.True(Vector3.Distance(pose.WorldPosition[(int)foot], rig.RestPose.WorldPosition[(int)foot]) < 0.02f);
                var expectedRotation = rig.RestPose.WorldRotation[(int)foot];
                Assert.True(MathF.Abs(Quaternion.Dot(pose.WorldRotation[(int)foot], expectedRotation)) > 0.9999f);
            }
        }
    }

    [Fact]
    public void HappyHopHasAirborneShoesAndAFkTuck()
    {
        var animation = SharedAnimations.SuccessHappyHop();
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var apex = rig.Evaluate(animation, 38);
        foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
        {
            var (_, lower, foot) = MiiSkeletonInfo.Chain(leg);
            Assert.Equal(0, apex.Limbs[(int)leg].IkBlend);
            Assert.True(animation.Evaluate(TrackId.Bone(lower, Channel.RotX), 38) > 35);
            Assert.True(apex.WorldPosition[(int)foot].Y > rig.RestPose.WorldPosition[(int)foot].Y + 12);
        }
    }

    [Fact]
    public void WaitingTapLiftsOneShoeAndKeepsTheOtherPlanted()
    {
        var animation = SharedAnimations.LoadingFootTap();
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var lifted = rig.Evaluate(animation, 52);
        var landed = rig.Evaluate(animation, 60);
        Assert.True(lifted.WorldPosition[(int)MiiBone.FootR].Y > landed.WorldPosition[(int)MiiBone.FootR].Y + 3);
        for (var frame = 0; frame <= animation.Length; frame++)
            Assert.True(Vector3.Distance(rig.Evaluate(animation, frame).WorldPosition[(int)MiiBone.FootL],
                rig.RestPose.WorldPosition[(int)MiiBone.FootL]) < 0.02f);
        Assert.Equal(new[] { 60, 88, 116, 144 }, animation.Events.Where(e => e.Name == "tap").Select(e => e.Frame).ToArray());
    }

    [Theory]
    [InlineData("shared/disappear/Disappear_goodbye_poof.miianim", 96)]
    [InlineData("shared/disappear/Disappear_balloon_away.miianim", 132)]
    public void DeparturesStayHiddenAfterVanishing(string path, int vanished)
    {
        var animation = Get(path);
        Assert.Contains(animation.Events, e => e.Frame == vanished && e.Name == "vanished");
        Assert.Equal(1, animation.Evaluate(TrackId.Visible, vanished - 1));
        for (var frame = vanished; frame <= animation.Length; frame++)
            Assert.Equal(0, animation.Evaluate(TrackId.Visible, frame));
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void ClipsRoundTripLosslesslyAndHaveCompleteTransitions(string path)
    {
        var animation = Get(path);
        var bytes = MiiAnimFormat.Write(animation);
        var read = MiiAnimFormat.Read(bytes);
        Assert.Equal(bytes, MiiAnimFormat.Write(read));
        Assert.Null(read.AuthorMii);
        Assert.Empty(read.Actors);
        Assert.Equal(60, read.Fps);
        Assert.All(read.Tracks.Values.SelectMany(c => c.Keys), key =>
        {
            Assert.InRange(key.Frame, 0, read.Length);
            Assert.True(float.IsFinite(key.Value));
        });
        Assert.All(read.Events, e => Assert.InRange(e.Frame, 0, read.Length));
        Assert.All(read.Particles, effect => Assert.True(effect.EndFrame(read.Fps) < read.Length,
            $"{path}: {effect.Name} is still alive when done fires"));
        if (!SharedAnimations.IsLoop(path))
            Assert.Single(read.Events, e => e.Name == "done" && e.Frame == read.Length);
        for (var frame = 0f; frame <= read.Length; frame += 0.5f)
        foreach (var id in animation.Tracks.Keys)
            Assert.Equal(animation.Evaluate(id, frame), read.Evaluate(id, frame));
    }
}

public class SharedAnimationWriter
{
    [Fact]
    public void WriteSharedAnimations()
    {
        foreach (var (path, create) in SharedAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }
}
