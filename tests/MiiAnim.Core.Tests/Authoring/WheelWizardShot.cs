using System.Numerics;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Rig;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>
/// WheelWizard's realtime camera (NativeMiiRenderer.GetRealtimeFrameSetup) for one of its Mii spots, so authoring
/// checks can tell where a point of the Mii lands in that spot's pixels. <paramref name="Origin"/> is where the view's
/// top-left corner sits in the spot (e.g. a friends card), <paramref name="Size"/> the view's size in pixels.
/// </summary>
public sealed record WheelWizardShot(bool Face, float Pitch, float Zoom, float VerticalOffset, Vector2 Size, Vector2 Origin)
{
    /// <summary>The 144 px head-and-shoulders view of a friends card, placed like FriendsListItem does it.</summary>
    public static readonly WheelWizardShot FriendsCard = new(true, 12f, 1f, 0f, new(144, 144), new(-33, -20));

    /// <summary>The 80 px sidebar portrait (same framing as the friends card).</summary>
    public static readonly WheelWizardShot Sidebar = new(true, 12f, 1f, 0f, new(80, 80), Vector2.Zero);

    /// <summary>The profile page's license card (the card's bottom edge is the window sill).</summary>
    public static readonly WheelWizardShot ProfileWindow = new(true, 0f, 1f, ProfileWindowOffset, new(420, 206), Vector2.Zero);

    /// <summary>Raises the profile camera so the window sill (y 88) sits on the card's bottom edge.</summary>
    public const float ProfileWindowOffset = 6.5f;

    /// <summary>The leaderboard podium stage.</summary>
    public static readonly WheelWizardShot Podium = new(false, 0f, 1.32f, 10f, new(430, 380), Vector2.Zero);

    /// <summary>Pixel position of a stage point (render units, see <see cref="MiiStage"/>) for a Mii of this size.</summary>
    public Vector2 ToPixels(Vector3 stagePoint, Vector3 bodyScale, bool female = false)
    {
        var (view, projection) = Matrices(bodyScale, female);
        var clip = Vector4.Transform(new Vector4(stagePoint, 1f), view * projection);
        var ndc = new Vector2(clip.X / clip.W, clip.Y / clip.W);
        return Origin + new Vector2((ndc.X * 0.5f + 0.5f) * Size.X, (0.5f - ndc.Y * 0.5f) * Size.Y);
    }

    /// <summary>Pixel position of a canonical point of a posed Mii (e.g. a bone's <see cref="MiiPose.WorldPosition"/>).</summary>
    public Vector2 ToPixels(MiiPose pose, Vector3 canonical, Vector3 bodyScale, bool female = false) =>
        ToPixels(Vector3.Transform(canonical, MiiStage.BodyToStage(pose, bodyScale)), bodyScale, female);

    /// <summary>Pixel position of a point on the FFL head (head render units, e.g. (0, 35, -3.7) is its middle).</summary>
    public Vector2 HeadToPixels(MiiPose pose, Vector3 headPoint, Vector3 bodyScale, bool female = false) =>
        ToPixels(Vector3.Transform(headPoint, MiiStage.HeadToStage(pose, bodyScale)), bodyScale, female);

    /// <summary>Pixels per render unit at the Mii's depth.</summary>
    public float PixelsPerUnit => Size.Y / (2f * OrbitRadius * Zoom * MathF.Tan(7.5f * MathF.PI / 180f));

    private float OrbitRadius => Face ? 57.553f / 0.14f : 760f;

    private (Matrix4x4 View, Matrix4x4 Projection) Matrices(Vector3 bodyScale, bool female)
    {
        var pitch = Pitch * MathF.PI / 180f;
        var radius = OrbitRadius * Zoom;
        var position = new Vector3(0f, radius * MathF.Sin(pitch), radius * MathF.Cos(pitch));
        Vector3 target;
        if (Face)
        {
            var y = 4.805f / 0.14f;
            position.Y += y + VerticalOffset;
            target = new Vector3(0f, y + VerticalOffset, 0f);
            var restHead = new MiiRig(MiiBodyModel.Get(female)).RestPose.HeadMatrixForRender(bodyScale).Translation;
            position += restHead;
            target += restHead;
        }
        else
        {
            position.Y += 90f + VerticalOffset;
            target = new Vector3(0f, 95f + VerticalOffset, 0f);
        }

        var aspect = Size.X / Size.Y;
        var halfFov = 7.5f * MathF.PI / 180f;
        var fovY = aspect >= 1f ? halfFov * 2f : 2f * MathF.Atan(MathF.Tan(halfFov) / aspect);
        return (Matrix4x4.CreateLookAt(position, target, Vector3.UnitY), Matrix4x4.CreatePerspectiveFieldOfView(fovY, aspect, 10f, 1200f));
    }
}
