using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;
using Xunit;
using Xunit.Abstractions;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests;

public class ActorAndParticleTests(ITestOutputHelper output)
{
    private static readonly MiiRig Rig = new(MiiBodyModel.Get(false));

    private sealed class FixedStage : IParticleStage
    {
        public Vector3 Point { get; set; } = new(0, 50, 0);
        public Vector3 Color { get; set; } = new(1, 0, 0);

        public Vector3 AttachPoint(int actor, int frame, MiiBone? bone) => Point + new Vector3(actor * 100, 0, 0);

        public Vector3 FavoriteColor(int actor) => Color;
    }

    [Fact]
    public void ActorsAndParticles_RoundTripThroughTheFile()
    {
        var animation = new MiiAnimation { Name = "Two", Length = 90 };
        animation.GetOrCreateCurve(TrackId.Bone(MiiBone.Head, Channel.RotY)).SetKey(10, 20f);
        var old = animation.AddActor("old");
        old.PreviewMii = Enumerable.Range(0, MiiAnimation.MiiDataLength).Select(i => (byte)i).ToArray();
        old.Motion.GetOrCreateCurve(TrackId.Bone(MiiBone.Root, Channel.PosX)).SetKey(30, -150f);
        old.Motion.GetOrCreateCurve(TrackId.Visible).SetKey(60, 0f, Interpolation.Hold);
        var effect = ParticlePresets.Sparks();
        effect.StartFrame = 12;
        effect.Actor = 1;
        effect.Offset = new Vector3(1.5f, -2, 3);
        effect.ColorStart = new Vector4(1, 0.5f, 0, 1);
        effect.StopAtFloor = true;
        animation.Particles.Add(effect);
        animation.AddEvent(60, "old_gone");

        var read = MiiAnimFormat.Read(MiiAnimFormat.Write(animation));

        Assert.Equal(2, read.ActorCount);
        Assert.Equal("old", read.Actors[0].Name);
        Assert.Equal(old.PreviewMii, read.Actors[0].PreviewMii);
        Assert.Equal(-150f, read.ForActor(1).Evaluate(TrackId.Bone(MiiBone.Root, Channel.PosX), 30));
        Assert.Equal(20f, read.ForActor(0).Evaluate(TrackId.Bone(MiiBone.Head, Channel.RotY), 10));
        Assert.Equal(1, read.FindActor("OLD"));
        // Actor motions share the timing, events and particles.
        Assert.Equal(90, read.ForActor(1).Length);
        Assert.Single(read.ForActor(1).Events);
        Assert.Same(read.Particles, read.ForActor(1).Particles);

        var p = Assert.Single(read.Particles);
        Assert.Equal(effect.Name, p.Name);
        Assert.Equal(12, p.StartFrame);
        Assert.Equal(1, p.Actor);
        Assert.Equal(effect.Offset, p.Offset);
        Assert.Equal(effect.Gravity, p.Gravity);
        Assert.Equal(BuiltInShapes.Spark, p.Shape);
        Assert.True(p.Additive && p.StopAtFloor);
        Assert.Equal(new Vector4(1, 128 / 255f, 0, 1), p.ColorStart);
        Assert.Equal(MiiAnimFormat.Write(animation), MiiAnimFormat.Write(read));
    }

    [Fact]
    public void Clone_CopiesActorsAndParticlesDeeply()
    {
        var animation = ProfileAnimations.ChangeMii();
        var clone = animation.ForActor(1).Clone();
        Assert.Equal(MiiAnimFormat.Write(animation), MiiAnimFormat.Write(clone));
        clone.Particles[0].Count = 1;
        clone.ForActor(1).Tracks.Clear();
        Assert.NotEqual(1, animation.Particles[0].Count);
        Assert.NotEmpty(animation.ForActor(1).Tracks);
    }

    [Fact]
    public void Particles_AreAPureFunctionOfTheFrame()
    {
        var animation = new MiiAnimation { Length = 120 };
        var effect = ParticlePresets.Poof();
        effect.StartFrame = 10;
        animation.Particles.Add(effect);
        var stage = new FixedStage();

        List<Particle> At(float frame)
        {
            var list = new List<Particle>();
            ParticleEvaluator.Evaluate(animation, frame, stage, list);
            return list;
        }

        Assert.Empty(At(9));
        Assert.Equal(effect.Count, At(10.5f).Count);
        Assert.Equal(At(25), At(25)); // same frame, same particles
        Assert.Empty(At(10 + effect.LifeMax * animation.Fps + 1));

        // Tinted by the Mii's favourite colour (red here): no green or blue left.
        Assert.All(At(20), particle => Assert.True(particle.Color.Y == 0 && particle.Color.Z == 0 && particle.Color.X > 0));

        // A burst spreads out from its attach point and fades.
        var early = At(11);
        var late = At(40);
        float Spread(List<Particle> ps) => ps.Average(x => Vector3.Distance(x.Position, stage.Point));
        Assert.True(Spread(late) > Spread(early));
        Assert.True(late.Average(x => x.Color.W) < early.Average(x => x.Color.W));
    }

    [Fact]
    public void Particles_StreamOverTheirDurationAndStopAtTheFloor()
    {
        var animation = new MiiAnimation { Length = 200 };
        var dust = ParticlePresets.DustRing();
        dust.Duration = 60;
        dust.Count = 60;
        dust.Gravity = -500;
        dust.LifeMin = dust.LifeMax = 2;
        animation.Particles.Add(dust);
        var stage = new FixedStage { Point = Vector3.Zero };
        var list = new List<Particle>();
        ParticleEvaluator.Evaluate(animation, 30, stage, list);
        Assert.InRange(list.Count, 25, 35);
        list.Clear();
        ParticleEvaluator.Evaluate(animation, 150, stage, list);
        Assert.All(list, particle => Assert.True(particle.Position.Y >= particle.Size * 0.5f - 1e-3f));
    }

    [Fact]
    public void RigStage_AttachesToTheActorsBone()
    {
        var animation = ProfileAnimations.ChangeMii();
        var stage = new RigParticleStage(animation, _ => Rig, _ => MiiStage.DefaultBodyScale, _ => Vector3.One);
        var hand = stage.AttachPoint(0, ProfileAnimations.ShoveFrame, MiiBone.HandL);
        var oldGround = stage.AttachPoint(1, 136, null);
        var expectedHand = MiiStage.PointOf(Rig.Evaluate(animation, ProfileAnimations.ShoveFrame), MiiStage.DefaultBodyScale, MiiBone.HandL);
        Assert.True(Vector3.Distance(hand, expectedHand) < 1e-3f);
        Assert.Equal(0f, oldGround.Y);
        Assert.True(oldGround.X < -60f, $"The old Mii landed at {oldGround}.");
    }

    [Fact]
    public void Visible_HidesAnActorAfterItsKey()
    {
        var animation = ProfileAnimations.ChangeMii();
        Assert.True(Rig.Evaluate(animation.ForActor(1), ProfileAnimations.OldGoneFrame - 1).Visible);
        Assert.False(Rig.Evaluate(animation.ForActor(1), ProfileAnimations.OldGoneFrame).Visible);
        Assert.True(Rig.Evaluate(animation.ForActor(0), ProfileAnimations.OldGoneFrame).Visible);
    }

    [Fact]
    public void TravelCorrection_MakesEveryMiiWalkTheSameDistance()
    {
        var pose = Rig.Evaluate(id => id == TrackId.Bone(MiiBone.Root, Channel.PosX) ? 100f : float.NaN);
        var small = MiiBodyModel.CalculateBodyScale(0, 0);
        var big = MiiBodyModel.CalculateBodyScale(127, 127);
        var a = MiiStage.PointOf(pose, small, null);
        var b = MiiStage.PointOf(pose, big, null);
        Assert.Equal(a.X, b.X, 3);
        Assert.Equal(MiiStage.PointOf(pose, MiiStage.DefaultBodyScale, null).X, a.X, 3);
    }

    // ---------- The change-Mii demo ----------

    [Fact]
    public void ChangeMii_FeetStayOnTheGroundAndReachTheirTargets()
    {
        var animation = ProfileAnimations.ChangeMii();
        var problems = new List<string>();
        for (var actor = 0; actor < animation.ActorCount; actor++)
        {
            var motion = animation.ForActor(actor);
            for (var frame = 0; frame <= ProfileAnimations.OldGoneFrame; frame++)
            {
                var pose = Rig.Evaluate(motion, frame);
                foreach (var leg in new[] { MiiLimb.LegL, MiiLimb.LegR })
                {
                    // IK targets are before the global squash (Root stretch), so compare unsquashed.
                    var miss = Vector3.Distance(pose.WorldPosition[(int)MiiSkeletonInfo.Chain(leg).End], pose.Limbs[(int)leg].Target);
                    if (pose.Limbs[(int)leg].IkBlend >= 0.99f && miss > 1f)
                        problems.Add($"actor {actor} {leg} misses by {miss:0.0} at {frame}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void ChangeMii_ArmsStayOutOfTheirOwnBodies()
    {
        var animation = ProfileAnimations.ChangeMii();
        for (var actor = 0; actor < animation.ActorCount; actor++)
        {
            var results = ClipCheck.Run(animation.ForActor(actor), female: false);
            var clipping = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
            output.WriteLine($"actor {actor}:\n{clipping}");
            Assert.True(clipping.Length == 0, $"actor {actor}:\n{clipping}");
            var lowest = results.Where(r => r.Frame <= ProfileAnimations.OldGoneFrame).MinBy(r => r.FootY)!;
            Assert.True(lowest.FootY > -1f, $"actor {actor}: a foot goes {-lowest.FootY:0.0} into the ground at {lowest.Frame}");
        }
    }

    [Fact]
    public void ChangeMii_HeadsNeverOverlapAndPalmsMeet()
    {
        var animation = ProfileAnimations.ChangeMii();
        var scale = MiiStage.DefaultBodyScale;
        var worst = float.MaxValue;
        var worstFrame = -1;
        for (var frame = 0; frame <= ProfileAnimations.OldGoneFrame; frame++)
        {
            var a = Rig.Evaluate(animation.ForActor(0), frame);
            var b = Rig.Evaluate(animation.ForActor(1), frame);
            var overlap = HeadOverlap(MiiStage.HeadToStage(a, scale), MiiStage.HeadToStage(b, scale));
            if (overlap < worst)
                (worst, worstFrame) = (overlap, frame);
        }

        output.WriteLine($"closest heads: {worst:0.00} at frame {worstFrame}");
        Assert.True(worst > 1f, $"Heads overlap at frame {worstFrame} ({worst:0.00}).");

        // Hands are close together (but not through each other) while they push.
        for (var frame = ProfileAnimations.ContactFrame; frame < ProfileAnimations.ShoveFrame; frame++)
        {
            var a = Rig.Evaluate(animation.ForActor(0), frame);
            var b = Rig.Evaluate(animation.ForActor(1), frame);
            var gapX = MiiStage.PointOf(a, scale, MiiBone.HandL).X - MiiStage.PointOf(b, scale, MiiBone.HandR).X;
            Assert.InRange(gapX, 4f, 18f);
        }
    }

    // Default FFL head ellipsoid (render units, head-local), same as ClipCheck.
    private static readonly Vector3 HeadCenter = new(0f, 35f, -3.7f);
    private static readonly Vector3 HeadRadii = new(28.8f, 35f, 32.4f);

    /// <summary>Smallest normalized distance of head A's surface into head B's ellipsoid (&lt; 1 = overlapping).</summary>
    private static float HeadOverlap(Matrix4x4 headA, Matrix4x4 headB)
    {
        Matrix4x4.Invert(headB, out var toB);
        var min = float.MaxValue;
        for (var i = 0; i < 24; i++)
        for (var j = -5; j <= 5; j++)
        {
            var yaw = i / 24f * MathF.Tau;
            var pitch = j / 11f * MathF.PI;
            var local = HeadCenter + HeadRadii * new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw));
            var inB = Vector3.Transform(Vector3.Transform(local, headA), toB);
            min = MathF.Min(min, ((inB - HeadCenter) / HeadRadii).Length());
        }

        return min;
    }
}

public class ProfileAnimationWriter
{
    [Fact]
    public void WriteProfileAnimations()
    {
        foreach (var (name, create) in ProfileAnimations.All)
            WheelWizardSampleWriter.Write(name, create());
    }
}
