using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;

namespace MiiAnim.Core.Tests;

public class FriendsAnimationTests
{
    private static readonly MiiBone[] LowerBody =
        [MiiBone.Root, MiiBone.Hip, MiiBone.LegL1, MiiBone.LegL2, MiiBone.FootL, MiiBone.LegR1, MiiBone.LegR2, MiiBone.FootR];

    public static IEnumerable<object[]> Animations =>
        FriendsAnimations.All.SelectMany(a => new[] { new object[] { a.Path, false }, new object[] { a.Path, true } });

    private static MiiAnimation Get(string path) => FriendsAnimations.All.Single(a => a.Path == path).Create();

    [Theory]
    [MemberData(nameof(Animations))]
    public void PositiveYaw_FacesScreenRight_AndFaceRemainsReadable(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        for (var frame = 0; frame <= animation.Length; frame++)
        {
            // Friends' front camera has zero horizontal orbit: +X maps directly to screen right.
            // Test the rendered head transform, rather than trusting the sign of an Euler track.
            var head = rig.Evaluate(animation, frame).HeadMatrix;
            var forward = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, head));
            var rightTurn = MathF.Atan2(forward.X, forward.Z) * 180f / MathF.PI;
            Assert.InRange(rightTurn, 10f, 24f);
            Assert.True(forward.Z > 0.90f, $"{path} frame {frame}: face turns too far from the camera");
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void HiddenLowerBodyAndRootTravel_StayFixed(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var baseline = rig.Evaluate(animation, 0);
        for (var frame = 0; frame <= animation.Length; frame++)
        {
            var pose = rig.Evaluate(animation, frame);
            Assert.Equal(Vector3.Zero, pose.RootTravel);
            foreach (var bone in LowerBody)
            {
                Assert.True(Vector3.Distance(baseline.WorldPosition[(int)bone], pose.WorldPosition[(int)bone]) < 0.001f,
                    $"{path} frame {frame}: {bone} moved below the crop");
                Assert.True(QuaternionDistance(baseline.WorldRotation[(int)bone], pose.WorldRotation[(int)bone]) < 0.001f,
                    $"{path} frame {frame}: {bone} rotated below the crop");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void HeadAndShoulders_StayWithinTheTightCrop(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        // Standard FFL head landmark coordinates in render units. A fixed front camera with the
        // source variant's 12-degree pitch is sufficient for comparing movement in card space.
        Vector3[] landmarks = [new(0, 35, -3.7f), new(-24, 35, 24), new(24, 35, 24), new(0, 65, 0), new(0, 8, 0)];
        var camera = Matrix4x4.CreateRotationX(-12f * MathF.PI / 180f);
        foreach (var scale in new[] { MiiBodyModel.CalculateBodyScale(0, 0), MiiBodyModel.CalculateBodyScale(64, 64), MiiBodyModel.CalculateBodyScale(127, 127) })
        {
            var baseline = rig.Evaluate(animation, 0);
            var first = baseline.HeadMatrixForRender(scale);
            var initial = landmarks.Select(p => Vector3.Transform(Vector3.Transform(p, first), camera)).ToArray();
            for (var frame = 0; frame <= animation.Length; frame++)
            {
                var pose = rig.Evaluate(animation, frame);
                var head = pose.HeadMatrixForRender(scale);
                for (var i = 0; i < landmarks.Length; i++)
                {
                    var point = Vector3.Transform(Vector3.Transform(landmarks[i], head), camera);
                    Assert.True(Vector2.Distance(new(point.X, point.Y), new(initial[i].X, initial[i].Y)) < 14f,
                        $"{path} frame {frame}: head landmark {i} moves beyond the card allowance");
                }
                foreach (var shoulder in new[] { MiiBone.ArmL1, MiiBone.ArmR1 })
                {
                    var initialShoulder = Vector3.Transform(baseline.WorldPosition[(int)shoulder] * MiiBodyModel.CanonicalToRenderUnits * scale, camera);
                    var currentShoulder = Vector3.Transform(pose.WorldPosition[(int)shoulder] * MiiBodyModel.CanonicalToRenderUnits * scale, camera);
                    Assert.True(Vector2.Distance(new(initialShoulder.X, initialShoulder.Y), new(currentShoulder.X, currentShoulder.Y)) < 3f,
                        $"{path} frame {frame}: {shoulder} shifts beyond the shoulder crop allowance");
                }
                // Arms remain discreet, below the neck. No hand enters the face to make a greeting readable.
                foreach (var hand in new[] { MiiBone.HandL, MiiBone.HandR })
                    Assert.True(pose.WorldPosition[(int)hand].Y < pose.WorldPosition[(int)MiiBone.Head].Y - 22f,
                        $"{path} frame {frame}: {hand} rises into the face crop");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void EveryClip_StartsAndEndsAtTheSameRightFacingBaseline(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var baseline = rig.Evaluate(FriendsAnimations.OnlineIdleAttentive(), 0);
        foreach (var frame in new[] { 0, animation.Length })
        {
            var pose = rig.Evaluate(animation, frame);
            foreach (var bone in MiiSkeletonInfo.AllBones)
            {
                Assert.True(Vector3.Distance(baseline.WorldPosition[(int)bone], pose.WorldPosition[(int)bone]) < 0.001f,
                    $"{path}: {bone} baseline differs");
                Assert.True(QuaternionDistance(baseline.WorldRotation[(int)bone], pose.WorldRotation[(int)bone]) < 0.001f,
                    $"{path}: {bone} baseline rotation differs");
                Assert.True(Vector3.Distance(baseline.LocalScale[(int)bone], pose.LocalScale[(int)bone]) < 0.001f);
            }
            Assert.Equal(MiiExpression.Normal, pose.Expression);
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void IdlesHaveQuietLoopSeams_AndAllClipsContainVisibleMotion(string path, bool female)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(female));
        var first = rig.Evaluate(animation, 0);
        var maxHeadAngle = 0f;
        for (var frame = 0; frame <= animation.Length; frame++)
            maxHeadAngle = MathF.Max(maxHeadAngle,
                QuaternionDistance(first.WorldRotation[(int)MiiBone.Head], rig.Evaluate(animation, frame).WorldRotation[(int)MiiBone.Head]));
        Assert.True(maxHeadAngle > 2f, $"{path} has no readable head motion");
        Assert.Contains(animation.Tracks[TrackId.Expression].Keys, k => k.Value == (float)MiiExpression.Blink);
        if (!path.Contains("_idle_", StringComparison.Ordinal))
            return;

        var beforeWrap = rig.Evaluate(animation, animation.Length - 1);
        var afterWrap = rig.Evaluate(animation, 1);
        Assert.True(QuaternionDistance(beforeWrap.WorldRotation[(int)MiiBone.Head], afterWrap.WorldRotation[(int)MiiBone.Head]) < 0.05f,
            $"{path} jumps on its loop seam");
        Assert.True(Vector3.Distance(beforeWrap.WorldPosition[(int)MiiBone.Head], afterWrap.WorldPosition[(int)MiiBone.Head]) < 0.02f);
        Assert.Equal(MiiExpression.Normal, beforeWrap.Expression);
        Assert.Equal(MiiExpression.Normal, afterWrap.Expression);
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void Hands_DoNotIntersectTheHeadOrTorso(string path, bool female)
    {
        var report = ClipCheck.Describe(ClipCheck.Run(Get(path), female), headLimit: 1f, torsoLimit: 0.8f);
        Assert.True(report.Length == 0, $"{path}\n{report}");
    }

    [Fact]
    public void PresenceChanges_HaveStateAndCompletionEvents_AndDifferentTiming()
    {
        var online = FriendsAnimations.BecameOnline();
        var offline = FriendsAnimations.BecameOffline();
        Assert.Single(online.Events, e => e.Name == "online" && e.Frame == 24);
        Assert.Single(offline.Events, e => e.Name == "offline" && e.Frame == 70);
        foreach (var animation in new[] { online, offline })
            Assert.Single(animation.Events, e => e.Name == "done" && e.Frame == animation.Length);
        Assert.True(online.Length < offline.Length);
        Assert.Equal(MiiExpression.Smile, MiiExpressionInfo.FromValue(online.Evaluate(TrackId.Expression, 48)));
        Assert.Equal(MiiExpression.Blink, MiiExpressionInfo.FromValue(offline.Evaluate(TrackId.Expression, 60)));
    }

    [Fact]
    public void FilesRoundTrip_WithoutLosingTheRightFacingBaseline()
    {
        foreach (var (path, create) in FriendsAnimations.All)
        {
            var source = create();
            var loaded = MiiAnimFormat.Read(MiiAnimFormat.Write(source));
            Assert.Equal(source.Name, loaded.Name);
            Assert.Equal(source.Length, loaded.Length);
            Assert.Equal(FriendsAnimations.BaselineYaw, loaded.Evaluate(TrackId.Bone(MiiBone.Root, Channel.RotY), 0));
            Assert.Equal(FriendsAnimations.BaselineYaw, loaded.Evaluate(TrackId.Bone(MiiBone.Root, Channel.RotY), loaded.Length));
            Assert.Equal(source.Events, loaded.Events);
        }
    }

    [Fact]
    public void WriteFriendsAnimations()
    {
        foreach (var (path, create) in FriendsAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }

    private static float QuaternionDistance(Quaternion a, Quaternion b)
    {
        // atan2 remains accurate at the tiny angles checked at baseline and loop seams.
        var relative = Quaternion.Normalize(Quaternion.Concatenate(a, Quaternion.Inverse(b)));
        var sine = new Vector3(relative.X, relative.Y, relative.Z).Length();
        return 2f * MathF.Atan2(sine, MathF.Abs(relative.W)) * 180f / MathF.PI;
    }
}
