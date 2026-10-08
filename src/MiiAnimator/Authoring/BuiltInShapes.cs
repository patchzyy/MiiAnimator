using MiiAnim.Core.Animation;

namespace MiiAnimator.Authoring;

/// <summary>
/// The particle images the animator offers out of the box (SVGs in Assets/ParticleShapes). They're ordinary
/// <see cref="ParticleShape"/>s: a file that uses one carries its own copy, so players don't need these.
/// </summary>
public static class BuiltInShapes
{
    public static ParticleShape Dot { get; } = Load("Dot");

    /// <summary>Fluffy cloud puff (dust, smoke, poof), shaded a little darker at the bottom.</summary>
    public static ParticleShape Puff { get; } = Load("Puff");

    /// <summary>Narrow soft streak; use with <see cref="ParticleOrientation.AlongVelocity"/>.</summary>
    public static ParticleShape Spark { get; } = Load("Spark");

    /// <summary>Four-pointed twinkle.</summary>
    public static ParticleShape Star { get; } = Load("Star");

    public static ParticleShape Heart { get; } = Load("Heart");

    /// <summary>Hollow ring (shockwaves, bubbles).</summary>
    public static ParticleShape Ring { get; } = Load("Ring");

    /// <summary>Narrow wavy stroke for frustration or gloom marks; use with <see cref="ParticleOrientation.Upright"/>.</summary>
    public static ParticleShape Squiggle { get; } = Load("Squiggle");

    public static IReadOnlyList<ParticleShape> All { get; } = [Dot, Puff, Spark, Star, Heart, Ring, Squiggle];

    private static ParticleShape Load(string name)
    {
        using var stream =
            typeof(BuiltInShapes).Assembly.GetManifestResourceStream($"ParticleShapes/{name}.svg")
            ?? throw new InvalidOperationException($"Built-in particle shape '{name}' is missing.");
        using var reader = new StreamReader(stream);
        return new ParticleShape(name, reader.ReadToEnd());
    }
}
