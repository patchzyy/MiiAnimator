using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class FormatAndCurveTests(ITestOutputHelper output)
{
    private static MiiAnimation BuildSample()
    {
        var animation = new MiiAnimation
        {
            Name = "Wave 👋",
            Fps = 60,
            Length = 120,
            AuthorMii = Enumerable.Range(0, 74).Select(i => (byte)(i * 3)).ToArray(),
        };
        var rot = animation.GetOrCreateCurve(TrackId.Bone(MiiBone.ArmR1, Channel.RotZ));
        rot.SetKey(0, 0f);
        rot.SetKey(15, 72.5f, Interpolation.Overshoot);
        rot.SetKey(30, -12.25f, Interpolation.Hold);
        rot.SetKey(119, 0f);
        var custom = new Keyframe(60, 1.234567f, Interpolation.Custom) { HandleOut = new(0.2f, 5.5f), HandleIn = new(0.45f, -3.125f) };
        rot.SetKey(custom);

        var blend = animation.GetOrCreateCurve(TrackId.Limb(MiiLimb.ArmR, Channel.IkBlend));
        blend.SetKey(0, 0f);
        blend.SetKey(40, 0.5f);
        blend.SetKey(80, 1f);

        var y = animation.GetOrCreateCurve(TrackId.Bone(MiiBone.Root, Channel.PosY));
        y.SetKey(0, 0f);
        y.SetKey(30, 13.37f, Interpolation.EaseOut);
        y.SetKey(60, 0f, Interpolation.Bouncy);

        var expression = animation.GetOrCreateCurve(TrackId.Expression);
        expression.SetKey(0, (float)MiiExpression.Smile);
        expression.SetKey(50, (float)MiiExpression.Blink);
        expression.SetKey(55, (float)MiiExpression.Smile);
        return animation;
    }

    [Fact]
    public void Format_RoundTripsExactly()
    {
        var original = BuildSample();
        var bytes = MiiAnimFormat.Write(original);
        var loaded = MiiAnimFormat.Read(bytes);
        output.WriteLine($"{bytes.Length} bytes for {original.Tracks.Sum(t => t.Value.Count)} keys (+74 byte Mii)");

        Assert.Equal(original.Name, loaded.Name);
        Assert.Equal(original.Fps, loaded.Fps);
        Assert.Equal(original.Length, loaded.Length);
        Assert.Equal(original.AuthorMii, loaded.AuthorMii);
        Assert.Equal(original.Tracks.Count, loaded.Tracks.Count);
        foreach (var (id, curve) in original.Tracks)
        {
            var other = loaded.Tracks[id];
            Assert.Equal(curve.Count, other.Count);
            for (var i = 0; i < curve.Count; i++)
            {
                var a = curve.Keys[i];
                var b = other.Keys[i];
                Assert.Equal(a.Frame, b.Frame);
                Assert.Equal(a.Value, b.Value);
                Assert.Equal(a.Interpolation, b.Interpolation);
                if (a.Interpolation == Interpolation.Custom)
                {
                    Assert.Equal(a.HandleOut, b.HandleOut);
                    Assert.Equal(a.HandleIn, b.HandleIn);
                }
            }
        }

        for (var f = 0f; f < 120f; f += 0.5f)
        foreach (var id in original.Tracks.Keys)
            Assert.Equal(original.Evaluate(id, f), loaded.Evaluate(id, f));
    }

    [Fact]
    public void Format_IsCompact()
    {
        var animation = new MiiAnimation();
        foreach (var bone in MiiSkeletonInfo.AllBones)
        {
            foreach (var channel in new[] { Channel.RotX, Channel.RotY, Channel.RotZ })
            {
                var curve = animation.GetOrCreateCurve(TrackId.Bone(bone, channel));
                for (var f = 0; f <= 120; f += 10)
                    curve.SetKey(f, MathF.Round(MathF.Sin(f * 0.1f + (int)bone) * 4500f) / 100f);
            }
        }

        var bytes = MiiAnimFormat.Write(animation);
        var keys = animation.Tracks.Sum(t => t.Value.Count);
        output.WriteLine($"{bytes.Length} bytes, {keys} keys, {(bytes.Length / (float)keys):F2} bytes/key");
        Assert.True(bytes.Length / (float)keys < 4f);
    }

    [Fact]
    public void Format_RejectsCorruption()
    {
        var bytes = MiiAnimFormat.Write(BuildSample());
        bytes[20] ^= 0xFF;
        Assert.Throws<InvalidDataException>(() => MiiAnimFormat.Read(bytes));
    }

    [Fact]
    public void Smooth_DoesNotOvershoot()
    {
        var curve = new AnimCurve();
        curve.SetKey(0, 0f);
        curve.SetKey(10, 10f);
        curve.SetKey(20, 10f);
        curve.SetKey(30, 0f);
        for (var f = 0f; f <= 30f; f += 0.25f)
        {
            var v = curve.Evaluate(f, 0f);
            Assert.InRange(v, -1e-4f, 10.0001f);
        }
    }

    [Fact]
    public void Presets_HitKeysExactly()
    {
        foreach (var interpolation in InterpolationInfo.Presets)
        {
            var curve = new AnimCurve();
            curve.SetKey(0, 2f, interpolation);
            curve.SetKey(10, 8f);
            Assert.Equal(2f, curve.Evaluate(0f, 0f), 4);
            Assert.Equal(8f, curve.Evaluate(10f, 0f), 4);
        }
    }

    [Fact]
    public void Custom_DefaultHandlesMatchLinear()
    {
        var curve = new AnimCurve();
        curve.SetKey(new Keyframe(0, 0f, Interpolation.Custom) { HandleOut = new(1f / 3f, 10f / 3f), HandleIn = new(1f / 3f, -10f / 3f) });
        curve.SetKey(30, 10f);
        Assert.Equal(5f, curve.Evaluate(15f, 0f), 2);
    }

    [Fact]
    public void Animation_WrapsTime()
    {
        var animation = new MiiAnimation { Fps = 60, Length = 120 };
        Assert.Equal(30f, animation.FrameAtTime(2.5), 3);
    }
}
