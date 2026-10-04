using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class TemplateTests(ITestOutputHelper output)
{
    private static readonly MiiRig Rig = new(MiiBodyModel.Get(female: false));

    [Fact]
    public void AllTemplates_EvaluateCleanly_AndLoop()
    {
        foreach (var (name, _, create) in Templates.All)
        {
            var animation = create();
            for (var f = 0f; f <= animation.Length; f += 0.5f)
            {
                var pose = Rig.Evaluate(animation, f);
                foreach (var m in pose.SkinMatrix)
                    Assert.False(float.IsNaN(m.M11) || float.IsNaN(m.M42), $"{name} NaN at {f}");
            }

            var first = Rig.Evaluate(animation, 0);
            var last = Rig.Evaluate(animation, animation.Length);
            foreach (var bone in MiiSkeletonInfo.AllBones)
                Assert.True(Vector3.Distance(first.WorldPosition[(int)bone], last.WorldPosition[(int)bone]) < 0.5f, $"{name} doesn't loop ({bone})");
            output.WriteLine($"{name}: {MiiAnimFormat.Write(animation).Length} bytes");
        }
    }

    [Fact]
    public void Wave_RaisesRightHandAboveHead()
    {
        var pose = Rig.Evaluate(Templates.Wave(), 50);
        var hand = pose.WorldPosition[(int)MiiBone.HandR];
        output.WriteLine($"hand {hand}, head {pose.WorldPosition[(int)MiiBone.Head]}");
        Assert.True(hand.Y > pose.WorldPosition[(int)MiiBone.Head].Y, "hand should be above the head joint");
        Assert.True(hand.X < 0, "right hand is on -X");
    }

    [Fact]
    public void Jump_LeavesTheGround()
    {
        var pose = Rig.Evaluate(Templates.Jump(), 36);
        var foot = pose.WorldPositionOf(MiiBone.FootL).Y;
        Assert.True(foot > Rig.RestPose.WorldPositionOf(MiiBone.FootL).Y + 15, $"foot only at {foot}");
    }

    [Fact]
    public void ArmAxes_RaiseSideways()
    {
        float Sample(TrackId id) => id == TrackId.Bone(MiiBone.ArmL1, Channel.RotZ) ? 66f : float.NaN;
        var pose = Rig.Evaluate(Sample);
        var shoulder = pose.WorldPosition[(int)MiiBone.ArmL1];
        var hand = pose.WorldPosition[(int)MiiBone.HandL];
        output.WriteLine($"shoulder {shoulder} hand {hand}");
        Assert.True(hand.X > shoulder.X + 30, "left arm should point out to +X");
        Assert.True(MathF.Abs(hand.Y - shoulder.Y) < 15, "and be roughly horizontal");
    }
}

public class SampleWriter
{
    /// <summary>Writes every template as a .miianim sample (handy for testing players).</summary>
    [Fact]
    public void WriteSamples()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples");
        Directory.CreateDirectory(dir);
        foreach (var (name, _, create) in Templates.All)
        {
            if (name == "Empty")
                continue;
            File.WriteAllBytes(Path.Combine(dir, name.Replace(' ', '_') + MiiAnimFormat.FileExtension), MiiAnimFormat.Write(create()));
        }
    }
}
