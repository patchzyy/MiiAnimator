using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;

namespace MiiAnim.Core.Tests;

public class RoomsAnimationTests
{
    public static IEnumerable<object[]> Animations => RoomsAnimations.All.Select(a => new object[] { a.Path });

    private static MiiAnimation Get(string path) => RoomsAnimations.All.Single(a => a.Path == path).Create();

    private static bool IsLoop(string path) => path.Contains("/Lobby_idle_", StringComparison.Ordinal);

    [Fact]
    public void CatalogUsesUniqueLobbyFiles()
    {
        Assert.Equal(8, RoomsAnimations.All.Length);
        Assert.Equal(RoomsAnimations.All.Length, RoomsAnimations.All.Select(a => a.Path).Distinct().Count());
        Assert.Equal(4, RoomsAnimations.All.Count(a => IsLoop(a.Path)));
        foreach (var (path, _) in RoomsAnimations.All)
        {
            Assert.StartsWith("rooms/lobby/", path);
            Assert.EndsWith(MiiAnimFormat.FileExtension, path);
            Assert.DoesNotContain("..", path);
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void OnlyTheHeadAndFaceAreAnimated(string path)
    {
        var animation = Get(path);
        Assert.Empty(animation.Actors);
        Assert.Empty(animation.Particles);
        Assert.Null(animation.AuthorMii);
        Assert.Equal(4, animation.Tracks.Count);
        foreach (var (id, curve) in animation.Tracks)
        {
            Assert.True(
                id == TrackId.Expression ||
                (id.Target.Kind == TrackTargetKind.Bone && id.Target.AsBone == MiiBone.Head &&
                 id.Channel is Channel.RotX or Channel.RotY or Channel.RotZ),
                $"{path}: unexpected track {id}");
            Assert.NotEmpty(curve.Keys);
            Assert.Equal(0, curve.Keys[0].Frame);
            Assert.Equal(animation.Length, curve.Keys[^1].Frame);
            Assert.All(curve.Keys, key => Assert.InRange(key.Frame, 0, animation.Length));
        }

        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            for (var frame = 0; frame <= animation.Length; frame++)
            {
                var pose = rig.Evaluate(animation, frame);
                Assert.Equal(Vector3.Zero, pose.RootTravel);
                Assert.Equal(rig.RestPose.GlobalMatrix, pose.GlobalMatrix);
                Assert.True(pose.Visible);
                foreach (var bone in MiiSkeletonInfo.AllBones)
                {
                    var index = (int)bone;
                    Assert.Equal(rig.RestPose.LocalTranslation[index], pose.LocalTranslation[index]);
                    Assert.Equal(rig.RestPose.LocalScale[index], pose.LocalScale[index]);
                    Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[index], pose.WorldPosition[index]) < 0.001f,
                        $"{path}: {bone} moved at {frame}");
                    if (bone != MiiBone.Head)
                        Assert.True(MathF.Abs(Quaternion.Dot(rig.RestPose.WorldRotation[index], pose.WorldRotation[index])) > 0.99999f,
                            $"{path}: {bone} rotated at {frame}");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void MovementStaysSmallAndSlowForTinyCircularPortraits(string path)
    {
        var animation = Get(path);
        var pitch = TrackId.Bone(MiiBone.Head, Channel.RotX);
        var yaw = TrackId.Bone(MiiBone.Head, Channel.RotY);
        var roll = TrackId.Bone(MiiBone.Head, Channel.RotZ);
        Vector3 Rotation(float frame) => new(animation.Evaluate(pitch, frame), animation.Evaluate(yaw, frame), animation.Evaluate(roll, frame));
        var previous = Rotation(0);
        var moved = false;
        for (var frame = 0.5f; frame <= animation.Length; frame += 0.5f)
        {
            var rotation = Rotation(frame);
            Assert.InRange(rotation.X, -5f, 5f);
            Assert.InRange(rotation.Y, -7f, 7f);
            Assert.InRange(rotation.Z, -2.5f, 2.5f);
            Assert.InRange(rotation.Length(), 0, 8f);
            // Half-frame samples catch abrupt movement between keys, including interpolation overshoot.
            Assert.True(Vector3.Distance(previous, rotation) <= 0.375f, $"{path}: head snapped at {frame}");
            moved |= rotation.Length() > 0.5f;
            previous = rotation;
        }
        Assert.True(moved, $"{path}: no readable head motion");
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void TheMiisOwnFaceIsVisibleMostOfTheTime(string path)
    {
        var animation = Get(path);
        var normalFrames = Enumerable.Range(0, animation.Length)
            .Count(frame => animation.Evaluate(TrackId.Expression, frame) == (float)MiiExpression.Normal);
        Assert.True(normalFrames >= animation.Length * 0.8f, $"{path}: expressions replace the Mii's face too often");
        var keys = animation.Tracks[TrackId.Expression].Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var expression = MiiExpressionInfo.FromValue(keys[i].Value);
            Assert.Contains(expression, new[] { MiiExpression.Normal, MiiExpression.Blink, MiiExpression.Smile, MiiExpression.Surprise });
            Assert.Equal(Interpolation.Hold, keys[i].Interpolation);
            if (expression == MiiExpression.Normal)
                continue;
            Assert.True(i + 1 < keys.Count);
            Assert.Equal((float)MiiExpression.Normal, keys[i + 1].Value);
            var duration = keys[i + 1].Frame - keys[i].Frame;
            Assert.InRange(duration, 6, expression switch
            {
                MiiExpression.Blink => 10,
                MiiExpression.Surprise => 16,
                _ => 30,
            });
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void ClipsReturnToNeutralAndIdleSeamsAreQuiet(string path)
    {
        var animation = Get(path);
        Assert.Equal(60, animation.Fps);
        Assert.InRange(animation.DurationSeconds, 2.5f, 10f);
        foreach (var id in animation.Tracks.Keys)
        {
            Assert.Equal(ChannelInfo.DefaultValue(id), animation.Evaluate(id, 0));
            Assert.Equal(ChannelInfo.DefaultValue(id), animation.Evaluate(id, animation.Length));
            for (var frame = 0; frame <= 12; frame++)
            {
                Assert.Equal(ChannelInfo.DefaultValue(id), animation.Evaluate(id, frame));
                Assert.Equal(ChannelInfo.DefaultValue(id), animation.Evaluate(id, animation.Length - frame));
            }
        }
        if (IsLoop(path))
        {
            Assert.InRange(animation.DurationSeconds, 8f, 10f);
            Assert.Empty(animation.Events);
        }
        else
        {
            Assert.Contains(animation.Events, e => e.Name == "done" && e.Frame == animation.Length);
            Assert.Contains(animation.Events, e => e.Name != "done" && e.Frame > 12 && e.Frame < animation.Length - 12);
        }
    }

    [Theory]
    [MemberData(nameof(Animations))]
    public void FileFormatKeepsEveryHeadAndExpressionSample(string path)
    {
        var original = Get(path);
        var bytes = MiiAnimFormat.Write(original);
        var loaded = MiiAnimFormat.Read(bytes);
        Assert.Equal(original.Name, loaded.Name);
        Assert.Equal(original.Fps, loaded.Fps);
        Assert.Equal(original.Length, loaded.Length);
        Assert.Equal(original.Events, loaded.Events);
        Assert.Equal(original.Tracks.Count, loaded.Tracks.Count);
        foreach (var id in original.Tracks.Keys)
        {
            Assert.True(loaded.Tracks.ContainsKey(id));
            for (var frame = 0f; frame <= original.Length; frame += 0.5f)
                Assert.Equal(original.Evaluate(id, frame), loaded.Evaluate(id, frame));
        }
        Assert.Equal(bytes, MiiAnimFormat.Write(loaded));
    }

    [Fact]
    public void WriteRoomsAnimations()
    {
        foreach (var (path, create) in RoomsAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }
}
