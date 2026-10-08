using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class ProfileExtraAnimationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Animations => ProfileExtraAnimations.All.Select(a => new object[] { a.Path });

    private static MiiAnimation Get(string path) => ProfileExtraAnimations.All.Single(a => a.Path == path).Create();

    [Theory]
    [MemberData(nameof(Animations))]
    public void Hands_ClearTheHeadBodyAndCard(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var window = path.Contains("/window/") ? WindowAnimations.Window : null;
            var results = ClipCheck.Run(animation, female, window: window);
            var report = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
            output.WriteLine($"{path}, female={female}: {(report.Length == 0 ? "clean" : report)}");
            Assert.True(report.Length == 0, report);
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void BothFeetRemainPlantedWithoutDisablingIk(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            for (var frame = 0; frame <= animation.Length; frame++)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
                {
                    Assert.Equal(1f, animation.Evaluate(TrackId.Limb(leg, Channel.IkBlend), frame));
                    var foot = MiiSkeletonInfo.Chain(leg).End;
                    var at = pose.WorldPosition[(int)foot];
                    var planted = rig.RestPose.WorldPosition[(int)foot];
                    Assert.True(Vector3.Distance(at, planted) < 0.5f, $"{path}: {foot} slides at frame {frame}: {at} instead of {planted}");
                    Assert.False(pose.Limbs[(int)leg].Stretched, $"{path}: {leg} stretches at frame {frame}");
                }
            }

            var results = ClipCheck.Run(animation, female);
            var sunk = results.Where(r => r.FootY < -1.5f).ToArray();
            Assert.True(sunk.Length == 0, $"{path}: feet below ground at {string.Join(", ", sunk.Select(r => $"{r.Frame}:{r.FootY:0.0}"))}");
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void RestEndpointsMakeActionLoopsAndIdleTransitionsSeamless(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            foreach (var frame in new[] { 0, animation.Length })
            {
                var pose = rig.Evaluate(animation, frame);
                Assert.Equal(MiiExpression.Normal, pose.Expression);
                foreach (var bone in MiiSkeletonInfo.AllBones)
                {
                    var i = (int)bone;
                    Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[i], pose.WorldPosition[i]) < 0.5f, $"{path}: {bone} is not at rest on frame {frame}");
                    Assert.True(MathF.Abs(Quaternion.Dot(rig.RestPose.WorldRotation[i], pose.WorldRotation[i])) > 0.9999f, $"{path}: {bone} rotation changes at the loop boundary");
                }
            }
        }

        // The pose holds at each side of the loop, so the seam does not produce a sudden velocity change.
        foreach (var id in animation.Tracks.Keys)
        {
            Assert.Equal(animation.Evaluate(id, 0), animation.Evaluate(id, 1));
            Assert.Equal(animation.Evaluate(id, animation.Length), animation.Evaluate(id, animation.Length - 1));
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void FileRoundTripPreservesPosesExpressionsAndActionMarkers(string path)
    {
        var animation = Get(path);
        var bytes = MiiAnimFormat.Write(animation);
        var loaded = MiiAnimFormat.Read(bytes);
        Assert.Equal(bytes, MiiAnimFormat.Write(loaded));
        Assert.Equal(animation.Name, loaded.Name);
        Assert.Equal(animation.Fps, loaded.Fps);
        Assert.Equal(animation.Length, loaded.Length);
        Assert.Equal(animation.Events, loaded.Events);
        Assert.Equal(animation.Tracks.Count, loaded.Tracks.Count);
        Assert.Contains(loaded.Events, e => e.Frame == animation.Length && e.Name == "done");
        Assert.Contains(loaded.Events, e => e.Frame > 0 && e.Frame < animation.Length && e.Name != "done");

        for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
        foreach (var id in animation.Tracks.Keys)
        {
            Assert.Equal(animation.Evaluate(id, frame), loaded.Evaluate(id, frame));
            Assert.True(float.IsFinite(loaded.Evaluate(id, frame)), $"{path}: {id} is non-finite at {frame}");
        }
    }

    [Fact]
    public void WriteNewProfileAnimations()
    {
        foreach (var (path, create) in ProfileExtraAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }
}
