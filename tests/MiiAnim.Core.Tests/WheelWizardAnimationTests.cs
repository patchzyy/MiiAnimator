using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class WheelWizardAnimationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Animations => WheelWizardAnimations.All.Select(a => new object[] { a.Name });

    private static (MiiAnimation Animation, bool Female) Get(string name)
    {
        var entry = WheelWizardAnimations.All.Single(a => a.Name == name);
        return (entry.Create(), entry.Female);
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void Hands_StayOutOfTheHeadAndBody(string name)
    {
        var (animation, female) = Get(name);
        var results = ClipCheck.Run(animation, female);
        var report = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
        output.WriteLine(report.Length == 0 ? "clean" : report);
        Assert.True(report.Length == 0, report);
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void EndsInTheRestPose_AndFeetStayOnTheGround(string name)
    {
        var (animation, female) = Get(name);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var last = rig.Evaluate(animation, animation.Length);
        foreach (var bone in MiiSkeletonInfo.AllBones)
            Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[(int)bone], last.WorldPosition[(int)bone]) < 0.5f, $"{bone} not at rest");

        var results = ClipCheck.Run(animation, female);
        // Ignore the underground part of the appear animation.
        var rootY = TrackId.Bone(MiiBone.Root, Channel.PosY);
        var sunk = results.Where(r => r.FootY < -1.5f && animation.Evaluate(rootY, r.Frame) >= 0).ToList();
        Assert.True(sunk.Count == 0, $"feet below ground at {string.Join(",", sunk.Select(r => $"{r.Frame}:{r.FootY:0.0}"))}");
    }

    [Fact]
    public void Appear_StartsFullyUnderground()
    {
        var (animation, _) = Get("Appear");
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var shortest = MiiBodyModel.CalculateBodyScale(0, 0);
        var pose = rig.Evaluate(animation, 0);
        // Top of the tallest hair (~80 render units above the head origin) must be below the ground.
        var top = pose.HeadMatrixForRender(shortest).Translation.Y + 80f;
        Assert.True(top < 0, $"head top at {top}");
    }

    [Fact]
    public void Dump()
    {
        foreach (var (name, female, create) in WheelWizardAnimations.All)
        {
            var animation = create();
            var rig = new MiiRig(MiiBodyModel.Get(female));
            output.WriteLine($"== {name} ({MiiAnimFormat.Write(animation).Length} bytes)");
            var clip = ClipCheck.Run(animation, female);
            for (var f = 0; f <= animation.Length; f += 4)
            {
                var p = rig.Evaluate(animation, f);
                string V(MiiBone bone) => $"({p.WorldPosition[(int)bone].X,5:0},{p.WorldPosition[(int)bone].Y,5:0},{p.WorldPosition[(int)bone].Z,5:0})";
                output.WriteLine(
                    $"{f,4} root {V(MiiBone.Root)} handL {V(MiiBone.HandL)} handR {V(MiiBone.HandR)} kneeL {V(MiiBone.LegL2)} footL {V(MiiBone.FootL)} footR {V(MiiBone.FootR)} head {clip[f].Head:0.00} torso {clip[f].Torso:0.00} footY {clip[f].FootY:0.0} {p.Expression}"
                );
            }
        }
    }
}

public class WheelWizardAnimationWriter
{
    [Fact]
    public void WriteWheelWizardAnimations()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "wheelwizard");
        Directory.CreateDirectory(dir);
        foreach (var (name, _, create) in WheelWizardAnimations.All)
            File.WriteAllBytes(Path.Combine(dir, name + MiiAnimFormat.FileExtension), MiiAnimFormat.Write(create()));
    }
}
