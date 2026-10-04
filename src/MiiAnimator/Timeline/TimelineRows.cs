using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;
using MiiAnimator.Editing;

namespace MiiAnimator.Timeline;

public sealed record TimelineRow(string Label, IReadOnlyList<TrackId> Tracks, Selection Select, bool IsGroup = false);

public static class TimelineRows
{
    /// <summary>
    /// Simple mode groups a whole arm/leg (FK bones + IK handle) into one row; advanced mode lists every bone and IK handle.
    /// </summary>
    public static List<TimelineRow> Build(bool advanced)
    {
        var rows = new List<TimelineRow> { new("Face", [TrackId.Expression], Selection.Face) };
        if (!advanced)
        {
            foreach (var bone in new[] { MiiBone.Root, MiiBone.Hip, MiiBone.Chest, MiiBone.Head })
                rows.Add(new TimelineRow(MiiSkeletonInfo.FriendlyName(bone), AnimatorDocument.TracksOf(Selection.OfBone(bone)).ToList(), Selection.OfBone(bone)));

            foreach (var limb in MiiSkeletonInfo.AllLimbs)
            {
                var (u, m, e) = MiiSkeletonInfo.Chain(limb);
                var tracks = new[] { u, m, e }
                    .SelectMany(b => AnimatorDocument.TracksOf(Selection.OfBone(b)))
                    .Concat(AnimatorDocument.TracksOf(Selection.OfLimb(limb)))
                    .ToList();
                rows.Add(new TimelineRow(MiiSkeletonInfo.FriendlyName(limb), tracks, Selection.OfLimb(limb), IsGroup: true));
            }

            return rows;
        }

        foreach (var bone in MiiSkeletonInfo.AllBones)
            rows.Add(new TimelineRow(MiiSkeletonInfo.FriendlyName(bone), AnimatorDocument.TracksOf(Selection.OfBone(bone)).ToList(), Selection.OfBone(bone)));
        foreach (var limb in MiiSkeletonInfo.AllLimbs)
            rows.Add(
                new TimelineRow(
                    MiiSkeletonInfo.FriendlyName(limb) + " IK",
                    AnimatorDocument.TracksOf(Selection.OfLimb(limb)).ToList(),
                    Selection.OfLimb(limb)
                )
            );
        return rows;
    }

    public static bool Matches(TimelineRow row, Selection selection)
    {
        if (row.Select == selection)
            return true;
        if (!row.IsGroup || row.Select.Kind != SelectionKind.Limb)
            return false;
        return selection.Kind == SelectionKind.Bone && MiiSkeletonInfo.LimbOf(selection.Bone) == row.Select.Limb;
    }
}
