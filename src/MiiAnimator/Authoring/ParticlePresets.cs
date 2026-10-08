using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;

namespace MiiAnimator.Authoring;

/// <summary>Ready-made particle effects to start from (the animator's "Add effect" menu).</summary>
public static class ParticlePresets
{
    public static IReadOnlyList<(string Name, string Description, Func<ParticleEffect> Create)> All { get; } =
    [
        ("Poof", "A puffy cloud burst in the Mii's favourite colour.", Poof),
        ("Dust ring", "Dust kicked up along the ground around the feet.", DustRing),
        ("Sparks", "Glowing sparks shooting out and falling.", Sparks),
        ("Twinkles", "Sparkling stars drifting up.", Twinkles),
        ("Hearts", "Hearts floating up.", Hearts),
    ];

    public static ParticleEffect Poof() =>
        new()
        {
            Name = "Poof",
            Count = 26,
            AttachBone = (byte)MiiBone.Chest,
            SpawnRadius = new Vector3(14f),
            Direction = Vector3.Zero,
            Spread = 25f,
            SpeedMin = 50f,
            SpeedMax = 120f,
            Gravity = 25f,
            Drag = 4f,
            SpinMin = -90f,
            SpinMax = 90f,
            Shape = BuiltInShapes.Puff,
            LifeMin = 0.55f,
            LifeMax = 0.9f,
            SizeStart = 24f,
            SizeEnd = 40f,
            ColorStart = new Vector4(1f, 1f, 1f, 0.95f),
            ColorEnd = new Vector4(1f, 1f, 1f, 0f),
            Tint = ParticleTint.FavoriteColor,
        };

    public static ParticleEffect DustRing() =>
        new()
        {
            Name = "Dust",
            Count = 22,
            Bone = null,
            SpawnRadius = new Vector3(22f, 0f, 22f),
            SpawnShell = 0.2f,
            Direction = Vector3.Zero,
            Spread = 20f,
            SpeedMin = 40f,
            SpeedMax = 90f,
            Gravity = 30f,
            Drag = 3.5f,
            StopAtFloor = true,
            SpinMin = -60f,
            SpinMax = 60f,
            Shape = BuiltInShapes.Puff,
            LifeMin = 0.5f,
            LifeMax = 0.85f,
            SizeStart = 14f,
            SizeEnd = 26f,
            ColorStart = new Vector4(0.86f, 0.8f, 0.7f, 0.85f),
            ColorEnd = new Vector4(0.86f, 0.8f, 0.7f, 0f),
        };

    public static ParticleEffect Sparks() =>
        new()
        {
            Name = "Sparks",
            Count = 36,
            AttachBone = (byte)MiiBone.Chest,
            SpawnRadius = new Vector3(6f),
            Direction = Vector3.UnitY,
            Spread = 70f,
            SpeedMin = 160f,
            SpeedMax = 300f,
            Gravity = -420f,
            Drag = 1.2f,
            Shape = BuiltInShapes.Spark,
            Orientation = ParticleOrientation.AlongVelocity,
            Stretch = 0.05f,
            LifeMin = 0.4f,
            LifeMax = 0.75f,
            SizeStart = 5f,
            SizeEnd = 2f,
            ColorStart = new Vector4(1f, 0.92f, 0.55f, 1f),
            ColorEnd = new Vector4(1f, 0.45f, 0.75f, 0f),
            Additive = true,
        };

    public static ParticleEffect Twinkles() =>
        new()
        {
            Name = "Twinkles",
            Count = 18,
            Duration = 30,
            AttachBone = (byte)MiiBone.Head,
            Offset = new Vector3(0f, 40f, 0f),
            SpawnRadius = new Vector3(60f),
            Direction = Vector3.UnitY,
            Spread = 40f,
            SpeedMin = 10f,
            SpeedMax = 35f,
            Drag = 1f,
            SpinMin = -120f,
            SpinMax = 120f,
            Shape = BuiltInShapes.Star,
            LifeMin = 0.5f,
            LifeMax = 0.9f,
            SizeStart = 14f,
            SizeEnd = 4f,
            ColorStart = new Vector4(1f, 0.95f, 0.7f, 1f),
            ColorEnd = new Vector4(1f, 1f, 1f, 0f),
            Additive = true,
        };

    public static ParticleEffect Hearts() =>
        new()
        {
            Name = "Hearts",
            Count = 10,
            Duration = 40,
            AttachBone = (byte)MiiBone.Head,
            Offset = new Vector3(0f, 30f, 10f),
            SpawnRadius = new Vector3(30f),
            Direction = Vector3.UnitY,
            Spread = 30f,
            SpeedMin = 30f,
            SpeedMax = 60f,
            Drag = 0.8f,
            SpinMin = -30f,
            SpinMax = 30f,
            Shape = BuiltInShapes.Heart,
            LifeMin = 0.9f,
            LifeMax = 1.3f,
            SizeStart = 16f,
            SizeEnd = 12f,
            ColorStart = new Vector4(1f, 0.4f, 0.55f, 1f),
            ColorEnd = new Vector4(1f, 0.55f, 0.7f, 0f),
        };
}
