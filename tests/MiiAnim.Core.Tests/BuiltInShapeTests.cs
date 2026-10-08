using MiiAnim.Core.Animation;
using MiiAnimator.Authoring;
using SkiaSharp;
using Svg.Skia;

namespace MiiAnim.Core.Tests;

/// <summary>The built-in particle images are SVGs that players draw themselves; they must draw as intended.</summary>
public class BuiltInShapeTests
{
    /// <summary>Draws a shape the way the particle renderer does, 64 px tall.</summary>
    private static SKBitmap Draw(ParticleShape shape)
    {
        using var svg = new SKSvg();
        var picture = svg.FromSvg(shape.Svg);
        Assert.NotNull(picture);
        var bounds = picture.CullRect;
        var bitmap = new SKBitmap(new SKImageInfo((int)MathF.Round(64 * bounds.Width / bounds.Height), 64, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(bitmap.Width / bounds.Width, bitmap.Height / bounds.Height);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        return bitmap;
    }

    [Fact]
    public void EveryShapeDrawsWhiteInsideAndClearAtTheCorners()
    {
        foreach (var shape in BuiltInShapes.All)
        {
            using var bitmap = Draw(shape);
            var center = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
            // The ring and the squiggle may be hollow in the middle; look for their stroke instead.
            var brightest = Enumerable.Range(0, bitmap.Height).Max(y => bitmap.GetPixel(bitmap.Width / 2, y).Alpha);
            Assert.True(brightest > 200, $"{shape.Name} draws nothing ({brightest}).");
            if (shape != BuiltInShapes.Ring && shape != BuiltInShapes.Squiggle)
                Assert.True(center.Alpha > 120, $"{shape.Name} centre alpha {center.Alpha}");
            foreach (var (x, y) in new[] { (0, 0), (bitmap.Width - 1, 0), (0, bitmap.Height - 1), (bitmap.Width - 1, bitmap.Height - 1) })
                Assert.True(bitmap.GetPixel(x, y).Alpha < 10, $"{shape.Name} corner ({x}, {y})");
        }
    }

    [Fact]
    public void NarrowShapesKeepTheirProportions()
    {
        using var spark = Draw(BuiltInShapes.Spark);
        using var squiggle = Draw(BuiltInShapes.Squiggle);
        Assert.True(spark.Width < spark.Height / 2);
        Assert.True(squiggle.Width < squiggle.Height / 2);
    }
}
