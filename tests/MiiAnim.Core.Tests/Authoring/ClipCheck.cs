using System.Numerics;
using System.Text;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Rig;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// Frame-by-frame sanity check for authored animations: do the hands/arms go through the head or torso,
/// and do the feet sink into the ground? The head is approximated by an ellipsoid fitted to a typical FFL head
/// (face + hair) in its own render-space frame.
/// </summary>
public static class ClipCheck
{
    // Default FFL head (face + medium hair), render units, relative to the head origin.
    private static readonly Vector3 HeadCenter = new(0f, 35f, -3.7f);
    private static readonly Vector3 HeadRadii = new(28.8f, 35f, 32.4f);

    private static readonly MiiBone[] ArmBones = [MiiBone.ArmL2, MiiBone.HandL, MiiBone.ArmR2, MiiBone.HandR];
    private static readonly MiiBone[] FootBones = [MiiBone.FootL, MiiBone.FootR];

    public sealed record FrameResult(int Frame, float Head, MiiBone HeadBone, float Torso, MiiBone TorsoBone, MiiBone TorsoArm, float FootY);

    public static List<FrameResult> Run(MiiAnimation animation, bool female, float build = 64, float height = 64)
    {
        var body = MiiBodyModel.Get(female);
        var rig = new MiiRig(body);
        var scale = MiiBodyModel.CalculateBodyScale(build, height);
        var torso = new[] { MiiBone.Chest, MiiBone.Hip }.Select(b => (Bone: b, Ellipsoid: Fit(body, b))).ToList();

        var vertices = new List<(Vector3 Position, MiiBone Bone)>();
        foreach (var mesh in body.Meshes)
            for (var i = 0; i < mesh.Positions.Length; i++)
                vertices.Add((mesh.Positions[i], (MiiBone)mesh.Joints[i * 4]));

        var results = new List<FrameResult>();
        for (var frame = 0; frame <= animation.Length; frame++)
        {
            var pose = rig.Evaluate(animation, frame);
            Matrix4x4.Invert(pose.HeadMatrixForRender(scale), out var headInverse);
            float head = float.MaxValue,
                torsoMin = float.MaxValue,
                footY = float.MaxValue;
            MiiBone headBone = default,
                torsoBone = default,
                torsoArm = default;
            foreach (var (position, bone) in vertices)
            {
                var skinned = Vector3.Transform(position, pose.SkinMatrix[(int)bone]);
                if (FootBones.Contains(bone))
                    footY = MathF.Min(footY, skinned.Y);
                if (!ArmBones.Contains(bone))
                    continue;

                var render = skinned * MiiBodyModel.CanonicalToRenderUnits * scale;
                var inHead = Vector3.Transform(render, headInverse);
                var h = ((inHead - HeadCenter) / HeadRadii).Length();
                if (h < head)
                    (head, headBone) = (h, bone);

                foreach (var (torsoBoneId, (center, radii)) in torso)
                {
                    Matrix4x4.Invert(pose.BoneMatrix[(int)torsoBoneId], out var inverse);
                    var local = Vector3.Transform(skinned, inverse);
                    var t = ((local - center) / radii).Length();
                    if (t < torsoMin)
                        (torsoMin, torsoBone, torsoArm) = (t, torsoBoneId, bone);
                }
            }

            results.Add(new FrameResult(frame, head, headBone, torsoMin, torsoBone, torsoArm, footY));
        }

        return results;
    }

    /// <summary>Human-readable summary: frame ranges where something goes inside the head/torso (value &lt; 1).</summary>
    public static string Describe(List<FrameResult> results, float headLimit = 1f, float torsoLimit = 1f)
    {
        var sb = new StringBuilder();
        Ranges(sb, "head", results.Where(r => r.Head < headLimit).ToList(), r => $"{r.Head:0.00} {r.HeadBone}");
        Ranges(sb, "torso", results.Where(r => r.Torso < torsoLimit).ToList(), r => $"{r.Torso:0.00} {r.TorsoArm} in {r.TorsoBone}");
        return sb.ToString();
    }

    private static void Ranges(StringBuilder sb, string what, List<FrameResult> hits, Func<FrameResult, string> describe)
    {
        for (var i = 0; i < hits.Count; )
        {
            var j = i;
            while (j + 1 < hits.Count && hits[j + 1].Frame == hits[j].Frame + 1)
                j++;
            var worst = hits.Skip(i).Take(j - i + 1).MinBy(r => what == "head" ? r.Head : r.Torso)!;
            sb.AppendLine($"  {what} frames {hits[i].Frame}-{hits[j].Frame}, worst {worst.Frame}: {describe(worst)}");
            i = j + 1;
        }
    }

    private static (Vector3 Center, Vector3 Radii) Fit(MiiBodyModel body, MiiBone bone)
    {
        var points = body
            .Meshes.SelectMany(m => m.Positions.Where((_, i) => m.Joints[i * 4] == (int)bone))
            .Select(p => Vector3.Transform(p, body.InverseBind[(int)bone]))
            .ToList();
        var min = points.Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        var max = points.Aggregate(new Vector3(float.MinValue), Vector3.Max);
        return ((min + max) / 2f, Vector3.Max((max - min) / 2f, new Vector3(1f)));
    }
}
