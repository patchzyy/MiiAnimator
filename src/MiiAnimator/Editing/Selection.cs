using MiiAnim.Core.Animation;
using MiiAnim.Core.Rig;

namespace MiiAnimator.Editing;

public enum SelectionKind
{
    None,
    Bone,
    Limb,
    Face,
}

/// <summary>What the user is posing: a bone (FK), a limb's IK handle, or the face (expression).</summary>
public readonly record struct Selection(SelectionKind Kind, MiiBone Bone = MiiBone.Root, MiiLimb Limb = MiiLimb.ArmL)
{
    public static readonly Selection None = new(SelectionKind.None);
    public static readonly Selection Face = new(SelectionKind.Face);

    public static Selection OfBone(MiiBone bone) => new(SelectionKind.Bone, Bone: bone);

    public static Selection OfLimb(MiiLimb limb) => new(SelectionKind.Limb, Limb: limb);

    public TrackTarget? Target =>
        Kind switch
        {
            SelectionKind.Bone => TrackTarget.Bone(Bone),
            SelectionKind.Limb => TrackTarget.Limb(Limb),
            SelectionKind.Face => TrackTarget.Global,
            _ => null,
        };

    public string DisplayName =>
        Kind switch
        {
            SelectionKind.Bone => MiiSkeletonInfo.FriendlyName(Bone),
            SelectionKind.Limb => MiiSkeletonInfo.FriendlyName(Limb) + (MiiSkeletonInfo.IsLeg(Limb) ? " (foot handle)" : " (hand handle)"),
            SelectionKind.Face => "Face",
            _ => "Nothing selected",
        };

    /// <summary>Bit mask of bones to highlight in the viewport.</summary>
    public int HighlightMask
    {
        get
        {
            switch (Kind)
            {
                case SelectionKind.Bone:
                    return 1 << (int)Bone;
                case SelectionKind.Limb:
                {
                    var (u, m, e) = MiiSkeletonInfo.Chain(Limb);
                    return (1 << (int)u) | (1 << (int)m) | (1 << (int)e);
                }
                default:
                    return 0;
            }
        }
    }
}
