using System.Text.Json;

namespace MiiAnimator;

public sealed class AppSettings
{
    public string? FflResourcePath { get; set; }
    public bool AdvancedMode { get; set; }
    public bool ShowCurveEditor { get; set; }
    public Editing.SceneProps Scene { get; set; } = new();

    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiiAnimator", "settings.json");

    /// <summary>WheelWizard downloads FFLResHigh.dat here during its first-run setup.</summary>
    public static string WheelWizardFflPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CT-MKWII", "MiiRendering", "FFLResHigh.dat");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            // Corrupt settings: start fresh.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Settings are a convenience only.
        }
    }

    public string? ResolveFflPath()
    {
        if (!string.IsNullOrWhiteSpace(FflResourcePath) && File.Exists(FflResourcePath))
            return FflResourcePath;
        return File.Exists(WheelWizardFflPath) ? WheelWizardFflPath : null;
    }
}
