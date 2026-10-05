namespace MiiAnimator.Editing;

/// <summary>
/// Preview-only helpers drawn around the Mii (not saved in .miianim files): a floor and a window frame
/// to animate peeking/leaning against. Window sizes are canonical rig units, like the animation channels.
/// </summary>
public sealed class SceneProps
{
    public bool ShowFloor { get; set; }

    /// <summary>Keeps IK feet from being dragged below the floor and snaps them onto it when close.</summary>
    public bool SnapFeetToFloor { get; set; } = true;

    public bool ShowWindow { get; set; }

    /// <summary>Height of the window sill (rest shoulders are at ~103, the hips at ~68).</summary>
    public float WindowSill { get; set; } = 88f;

    /// <summary>How far in front of the Mii the wall is (the face is ~41 in front of the body).</summary>
    public float WindowDistance { get; set; } = 58f;

    /// <summary>Half the width of the window opening.</summary>
    public float WindowHalfWidth { get; set; } = 80f;

    /// <summary>Height of the opening above the sill.</summary>
    public float WindowHeight { get; set; } = 190f;

    /// <summary>Depth of the sill ledge that sticks out on the Mii's side (for resting hands on).</summary>
    public const float SillDepth = 10f;

    /// <summary>Snap distance for <see cref="SnapFeetToFloor"/>.</summary>
    public const float FloorSnapDistance = 2.5f;
}
