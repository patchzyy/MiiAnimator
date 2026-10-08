using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using Xunit;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests;

public class SquiggleParticleTests
{
    private sealed class RedShirtStage : IParticleStage
    {
        public Vector3 AttachPoint(int actor, int frame, MiiBone? bone) => new(0, 80, 0);
        public Vector3 FavoriteColor(int actor) => Vector3.UnitX;
    }

    [Fact]
    public void PurpleFrustrationMarkSurvivesExportAndStaysStillRegardlessOfShirtColor()
    {
        var animation = new MiiAnimation { Length = 150, Fps = 60 };
        animation.Particles.Add(new ParticleEffect
        {
            Shape = BuiltInShapes.Squiggle,
            Orientation = ParticleOrientation.Upright,
            StartFrame = 20,
            Count = 1,
            AttachBone = (byte)MiiBone.Head,
            Offset = new Vector3(-30, 120, 35),
            SpawnRadius = Vector3.Zero,
            SpeedMin = 0,
            SpeedMax = 0,
            Gravity = 0,
            LifeMin = 1.5f,
            LifeMax = 1.5f,
            SizeStart = 30,
            SizeEnd = 30,
            ColorStart = new Vector4(0.55f, 0.2f, 0.85f, 1),
            ColorEnd = new Vector4(0.55f, 0.2f, 0.85f, 0),
            Tint = ParticleTint.None,
        });
        var read = MiiAnimFormat.Read(MiiAnimFormat.Write(animation));
        Assert.Equal(BuiltInShapes.Squiggle, Assert.Single(read.Particles).Shape);
        Assert.Equal(MiiAnimFormat.Write(animation), MiiAnimFormat.Write(read));

        var stage = new RedShirtStage();
        var early = new List<Particle>();
        var late = new List<Particle>();
        ParticleEvaluator.Evaluate(read, 25, stage, early);
        ParticleEvaluator.Evaluate(read, 80, stage, late);
        var a = Assert.Single(early);
        var b = Assert.Single(late);
        Assert.Equal(BuiltInShapes.Squiggle, a.Effect.Shape);
        Assert.Equal(ParticleOrientation.Upright, a.Effect.Orientation);
        Assert.Equal(a.Position, b.Position);
        Assert.Equal(Vector3.Zero, a.Velocity);
        Assert.True(a.Color.Z > a.Color.X && a.Color.X > a.Color.Y);
        Assert.Equal(new Vector3(a.Color.X, a.Color.Y, a.Color.Z), new Vector3(b.Color.X, b.Color.Y, b.Color.Z));
        Assert.True(b.Color.W < a.Color.W);
    }
}
