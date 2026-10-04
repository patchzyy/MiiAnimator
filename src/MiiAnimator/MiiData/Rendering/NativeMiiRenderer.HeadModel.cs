using System.Numerics;

namespace MiiAnimator.MiiData.Rendering;

/// <summary>A GPU-ready copy of one FFL head draw call (same data WheelWizard's CPU rasterizer consumes).</summary>
public sealed class HeadMeshData
{
    public required Vector3[] Positions { get; init; }
    public required Vector3[] Normals { get; init; }
    public required Vector3[] Tangents { get; init; }
    public required Vector2[] Texcoords { get; init; }
    public required Vector4[] Parameters { get; init; }

    /// <summary>Triangle list (strips are converted).</summary>
    public required int[] Indices { get; init; }

    public required int CullMode { get; init; }
    public required int ModulateMode { get; init; }
    public required Vector4 ColorR { get; init; }
    public required Vector4 ColorG { get; init; }
    public required Vector4 ColorB { get; init; }

    /// <summary>RGBA8 pixels, or null when the draw call has no texture.</summary>
    public byte[]? TexturePixels { get; init; }

    public int TextureWidth { get; init; }
    public int TextureHeight { get; init; }
    public required bool HasTangent { get; init; }
    public required Vector3 Ambient { get; init; }
    public required Vector3 Diffuse { get; init; }
    public required Vector3 Specular { get; init; }
    public required float SpecularPower { get; init; }
    public required int SpecularMode { get; init; }
    public required Vector3 RimColor { get; init; }
}

public sealed partial class NativeMiiRenderer
{
    public static readonly Vector4 PantsColor = new(0.2509804f, 0.2745099f, 0.30588239f, 1.0f);

    public static Vector4 FavoriteColor(int index) => GetFavoriteColor(index);

    /// <summary>Lighting constants shared by the GPU shader so it matches the CPU renderer.</summary>
    public static (Vector3 Ambient, Vector3 Diffuse, Vector3 Specular, Vector3 Direction) LightConstants =>
        (LightAmbient, LightDiffuse, LightSpecular, LightDirection);

    /// <summary>Body/pants material (modulate types 9/10) for the GPU body shader.</summary>
    public static (Vector3 Ambient, Vector3 Diffuse, Vector3 Specular, float Power, Vector3 Rim) BodyMaterial(bool pants)
    {
        var m = ResolveMaterial(pants ? FflNativeInterop.ModulateTypeCustomPants : FflNativeInterop.ModulateTypeCustomBody);
        return (m.Ambient, m.Diffuse, m.Specular, m.SpecularPower, m.RimColor);
    }

    /// <summary>Builds the head meshes for a Mii (studio data hex) and FFL expression id.</summary>
    public OperationResult<List<HeadMeshData>> BuildHeadModel(string studioData, int expressionId)
    {
        var archiveResult = GetManagedArchive(resourcePath);
        if (archiveResult.IsFailure)
            return archiveResult.Error!;

        var charInfoResult = GetOrCreateCharInfo(studioData);
        if (charInfoResult.IsFailure)
            return charInfoResult.Error!;

        var specifications = new MiiImageSpecifications
        {
            Size = MiiImageSpecifications.ImageSize.medium,
            Type = MiiImageSpecifications.BodyType.all_body,
        };
        var request = BuildRequest(studioData, specifications);
        var cachedResult = GetOrCreateCachedHeadDrawParams(
            archiveResult.Value,
            charInfoResult.Value,
            request,
            expressionId,
            studioData,
            resourcePath
        );
        if (cachedResult.IsFailure)
            return cachedResult.Error!;

        var meshes = new List<HeadMeshData>();
        foreach (var mesh in cachedResult.Value.DrawMeshes)
        {
            var modulate = BuildModulateContext(mesh.DrawParam.modulateParam);
            byte[]? pixels = null;
            if (modulate.Texture is { } texture)
                pixels = ToRgba(texture);

            meshes.Add(
                new HeadMeshData
                {
                    Positions = mesh.Positions.ToArray(),
                    Normals = mesh.Normals.ToArray(),
                    Tangents = mesh.Tangents.ToArray(),
                    Texcoords = mesh.Texcoords.ToArray(),
                    Parameters = mesh.VertexParameters.Values.ToArray(),
                    Indices = ToTriangleList(mesh.Indices, mesh.DrawParam.primitiveParam.primitiveType),
                    CullMode = mesh.DrawParam.cullMode,
                    ModulateMode = modulate.Mode,
                    ColorR = modulate.ColorR,
                    ColorG = modulate.ColorG,
                    ColorB = modulate.ColorB,
                    TexturePixels = pixels,
                    TextureWidth = modulate.Texture?.Width ?? 0,
                    TextureHeight = modulate.Texture?.Height ?? 0,
                    HasTangent = mesh.HasTangent,
                    Ambient = mesh.Material.Ambient,
                    Diffuse = mesh.Material.Diffuse,
                    Specular = mesh.Material.Specular,
                    SpecularPower = mesh.Material.SpecularPower,
                    SpecularMode = mesh.Material.SpecularMode,
                    RimColor = mesh.Material.RimColor,
                }
            );
        }

        return meshes;
    }

    private static int[] ToTriangleList(int[] indices, uint primitiveType)
    {
        if (primitiveType != FflNativeInterop.PrimitiveTriangleStrip)
            return indices.ToArray();

        var list = new List<int>(Math.Max(0, (indices.Length - 2) * 3));
        for (var i = 0; i + 2 < indices.Length; i++)
        {
            int a = indices[i],
                b = indices[i + 1],
                c = indices[i + 2];
            if ((i & 1) == 1)
                (b, c) = (c, b);
            list.Add(a);
            list.Add(b);
            list.Add(c);
        }

        return list.ToArray();
    }

    private static byte[] ToRgba(TextureData texture)
    {
        var rgba = new byte[texture.Width * texture.Height * 4];
        for (var y = 0; y < texture.Height; y++)
        for (var x = 0; x < texture.Width; x++)
        {
            var c = ReadTextureTexel(texture, x, y);
            var o = (y * texture.Width + x) * 4;
            rgba[o] = (byte)Math.Clamp((int)MathF.Round(c.X * 255f), 0, 255);
            rgba[o + 1] = (byte)Math.Clamp((int)MathF.Round(c.Y * 255f), 0, 255);
            rgba[o + 2] = (byte)Math.Clamp((int)MathF.Round(c.Z * 255f), 0, 255);
            rgba[o + 3] = (byte)Math.Clamp((int)MathF.Round(c.W * 255f), 0, 255);
        }

        return rgba;
    }
}

public readonly record struct NativeMiiPixelBuffer(int Width, int Height, byte[] BgraPixels);
