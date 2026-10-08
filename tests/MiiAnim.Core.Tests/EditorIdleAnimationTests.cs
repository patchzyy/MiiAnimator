using System.Numerics;
using System.Text;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;

namespace MiiAnim.Core.Tests;

public class EditorIdleAnimationTests
{
    public static IEnumerable<object[]> Clips => EditorIdleAnimations.All.Select(a => new object[] { a.Path });
    public static IEnumerable<object[]> UpperClips => EditorIdleAnimations.All
        .Where(a => a.Path.EndsWith("_upper.miianim", StringComparison.Ordinal))
        .Select(a => new object[] { a.Path });

    private static MiiAnimation Get(string path) => EditorIdleAnimations.All.Single(a => a.Path == path).Create();

    [Fact]
    public void CatalogProvidesTwentyDifferentMotionsIncludingTenCloseCameraChoices()
    {
        Assert.Equal(20, EditorIdleAnimations.All.Length);
        Assert.Equal(10, UpperClips.Count());
        Assert.Equal(20, EditorIdleAnimations.All.Select(a => a.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var signatures = new HashSet<string>();
        var rig = new MiiRig(MiiBodyModel.Get(false));
        foreach (var (path, create) in EditorIdleAnimations.All)
        {
            Assert.StartsWith("editor/idle/Editor_idle_", path);
            var animation = create();
            var signature = new StringBuilder();
            // Compare actual motion at normalized times, ignoring names and duration differences.
            for (var sample = 1; sample < 40; sample++)
            {
                var pose = rig.Evaluate(animation, animation.Length * sample / 40f);
                foreach (var bone in new[] { MiiBone.Hip, MiiBone.Chest, MiiBone.Head, MiiBone.HandL, MiiBone.HandR })
                {
                    var position = pose.WorldPosition[(int)bone];
                    var rotation = pose.WorldRotation[(int)bone];
                    foreach (var value in new[] { position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z })
                        signature.Append(MathF.Round(value * 1000)).Append(',');
                }
            }
            Assert.True(signatures.Add(signature.ToString()), $"{path}: duplicates an existing idle's motion");
        }
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void LoopsKeepTheEditedFaceVisibleAndHaveQuietNeutralSeams(string path)
    {
        var animation = Get(path);
        Assert.Equal(60, animation.Fps);
        Assert.InRange(animation.DurationSeconds, 10f, 16f);
        Assert.Empty(animation.Events);
        Assert.Empty(animation.Particles);
        Assert.Empty(animation.Actors);
        Assert.Null(animation.AuthorMii);
        foreach (var curve in animation.Tracks.Values)
            Assert.All(curve.Keys, key => Assert.InRange(key.Frame, 0, animation.Length));

        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var first = rig.Evaluate(animation, 0);
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                Assert.Equal(MiiExpression.Normal, pose.Expression);
                Assert.True(pose.Visible);
                foreach (var matrix in pose.SkinMatrix)
                    Assert.True(IsFinite(matrix), $"{path}: invalid pose at {frame}");
                foreach (var foot in new[] { MiiBone.FootL, MiiBone.FootR })
                    Assert.True(Vector3.Distance(first.WorldPosition[(int)foot], pose.WorldPosition[(int)foot]) < 0.15f,
                        $"{path}: {foot} left its planted position at {frame}");
                if (frame <= 30 || frame >= animation.Length - 30)
                    foreach (var bone in MiiSkeletonInfo.AllBones)
                    {
                        var i = (int)bone;
                        Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[i], pose.WorldPosition[i]) < 0.15f,
                            $"{path}: non-neutral {bone} near the seam at {frame}");
                        Assert.True(MathF.Abs(Quaternion.Dot(first.WorldRotation[i], pose.WorldRotation[i])) > 0.99999f,
                            $"{path}: rotating {bone} at the seam");
                    }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void HandsClearTheHeadAndBodyAndTheFootMeshStaysAboveGround(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        foreach (var (build, height) in new[] { (64f, 64f), (0f, 0f) })
        {
            var results = ClipCheck.Run(animation, female, build, height);
            var problems = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
            Assert.True(problems.Length == 0, $"{path}, female={female}, {build}/{height}: {problems}");
            Assert.All(results, frame => Assert.True(frame.FootY > -1f, $"{path}: a foot sinks at {frame.Frame}"));
        }
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void MotionStaysSlowAndWithinTheEditorFraming(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var previous = rig.Evaluate(animation, 0);
            for (var frame = 0.5f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var bone in new[] { MiiBone.Head, MiiBone.HandL, MiiBone.HandR, MiiBone.Chest })
                {
                    var i = (int)bone;
                    Assert.True(Vector3.Distance(previous.WorldPosition[i], pose.WorldPosition[i]) < 0.5f,
                        $"{path}: {bone} moves abruptly at {frame}");
                    var dot = Math.Clamp(MathF.Abs(Quaternion.Dot(previous.WorldRotation[i], pose.WorldRotation[i])), 0, 1);
                    var degrees = 2f * MathF.Acos(dot) * 180f / MathF.PI;
                    Assert.True(degrees < 0.5f, $"{path}: {bone} turns abruptly at {frame}");
                }
                foreach (var hand in new[] { MiiBone.HandL, MiiBone.HandR })
                    Assert.True(pose.WorldPosition[(int)hand].Y < pose.WorldPosition[(int)MiiBone.Head].Y - 20f,
                        $"{path}: {hand} obscures the face");
                Assert.InRange(animation.Evaluate(TrackId.Bone(MiiBone.Root, Channel.PosX), frame), -4f, 4f);
                Assert.InRange(animation.Evaluate(TrackId.Bone(MiiBone.Root, Channel.PosY), frame), -3f, 0f);
                Assert.InRange(animation.Evaluate(TrackId.Bone(MiiBone.Head, Channel.RotX), frame), -5f, 7f);
                Assert.InRange(animation.Evaluate(TrackId.Bone(MiiBone.Head, Channel.RotY), frame), -9f, 9f);
                Assert.InRange(animation.Evaluate(TrackId.Bone(MiiBone.Head, Channel.RotZ), frame), -4f, 4f);
                previous = pose;
            }
        }
    }

    [Theory]
    [MemberData(nameof(UpperClips))]
    public void CloseCameraChoicesKeepEverythingBelowTheNeckFixed(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var first = rig.Evaluate(animation, 0);
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var bone in MiiSkeletonInfo.AllBones)
                {
                    var i = (int)bone;
                    Assert.True(Vector3.Distance(first.WorldPosition[i], pose.WorldPosition[i]) < 0.001f,
                        $"{path}: {bone} drifts in close-up at {frame}");
                    if (bone != MiiBone.Head)
                        Assert.True(MathF.Abs(Quaternion.Dot(first.WorldRotation[i], pose.WorldRotation[i])) > 0.99999f);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void ExportPreservesTheWholeLoop(string path)
    {
        var original = Get(path);
        var bytes = MiiAnimFormat.Write(original);
        var loaded = MiiAnimFormat.Read(bytes);
        Assert.Equal(original.Name, loaded.Name);
        Assert.Equal(original.Length, loaded.Length);
        Assert.Equal(original.Fps, loaded.Fps);
        Assert.Equal(bytes, MiiAnimFormat.Write(loaded));
        foreach (var id in original.Tracks.Keys)
            for (var frame = 0f; frame <= original.Length; frame += 0.5f)
                Assert.Equal(original.Evaluate(id, frame), loaded.Evaluate(id, frame));
    }

    private static bool IsFinite(Matrix4x4 matrix) =>
        new[] { matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44 }.All(float.IsFinite);
}

public class EditorIdleAnimationWriter
{
    [Fact]
    public void WriteEditorIdleAnimations()
    {
        foreach (var (path, create) in EditorIdleAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }
}
