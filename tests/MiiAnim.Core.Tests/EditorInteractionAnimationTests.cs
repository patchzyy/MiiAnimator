using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;

namespace MiiAnim.Core.Tests;

public class EditorInteractionAnimationTests
{
    public static IEnumerable<object[]> Clips => EditorInteractionAnimations.All.Select(a => new object[] { a.Path });
    private static MiiAnimation Get(string path) => EditorInteractionAnimations.All.Single(a => a.Path == path).Create();

    [Fact]
    public void SuiteContainsThreeFrontClickReactionsAndFourHitAreas()
    {
        var catalog = EditorInteractionAnimations.All;
        Assert.Equal(7, catalog.Length);
        Assert.Equal(7, catalog.Select(a => a.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(3, catalog.Count(a => a.Path.Contains("/front_click/", StringComparison.Ordinal)));
        var hits = catalog.Where(a => a.Path.Contains("/flinch/", StringComparison.Ordinal)).Select(a => Path.GetFileName(a.Path));
        Assert.Equal(new[] { "Flinch_body.miianim", "Flinch_head.miianim", "Flinch_left_leg.miianim", "Flinch_right_leg.miianim" }, hits.Order());
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var shrugs = rig.Evaluate(EditorInteractionAnimations.TicklishShrug(), 50);
        var dodge = rig.Evaluate(EditorInteractionAnimations.PlayfulDodge(), 38);
        var scold = rig.Evaluate(EditorInteractionAnimations.MockScold(), 60);
        Assert.True(MathF.Abs(dodge.RootTravel.X) > 3f, "The dodge should lean visibly to the side.");
        Assert.True(shrugs.WorldPosition[(int)MiiBone.HandL].Y > rig.RestPose.WorldPosition[(int)MiiBone.HandL].Y + 20f);
        Assert.True(scold.WorldPosition[(int)MiiBone.HandR].Y > scold.WorldPosition[(int)MiiBone.HandL].Y + 25f,
            "The mock scold should have its own one-handed gesture.");
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void ReactionsStartAtRestAndRecoverToTheEditorIdleWithoutChangingTheFace(string path)
    {
        var animation = Get(path);
        Assert.Equal(60, animation.Fps);
        Assert.InRange(animation.DurationSeconds, 2f, 4f);
        Assert.Contains(animation.Events, e => e.Frame == 0 && e.Name == "click");
        Assert.Contains(animation.Events, e => e.Frame == animation.Length && e.Name == "done");
        Assert.Empty(animation.Actors);
        Assert.Empty(animation.Particles);
        Assert.Null(animation.AuthorMii);
        Assert.All(animation.Events, e => Assert.InRange(e.Frame, 0, animation.Length));
        foreach (var curve in animation.Tracks.Values)
            Assert.All(curve.Keys, k => Assert.InRange(k.Frame, 0, animation.Length));
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                Assert.Equal(MiiExpression.Normal, pose.Expression);
                Assert.True(pose.Visible);
                foreach (var matrix in pose.SkinMatrix)
                    Assert.True(IsFinite(matrix), $"{path}: invalid pose at {frame}");
                Assert.InRange(pose.RootTravel.X, -5f, 5f);
                Assert.InRange(pose.RootTravel.Z, -11f, 0f);
                if (frame == 0 || frame >= animation.Length - 12)
                    foreach (var bone in MiiSkeletonInfo.AllBones)
                    {
                        var i = (int)bone;
                        Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[i], pose.WorldPosition[i]) < 0.15f,
                            $"{path}: {bone} does not return to rest at {frame}");
                        Assert.True(MathF.Abs(Quaternion.Dot(rig.RestPose.WorldRotation[i], pose.WorldRotation[i])) > 0.9999f);
                    }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void HandsClearTheHeadAndTorsoAndFeetDoNotSink(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        foreach (var (build, height) in new[] { (64f, 64f), (0f, 0f) })
        {
            var results = ClipCheck.Run(animation, female, build, height);
            var problems = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
            Assert.True(problems.Length == 0, $"{path}, female={female}, {build}/{height}: {problems}");
            Assert.All(results, result => Assert.True(result.FootY > -1f,
                $"{path}: foot mesh sinks at {result.Frame} ({result.FootY})"));
        }
    }

    [Fact]
    public void FrontFlinchesMoveAwayFromTheCameraPromptlyAndHeadAndBodyReactDifferently()
    {
        var animations = new[] { EditorInteractionAnimations.FlinchHead(), EditorInteractionAnimations.FlinchBody(),
            EditorInteractionAnimations.FlinchLeftLeg(), EditorInteractionAnimations.FlinchRightLeg() };
        foreach (var animation in animations)
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var recoil = Assert.Single(animation.Events, e => e.Name == "recoil").Frame;
            Assert.InRange(recoil, 1, 8);
            var first = rig.Evaluate(animation, 0);
            var hit = rig.Evaluate(animation, recoil);
            var point = new Vector3(0, 35, -3.7f);
            var before = Vector3.Transform(point, first.HeadMatrixForRender(MiiStage.DefaultBodyScale));
            var after = Vector3.Transform(point, hit.HeadMatrixForRender(MiiStage.DefaultBodyScale));
            Assert.True(after.Z < before.Z - 4f, $"{animation.Name}: the flinch should retreat from a front camera.");
            Assert.True(hit.RootTravel.Z <= -3f);
        }
        var head = EditorInteractionAnimations.FlinchHead();
        var body = EditorInteractionAnimations.FlinchBody();
        Assert.True(head.Evaluate(TrackId.Bone(MiiBone.Head, Channel.RotX), 6) < -15f);
        Assert.True(body.Evaluate(TrackId.Bone(MiiBone.Chest, Channel.RotX), 8) < -10f);
        Assert.True(body.Evaluate(TrackId.Bone(MiiBone.Head, Channel.RotX), 8) > 5f,
            "The body flinch counterbalances the neck rather than reusing the head flinch.");
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void SupportingFeetStayPlantedAndOnlyTheClickedLegWithdraws(string path)
    {
        var animation = Get(path);
        var lifted = path.EndsWith("Flinch_left_leg.miianim", StringComparison.Ordinal) ? MiiBone.FootL
            : path.EndsWith("Flinch_right_leg.miianim", StringComparison.Ordinal) ? MiiBone.FootR : (MiiBone?)null;
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var first = rig.Evaluate(animation, 0);
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var foot in new[] { MiiBone.FootL, MiiBone.FootR }.Where(foot => foot != lifted))
                    Assert.True(Vector3.Distance(first.WorldPosition[(int)foot], pose.WorldPosition[(int)foot]) < 0.15f,
                        $"{path}: supporting {foot} slides at {frame}");
            }
            if (lifted is { } clickedFoot)
            {
                var before = first.WorldPosition[(int)clickedFoot];
                var withdrawn = rig.Evaluate(animation, 8).WorldPosition[(int)clickedFoot];
                Assert.True(withdrawn.Y > before.Y + 12f);
                Assert.True(withdrawn.Z < before.Z - 11f);
                Assert.True(Vector3.Distance(before, rig.Evaluate(animation, 36).WorldPosition[(int)clickedFoot]) < 0.15f);
            }
        }
    }

    [Fact]
    public void LegFlinchesMirrorTheClickedSide()
    {
        var left = EditorInteractionAnimations.FlinchLeftLeg();
        var right = EditorInteractionAnimations.FlinchRightLeg();
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var initialLeft = rig.Evaluate(left, 0);
            var initialRight = rig.Evaluate(right, 0);
            for (var frame = 0; frame <= left.Length; frame++)
            {
                var a = rig.Evaluate(left, frame);
                var b = rig.Evaluate(right, frame);
                Assert.True(Vector3.Distance(new Vector3(-a.RootTravel.X, a.RootTravel.Y, a.RootTravel.Z), b.RootTravel) < 0.01f);
                // The imported rigs are not perfectly symmetric at rest. Compare the movement
                // of the clicked foot and supporting foot, rather than their absolute positions.
                foreach (var (boneA, boneB) in new[] { (MiiBone.Head, MiiBone.Head), (MiiBone.FootL, MiiBone.FootR),
                    (MiiBone.FootR, MiiBone.FootL) })
                {
                    var p = a.WorldPosition[(int)boneA] - initialLeft.WorldPosition[(int)boneA];
                    var q = b.WorldPosition[(int)boneB] - initialRight.WorldPosition[(int)boneB];
                    Assert.True(Vector3.Distance(new Vector3(-p.X, p.Y, p.Z), q) < 0.15f,
                        $"{boneA}/{boneB}: left and right clicks differ at {frame}");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void ExportKeepsTheReactionAndEvents(string path)
    {
        var original = Get(path);
        var bytes = MiiAnimFormat.Write(original);
        var loaded = MiiAnimFormat.Read(bytes);
        Assert.Equal(original.Name, loaded.Name);
        Assert.Equal(original.Length, loaded.Length);
        Assert.Equal(original.Events, loaded.Events);
        Assert.Equal(bytes, MiiAnimFormat.Write(loaded));
        foreach (var id in original.Tracks.Keys)
            for (var frame = 0f; frame <= original.Length; frame += 0.5f)
                Assert.Equal(original.Evaluate(id, frame), loaded.Evaluate(id, frame));
    }

    private static bool IsFinite(Matrix4x4 matrix) =>
        new[] { matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44 }.All(float.IsFinite);
}

public class EditorInteractionAnimationWriter
{
    [Fact]
    public void WriteEditorInteractionAnimations()
    {
        foreach (var (path, create) in EditorInteractionAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }
}
