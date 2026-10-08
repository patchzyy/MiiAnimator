using MiiAnim.Core.Animation;
using MiiAnim.Core.Format;

namespace MiiAnim.Core.Tests.Authoring;

/// <summary>Routes generated WheelWizard clips into their screen/action folders.</summary>
public static class WheelWizardSampleWriter
{
    public static void Write(string name, MiiAnimation animation)
    {
        var folder = name switch
        {
            "Appear" => Path.Combine("shared", "appear"),
            "Idle_breathing" or "Idle_weight_shift" => Path.Combine("shared", "idle"),
            "Become_boy" or "Become_girl" => Path.Combine("editor", "gender"),
            "Shirt_color_surprise" => Path.Combine("editor", "favoritecolor"),
            "Window_peek" => Path.Combine("profile", "window", "funny"),
            "Change_mii" => Path.Combine("profile", "change_mii"),
            "Window_hello_pop" or "Window_hello_slide" => Path.Combine("profile", "window", "hello"),
            "Window_idle_lean" or "Window_idle_sway" => Path.Combine("profile", "window", "idle"),
            _ => throw new ArgumentException($"Add an action folder for the new WheelWizard clip '{name}'.", nameof(name)),
        };
        WriteRelative(Path.Combine(folder, name + MiiAnimFormat.FileExtension), animation);
    }

    /// <summary>Writes a clip to a path relative to samples/wheelwizard, including the .miianim extension.</summary>
    public static void WriteRelative(string relativePath, MiiAnimation animation)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "wheelwizard"));
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        if (Path.IsPathRooted(relativePath)
            || !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(path), MiiAnimFormat.FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Use a .miianim path inside samples/wheelwizard.", nameof(relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, MiiAnimFormat.Write(animation));
    }
}
