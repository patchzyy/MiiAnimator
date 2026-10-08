using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Silk.NET.OpenGL;

namespace MiiAnimator.Viewport;

/// <summary>Avalonia OpenGL surface. The UI thread prepares a <see cref="SceneFrame"/>; this control only draws it.</summary>
public sealed class MiiGlView : OpenGlControlBase
{
    private SceneRenderer? _renderer;
    private volatile RenderRequest? _request;
    private IReadOnlySet<string>? _retainHeads;

    public string? InitError { get; private set; }

    public event Action<string>? RenderError;

    private sealed record RenderRequest(SceneFrame Frame, Matrix4x4 View, Matrix4x4 Projection, Vector4 Clear);

    public void Present(SceneFrame frame, Matrix4x4 view, Matrix4x4 projection, Vector4 clearColor)
    {
        _request = new RenderRequest(frame, view, projection, clearColor);
        RequestNextFrameRendering();
    }

    /// <summary>Frees GPU heads not in <paramref name="keep"/> on the next frame.</summary>
    public void RetainHeads(IReadOnlySet<string> keep)
    {
        _retainHeads = keep;
        RequestNextFrameRendering();
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            var api = GL.GetApi(gl.GetProcAddress);
            _renderer = new SceneRenderer(api, GlVersion.Type == GlProfileType.OpenGLES);
            if (_renderer.ParticleError is { } particleError)
                RenderError?.Invoke($"Particles are disabled: {particleError}");
        }
        catch (Exception exception)
        {
            InitError = exception.Message;
            RenderError?.Invoke(exception.Message);
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _renderer?.Dispose();
        _renderer = null;
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_renderer is null || _request is not { } request)
            return;
        if (_retainHeads is { } keep)
        {
            _retainHeads = null;
            _renderer.RetainHeads(keep);
        }

        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var width = Math.Max(1, (int)(Bounds.Width * scaling));
        var height = Math.Max(1, (int)(Bounds.Height * scaling));
        try
        {
            _renderer.Render(request.Frame, request.View, request.Projection, width, height, request.Clear);
        }
        catch (Exception exception)
        {
            RenderError?.Invoke(exception.Message);
        }
    }
}
