using System.Text.Json;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;

namespace MiiAnim.Core.Tests;

/// <summary>Final bundle validation and a machine-readable inventory for WheelWizard.</summary>
public class WheelWizardBundleTests
{
    [Fact]
    public void ValidateBundleAndWriteInventory()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "wheelwizard"));
        var files = Directory.GetFiles(root, "*.miianim", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(files);
        var rows = new List<object>();
        var rigs = new[] { new MiiRig(MiiBodyModel.Get(false)), new MiiRig(MiiBodyModel.Get(true)) };
        foreach (var file in files)
        {
            var animation = MiiAnimFormat.Read(File.ReadAllBytes(file));
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            Assert.InRange(animation.Fps, 1, 255);
            Assert.True(animation.Length > 0, relative);
            for (var actor = 0; actor < animation.ActorCount; actor++)
            {
                var motion = animation.ForActor(actor);
                foreach (var curve in motion.Tracks.Values)
                    foreach (var key in curve.Keys)
                    {
                        Assert.InRange(key.Frame, 0, animation.Length);
                        Assert.True(float.IsFinite(key.Value), relative);
                    }
                foreach (var rig in rigs)
                    for (var frame = 0; frame <= animation.Length; frame += 2)
                    {
                        var pose = rig.Evaluate(motion, frame);
                        foreach (var matrix in pose.SkinMatrix)
                            Assert.True(AllFinite(matrix), $"{relative}, actor {actor}, frame {frame}");
                    }
            }
            rows.Add(new
            {
                path = relative,
                animation.Name,
                animation.Fps,
                frames = animation.Length,
                seconds = animation.DurationSeconds,
                actorCount = animation.ActorCount,
                extraActorRoles = animation.Actors.Select(a => a.Name).ToArray(),
                events = animation.Events.Select(e => new { e.Frame, e.Name }).ToArray(),
                particleEffects = animation.Particles.Count,
            });
        }

        // Every retained action folder must contain a usable clip, rather than just a placeholder.
        foreach (var folder in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
        {
            if (Directory.GetDirectories(folder).Length != 0)
                continue;
            // Asset directories may hold the GLB and its placement manifest rather than a Mii clip.
            if (Directory.GetFiles(folder, "*.glb").Length != 0)
                continue;
            Assert.True(Directory.GetFiles(folder, "*.miianim").Length != 0,
                $"No animation in {Path.GetRelativePath(root, folder)}");
        }
        var json = JsonSerializer.Serialize(new { format = "miianim", clips = rows }, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        File.WriteAllText(Path.Combine(root, "inventory.json"), json + Environment.NewLine);
    }

    private static bool AllFinite(System.Numerics.Matrix4x4 m) =>
        float.IsFinite(m.M11) && float.IsFinite(m.M12) && float.IsFinite(m.M13) && float.IsFinite(m.M14)
        && float.IsFinite(m.M21) && float.IsFinite(m.M22) && float.IsFinite(m.M23) && float.IsFinite(m.M24)
        && float.IsFinite(m.M31) && float.IsFinite(m.M32) && float.IsFinite(m.M33) && float.IsFinite(m.M34)
        && float.IsFinite(m.M41) && float.IsFinite(m.M42) && float.IsFinite(m.M43) && float.IsFinite(m.M44);
}
