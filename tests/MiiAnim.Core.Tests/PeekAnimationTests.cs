using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class PeekAnimationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Animations => PeekAnimations.All.Select(a => new object[] { a.Path });

    private static MiiAnimation Get(string path) => PeekAnimations.All.Single(a => a.Path == path).Create();

    /// <summary>A MiiPeeker of about this size (the profile page's license picker), with the edge at its bottom.</summary>
    private static readonly WheelWizardShot Peeker = WheelWizardShot.ProfileWindow with { Size = new Vector2(100, 96) };

    private static readonly Vector3 HeadCenter = new(0f, 35f, -3.7f);
    private static readonly Vector3 HeadTop = new(0f, 72f, -3.7f);

    [Theory]
    [MemberData(nameof(Animations))]
    public void Hands_ClearTheHeadBodyAndCard(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var results = ClipCheck.Run(animation, female, window: PeekAnimations.Window);
            var report = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
            output.WriteLine($"{path}, female={female}: {(report.Length == 0 ? "clean" : report)}");
            Assert.True(report.Length == 0, report);
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void HeadPeeksOverTheEdgeWithRoomForANametag(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        foreach (var (build, height) in new[] { (64f, 64f), (0f, 0f), (127f, 127f) })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var scale = MiiBodyModel.CalculateBodyScale(build, height);
            float lowest = float.MinValue, highest = float.MaxValue;
            for (var frame = 0; frame <= animation.Length; frame++)
            {
                var pose = rig.Evaluate(animation, frame);
                var center = Peeker.HeadToPixels(pose, HeadCenter, scale, female);
                var top = Peeker.HeadToPixels(pose, HeadTop, scale, female);
                lowest = MathF.Max(lowest, center.Y);
                highest = MathF.Min(highest, top.Y);
            }

            output.WriteLine($"{path} female={female} {build}/{height}: head centre lowest {lowest:0}px, head top highest {highest:0}px of {Peeker.Size.Y}");
            // The face stays above the edge (asleep the chin rests on it; the slip goes down to just the eyes), and the
            // head never leaves the top.
            var limit = path.Contains("slip") ? Peeker.Size.Y - 6 : Peeker.Size.Y - 18;
            Assert.True(lowest < limit, $"{path}: head sinks too far ({lowest:0}px)");
            Assert.True(highest > -6, $"{path}: head goes above the view ({highest:0}px)");
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void StartsAndEndsOnTheRightBaseline(string path)
    {
        var animation = Get(path);
        var asleep = PeekAnimations.AsleepBreathing();
        var awake = PeekAnimations.AwakeLookAround();
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var sleeping = rig.Evaluate(asleep, 0);
            var waking = rig.Evaluate(awake, 0);
            var start = rig.Evaluate(animation, 0);
            var end = rig.Evaluate(animation, animation.Length);
            Same(PeekAnimations.StartsAsleep(path) ? sleeping : waking, start, $"{path} start");
            Same(PeekAnimations.EndsAsleep(path) ? sleeping : waking, end, $"{path} end");
        }
    }

    private static void Same(MiiPose expected, MiiPose actual, string what)
    {
        Assert.Equal(expected.Expression, actual.Expression);
        foreach (var bone in MiiSkeletonInfo.AllBones)
        {
            var i = (int)bone;
            Assert.True(Vector3.Distance(expected.WorldPosition[i], actual.WorldPosition[i]) < 0.5f, $"{what}: {bone} at {actual.WorldPosition[i]} instead of {expected.WorldPosition[i]}");
            Assert.True(MathF.Abs(Quaternion.Dot(expected.WorldRotation[i], actual.WorldRotation[i])) > 0.9999f, $"{what}: {bone} is turned differently");
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void FileRoundTrips(string path)
    {
        var animation = Get(path);
        var bytes = MiiAnimFormat.Write(animation);
        var loaded = MiiAnimFormat.Read(bytes);
        Assert.Equal(bytes, MiiAnimFormat.Write(loaded));
        Assert.Equal(animation.Length, loaded.Length);
        Assert.Contains(loaded.Events, e => e.Frame == animation.Length && e.Name == "done");
    }

    [Fact]
    public void WritePeekAnimations()
    {
        foreach (var (path, create) in PeekAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }
}
