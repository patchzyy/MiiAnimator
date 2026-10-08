using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests;

/// <summary>The file carries its particle images and everything else a player needs; players know no shapes.</summary>
public class ParticleShapeFormatTests
{
    private sealed class FixedStage : IParticleStage
    {
        public Vector3 AttachPoint(int actor, int frame, MiiBone? bone) => Vector3.Zero;

        public Vector3 FavoriteColor(int actor) => Vector3.One;
    }

    private static MiiAnimation WithEffects(params ParticleEffect[] effects)
    {
        var animation = new MiiAnimation { Name = "Shapes", Length = 60 };
        animation.Particles.AddRange(effects);
        return animation;
    }

    [Fact]
    public void ShapesAreStoredOnceAndComeBackEqual()
    {
        var custom = new ParticleShape("Mine", BuiltInShapes.Heart.Svg);
        var animation = WithEffects(
            new ParticleEffect { Shape = BuiltInShapes.Puff },
            new ParticleEffect { Shape = BuiltInShapes.Puff },
            new ParticleEffect { Shape = custom },
            new ParticleEffect { Shape = null }
        );

        var bytes = MiiAnimFormat.Write(animation);
        var read = MiiAnimFormat.Read(bytes);

        Assert.Equal(BuiltInShapes.Puff, read.Particles[0].Shape);
        Assert.Same(read.Particles[0].Shape, read.Particles[1].Shape);
        // Same image as the built-in heart, so it's the same shape.
        Assert.Equal(BuiltInShapes.Heart, read.Particles[2].Shape);
        Assert.Null(read.Particles[3].Shape);
        Assert.Equal(bytes, MiiAnimFormat.Write(read));
        var size = BuiltInShapes.Puff.Svg.Length + BuiltInShapes.Heart.Svg.Length;
        Assert.InRange(bytes.Length, size, size + 600);
    }

    [Fact]
    public void LookFieldsRoundTrip()
    {
        var effect = new ParticleEffect
        {
            Shape = BuiltInShapes.Spark,
            Orientation = ParticleOrientation.AlongVelocity,
            Aspect = 0.35f,
            Stretch = 0.05f,
            SpawnRadius = new Vector3(22, 0, 11),
            SpawnShell = 0.2f,
        };
        var read = Assert.Single(MiiAnimFormat.Read(MiiAnimFormat.Write(WithEffects(effect))).Particles);
        Assert.Equal(ParticleOrientation.AlongVelocity, read.Orientation);
        Assert.Equal(0.35f, read.Aspect);
        Assert.Equal(0.05f, read.Stretch);
        Assert.Equal(new Vector3(22, 0, 11), read.SpawnRadius);
        Assert.Equal(0.2f, read.SpawnShell);
    }

    [Fact]
    public void UnknownChunksAreSkipped()
    {
        var bytes = MiiAnimFormat.Write(WithEffects(new ParticleEffect { Shape = BuiltInShapes.Star }));
        // Insert a chunk from some future version right after the version byte, and fix the checksum.
        var future = new List<byte>(bytes[..5]);
        future.AddRange(Encoding.ASCII.GetBytes("ZZZZ"));
        future.Add(3);
        future.AddRange([1, 2, 3]);
        future.AddRange(bytes[5..^4]);
        var body = future.ToArray();
        var withCrc = new byte[body.Length + 4];
        body.CopyTo(withCrc, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(withCrc.AsSpan(body.Length), Crc(body));

        var read = MiiAnimFormat.Read(withCrc);
        Assert.Equal("Shapes", read.Name);
        Assert.Equal(BuiltInShapes.Star, Assert.Single(read.Particles).Shape);
    }

    [Fact]
    public void ShapesMustBeSvg()
    {
        Assert.Throws<ArgumentException>(() => new ParticleShape("Text", "not an image"));
        Assert.Throws<ArgumentException>(() => new ParticleShape("Html", "<html><body /></html>"));
        Assert.Throws<ArgumentException>(() => new ParticleShape("Broken", "<svg><circle></svg>"));
        Assert.Throws<ArgumentException>(
            () => new ParticleShape("Dtd", """<!DOCTYPE svg [<!ENTITY a "aaaa">]><svg xmlns="http://www.w3.org/2000/svg">&a;</svg>""")
        );
        Assert.Equal("Dot", new ParticleShape(" Dot ", """<svg xmlns="http://www.w3.org/2000/svg"><circle r="1" /></svg>""").Name);
    }

    [Fact]
    public void FlatThinSpawnAreaIsARing()
    {
        var effect = new ParticleEffect
        {
            Count = 200,
            Bone = null,
            SpawnRadius = new Vector3(20, 0, 20),
            SpawnShell = 0.2f,
            SpeedMin = 0,
            SpeedMax = 0,
            Drag = 0,
            LifeMin = 1,
            LifeMax = 1,
            SizeStart = 0.001f,
            SizeEnd = 0.001f,
        };
        var particles = new List<Particle>();
        ParticleEvaluator.Evaluate(WithEffects(effect), 1, new FixedStage(), particles);
        Assert.Equal(200, particles.Count);
        foreach (var particle in particles)
        {
            var flat = new Vector2(particle.Position.X, particle.Position.Z) / MiiStage.ParticleScale;
            Assert.Equal(0f, particle.Position.Y, 4);
            Assert.InRange(flat.Length(), 20 * 0.8f - 0.01f, 20 + 0.01f);
        }
    }

    [Fact]
    public void EverySampleReadsAndWritesBackTheSame()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));
        foreach (var file in Directory.GetFiles(root, "*" + MiiAnimFormat.FileExtension, SearchOption.AllDirectories))
        {
            var bytes = File.ReadAllBytes(file);
            Assert.Equal(bytes, MiiAnimFormat.Write(MiiAnimFormat.Read(bytes)));
        }
    }

    private static uint Crc(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
