using System.Numerics;
using MiiAnim.Core.Animation;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Format;
using MiiAnim.Core.Rig;
using MiiAnim.Core.Tests.Authoring;
using Xunit.Abstractions;
using MiiAnimator.Authoring;

namespace MiiAnim.Core.Tests;

public class EditorAnimationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Clips => EditorAnimations.All.Select(a => new object[] { a.Path });
    public static IEnumerable<object[]> CloseUpClips => EditorAnimations.All
        .Where(a => a.Path.EndsWith("_upper.miianim", StringComparison.Ordinal))
        .Select(a => new object[] { a.Path });

    private static MiiAnimation Get(string path) => EditorAnimations.All.Single(a => a.Path == path).Create();

    [Theory]
    [MemberData(nameof(Clips))]
    public void Reactions_KeepHandsOutsideTheHeadAndBody_AndFeetAboveGround(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var results = ClipCheck.Run(animation, female);
            var collisions = ClipCheck.Describe(results, headLimit: 1f, torsoLimit: 0.8f);
            var sunk = results.Where(r => r.FootY < -1.5f).ToArray();
            output.WriteLine($"{path} {(female ? "female" : "male")}: {(collisions.Length == 0 ? "clear" : collisions)}");
            Assert.True(collisions.Length == 0, $"{path} female={female}: {collisions}");
            Assert.True(sunk.Length == 0, $"{path} female={female}: feet below ground at {string.Join(", ", sunk.Select(r => $"{r.Frame}:{r.FootY:0.00}"))}");
        }
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void Reactions_EvaluateFinite_AndSettleToRest(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var matrix in pose.SkinMatrix)
                    Assert.True(IsFinite(matrix), $"{path} female={female}: non-finite pose at {frame}");
            }
            foreach (var frame in new[] { animation.Length - 12, animation.Length })
            {
                var pose = rig.Evaluate(animation, frame);
                Assert.Equal(MiiExpression.Normal, pose.Expression);
                foreach (var bone in MiiSkeletonInfo.AllBones)
                {
                    var i = (int)bone;
                    Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[i], pose.WorldPosition[i]) < 0.15f,
                        $"{path} female={female}: {bone} does not settle at frame {frame}");
                    Assert.True(MathF.Abs(Quaternion.Dot(rig.RestPose.WorldRotation[i], pose.WorldRotation[i])) > 0.9999f,
                        $"{path} female={female}: {bone} is still rotated at frame {frame}");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(CloseUpClips))]
    public void CloseUpVariants_FixRootLegsAndHeadOrigin_AndKeepHandsLow(string path)
    {
        var animation = Get(path);
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var first = rig.Evaluate(animation, 0);
            var fixedBones = new[] { MiiBone.Root, MiiBone.Hip, MiiBone.Chest, MiiBone.LegL1, MiiBone.LegL2, MiiBone.FootL, MiiBone.LegR1, MiiBone.LegR2, MiiBone.FootR };
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var bone in fixedBones)
                {
                    var i = (int)bone;
                    Assert.True(Vector3.Distance(first.WorldPosition[i], pose.WorldPosition[i]) < 0.01f,
                        $"{path}: {bone} moved at {frame}");
                    Assert.True(MathF.Abs(Quaternion.Dot(first.WorldRotation[i], pose.WorldRotation[i])) > 0.99999f,
                        $"{path}: {bone} rotated at {frame}");
                }
                Assert.True(Vector3.Distance(first.WorldPosition[(int)MiiBone.Head], pose.WorldPosition[(int)MiiBone.Head]) < 0.01f,
                    $"{path}: head origin drifted at {frame}");
                foreach (var hand in new[] { MiiBone.HandL, MiiBone.HandR })
                    Assert.True(pose.WorldPosition[(int)hand].Y < pose.WorldPosition[(int)MiiBone.Head].Y - 20f,
                        $"{path}: {hand} entered the close-up face area at {frame}");
            }
            // The small/short body exposes the head to a greater part of the shoulder region. Check it
            // separately instead of assuming the default FFL head/body ratio covers close zoom shots.
            var compact = ClipCheck.Run(animation, female, build: 0, height: 0);
            var clipping = ClipCheck.Describe(compact, headLimit: 1f, torsoLimit: 0.8f);
            Assert.True(clipping.Length == 0, $"{path} compact female={female}: {clipping}");
        }
    }

    [Theory]
    [MemberData(nameof(CloseUpClips))]
    public void CloseUpVariants_HaveMatchingLoopBoundaries_AndShowTheEditedFeature(string path)
    {
        var animation = Get(path);
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var first = rig.Evaluate(animation, 0);
        var last = rig.Evaluate(animation, animation.Length);
        foreach (var bone in MiiSkeletonInfo.AllBones)
        {
            var i = (int)bone;
            Assert.True(Vector3.Distance(first.WorldPosition[i], last.WorldPosition[i]) < 0.01f);
            Assert.True(MathF.Abs(Quaternion.Dot(first.WorldRotation[i], last.WorldRotation[i])) > 0.99999f);
        }
        var normalFrames = Enumerable.Range(0, animation.Length + 1)
            .Count(frame => rig.Evaluate(animation, frame).Expression == MiiExpression.Normal);
        Assert.True(normalFrames >= animation.Length * 0.65f, $"{path}: expressions obscure the edited feature for too long");
        var marker = Assert.Single(animation.Events, e => e.Name == "feature_visible");
        Assert.Equal(MiiExpression.Normal, rig.Evaluate(animation, marker.Frame).Expression);
    }

    [Theory]
    [MemberData(nameof(Clips))]
    public void Clips_RoundTripWithTheirTimingEventsAndMotion(string path)
    {
        var animation = Get(path);
        var decoded = MiiAnimFormat.Read(MiiAnimFormat.Write(animation));
        Assert.Equal(animation.Name, decoded.Name);
        Assert.Equal(60, decoded.Fps);
        Assert.Equal(animation.Length, decoded.Length);
        Assert.Equal(animation.Events, decoded.Events);
        Assert.Contains(decoded.Events, e => e.Frame == decoded.Length && e.Name == "done");
        var rig = new MiiRig(MiiBodyModel.Get(false));
        foreach (var frame in Enumerable.Range(0, animation.Length + 1))
        {
            var expected = rig.Evaluate(animation, frame);
            var actual = rig.Evaluate(decoded, frame);
            Assert.Equal(expected.Expression, actual.Expression);
            foreach (var bone in MiiSkeletonInfo.AllBones)
            {
                var i = (int)bone;
                Assert.True(Vector3.Distance(expected.WorldPosition[i], actual.WorldPosition[i]) < 0.01f,
                    $"{path}: {bone} changed during file round trip at {frame}");
            }
        }
    }

    [Fact]
    public void Catalog_CoversEveryCurrentAction_AndEveryFacialActionHasAnUpperVariant()
    {
        var expected = new[] { "body_shape", "enter", "eyes_eyebrows", "face", "facial_hair", "favorite", "favoritecolor", "gender", "glasses", "hair", "idle", "interactions", "mole", "mouth", "name", "nose", "randomize", "save" };
        var paths = EditorAnimations.All.Select(a => a.Path).Concat(EditorAnimations.PreservedPaths)
            .Concat(EditorIdleAnimations.All.Select(a => a.Path))
            .Concat(EditorInteractionAnimations.All.Select(a => a.Path)).ToArray();
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(expected.OrderBy(a => a), paths.Select(p => p.Split('/')[1]).Distinct().OrderBy(a => a));
        foreach (var action in EditorAnimations.CloseUpActions)
            Assert.Contains(EditorAnimations.All, entry => entry.Path.StartsWith($"editor/{action}/", StringComparison.Ordinal) && entry.Path.EndsWith("_upper.miianim", StringComparison.Ordinal));
        Assert.DoesNotContain(EditorAnimations.All, entry => EditorAnimations.PreservedPaths.Contains(entry.Path));
    }

    [Fact]
    public void NoseProfiles_ExposeBothSides_AndRandomizeHasAMaskedSwapBeat()
    {
        var rig = new MiiRig(MiiBodyModel.Get(false));
        var nose = EditorAnimations.NoseProfileCheck();
        var leftForward = Vector3.Transform(Vector3.UnitZ, rig.Evaluate(nose, 44).WorldRotation[(int)MiiBone.Head]);
        var rightForward = Vector3.Transform(Vector3.UnitZ, rig.Evaluate(nose, 106).WorldRotation[(int)MiiBone.Head]);
        Assert.True(leftForward.X > 0.55f && rightForward.X < -0.55f, "The nose should visibly turn to both profiles.");
        foreach (var animation in new[] { EditorAnimations.RandomizeShuffle(), EditorAnimations.RandomizeReveal() })
        {
            var marker = Assert.Single(animation.Events, e => e.Name == "randomize");
            Assert.Contains(rig.Evaluate(animation, marker.Frame).Expression, new[] { MiiExpression.Blink, MiiExpression.BlinkOpenMouth });
            var reveal = Assert.Single(animation.Events, e => e.Name == "reveal");
            Assert.True(reveal.Frame > marker.Frame);
        }
    }

    [Fact]
    public void MouthInspection_AlwaysShowsTheMiisOwnMouth()
    {
        var rig = new MiiRig(MiiBodyModel.Get(false));
        foreach (var authored in new[] { EditorAnimations.MouthSayHello(), EditorAnimations.MouthSmileTestUpper() })
        {
            foreach (var animation in new[] { authored, MiiAnimFormat.Read(MiiAnimFormat.Write(authored)) })
            {
                for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
                    Assert.Equal(MiiExpression.Normal, rig.Evaluate(animation, frame).Expression);
                var first = rig.Evaluate(animation, 0).WorldRotation[(int)MiiBone.Head];
                var inspecting = rig.Evaluate(animation, 40).WorldRotation[(int)MiiBone.Head];
                Assert.True(MathF.Abs(Quaternion.Dot(first, inspecting)) < 0.999f,
                    $"{animation.Name}: keep a readable inspection motion when expression changes are disabled");
            }
        }
    }

    [Fact]
    public void EntranceFavoriteAndRandomizeReveal_DoNotUseOneEyeExpressions()
    {
        var entrance = EditorAnimations.EnterWave();
        foreach (var animation in new[] { entrance, EditorAnimations.FavoriteSelected(), EditorAnimations.RandomizeReveal() })
        {
            for (var frame = 0; frame <= animation.Length; frame++)
            {
                var expression = MiiExpressionInfo.FromValue(animation.Evaluate(TrackId.Expression, frame));
                Assert.False(MiiExpressionInfo.Replaces(expression).OneEye, $"{animation.Name}: wink at {frame}");
                if (ReferenceEquals(animation, entrance))
                    Assert.DoesNotContain(expression, new[] { MiiExpression.Blink, MiiExpression.BlinkOpenMouth });
            }
        }
    }

    [Fact]
    public void ShirtComparison_HoldsEachRaisedHandForTheSameInspectionTime()
    {
        var animation = EditorAnimations.ShirtColorCompare();
        var leftStart = Assert.Single(animation.Events, e => e.Name == "left_sleeve").Frame;
        var leftEnd = Assert.Single(animation.Events, e => e.Name == "left_sleeve_end").Frame;
        var rightStart = Assert.Single(animation.Events, e => e.Name == "right_sleeve").Frame;
        var rightEnd = Assert.Single(animation.Events, e => e.Name == "right_sleeve_end").Frame;
        Assert.Equal(leftEnd - leftStart, rightEnd - rightStart);
        Assert.True(leftEnd - leftStart >= animation.Fps * 0.5f, "Each sleeve needs a readable half-second hold.");
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            foreach (var (hand, from, to) in new[] { (MiiBone.HandL, leftStart, leftEnd), (MiiBone.HandR, rightStart, rightEnd) })
            {
                var start = rig.Evaluate(animation, from);
                var i = (int)hand;
                Assert.True(start.WorldPosition[i].Y > rig.RestPose.WorldPosition[i].Y + 20f, "The inspected sleeve should be raised.");
                for (var frame = (float)from; frame <= to; frame += 0.5f)
                {
                    var pose = rig.Evaluate(animation, frame);
                    Assert.True(Vector3.Distance(start.WorldPosition[i], pose.WorldPosition[i]) < 0.03f,
                        $"{hand} is still moving during its inspection hold at {frame}");
                    Assert.True(MathF.Abs(Quaternion.Dot(start.WorldRotation[i], pose.WorldRotation[i])) > 0.99999f);
                }
            }
        }
    }

    [Fact]
    public void GlassesLookOver_KeepsBothArmsAtRest_WhileInspectingBothSides()
    {
        var animation = EditorAnimations.GlassesLookOver();
        var arms = new[] { MiiBone.ArmL1, MiiBone.ArmL2, MiiBone.HandL, MiiBone.ArmR1, MiiBone.ArmR2, MiiBone.HandR };
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                foreach (var bone in arms)
                {
                    var i = (int)bone;
                    Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[i], pose.WorldPosition[i]) < 0.01f);
                    Assert.True(MathF.Abs(Quaternion.Dot(rig.RestPose.WorldRotation[i], pose.WorldRotation[i])) > 0.99999f);
                }
            }
            var left = Vector3.Transform(Vector3.UnitZ, rig.Evaluate(animation, 46).WorldRotation[(int)MiiBone.Head]);
            var right = Vector3.Transform(Vector3.UnitZ, rig.Evaluate(animation, 104).WorldRotation[(int)MiiBone.Head]);
            Assert.True(left.X > 0.25f && right.X < -0.25f, "Both glasses inspection angles should remain readable.");
        }
    }

    [Fact]
    public void GlassesCloseUp_SlowsTheHeadInspection_WithoutLengtheningTheBlink()
    {
        var animation = EditorAnimations.GlassesFocusUpper();
        Assert.InRange(animation.Length / (float)animation.Fps, 3.3f, 3.7f);
        var blinkFrames = Enumerable.Range(0, animation.Length + 1)
            .Count(frame => MiiExpressionInfo.FromValue(animation.Evaluate(TrackId.Expression, frame)) == MiiExpression.Blink);
        Assert.InRange(blinkFrames, 6, 10);
        var yaw = TrackId.Bone(MiiBone.Head, Channel.RotY);
        var firstHeldAngle = Enumerable.Range(0, animation.Length + 1).First(frame => animation.Evaluate(yaw, frame) >= 5.99f);
        Assert.True(firstHeldAngle >= animation.Fps * 0.6f, "Give the first inspection turn time to read in close-up.");
        var rig = new MiiRig(MiiBodyModel.Get(false));
        for (var frame = 1; frame <= animation.Length; frame++)
        {
            var before = rig.Evaluate(animation, frame - 1).WorldRotation[(int)MiiBone.Head];
            var after = rig.Evaluate(animation, frame).WorldRotation[(int)MiiBone.Head];
            var degrees = 2f * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(before, after)), 0f, 1f)) * 180f / MathF.PI;
            Assert.True(degrees < 0.55f, $"Close-up head turn is too abrupt at frame {frame}: {degrees} degrees");
        }
    }

    [Fact]
    public void HaircutInspection_LingersAtBothAngles_WithoutSwishing()
    {
        var animation = EditorAnimations.HairSwish();
        var rig = new MiiRig(MiiBodyModel.Get(false));
        foreach (var (from, to) in new[] { (42, 78), (132, 168) })
        {
            var first = rig.Evaluate(animation, from);
            for (var frame = (float)from; frame <= to; frame += 0.5f)
            {
                var pose = rig.Evaluate(animation, frame);
                Assert.True(Vector3.Distance(first.WorldPosition[(int)MiiBone.Head], pose.WorldPosition[(int)MiiBone.Head]) < 0.01f);
                Assert.True(MathF.Abs(Quaternion.Dot(first.WorldRotation[(int)MiiBone.Head], pose.WorldRotation[(int)MiiBone.Head])) > 0.99999f);
                Assert.Equal(MiiExpression.Normal, pose.Expression);
            }
        }
        for (var frame = 0; frame <= animation.Length; frame++)
            Assert.Equal(0f, animation.Evaluate(TrackId.Bone(MiiBone.Head, Channel.RotZ), frame));
    }

    [Fact]
    public void FavoriteRemoval_HasThreeFixedPurpleFrustrationMarks_ThatEndBeforeDone()
    {
        foreach (var animation in new[] { EditorAnimations.FavoriteRemoved(), EditorAnimations.FavoriteRemovedStomp() })
        {
            Assert.Contains(Enumerable.Range(0, animation.Length + 1), frame =>
                MiiExpressionInfo.FromValue(animation.Evaluate(TrackId.Expression, frame)) == MiiExpression.Frustrated);
            var lines = animation.Particles.Where(p => p.Shape == BuiltInShapes.Squiggle).ToArray();
            Assert.Equal(3, lines.Length);
            Assert.Equal(3, lines.Select(p => p.Offset).Distinct().Count());
            foreach (var line in lines)
            {
                Assert.Equal(1, line.Count);
                Assert.Equal(0, line.Duration);
                Assert.Equal((byte)MiiBone.Head, line.AttachBone);
                Assert.InRange(line.Offset.Y, 110f, 140f);
                Assert.Equal(Vector3.Zero, line.SpawnRadius);
                Assert.Equal(0f, line.SpeedMin);
                Assert.Equal(0f, line.SpeedMax);
                Assert.Equal(0f, line.Gravity);
                Assert.Equal(ParticleTint.None, line.Tint);
                Assert.True(line.ColorStart.Z > 0.85f && line.ColorStart.X > 0.6f && line.ColorStart.Y < 0.35f, "Frustration lines should stay purple.");
                Assert.Equal(new Vector3(line.ColorStart.X, line.ColorStart.Y, line.ColorStart.Z), new Vector3(line.ColorEnd.X, line.ColorEnd.Y, line.ColorEnd.Z));
                Assert.Equal(0f, line.ColorEnd.W);
                Assert.True(line.EndFrame(animation.Fps) <= animation.Length - 12);
            }
            var early = ParticlesAt(animation, lines[0].StartFrame + 8).Where(p => p.Effect.Shape == BuiltInShapes.Squiggle).ToArray();
            var later = ParticlesAt(animation, lines[0].StartFrame + 20).Where(p => p.Effect.Shape == BuiltInShapes.Squiggle).ToArray();
            Assert.Equal(3, early.Length);
            Assert.Equal(early.Select(p => p.Position), later.Select(p => p.Position));
            Assert.All(early, particle => Assert.True(particle.Color.Z > 0.85f && particle.Color.Y > 0.2f,
                "A red shirt must not recolor the purple frustration lines."));
        }
    }

    [Fact]
    public void ShirtColorPresent_UsesTheShirtTintedPoofPreset_AtFrameOne()
    {
        var animation = EditorAnimations.ShirtColorPresent();
        var poof = Assert.Single(animation.Particles);
        var preset = ParticlePresets.Poof();
        Assert.Equal(1, poof.StartFrame);
        Assert.Equal(ParticleTint.FavoriteColor, poof.Tint);
        Assert.Equal(preset.Shape, poof.Shape);
        Assert.Equal(preset.Count, poof.Count);
        Assert.Equal(preset.SpawnRadius, poof.SpawnRadius);
        Assert.Equal(preset.SizeStart, poof.SizeStart);
        Assert.Equal(preset.SizeEnd, poof.SizeEnd);
        Assert.Equal(preset.LifeMin, poof.LifeMin);
        Assert.Equal(preset.LifeMax, poof.LifeMax);
        Assert.All(ParticlesAt(animation, 12), p => Assert.True(p.Color.X > 0 && p.Color.Y == 0 && p.Color.Z == 0));
    }

    [Fact]
    public void RandomizeReveal_HasALargePoofDuringSwapAndReveal_ThenClearsBeforeDone()
    {
        var animation = EditorAnimations.RandomizeReveal();
        var swap = Assert.Single(animation.Events, e => e.Name == "randomize").Frame;
        var reveal = Assert.Single(animation.Events, e => e.Name == "reveal").Frame;
        var poofs = animation.Particles.Where(p => p.Shape == BuiltInShapes.Puff).ToArray();
        Assert.True(poofs.Sum(p => p.Count) >= 70, "The transformation needs a large, full body and head burst.");
        Assert.All(poofs, p =>
        {
            Assert.True(p.SizeStart >= ParticlePresets.Poof().SizeStart * 1.7f);
            Assert.True(p.StartFrame <= swap && p.EndFrame(animation.Fps) > reveal);
        });
        Assert.NotEmpty(ParticlesAt(animation, swap + 3));
        Assert.True(ParticlesAt(animation, reveal).Count >= 50, "The poof should still be alive when the reveal begins.");
        Assert.Empty(ParticlesAt(animation, animation.Length - 12));
        Assert.Empty(ParticlesAt(animation, animation.Length));
    }

    [Fact]
    public void RevisedParticles_HaveValidLifetimesAndNoCarryoverIntoTheFinalRestPose()
    {
        foreach (var entry in EditorAnimations.All.Where(entry => EditorAnimations.RevisedPaths.Contains(entry.Path)))
        {
            var animation = entry.Create();
            foreach (var particle in animation.Particles)
            {
                Assert.True(particle.StartFrame >= 0 && particle.Count > 0);
                Assert.True(particle.LifeMin > 0 && particle.LifeMax >= particle.LifeMin);
                Assert.True(particle.EndFrame(animation.Fps) <= animation.Length - 12,
                    $"{entry.Path}: {particle.Name} outlives the action");
                Assert.Equal(0f, particle.ColorEnd.W);
            }
            Assert.Empty(ParticlesAt(animation, animation.Length));
            var decoded = MiiAnimFormat.Read(MiiAnimFormat.Write(animation));
            Assert.Equal(animation.Particles.Count, decoded.Particles.Count);
            for (var i = 0; i < animation.Particles.Count; i++)
            {
                var expected = animation.Particles[i];
                var actual = decoded.Particles[i];
                Assert.Equal(expected.Shape, actual.Shape);
                Assert.Equal(expected.Tint, actual.Tint);
                Assert.Equal(expected.StartFrame, actual.StartFrame);
                Assert.Equal(expected.EndFrame(animation.Fps), actual.EndFrame(decoded.Fps));
            }
        }
    }

    [Fact]
    public void FavoriteStomp_LiftsHoldsAndImpactsOneFoot_WhileTheOtherRemainsPlanted()
    {
        var animation = EditorAnimations.FavoriteRemovedStomp();
        var lift = Assert.Single(animation.Events, e => e.Name == "stomp_lift").Frame;
        var impact = Assert.Single(animation.Events, e => e.Name == "stomp").Frame;
        Assert.InRange(impact - lift, 4, 8);
        foreach (var female in new[] { false, true })
        {
            var rig = new MiiRig(MiiBodyModel.Get(female));
            var foot = (int)MiiBone.FootR;
            var support = (int)MiiBone.FootL;
            var lifted = rig.Evaluate(animation, lift);
            var hit = rig.Evaluate(animation, impact);
            Assert.True(lifted.WorldPosition[foot].Y > rig.RestPose.WorldPosition[foot].Y + 12f, "The raised foot should clearly leave the floor.");
            Assert.True(Vector3.Distance(lifted.WorldPosition[foot], rig.Evaluate(animation, lift - 4).WorldPosition[foot]) < 0.15f, "The lift should briefly hold before the stomp.");
            Assert.True(Vector3.Distance(hit.WorldPosition[foot], rig.RestPose.WorldPosition[foot]) < 0.15f, "The impact should return the foot to the floor.");
            Assert.True(rig.Evaluate(animation, impact - 2).WorldPosition[foot].Y > hit.WorldPosition[foot].Y + 8f,
                "The final two frames should contain a sharp impact rather than a slow foot lowering.");
            for (var frame = 0f; frame <= animation.Length; frame += 0.5f)
                Assert.True(Vector3.Distance(rig.RestPose.WorldPosition[support], rig.Evaluate(animation, frame).WorldPosition[support]) < 0.15f,
                    $"Supporting foot slid at {frame}");
            Assert.Equal(MiiExpression.Frustrated, hit.Expression);
        }
    }

    private sealed class FixedEditorParticleStage : IParticleStage
    {
        public Vector3 AttachPoint(int actor, int frame, MiiBone? bone) => bone switch
        {
            MiiBone.Head => new(0, 108, 0),
            MiiBone.Chest => new(0, 90, 0),
            _ => Vector3.Zero,
        };

        public Vector3 FavoriteColor(int actor) => new(1, 0, 0);
    }

    private static List<Particle> ParticlesAt(MiiAnimation animation, float frame)
    {
        var particles = new List<Particle>();
        ParticleEvaluator.Evaluate(animation, frame, new FixedEditorParticleStage(), particles);
        return particles;
    }

    private static bool IsFinite(Matrix4x4 m) =>
        new[] { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 }
        .All(float.IsFinite);
}

public class EditorAnimationWriter
{
    [Fact]
    public void WriteEditorAnimations()
    {
        foreach (var (path, create) in EditorAnimations.All)
            WheelWizardSampleWriter.WriteRelative(path, create());
    }

    [Fact]
    public void WriteRevisedEditorAnimations()
    {
        foreach (var (path, create) in EditorAnimations.All.Where(entry => EditorAnimations.RevisedPaths.Contains(entry.Path)))
            WheelWizardSampleWriter.WriteRelative(path, create());
    }
}
