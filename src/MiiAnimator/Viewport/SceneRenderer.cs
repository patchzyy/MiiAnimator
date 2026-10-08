using System.Numerics;
using System.Runtime.InteropServices;
using MiiAnim.Core.Evaluation;
using MiiAnim.Core.Rig;
using MiiAnimator.MiiData.Rendering;
using Silk.NET.OpenGL;

namespace MiiAnimator.Viewport;

/// <summary>One Mii to draw: a pose plus how to tint it. Body, scale and colour default to the frame's.</summary>
public sealed record MiiDrawItem(
    MiiPose Pose,
    IReadOnlyList<HeadMeshData>? Head,
    string? HeadKey,
    Vector4 Tint,
    float Alpha,
    int HighlightMask = 0,
    int HoverMask = 0,
    MiiBodyModel? Body = null,
    Vector3? BodyScale = null,
    Vector4? BodyColor = null
);

public sealed class SceneFrame
{
    public required MiiBodyModel Body { get; init; }
    public required Vector3 BodyScale { get; init; }
    public required Vector4 BodyColor { get; init; }
    public required MiiDrawItem Main { get; init; }

    /// <summary>The animation's other Miis (extra actors, or the main Mii while an actor is being edited).</summary>
    public IReadOnlyList<MiiDrawItem> Others { get; init; } = [];

    public IReadOnlyList<Particle> Particles { get; init; } = [];
    public IReadOnlyList<MiiDrawItem> Ghosts { get; init; } = [];
    public bool ShowGrid { get; init; } = true;
    public Editing.SceneProps? Scene { get; init; }
    public Vector3 CameraPosition { get; init; }
}

/// <summary>OpenGL renderer for the animator viewport. All calls must happen on the GL thread.</summary>
internal sealed unsafe class SceneRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly uint _headProgram;
    private readonly uint _bodyProgram;
    private readonly uint _lineProgram;
    private readonly ParticleRenderer? _particles;

    /// <summary>Set when the particle shader couldn't be built; everything else still draws.</summary>
    public string? ParticleError { get; }
    private readonly Dictionary<string, GpuHead> _heads = new();
    private readonly Dictionary<MiiBodyModel, GpuBody> _bodies = new();
    private uint _gridVao;
    private uint _gridVbo;
    private int _gridVertexCount;
    private uint _shadowVao;
    private uint _shadowVbo;
    private int _shadowVertexCount;
    private uint _floorVao;
    private uint _floorVbo;
    private int _floorVertexCount;
    private uint _propVao;
    private uint _propVbo;

    public SceneRenderer(GL gl, bool isEs)
    {
        _gl = gl;
        var header = Shaders.Header(isEs);
        _headProgram = CreateProgram(header + Shaders.HeadVertex, header + Shaders.HeadFragment);
        _bodyProgram = CreateProgram(header + Shaders.BodyVertex, header + Shaders.BodyFragment);
        _lineProgram = CreateProgram(header + Shaders.LineVertex, header + Shaders.LineFragment);
        try
        {
            _particles = new ParticleRenderer(gl, header);
        }
        catch (Exception exception)
        {
            ParticleError = exception.Message;
        }

        CreateGrid();
        CreateShadow();
        CreateFloor();
        (_propVao, _propVbo, _) = UploadColored([]);
    }

    public void Render(SceneFrame frame, Matrix4x4 view, Matrix4x4 projection, int width, int height, Vector4 clearColor)
    {
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        _gl.ClearDepth(1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Lequal);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        _gl.FrontFace(FrontFaceDirection.Ccw);

        if (frame.Scene?.ShowFloor == true)
            DrawFloor(view, projection);
        if (frame.ShowGrid)
            DrawGrid(frame, view, projection);

        DrawMii(frame, frame.Main, view, projection, depthWrite: true);
        foreach (var other in frame.Others)
            DrawMii(frame, other, view, projection, depthWrite: true);
        if (frame.Scene is { ShowWindow: true } scene)
            DrawWindow(frame, scene, view, projection);
        _particles?.Draw(frame.Particles, Matrix4x4.Identity, view, projection);

        // Onion skins: translucent, no depth writes so they never hide the real pose.
        foreach (var ghost in frame.Ghosts)
            DrawMii(frame, ghost, view, projection, depthWrite: false);

        _gl.DepthMask(true);
        _gl.Disable(EnableCap.CullFace);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }

    private void DrawMii(SceneFrame frame, MiiDrawItem item, Matrix4x4 view, Matrix4x4 projection, bool depthWrite)
    {
        if (!item.Pose.Visible)
            return;
        _gl.DepthMask(depthWrite);
        var bodyScale = item.BodyScale ?? frame.BodyScale;
        var renderScale = MiiStage.BodyToStage(item.Pose, bodyScale);

        // Body.
        var body = GetBody(item.Body ?? frame.Body);
        _gl.UseProgram(_bodyProgram);
        SetLighting(_bodyProgram);
        SetMatrix(_bodyProgram, "uView", view);
        SetMatrix(_bodyProgram, "uProj", projection);
        var bones = stackalloc float[16 * 16];
        for (var i = 0; i < MiiSkeletonInfo.BoneCount; i++)
        {
            var m = item.Pose.SkinMatrix[i] * renderScale;
            Unsafe.CopyBlock(bones + i * 16, &m, 64);
        }

        _gl.UniformMatrix4(_gl.GetUniformLocation(_bodyProgram, "uBones"), 16, false, bones);
        _gl.Uniform1(_gl.GetUniformLocation(_bodyProgram, "uHighlightMask"), item.HighlightMask);
        _gl.Uniform1(_gl.GetUniformLocation(_bodyProgram, "uHoverMask"), item.HoverMask);
        SetVec4(_bodyProgram, "uTint", item.Tint);
        _gl.Uniform1(_gl.GetUniformLocation(_bodyProgram, "uAlpha"), item.Alpha);
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        foreach (var mesh in body.Meshes)
        {
            var color = mesh.IsPants ? NativeMiiRenderer.PantsColor : item.BodyColor ?? frame.BodyColor;
            var material = NativeMiiRenderer.BodyMaterial(mesh.IsPants);
            SetMaterial(_bodyProgram, material.Ambient, material.Diffuse, material.Specular, material.Power, 0, material.Rim);
            SetVec4(_bodyProgram, "uColor", color);
            _gl.BindVertexArray(mesh.Vao);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.IndexCount, DrawElementsType.UnsignedInt, null);
        }

        // Head.
        if (item.Head is null || item.HeadKey is null)
            return;
        var head = GetHead(item.HeadKey, item.Head);
        _gl.UseProgram(_headProgram);
        SetLighting(_headProgram);
        SetMatrix(_headProgram, "uView", view);
        SetMatrix(_headProgram, "uProj", projection);
        SetMatrix(_headProgram, "uModel", MiiStage.HeadToStage(item.Pose, bodyScale));
        SetVec4(_headProgram, "uTint", item.Tint);
        _gl.Uniform1(_gl.GetUniformLocation(_headProgram, "uAlpha"), item.Alpha);
        _gl.Uniform1(_gl.GetUniformLocation(_headProgram, "uTex"), 0);
        foreach (var mesh in head.Meshes)
        {
            var data = mesh.Data;
            if (data.CullMode == 1)
            {
                _gl.Enable(EnableCap.CullFace);
                _gl.CullFace(TriangleFace.Back);
            }
            else if (data.CullMode == 2)
            {
                _gl.Enable(EnableCap.CullFace);
                _gl.CullFace(TriangleFace.Front);
            }
            else
            {
                _gl.Disable(EnableCap.CullFace);
            }

            SetMaterial(_headProgram, data.Ambient, data.Diffuse, data.Specular, data.SpecularPower, data.HasTangent ? data.SpecularMode : 0, data.RimColor);
            _gl.Uniform1(_gl.GetUniformLocation(_headProgram, "uMode"), data.ModulateMode);
            SetVec4(_headProgram, "uColR", data.ColorR);
            SetVec4(_headProgram, "uColG", data.ColorG);
            SetVec4(_headProgram, "uColB", data.ColorB);
            _gl.Uniform1(_gl.GetUniformLocation(_headProgram, "uHasTangent"), data.HasTangent ? 1 : 0);
            _gl.Uniform1(_gl.GetUniformLocation(_headProgram, "uHasTex"), mesh.Texture != 0 ? 1 : 0);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, mesh.Texture);
            _gl.BindVertexArray(mesh.Vao);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.IndexCount, DrawElementsType.UnsignedInt, null);
        }

        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private void DrawGrid(SceneFrame frame, Matrix4x4 view, Matrix4x4 projection)
    {
        _gl.UseProgram(_lineProgram);
        SetMatrix(_lineProgram, "uView", view);
        SetMatrix(_lineProgram, "uProj", projection);
        _gl.DepthMask(false);
        _gl.Disable(EnableCap.CullFace);
        _gl.BindVertexArray(_gridVao);
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_gridVertexCount);

        // Soft contact shadow under each body, following its ground position.
        _gl.BindVertexArray(_shadowVao);
        foreach (var item in frame.Others.Prepend(frame.Main).Where(i => i.Pose.Visible))
        {
            var bodyScale = item.BodyScale ?? frame.BodyScale;
            var root = Vector3.Transform(item.Pose.WorldPositionOf(MiiBone.Root), MiiStage.BodyToStage(item.Pose, bodyScale));
            var shadowScale = 1f / (1f + MathF.Max(0f, root.Y - 47f * bodyScale.Y) * 0.02f);
            var shadowView = Matrix4x4.CreateScale(shadowScale * bodyScale.X, 1f, shadowScale * bodyScale.Z)
                * Matrix4x4.CreateTranslation(root.X, 0.05f, root.Z)
                * view;
            SetMatrix(_lineProgram, "uView", shadowView);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_shadowVertexCount);
        }

        _gl.DepthMask(true);
    }

    private void CreateGrid()
    {
        var vertices = new List<float>();
        const int half = 300;
        const int step = 20;
        for (var x = -half; x <= half; x += step)
        {
            var major = x == 0;
            var a = major ? 0.45f : 0.16f;
            AddLine(vertices, new Vector3(x, 0, -half), new Vector3(x, 0, half), major ? new Vector4(0.35f, 0.55f, 0.95f, a) : new Vector4(1, 1, 1, a));
            AddLine(vertices, new Vector3(-half, 0, x), new Vector3(half, 0, x), major ? new Vector4(0.95f, 0.4f, 0.4f, a) : new Vector4(1, 1, 1, a));
        }

        (_gridVao, _gridVbo, _gridVertexCount) = UploadColored(vertices);
    }

    // ---------- Props (preview only) ----------

    private void DrawFloor(Matrix4x4 view, Matrix4x4 projection)
    {
        // Opaque, so anything below the ground (e.g. the start of an "appear from a hole") is hidden like in the app.
        _gl.UseProgram(_lineProgram);
        SetMatrix(_lineProgram, "uView", view);
        SetMatrix(_lineProgram, "uProj", projection);
        _gl.DepthMask(true);
        _gl.Disable(EnableCap.CullFace);
        _gl.BindVertexArray(_floorVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_floorVertexCount);
    }

    private void CreateFloor()
    {
        var vertices = new List<float>();
        const int segments = 64;
        const float radius = 330f;
        var center = new Vector4(0.27f, 0.29f, 0.33f, 1f);
        var edge = new Vector4(0.2f, 0.215f, 0.25f, 1f);
        for (var i = 0; i < segments; i++)
        {
            var a0 = i / (float)segments * MathF.Tau;
            var a1 = (i + 1) / (float)segments * MathF.Tau;
            AddVertex(vertices, new Vector3(0, -0.15f, 0), center);
            AddVertex(vertices, new Vector3(MathF.Cos(a1) * radius, -0.15f, MathF.Sin(a1) * radius), edge);
            AddVertex(vertices, new Vector3(MathF.Cos(a0) * radius, -0.15f, MathF.Sin(a0) * radius), edge);
        }

        (_floorVao, _floorVbo, _floorVertexCount) = UploadColored(vertices);
    }

    /// <summary>A wall with a window opening and a sill ledge, in front of the Mii (see <see cref="Editing.SceneProps"/>).</summary>
    private void DrawWindow(SceneFrame frame, Editing.SceneProps scene, Matrix4x4 view, Matrix4x4 projection)
    {
        var s = frame.BodyScale * MiiBodyModel.CanonicalToRenderUnits;
        // From behind the wall it would hide everything; only draw it from the front.
        if (frame.CameraPosition.Z < scene.WindowDistance * s.Z)
            return;

        var v = new List<float>();
        float d = scene.WindowDistance,
            ow = scene.WindowHalfWidth,
            sill = scene.WindowSill,
            top = sill + scene.WindowHeight,
            wallHalf = ow + 160f,
            wallTop = top + 80f,
            frameWidth = 8f;
        var wall = new Vector4(0.78f, 0.72f, 0.63f, 1f);
        var wood = new Vector4(0.5f, 0.33f, 0.2f, 1f);
        var ledge = new Vector4(0.63f, 0.43f, 0.27f, 1f);

        void Box(Vector3 min, Vector3 max, Vector4 color) => AddBox(v, min * s, max * s, color);

        // Wall around the opening (4 pieces), a little thick so the opening has depth.
        // The wall goes well below the floor so a Mii hiding below the sill stays hidden.
        Box(new(-wallHalf, -420, d - 4), new(-ow, wallTop, d), wall);
        Box(new(ow, -420, d - 4), new(wallHalf, wallTop, d), wall);
        Box(new(-ow, -420, d - 4), new(ow, sill, d), wall);
        Box(new(-ow, top, d - 4), new(ow, wallTop, d), wall);
        // Frame on the camera side and the sill ledge sticking out on the Mii's side.
        Box(new(-ow - frameWidth, sill, d), new(-ow, top, d + 3), wood);
        Box(new(ow, sill, d), new(ow + frameWidth, top, d + 3), wood);
        Box(new(-ow - frameWidth, top, d), new(ow + frameWidth, top + frameWidth, d + 3), wood);
        Box(new(-ow - frameWidth - 4, sill - 5, d - Editing.SceneProps.SillDepth), new(ow + frameWidth + 4, sill, d + 6), ledge);

        _gl.UseProgram(_lineProgram);
        SetMatrix(_lineProgram, "uView", view);
        SetMatrix(_lineProgram, "uProj", projection);
        _gl.DepthMask(true);
        _gl.Disable(EnableCap.CullFace);
        _gl.BindVertexArray(_propVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _propVbo);
        var data = v.ToArray();
        fixed (float* p = data)
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * 4), p, BufferUsageARB.DynamicDraw);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(data.Length / 7));
    }

    /// <summary>An axis-aligned box with simple fixed shading per face (top brightest).</summary>
    private static void AddBox(List<float> v, Vector3 min, Vector3 max, Vector4 color)
    {
        Vector4 Shade(float f) => new(color.X * f, color.Y * f, color.Z * f, color.W);

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4 col)
        {
            AddVertex(v, a, col);
            AddVertex(v, b, col);
            AddVertex(v, c, col);
            AddVertex(v, a, col);
            AddVertex(v, c, col);
            AddVertex(v, d, col);
        }

        Vector3 P(float x, float y, float z) => new(x, y, z);
        Quad(P(min.X, max.Y, min.Z), P(max.X, max.Y, min.Z), P(max.X, max.Y, max.Z), P(min.X, max.Y, max.Z), Shade(1f)); // top
        Quad(P(min.X, min.Y, min.Z), P(max.X, min.Y, min.Z), P(max.X, min.Y, max.Z), P(min.X, min.Y, max.Z), Shade(0.55f)); // bottom
        Quad(P(min.X, min.Y, max.Z), P(max.X, min.Y, max.Z), P(max.X, max.Y, max.Z), P(min.X, max.Y, max.Z), Shade(0.9f)); // front (+Z)
        Quad(P(min.X, min.Y, min.Z), P(max.X, min.Y, min.Z), P(max.X, max.Y, min.Z), P(min.X, max.Y, min.Z), Shade(0.7f)); // back
        Quad(P(min.X, min.Y, min.Z), P(min.X, min.Y, max.Z), P(min.X, max.Y, max.Z), P(min.X, max.Y, min.Z), Shade(0.75f)); // left
        Quad(P(max.X, min.Y, min.Z), P(max.X, min.Y, max.Z), P(max.X, max.Y, max.Z), P(max.X, max.Y, min.Z), Shade(0.75f)); // right
    }

    private void CreateShadow()
    {
        var vertices = new List<float>();
        const int segments = 32;
        for (var i = 0; i < segments; i++)
        {
            var a0 = i / (float)segments * MathF.Tau;
            var a1 = (i + 1) / (float)segments * MathF.Tau;
            AddVertex(vertices, Vector3.Zero, new Vector4(0, 0, 0, 0.32f));
            AddVertex(vertices, new Vector3(MathF.Cos(a0) * 26f, 0, MathF.Sin(a0) * 20f), new Vector4(0, 0, 0, 0f));
            AddVertex(vertices, new Vector3(MathF.Cos(a1) * 26f, 0, MathF.Sin(a1) * 20f), new Vector4(0, 0, 0, 0f));
        }

        (_shadowVao, _shadowVbo, _shadowVertexCount) = UploadColored(vertices);
    }

    private static void AddLine(List<float> v, Vector3 a, Vector3 b, Vector4 color)
    {
        AddVertex(v, a, color);
        AddVertex(v, b, color);
    }

    private static void AddVertex(List<float> v, Vector3 p, Vector4 c)
    {
        v.Add(p.X);
        v.Add(p.Y);
        v.Add(p.Z);
        v.Add(c.X);
        v.Add(c.Y);
        v.Add(c.Z);
        v.Add(c.W);
    }

    private (uint Vao, uint Vbo, int Count) UploadColored(List<float> vertices)
    {
        var vao = _gl.GenVertexArray();
        var vbo = _gl.GenBuffer();
        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        var data = vertices.ToArray();
        fixed (float* p = data)
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * 4), p, BufferUsageARB.StaticDraw);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 28, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 28, (void*)12);
        _gl.BindVertexArray(0);
        return (vao, vbo, data.Length / 7);
    }

    private GpuBody GetBody(MiiBodyModel model)
    {
        if (_bodies.TryGetValue(model, out var existing))
            return existing;

        var meshes = new List<GpuMesh>();
        foreach (var mesh in model.Meshes)
        {
            var interleaved = new float[mesh.Positions.Length * 14];
            for (var v = 0; v < mesh.Positions.Length; v++)
            {
                var o = v * 14;
                interleaved[o] = mesh.Positions[v].X;
                interleaved[o + 1] = mesh.Positions[v].Y;
                interleaved[o + 2] = mesh.Positions[v].Z;
                interleaved[o + 3] = mesh.Normals[v].X;
                interleaved[o + 4] = mesh.Normals[v].Y;
                interleaved[o + 5] = mesh.Normals[v].Z;
                // Put the dominant joint first so highlighting works per vertex.
                var order = Enumerable.Range(0, 4).OrderByDescending(k => mesh.Weights[v * 4 + k]).ToArray();
                for (var k = 0; k < 4; k++)
                {
                    interleaved[o + 6 + k] = mesh.Joints[v * 4 + order[k]];
                    interleaved[o + 10 + k] = mesh.Weights[v * 4 + order[k]];
                }
            }

            var vao = _gl.GenVertexArray();
            _gl.BindVertexArray(vao);
            var vbo = UploadArray(interleaved);
            var ebo = UploadIndices(mesh.Indices);
            const uint stride = 14 * 4;
            Attribute(0, 3, stride, 0);
            Attribute(1, 3, stride, 12);
            Attribute(2, 4, stride, 24);
            Attribute(3, 4, stride, 40);
            _gl.BindVertexArray(0);
            meshes.Add(new GpuMesh(vao, vbo, ebo, mesh.Indices.Length, 0, null, mesh.IsPants));
        }

        var body = new GpuBody(meshes);
        _bodies[model] = body;
        return body;
    }

    private GpuHead GetHead(string key, IReadOnlyList<HeadMeshData> data)
    {
        if (_heads.TryGetValue(key, out var existing))
            return existing;

        var meshes = new List<GpuMesh>();
        foreach (var mesh in data)
        {
            var count = mesh.Positions.Length;
            var interleaved = new float[count * 16];
            for (var v = 0; v < count; v++)
            {
                var o = v * 16;
                interleaved[o] = mesh.Positions[v].X;
                interleaved[o + 1] = mesh.Positions[v].Y;
                interleaved[o + 2] = mesh.Positions[v].Z;
                interleaved[o + 3] = mesh.Normals[v].X;
                interleaved[o + 4] = mesh.Normals[v].Y;
                interleaved[o + 5] = mesh.Normals[v].Z;
                interleaved[o + 6] = mesh.Tangents[v].X;
                interleaved[o + 7] = mesh.Tangents[v].Y;
                interleaved[o + 8] = mesh.Tangents[v].Z;
                interleaved[o + 9] = mesh.Texcoords[v].X;
                interleaved[o + 10] = mesh.Texcoords[v].Y;
                interleaved[o + 11] = mesh.Parameters[v].X;
                interleaved[o + 12] = mesh.Parameters[v].Y;
                interleaved[o + 13] = mesh.Parameters[v].Z;
                interleaved[o + 14] = mesh.Parameters[v].W;
            }

            var vao = _gl.GenVertexArray();
            _gl.BindVertexArray(vao);
            var vbo = UploadArray(interleaved);
            var ebo = UploadIndices(mesh.Indices);
            const uint stride = 16 * 4;
            Attribute(0, 3, stride, 0);
            Attribute(1, 3, stride, 12);
            Attribute(2, 3, stride, 24);
            Attribute(3, 2, stride, 36);
            Attribute(4, 4, stride, 44);
            _gl.BindVertexArray(0);

            uint texture = 0;
            if (mesh.TexturePixels is { } pixels && mesh.TextureWidth > 0 && mesh.TextureHeight > 0)
            {
                texture = _gl.GenTexture();
                _gl.BindTexture(TextureTarget.Texture2D, texture);
                fixed (byte* p = pixels)
                    _gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        InternalFormat.Rgba8,
                        (uint)mesh.TextureWidth,
                        (uint)mesh.TextureHeight,
                        0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        p
                    );
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
                _gl.BindTexture(TextureTarget.Texture2D, 0);
            }

            meshes.Add(new GpuMesh(vao, vbo, ebo, mesh.Indices.Length, texture, mesh, false));
        }

        var head = new GpuHead(meshes);
        _heads[key] = head;
        return head;
    }

    /// <summary>Frees uploaded heads that are no longer needed (e.g. after the Mii changed).</summary>
    public void RetainHeads(IReadOnlySet<string> keep)
    {
        foreach (var key in _heads.Keys.Where(k => !keep.Contains(k)).ToList())
        {
            foreach (var mesh in _heads[key].Meshes)
                DeleteMesh(mesh);
            _heads.Remove(key);
        }
    }

    private uint UploadArray(float[] data)
    {
        var vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (float* p = data)
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * 4), p, BufferUsageARB.StaticDraw);
        return vbo;
    }

    private uint UploadIndices(int[] indices)
    {
        var ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (int* p = indices)
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * 4), p, BufferUsageARB.StaticDraw);
        return ebo;
    }

    private void Attribute(uint index, int size, uint stride, int offset)
    {
        _gl.EnableVertexAttribArray(index);
        _gl.VertexAttribPointer(index, size, VertexAttribPointerType.Float, false, stride, (void*)offset);
    }

    private void SetLighting(uint program)
    {
        var (ambient, diffuse, specular, direction) = NativeMiiRenderer.LightConstants;
        SetVec3(program, "uLightAmb", ambient);
        SetVec3(program, "uLightDiff", diffuse);
        SetVec3(program, "uLightSpec", specular);
        SetVec3(program, "uLightDir", direction);
        var p = MiiLightingProfiles.Default;
        var profile = stackalloc float[]
        {
            p.AmbientScale,
            p.DirectionalLightInfluence,
            p.DiffuseScale,
            p.DiffuseFloor,
            p.SpecularScale,
            p.RimScale,
            p.RimPower,
        };
        _gl.Uniform1(_gl.GetUniformLocation(program, "uProfile"), 7, profile);
    }

    private void SetMaterial(uint program, Vector3 ambient, Vector3 diffuse, Vector3 specular, float power, int specularMode, Vector3 rim)
    {
        SetVec3(program, "uMatAmb", ambient);
        SetVec3(program, "uMatDiff", diffuse);
        SetVec3(program, "uMatSpec", specular);
        SetVec3(program, "uMatRim", rim);
        _gl.Uniform1(_gl.GetUniformLocation(program, "uSpecPow"), power);
        _gl.Uniform1(_gl.GetUniformLocation(program, "uSpecMode"), specularMode);
    }

    private void SetMatrix(uint program, string name, Matrix4x4 matrix) =>
        _gl.UniformMatrix4(_gl.GetUniformLocation(program, name), 1, false, (float*)&matrix);

    private void SetVec3(uint program, string name, Vector3 v) => _gl.Uniform3(_gl.GetUniformLocation(program, name), v.X, v.Y, v.Z);

    private void SetVec4(uint program, string name, Vector4 v) => _gl.Uniform4(_gl.GetUniformLocation(program, name), v.X, v.Y, v.Z, v.W);

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        var vs = Compile(ShaderType.VertexShader, vertexSource);
        var fs = Compile(ShaderType.FragmentShader, fragmentSource);
        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vs);
        _gl.AttachShader(program, fs);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status == 0)
            throw new InvalidOperationException("Shader link failed: " + _gl.GetProgramInfoLog(program));
        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);
        return program;
    }

    private uint Compile(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
        if (status == 0)
            throw new InvalidOperationException($"{type} compile failed: {_gl.GetShaderInfoLog(shader)}");
        return shader;
    }

    private void DeleteMesh(GpuMesh mesh)
    {
        _gl.DeleteVertexArray(mesh.Vao);
        _gl.DeleteBuffer(mesh.Vbo);
        _gl.DeleteBuffer(mesh.Ebo);
        if (mesh.Texture != 0)
            _gl.DeleteTexture(mesh.Texture);
    }

    public void Dispose()
    {
        foreach (var head in _heads.Values)
        foreach (var mesh in head.Meshes)
            DeleteMesh(mesh);
        foreach (var body in _bodies.Values)
        foreach (var mesh in body.Meshes)
            DeleteMesh(mesh);
        _heads.Clear();
        _bodies.Clear();
        _gl.DeleteVertexArray(_gridVao);
        _gl.DeleteBuffer(_gridVbo);
        _gl.DeleteVertexArray(_shadowVao);
        _gl.DeleteBuffer(_shadowVbo);
        _gl.DeleteVertexArray(_floorVao);
        _gl.DeleteBuffer(_floorVbo);
        _gl.DeleteVertexArray(_propVao);
        _gl.DeleteBuffer(_propVbo);
        _gl.DeleteProgram(_headProgram);
        _gl.DeleteProgram(_bodyProgram);
        _gl.DeleteProgram(_lineProgram);
        _particles?.Dispose();
    }

    private sealed record GpuMesh(uint Vao, uint Vbo, uint Ebo, int IndexCount, uint Texture, HeadMeshData? Data, bool IsPants)
    {
        public HeadMeshData Data { get; } = Data!;
    }

    private sealed record GpuBody(List<GpuMesh> Meshes);

    private sealed record GpuHead(List<GpuMesh> Meshes);
}

file static class Unsafe
{
    public static unsafe void CopyBlock(void* destination, void* source, uint byteCount) =>
        System.Buffer.MemoryCopy(source, destination, byteCount, byteCount);
}
