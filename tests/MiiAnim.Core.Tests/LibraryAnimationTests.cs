using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class LibraryAnimationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Animations => LibraryAnimations.All.Select(a => new object[] { a.Path });

    public static IEnumerable<object[]> BodyCases =>
        LibraryAnimations.All.SelectMany(a => new[] { false, true }.Select(female => new object[] { a.Path, female }));

    public static IEnumerable<object[]> PortraitCases =>
        LibraryAnimations.All.SelectMany(a =>
            new[] { false, true }.SelectMany(female =>
                new[] { (Build: 0f, Height: 0f), (Build: 64f, Height: 64f), (Build: 127f, Height: 127f), (Build: 127f, Height: 0f) }
                    .Select(size => new object[] { a.Path, female, size.Build, size.Height })));

    private static MiiAnimation Get(string path) => LibraryAnimations.All.Single(a => a.Path == path).Create();

    private static readonly MiiBone[] Arms =
        [MiiBone.ArmL1, MiiBone.ArmL2, MiiBone.HandL, MiiBone.ArmR1, MiiBone.ArmR2, MiiBone.HandR];

    [Theory]
    [MemberData(nameof(Animations))]
    public void OnlyPortraitRotationsAndExpressionsAreAuthored(string path)
    {
        var animation = Get(path);
        Assert.Equal(60, animation.Fps);
        Assert.Null(animation.AuthorMii);
        Assert.Empty(animation.Actors);
        Assert.Empty(animation.Particles);
        foreach (var (id, curve) in animation.Tracks)
        {
            Assert.NotEmpty(curve.Keys);
            Assert.All(curve.Keys, key => Assert.InRange(key.Frame, 0, animation.Length));
            if (id == TrackId.Expression)
            {
                Assert.All(curve.Keys, key => Assert.Equal(Interpolation.Hold, key.Interpolation));
                continue;
            }

            Assert.Equal(TrackTargetKind.Bone, id.Target.Kind);
            Assert.True(id.Target.AsBone == MiiBone.Head || Arms.Contains(id.Target.AsBone), $"{path}: body track {id}");
            Assert.Contains(id.Channel, new[] { Channel.RotX, Channel.RotY, Channel.RotZ });
        }
    }

    [Theory]
    [MemberData(nameof(BodyCases))]
    public void ArmsStayOutsideHeadAndTorso(string path, bool female)
    {
        var results = ClipCheck.Run(Get(path), female);
        var report = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
        output.WriteLine(report.Length == 0 ? $"{path}: clean" : report);
        Assert.True(report.Length == 0, report);
    }

    [Theory]
    [MemberData(nameof(PortraitCases))]
    public void HeadStaysInsideTheSquareTileAndArmsStayBelowTheFace(string path, bool female, float build, float height)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var scale = MiiBodyModel.CalculateBodyScale(build, height);
        var viewProjection = TileCamera(rig, scale);
        var restCenter = TilePixel(Vector3.Transform(new Vector3(0, 35, -3.7f), rig.RestPose.HeadMatrixForRender(scale)), viewProjection);
        var armVertices = rig.Body.Meshes
            .SelectMany(mesh => mesh.Positions.Select((position, i) => (Position: position, Bone: (MiiBone)mesh.Joints[i * 4])))
            .Where(vertex => Arms.Contains(vertex.Bone)).ToArray();

        for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
        {
            var pose = rig.Evaluate(animation, frame);
            var head = pose.HeadMatrixForRender(scale);
            // Match ClipCheck's ordinary face/medium-hair envelope, including its chin and scalp.
            for (var pitchIndex = 0; pitchIndex <= 12; pitchIndex++)
            for (var yawIndex = 0; yawIndex < 24; yawIndex++)
            {
                var pitch = -MathF.PI / 2 + pitchIndex * MathF.PI / 12;
                var yaw = yawIndex * MathF.Tau / 24;
                var outline = new Vector3(0, 35, -3.7f)
                    + new Vector3(28.8f, 35, 32.4f) * new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw));
                var pixel = TilePixel(Vector3.Transform(outline, head), viewProjection);
                Assert.True(pixel.X is >= 1 and <= 89 && pixel.Y is >= 1 and <= 89,
                    $"{path} ({female}, {build}/{height}) frame {frame}: head outline at {pixel}");
            }

            var center = TilePixel(Vector3.Transform(new Vector3(0, 35, -3.7f), head), viewProjection);
            Assert.True(Vector2.Distance(restCenter, center) < 8f, $"{path}: face drifts too far at {frame}");
            foreach (var (position, bone) in armVertices)
            {
                var render = Vector3.Transform(position, pose.SkinMatrix[(int)bone]) * MiiBodyModel.CanonicalToRenderUnits * scale;
                // The narrow neck band is permitted; an arm may never rise into the visible face.
                Assert.True(render.Y <= head.Translation.Y + 6f, $"{path} frame {frame}: {bone} enters the face band");
            }
        }
    }

    [Theory]
    [MemberData(nameof(BodyCases))]
    public void RootChestAndLegsRemainAtRest(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var fixedBones = new[] { MiiBone.Root, MiiBone.Hip, MiiBone.Chest, MiiBone.LegL1, MiiBone.LegL2, MiiBone.FootL, MiiBone.LegR1, MiiBone.LegR2, MiiBone.FootR };
        for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
        {
            var pose = rig.Evaluate(animation, frame);
            Assert.Equal(Vector3.Zero, pose.RootTravel);
            foreach (var bone in fixedBones)
                SameBone(rig.RestPose, pose, bone, $"{path} frame {frame}");
            Assert.Equal(Matrix4x4.Identity, pose.GlobalMatrix);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReactionsAndHoverTransitionsConnectToTheirIntendedPoses(bool female)
    {
        var rig = new MiiRig(MiiBodyModel.Get(female));
        foreach (var (path, create) in LibraryAnimations.All.Where(a => a.Path.StartsWith("library/favorite/", StringComparison.Ordinal)))
        {
            var animation = create();
            SamePose(rig.RestPose, rig.Evaluate(animation, 0), path + " start");
            SamePose(rig.RestPose, rig.Evaluate(animation, animation.Length), path + " finish");
        }

        foreach (var variant in new[] { "bright", "curious" })
        {
            var enter = Get($"library/hover/Hover_enter_{variant}.miianim");
            var idle = Get($"library/hover/Hover_idle_{variant}.miianim");
            var leave = Get($"library/hover/Hover_leave_{variant}.miianim");
            SamePose(rig.RestPose, rig.Evaluate(enter, 0), variant + " enter start");
            var anchor = rig.Evaluate(enter, enter.Length);
            Assert.Equal(MiiExpression.Smile, anchor.Expression);
            SamePose(anchor, rig.Evaluate(idle, 0), variant + " enter to idle");
            SamePose(anchor, rig.Evaluate(idle, idle.Length), variant + " idle seam");
            SamePose(anchor, rig.Evaluate(leave, 0), variant + " idle to leave");
            SamePose(rig.RestPose, rig.Evaluate(leave, leave.Length), variant + " leave finish");
        }
    }

    [Theory]
    [InlineData("bright")]
    [InlineData("curious")]
    public void HoverIdlesLoopWithoutASeamOrRepeatedCompletionEvent(string variant)
    {
        var animation = Get($"library/hover/Hover_idle_{variant}.miianim");
        Assert.Empty(animation.Events);
        foreach (var id in animation.Tracks.Keys)
        {
            Assert.Equal(animation.Evaluate(id, 0), animation.Evaluate(id, animation.Length));
            if (id == TrackId.Expression)
                continue;
            // Check the first/last half-frame as well as the endpoints so a loop cannot hide a sharp reset.
            Assert.InRange(MathF.Abs(animation.Evaluate(id, 0.5f) - animation.Evaluate(id, 0)), 0, 0.01f);
            Assert.InRange(MathF.Abs(animation.Evaluate(id, animation.Length) - animation.Evaluate(id, animation.Length - 0.5f)), 0, 0.01f);
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void FilesRoundTripWithoutChangingPosesOrEventTiming(string path)
    {
        var original = Get(path);
        var bytes = MiiAnimFormat.Write(original);
        var loaded = MiiAnimFormat.Read(bytes);
        Assert.Equal(original.Name, loaded.Name);
        Assert.Equal(original.Fps, loaded.Fps);
        Assert.Equal(original.Length, loaded.Length);
        Assert.Equal(original.Events, loaded.Events);
        Assert.Equal(bytes, MiiAnimFormat.Write(loaded));
        for (var frame = 0f; frame <= original.Length; frame += 0.5f)
        foreach (var id in original.Tracks.Keys)
            Assert.Equal(original.Evaluate(id, frame), loaded.Evaluate(id, frame));

        if (path.Contains("Hover_idle_", StringComparison.Ordinal))
            Assert.Empty(loaded.Events);
        else if (path.Contains("Hover_enter_", StringComparison.Ordinal))
            Assert.Equal(new AnimEvent(original.Length, "hover_ready"), loaded.Events.Last());
        else
            Assert.Equal(new AnimEvent(original.Length, "done"), loaded.Events.Last());
        Assert.All(loaded.Events, marker => Assert.InRange(marker.Frame, 0, loaded.Length));
    }

    [Fact]
    public void WriteLibraryAnimations()
    {
        foreach (var (relativePath, create) in LibraryAnimations.All)
            WheelWizardSampleWriter.WriteRelative(relativePath, create());
    }

    private static Matrix4x4 TileCamera(MiiRig rig, Vector3 scale)
    {
        // WheelWizard MiiListTile: BodyType.face, camera/model rotations zero, zoom 1.
        // ResolveViewParameters uses these values and the realtime renderer adds the REST head translation.
        var restHead = rig.RestPose.HeadMatrixForRender(scale).Translation;
        var target = restHead + new Vector3(0, 4.805f / 0.14f, 0);
        var camera = target + new Vector3(0, 0, 57.553f / 0.14f);
        // MiiBlock's 90x90 tile has ImageOnlyMargin -4,-9,-4,0, so its image measures 98x99.
        const float aspect = 98f / 99f;
        var halfFov = 7.5f * MathF.PI / 180;
        var fovY = 2 * MathF.Atan(MathF.Tan(halfFov) / aspect);
        return Matrix4x4.CreateLookAt(camera, target, Vector3.UnitY) * Matrix4x4.CreatePerspectiveFieldOfView(fovY, aspect, 10, 1200);
    }

    private static Vector2 TilePixel(Vector3 point, Matrix4x4 viewProjection)
    {
        var clip = Vector4.Transform(new Vector4(point, 1), viewProjection);
        Assert.True(clip.W > 0, "portrait point behind camera");
        return new Vector2((clip.X / clip.W + 1) * 49 - 4, (1 - clip.Y / clip.W) * 49.5f - 9);
    }

    private static void SamePose(MiiPose expected, MiiPose actual, string context)
    {
        foreach (var bone in MiiSkeletonInfo.AllBones)
            SameBone(expected, actual, bone, context);
        Assert.Equal(expected.Expression, actual.Expression);
        Assert.Equal(expected.Visible, actual.Visible);
    }

    private static void SameBone(MiiPose expected, MiiPose actual, MiiBone bone, string context)
    {
        var index = (int)bone;
        Assert.True(Vector3.Distance(expected.WorldPosition[index], actual.WorldPosition[index]) < 0.001f, $"{context}: {bone} position");
        Assert.True(MathF.Abs(Quaternion.Dot(expected.WorldRotation[index], actual.WorldRotation[index])) > 0.999999f, $"{context}: {bone} rotation");
        Assert.Equal(expected.LocalScale[index], actual.LocalScale[index]);
    }
}
