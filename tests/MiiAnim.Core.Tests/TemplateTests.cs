using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using Xunit.Abstractions;
using MiiAnimator.Authoring;

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StandingAnimations_KeepFeetPlantedAndArmsClear(bool female)
    {
        var rig = new MiiRig(MiiBodyModel.Get(female));
        foreach (var animation in new[] { Templates.IdleBreathing(), Templates.IdleWeightShift(), Templates.ShirtColorSurprise() })
        {
            var clipping = Authoring.ClipCheck.Describe(Authoring.ClipCheck.Run(animation, female), headLimit: 1f, torsoLimit: 0.8f);
            Assert.True(clipping.Length == 0, $"{animation.Name}: {clipping}");
            for (var frame = 0; frame <= animation.Length; frame++)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var foot in new[] { MiiBone.FootL, MiiBone.FootR })
                    Assert.True(Vector3.Distance(pose.WorldPositionOf(foot), rig.RestPose.WorldPositionOf(foot)) < 0.1f,
                        $"{animation.Name}: {foot} moved at {frame}");
            }
            var first = rig.Evaluate(animation, 0);
            var last = rig.Evaluate(animation, animation.Length);
            Assert.Equal(first.Expression, last.Expression);
            foreach (var bone in MiiSkeletonInfo.AllBones)
                Assert.True(MathF.Abs(Quaternion.Dot(first.WorldRotation[(int)bone], last.WorldRotation[(int)bone])) > 0.99999f);
        }
    }

    [Fact]
    public void ShirtSurprise_LooksDownAtTheShirtThenReturnsToNeutral()
    {
        var animation = Templates.ShirtColorSurprise();
        var inspection = Rig.Evaluate(animation, 60);
        var forward = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, inspection.HeadMatrix));
        Assert.True(forward.Y < -0.5f, "The face should point down toward the shirt.");
        Assert.Equal(MiiExpression.SurpriseOpenMouth, Rig.Evaluate(animation, 10).Expression);
        Assert.Equal(MiiExpression.Smile, Rig.Evaluate(animation, 150).Expression);
        Assert.Equal(MiiExpression.Normal, Rig.Evaluate(animation, animation.Length).Expression);
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
            var fileName = name.Replace(' ', '_');
            var animation = create();
            File.WriteAllBytes(Path.Combine(dir, fileName + MiiAnimFormat.FileExtension), MiiAnimFormat.Write(animation));
            if (fileName is "Idle_breathing" or "Idle_weight_shift" or "Shirt_color_surprise")
                Authoring.WheelWizardSampleWriter.Write(fileName, animation);
        }
    }
}
