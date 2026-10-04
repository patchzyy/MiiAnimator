using System.Numerics;

namespace MiiAnimator.Viewport;

/// <summary>Orbit camera in render units (WheelWizard units: Mii is ~110 tall, feet at Y = 0).</summary>
public sealed class OrbitCamera
{
    public float Yaw { get; set; } = 20f;
    public float Pitch { get; set; } = 8f;
    public float Distance { get; set; } = 330f;
    public Vector3 Target { get; set; } = new(0f, 55f, 0f);
    public float FieldOfViewDegrees { get; set; } = 30f;

    public Vector3 Position
    {
        get
        {
            var yaw = Yaw * MathF.PI / 180f;
            var pitch = Pitch * MathF.PI / 180f;
            var offset = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
            return Target + offset * Distance;
        }
    }

    public Matrix4x4 View => Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitY);

    public Matrix4x4 Projection(float aspect) =>
        Matrix4x4.CreatePerspectiveFieldOfView(FieldOfViewDegrees * MathF.PI / 180f, MathF.Max(aspect, 0.01f), 5f, 4000f);

    public void Orbit(float deltaYaw, float deltaPitch)
    {
        Yaw = (Yaw + deltaYaw) % 360f;
        Pitch = Math.Clamp(Pitch + deltaPitch, -85f, 85f);
    }

    public void Zoom(float factor) => Distance = Math.Clamp(Distance * factor, 60f, 2000f);

    public void Pan(float dx, float dy)
    {
        Matrix4x4.Invert(View, out var inverse);
        var right = new Vector3(inverse.M11, inverse.M12, inverse.M13);
        var up = new Vector3(inverse.M21, inverse.M22, inverse.M23);
        var scale = Distance * 0.0015f;
        Target += (-right * dx + up * dy) * scale;
    }

    public void Reset()
    {
        Yaw = 20f;
        Pitch = 8f;
        Distance = 330f;
        Target = new Vector3(0f, 55f, 0f);
    }

    /// <summary>Projects a world point to pixel coordinates (top-left origin). Returns false if behind the camera.</summary>
    public bool Project(Vector3 world, float width, float height, out Vector2 screen, out float depth)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), View * Projection(width / height));
        screen = default;
        depth = clip.W;
        if (clip.W <= 0.01f)
            return false;
        var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
        screen = new Vector2((ndc.X * 0.5f + 0.5f) * width, (1f - (ndc.Y * 0.5f + 0.5f)) * height);
        return true;
    }

    /// <summary>World-space ray through a pixel.</summary>
    public (Vector3 Origin, Vector3 Direction) Ray(Vector2 screen, float width, float height)
    {
        var ndc = new Vector2(screen.X / width * 2f - 1f, 1f - screen.Y / height * 2f);
        Matrix4x4.Invert(View * Projection(width / height), out var inverse);
        var near = Vector4.Transform(new Vector4(ndc, -1f, 1f), inverse);
        var far = Vector4.Transform(new Vector4(ndc, 1f, 1f), inverse);
        var a = new Vector3(near.X, near.Y, near.Z) / near.W;
        var b = new Vector3(far.X, far.Y, far.Z) / far.W;
        return (a, Vector3.Normalize(b - a));
    }

    /// <summary>Moves a world point by a pixel delta, staying at the same depth (view-plane drag).</summary>
    public Vector3 DragInViewPlane(Vector3 world, Vector2 pixelDelta, float width, float height)
    {
        if (!Project(world, width, height, out var screen, out _))
            return world;
        var (origin, direction) = Ray(screen + pixelDelta, width, height);
        var forward = Vector3.Normalize(Target - Position);
        var denominator = Vector3.Dot(direction, forward);
        if (MathF.Abs(denominator) < 1e-5f)
            return world;
        var t = Vector3.Dot(world - origin, forward) / denominator;
        return origin + direction * t;
    }
}
