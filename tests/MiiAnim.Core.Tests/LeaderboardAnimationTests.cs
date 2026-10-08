using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;

namespace MiiAnim.Core.Tests;

public class LeaderboardAnimationTests
{
    public static IEnumerable<object[]> Scenes => LeaderboardAnimations.All.Select(a => new object[] { a.Path });
    public static IEnumerable<object[]> ScenesAndBodies => LeaderboardAnimations.All.SelectMany(a =>
        new[] { new object[] { a.Path, false }, new object[] { a.Path, true } });

    private static MiiAnimation Create(string path) => LeaderboardAnimations.All.Single(a => a.Path == path).Create();
    private static string BundleDirectory => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "samples", "wheelwizard", "leaderboard"));

    [Fact]
    public void WriteLeaderboardSamples()
    {
        foreach (var (path, create) in LeaderboardAnimations.All)
        {
            var scene = create();
            WheelWizardSampleWriter.WriteRelative(path, scene);
            var folder = Path.GetDirectoryName(Path.Combine(BundleDirectory, "..", path.Replace('/', Path.DirectorySeparatorChar)))!;
            File.WriteAllText(Path.Combine(folder, "scene.json"), LeaderboardAnimations.SceneManifest(path, scene));
        }
        Assert.True(File.Exists(Path.Combine(BundleDirectory, LeaderboardAnimations.PodiumFile)));
    }

    [Theory]
    [MemberData(nameof(Scenes))]
    public void EveryClipContainsTheSameThreeRoles_AndSurvivesSerialization(string path)
    {
        var scene = Create(path);
        Assert.Equal(3, scene.ActorCount);
        Assert.Equal(1, scene.FindActor(LeaderboardAnimations.SecondRole));
        Assert.Equal(2, scene.FindActor(LeaderboardAnimations.ThirdRole));
        Assert.InRange(scene.DurationSeconds, 24, 28);
        var bytes = MiiAnimFormat.Write(scene);
        var restored = MiiAnimFormat.Read(bytes);
        Assert.Equal(bytes, MiiAnimFormat.Write(restored));
        Assert.Equal(3, restored.ActorCount);
        for (var actor = 0; actor < 3; actor++)
        {
            var motion = restored.ForActor(actor);
            Assert.NotEmpty(motion.Tracks);
            foreach (var (id, curve) in motion.Tracks)
            {
                Assert.All(curve.Keys, k => Assert.InRange(k.Frame, 0, scene.Length));
                Assert.True(MathF.Abs(motion.Evaluate(id, 0) - motion.Evaluate(id, scene.Length)) < 0.001f,
                    $"{path} actor {actor}: {id} does not close its loop");
            }
        }
        using var manifest = JsonDocument.Parse(LeaderboardAnimations.SceneManifest(path, scene));
        var root = manifest.RootElement;
        Assert.Equal(Path.GetFileName(path), root.GetProperty("animation").GetString());
        Assert.Equal("../podium.glb", root.GetProperty("podium").GetString());
        Assert.Equal(3, root.GetProperty("actors").GetArrayLength());
        for (var actor = 0; actor < 3; actor++)
        {
            Assert.Equal(LeaderboardAnimations.Platforms[actor].Y,
                root.GetProperty("actors")[actor].GetProperty("podiumTop").GetSingle());
            Assert.Equal(actor, root.GetProperty("actors")[actor].GetProperty("index").GetInt32());
        }
    }

    [Theory]
    [MemberData(nameof(ScenesAndBodies))]
    public void BothBodiesKeepTheirSolesPlantedInsideTheMatchingPlatform(string path, bool female)
    {
        var scene = Create(path);
        var body = MiiBodyModel.Get(female);
        var rig = new MiiRig(body);
        var feet = BodyVertices(body, MiiBone.FootL, MiiBone.FootR);
        Assert.NotEmpty(feet);
        for (var actor = 0; actor < 3; actor++)
        {
            var platform = LeaderboardAnimations.Platforms[actor];
            for (var frame = 0; frame <= scene.Length; frame++)
            {
                var pose = rig.Evaluate(scene.ForActor(actor), frame);
                foreach (var limb in new[] { MiiLimb.LegL, MiiLimb.LegR })
                {
                    var end = MiiSkeletonInfo.Chain(limb).End;
                    Assert.False(pose.Limbs[(int)limb].Stretched);
                    Assert.True(Vector3.Distance(pose.WorldPositionOf(end), pose.Limbs[(int)limb].Target) < 0.025f,
                        $"{path} {female} actor {actor}, frame {frame}: {limb} missed its planted target");
                }
                var points = feet.Select(v => Skin(v.Mesh, v.Index, pose)).ToArray();
                var sole = points.Min(p => p.Y);
                Assert.True(MathF.Abs(sole - platform.Y) < 0.35f,
                    $"{path} {female} actor {actor}, frame {frame}: sole {sole} vs top {platform.Y}");
                Assert.All(points, p =>
                {
                    Assert.InRange(p.X, platform.X - 64, platform.X + 64);
                    Assert.InRange(p.Z, -46, 46);
                    Assert.True(p.Y >= platform.Y - 0.35f);
                });
            }
        }
    }

    [Theory]
    [MemberData(nameof(ScenesAndBodies))]
    public void ActorsHaveClearHeadsAndTorsosThroughoutTheInteractions(string path, bool female)
    {
        var scene = Create(path);
        var body = MiiBodyModel.Get(female);
        var rig = new MiiRig(body);
        var scale = MiiStage.DefaultBodyScale;
        var arms = BodyVertices(body, MiiBone.ArmL2, MiiBone.HandL, MiiBone.ArmR2, MiiBone.HandR);
        var torso = new[] { MiiBone.Chest, MiiBone.Hip }.Select(b => (Bone: b, Bounds: Fit(body, b))).ToArray();
        for (var actor = 0; actor < 3; actor++)
        {
            var own = ClipCheck.Run(scene.ForActor(actor), female);
            var report = ClipCheck.Describe(own, headLimit: 1, torsoLimit: 0.8f);
            Assert.True(report.Length == 0, $"{path}, actor {actor}, female={female}\n{report}");
        }
        for (var frame = 0; frame <= scene.Length; frame++)
        {
            var poses = Enumerable.Range(0, 3).Select(a => rig.Evaluate(scene.ForActor(a), frame)).ToArray();
            var heads = poses.Select(p => Ellipsoid(p.HeadMatrixForRender(scale), new Vector3(0, 35, -3.7f), new Vector3(28.8f, 35, 32.4f))).ToArray();
            for (var actor = 0; actor < 3; actor++)
            {
                for (var other = actor + 1; other < 3; other++)
                {
                    var delta = heads[other].Center - heads[actor].Center;
                    var direction = Vector3.Normalize(delta);
                    var clearance = delta.Length() - Support(heads[actor], direction) - Support(heads[other], direction);
                    Assert.True(clearance > 1f, $"{path} frame {frame}, head pair {actor}/{other}: clearance {clearance}");
                }
                foreach (var vertex in arms)
                {
                    var point = Vector3.Transform(Skin(vertex.Mesh, vertex.Index, poses[actor]), MiiStage.BodyToStage(poses[actor], scale));
                    for (var other = 0; other < 3; other++)
                    {
                        if (other == actor)
                            continue;
                        Assert.True(Outside(point, heads[other]) > 1f,
                            $"{path} frame {frame}: actor {actor}'s arm entered actor {other}'s head");
                        foreach (var (bone, bounds) in torso)
                        {
                            var matrix = poses[other].BoneMatrix[(int)bone] * MiiStage.BodyToStage(poses[other], scale);
                            Assert.True(Outside(point, Ellipsoid(matrix, bounds.Center, bounds.Radii)) > 1f,
                                $"{path} frame {frame}: actor {actor}'s arm entered actor {other}'s {bone}");
                        }
                    }
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HighFivePalmsMeet_AndTheApproachHasNoJump(bool female)
    {
        var scene = LeaderboardAnimations.FirstPlaceHighFives();
        var rig = new MiiRig(MiiBodyModel.Get(female));
        foreach (var (contact, neighbour, goldHand, otherHand, point) in new[]
        {
            (LeaderboardAnimations.FirstSilverContact, 1, MiiBone.HandR, MiiBone.HandL, new Vector3(-66, 166, 8)),
            (LeaderboardAnimations.FirstBronzeContact, 2, MiiBone.HandL, MiiBone.HandR, new Vector3(66, 150, 8)),
        })
        {
            Vector3? previousGold = null, previousOther = null;
            for (var frame = contact - 30; frame <= contact + 32; frame++)
            {
                var gold = rig.Evaluate(scene, frame).WorldPositionOf(goldHand);
                var other = rig.Evaluate(scene.ForActor(neighbour), frame).WorldPositionOf(otherHand);
                if (previousGold is { } g && previousOther is { } o)
                {
                    Assert.True(Vector3.Distance(gold, g) < 4f, $"gold hand jumped at {frame}");
                    Assert.True(Vector3.Distance(other, o) < 4f, $"neighbour hand jumped at {frame}");
                }
                if (frame is var f && f >= contact && f <= contact + 6)
                {
                    Assert.True(Vector3.Distance(gold, point) < 0.35f, $"gold contact error at {frame}: {gold}");
                    Assert.True(Vector3.Distance(other, point) < 0.35f, $"neighbour contact error at {frame}: {other}");
                    Assert.True(Vector3.Distance(gold, other) < 0.5f);
                }
                previousGold = gold;
                previousOther = other;
            }
        }
    }

    [Theory]
    [MemberData(nameof(Scenes))]
    public void LongQuietStretchesSeparateThePlayfulBits(string path)
    {
        var scene = Create(path);
        // Six calm seconds before the first bit and at least six after the last; arms are down and faces neutral.
        foreach (var frame in new[] { 0, 30, 90, 180, 300, scene.Length - 330, scene.Length - 180, scene.Length })
            for (var actor = 0; actor < 3; actor++)
            {
                var motion = scene.ForActor(actor);
                foreach (var arm in new[] { MiiBone.ArmL1, MiiBone.ArmL2, MiiBone.ArmR1, MiiBone.ArmR2 })
                    foreach (var channel in new[] { Channel.RotX, Channel.RotY, Channel.RotZ })
                        Assert.True(MathF.Abs(motion.Evaluate(TrackId.Bone(arm, channel), frame)) < 0.001f);
                Assert.InRange(motion.Evaluate(TrackId.Expression, frame), 0, 5); // Normal or occasional blink.
            }
    }

    [Fact]
    public void PodiumGlbIsSelfContained_WithCorrectStandingSurfacesAndValidTriangles()
    {
        var data = File.ReadAllBytes(Path.Combine(BundleDirectory, LeaderboardAnimations.PodiumFile));
        Assert.Equal(0x46546C67u, BinaryPrimitives.ReadUInt32LittleEndian(data));
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)));
        Assert.Equal(data.Length, BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8)));
        var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(12));
        Assert.Equal(0x4E4F534Au, BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(16)));
        Assert.Equal(0, jsonLength % 4);
        using var document = JsonDocument.Parse(data.AsMemory(20, jsonLength));
        var root = document.RootElement;
        var binaryStart = 20 + jsonLength + 8;
        var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(20 + jsonLength));
        Assert.Equal(0x004E4942u, BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(24 + jsonLength)));
        Assert.Equal(data.Length, binaryStart + binaryLength);
        Assert.Single(root.GetProperty("buffers").EnumerateArray());
        Assert.False(root.GetProperty("buffers")[0].TryGetProperty("uri", out _));
        Assert.False(root.TryGetProperty("images", out _));
        Assert.Equal(5, root.GetProperty("materials").GetArrayLength());
        var declaredBinaryLength = root.GetProperty("buffers")[0].GetProperty("byteLength").GetInt32();
        Assert.InRange(binaryLength - declaredBinaryLength, 0, 3);
        foreach (var view in root.GetProperty("bufferViews").EnumerateArray())
        {
            Assert.Equal(0, view.GetProperty("buffer").GetInt32());
            Assert.Equal(0, view.GetProperty("byteOffset").GetInt32() % 4);
            Assert.True(view.GetProperty("byteOffset").GetInt32() + view.GetProperty("byteLength").GetInt32() <= declaredBinaryLength);
        }
        var anchors = root.GetProperty("nodes").EnumerateArray().Where(n => n.GetProperty("name").GetString()!.EndsWith("_anchor")).ToArray();
        Assert.Equal(3, anchors.Length);
        var meshes = root.GetProperty("meshes");
        for (var actor = 0; actor < 3; actor++)
        {
            Assert.Equal(LeaderboardAnimations.Platforms[actor], ReadVector(anchors[actor].GetProperty("translation")));
            var meshName = new[] { "first_gold", "second_silver", "third_bronze" }[actor];
            var mesh = meshes.EnumerateArray().Single(m => m.GetProperty("name").GetString() == meshName);
            var accessor = mesh.GetProperty("primitives")[0].GetProperty("attributes").GetProperty("POSITION").GetInt32();
            Assert.Equal(LeaderboardAnimations.Platforms[actor].Y, root.GetProperty("accessors")[accessor].GetProperty("max")[1].GetSingle());
        }
        Assert.Equal("canonical", root.GetProperty("extras").GetProperty("units").GetString());
        Assert.Equal("+Z", root.GetProperty("extras").GetProperty("front").GetString());
        Assert.Equal(0.7f, root.GetProperty("extras").GetProperty("canonicalToRender").GetSingle());
        Assert.True(Vector3.Distance(MiiStage.DefaultBodyScale, ReadVector(root.GetProperty("extras").GetProperty("defaultBodyScale"))) < 0.00001f);
        foreach (var mesh in meshes.EnumerateArray())
            foreach (var primitive in mesh.GetProperty("primitives").EnumerateArray())
            {
                Assert.Equal(4, primitive.GetProperty("mode").GetInt32());
                Assert.InRange(primitive.GetProperty("material").GetInt32(), 0, 4);
                var positions = ReadVectors(root, data, binaryStart, primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32());
                var normals = ReadVectors(root, data, binaryStart, primitive.GetProperty("attributes").GetProperty("NORMAL").GetInt32());
                Assert.Equal(positions.Length, normals.Length);
                Assert.All(normals, n => Assert.InRange(n.Length(), 0.9999f, 1.0001f));
                var indexAccessor = root.GetProperty("accessors")[primitive.GetProperty("indices").GetInt32()];
                Assert.Equal(5123, indexAccessor.GetProperty("componentType").GetInt32());
                var view = root.GetProperty("bufferViews")[indexAccessor.GetProperty("bufferView").GetInt32()];
                var start = binaryStart + view.GetProperty("byteOffset").GetInt32();
                var count = indexAccessor.GetProperty("count").GetInt32();
                Assert.Equal(0, count % 3);
                for (var i = 0; i < count; i += 3)
                {
                    var a = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(start + i * 2));
                    var b = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(start + (i + 1) * 2));
                    var c = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(start + (i + 2) * 2));
                    Assert.InRange(a, 0, positions.Length - 1);
                    Assert.InRange(b, 0, positions.Length - 1);
                    Assert.InRange(c, 0, positions.Length - 1);
                    var normal = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
                    Assert.True(normal.LengthSquared() > 0.00001f);
                    Assert.True(Vector3.Dot(Vector3.Normalize(normal), normals[a]) > 0.999f);
                }
            }
    }

    private static List<(MiiBodyMesh Mesh, int Index)> BodyVertices(MiiBodyModel body, params MiiBone[] bones) => body.Meshes
        .SelectMany(mesh => Enumerable.Range(0, mesh.Positions.Length).Where(i => bones.Contains((MiiBone)mesh.Joints[i * 4])).Select(i => (Mesh: mesh, Index: i))).ToList();

    private static Vector3 Skin(MiiBodyMesh mesh, int index, MiiPose pose)
    {
        var point = Vector3.Zero;
        for (var influence = 0; influence < 4; influence++)
            point += Vector3.Transform(mesh.Positions[index], pose.SkinMatrix[mesh.Joints[index * 4 + influence]]) * mesh.Weights[index * 4 + influence];
        return point;
    }

    private static (Vector3 Center, Vector3 Radii) Fit(MiiBodyModel body, MiiBone bone)
    {
        var vertices = BodyVertices(body, bone).Select(v => Vector3.Transform(v.Mesh.Positions[v.Index], body.InverseBind[(int)bone])).ToArray();
        var min = vertices.Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        var max = vertices.Aggregate(new Vector3(float.MinValue), Vector3.Max);
        return ((min + max) / 2, Vector3.Max((max - min) / 2, Vector3.One));
    }

    private readonly record struct Bounds(Matrix4x4 Matrix, Vector3 LocalCenter, Vector3 Radii, Vector3 Center);
    private static Bounds Ellipsoid(Matrix4x4 matrix, Vector3 center, Vector3 radii) => new(matrix, center, radii, Vector3.Transform(center, matrix));
    private static float Support(Bounds bounds, Vector3 direction)
    {
        var x = Vector3.Dot(Vector3.TransformNormal(Vector3.UnitX * bounds.Radii.X, bounds.Matrix), direction);
        var y = Vector3.Dot(Vector3.TransformNormal(Vector3.UnitY * bounds.Radii.Y, bounds.Matrix), direction);
        var z = Vector3.Dot(Vector3.TransformNormal(Vector3.UnitZ * bounds.Radii.Z, bounds.Matrix), direction);
        return MathF.Sqrt(x * x + y * y + z * z);
    }
    private static float Outside(Vector3 point, Bounds bounds)
    {
        Matrix4x4.Invert(bounds.Matrix, out var inverse);
        return ((Vector3.Transform(point, inverse) - bounds.LocalCenter) / bounds.Radii).Length();
    }
    private static Vector3 ReadVector(JsonElement values) => new(values[0].GetSingle(), values[1].GetSingle(), values[2].GetSingle());
    private static Vector3[] ReadVectors(JsonElement root, byte[] data, int binaryStart, int accessorIndex)
    {
        var accessor = root.GetProperty("accessors")[accessorIndex];
        Assert.Equal("VEC3", accessor.GetProperty("type").GetString());
        Assert.Equal(5126, accessor.GetProperty("componentType").GetInt32());
        var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
        var start = binaryStart + view.GetProperty("byteOffset").GetInt32();
        var count = accessor.GetProperty("count").GetInt32();
        float At(int offset) => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(start + offset * 4)));
        return Enumerable.Range(0, count).Select(i => new Vector3(At(i * 3), At(i * 3 + 1), At(i * 3 + 2))).ToArray();
    }
}
