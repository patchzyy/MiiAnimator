using System.Buffers.Binary;
using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Rig;
using Xunit.Abstractions;

namespace MiiAnim.Core.Tests;

public class RigTests(ITestOutputHelper output)
{
    private static readonly MiiRig MaleRig = new(MiiBodyModel.Get(female: false));

    [Fact]
    public void RestPose_MatchesWheelWizardStaticBody()
    {
        var bytes = File.ReadAllBytes("mii_static_body_3ds_male_LE.rmdl");
        var meshListOffset = 0x10 + BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0x10));
        var pose = MaleRig.RestPose;
        var maxError = 0f;
        for (var meshIndex = 0; meshIndex < 2; meshIndex++)
        {
            var meshOffset = meshListOffset + meshIndex * 0x38;
            var vertexOffset = meshOffset + BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(meshOffset));
            var count = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(meshOffset + 4));
            var mesh = MaleRig.Body.Meshes[meshIndex];
            Assert.Equal(count, mesh.Positions.Length);
            for (var v = 0; v < count; v++)
            {
                var o = vertexOffset + v * 0x20;
                var expected = new Vector3(
                    BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(o)),
                    BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(o + 4)),
                    BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(o + 8))
                );
                var skinned = Vector3.Transform(mesh.Positions[v], pose.SkinMatrix[mesh.Joints[v * 4]]) * MiiBodyModel.CanonicalToRenderUnits;
                maxError = MathF.Max(maxError, Vector3.Distance(expected, skinned));
            }
        }

        output.WriteLine($"max vertex error vs static body: {maxError:F3} render units");
        Assert.True(maxError < 1.0f, $"Rest pose deviates {maxError} units from WheelWizard's static body.");
    }

    [Fact]
    public void LegIk_AtRestTarget_ReproducesFkRest()
    {
        var fk = MaleRig.Evaluate(id => id.Channel == Channel.IkBlend ? 0f : float.NaN);
        var ik = MaleRig.Evaluate(id => id.Channel == Channel.IkBlend ? 1f : float.NaN);
        foreach (var bone in MiiSkeletonInfo.AllBones)
            Assert.True(Vector3.Distance(fk.WorldPosition[(int)bone], ik.WorldPosition[(int)bone]) < 0.05f, $"{bone} moved");
    }

    [Fact]
    public void LegIk_ReachesTarget_AndKneeBendsForward()
    {
        // Crouch: lower the body, keep feet planted.
        var pose = MaleRig.Evaluate(id =>
            id == TrackId.Bone(MiiBone.Root, Channel.PosY) ? -15f : float.NaN
        );
        var rest = MaleRig.RestPose;
        var footRest = rest.WorldPosition[(int)MiiBone.FootL];
        Assert.True(Vector3.Distance(footRest, pose.WorldPosition[(int)MiiBone.FootL]) < 0.05f);
        Assert.True(pose.WorldPosition[(int)MiiBone.LegL2].Z > rest.WorldPosition[(int)MiiBone.LegL2].Z + 3f, "knee should go forward");
    }

    [Fact]
    public void ArmIk_ReachesTarget()
    {
        var offset = new Vector3(-10f, 25f, 15f);
        var pose = MaleRig.Evaluate(id =>
            id.Target == TrackTarget.Limb(MiiLimb.ArmL)
                ? id.Channel switch
                {
                    Channel.IkBlend => 1f,
                    Channel.TargetX => offset.X,
                    Channel.TargetY => offset.Y,
                    Channel.TargetZ => offset.Z,
                    _ => float.NaN,
                }
                : float.NaN
        );
        var expected = MaleRig.RestWorldPosition(MiiBone.HandL) + offset;
        Assert.True(Vector3.Distance(expected, pose.WorldPosition[(int)MiiBone.HandL]) < 0.05f);
    }

    [Fact]
    public void Ik_Stretch_ReachesFarTargets()
    {
        float Sample(TrackId id, bool stretch) =>
            id.Target == TrackTarget.Limb(MiiLimb.ArmR)
                ? id.Channel switch
                {
                    Channel.IkBlend => 1f,
                    Channel.TargetX => -80f,
                    Channel.IkStretch => stretch ? 1f : 0f,
                    _ => float.NaN,
                }
                : float.NaN;

        var target = MaleRig.RestWorldPosition(MiiBone.HandR) + new Vector3(-80f, 0, 0);
        var rigid = MaleRig.Evaluate(id => Sample(id, false));
        var stretchy = MaleRig.Evaluate(id => Sample(id, true));
        Assert.True(Vector3.Distance(target, rigid.WorldPosition[(int)MiiBone.HandR]) > 5f);
        Assert.True(Vector3.Distance(target, stretchy.WorldPosition[(int)MiiBone.HandR]) < 0.1f);
    }

    [Fact]
    public void Swivel_MovesElbow_KeepsHand()
    {
        float Sample(TrackId id, float swivel) =>
            id.Target == TrackTarget.Limb(MiiLimb.ArmL)
                ? id.Channel switch
                {
                    Channel.IkBlend => 1f,
                    Channel.TargetY => 20f,
                    Channel.TargetX => -15f,
                    Channel.Swivel => swivel,
                    _ => float.NaN,
                }
                : float.NaN;

        var a = MaleRig.Evaluate(id => Sample(id, 0f));
        var b = MaleRig.Evaluate(id => Sample(id, 60f));
        Assert.True(Vector3.Distance(a.WorldPosition[(int)MiiBone.HandL], b.WorldPosition[(int)MiiBone.HandL]) < 0.05f);
        Assert.True(Vector3.Distance(a.WorldPosition[(int)MiiBone.ArmL2], b.WorldPosition[(int)MiiBone.ArmL2]) > 3f);
    }

    [Fact]
    public void Squash_PreservesVolumeAndKeepsFeetOnGround()
    {
        var pose = MaleRig.Evaluate(id => id == TrackId.Bone(MiiBone.Root, Channel.Stretch) ? 0.5f : float.NaN);
        var scale = pose.LocalScale[(int)MiiBone.Root];
        Assert.Equal(0.5f, scale.Y, 3);
        Assert.Equal(1f, scale.X * scale.Y * scale.Z, 3);
        Assert.True(MathF.Abs(pose.WorldPositionOf(MiiBone.FootL).Y - MaleRig.RestPose.WorldPositionOf(MiiBone.FootL).Y * 0.5f) < 0.05f);
    }

    [Fact]
    public void RestHead_SitsWhereWheelWizardPutsIt()
    {
        var head = MaleRig.RestPose.HeadMatrixForRender(Vector3.One);
        Assert.Equal(0f, head.M41, 2);
        Assert.Equal(10.7766f * 7f, head.M42, 2);
        Assert.Equal(0f, head.M43, 2);
        Assert.True(Matrix4x4.Decompose(head, out var s, out var r, out _));
        Assert.True(MathF.Abs(r.W) > 0.9999f, "rest head must not be rotated");
        Assert.Equal(1f, s.Y, 3);
    }

    [Fact]
    public void Euler_RoundTrips()
    {
        var e = new Vector3(20f, -35f, 50f);
        var back = MiiRig.ToEulerDegrees(MiiRig.EulerDegrees(e.X, e.Y, e.Z));
        Assert.True(Vector3.Distance(e, back) < 0.01f, back.ToString());
    }

    [Fact]
    public void FemaleBodyLoads()
    {
        var rig = new MiiRig(MiiBodyModel.Get(female: true));
        Assert.Equal(2, rig.Body.Meshes.Count);
        Assert.Contains(rig.Body.Meshes, m => m.IsPants);
    }
}
