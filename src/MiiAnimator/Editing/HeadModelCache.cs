using System.Collections.Concurrent;
using Avalonia.Threading;
using MiiAnim.Core.Animation;
using MiiAnimator.MiiData.Rendering;

namespace MiiAnimator.Editing;

/// <summary>
/// Builds FFL head meshes (one per expression) in the background using WheelWizard's managed FFL decoder.
/// </summary>
public sealed class HeadModelCache
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<HeadMeshData>> _ready = new();
    private readonly ConcurrentDictionary<string, byte> _building = new();
    private NativeMiiRenderer? _renderer;

    public string? ResourcePath { get; private set; }
    public string? LastError { get; private set; }

    /// <summary>Raised on the UI thread when a head finished building.</summary>
    public event Action? HeadReady;

    public bool IsAvailable => _renderer is not null;

    public void SetResourcePath(string? path)
    {
        ResourcePath = path;
        _renderer = !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? new NativeMiiRenderer(path) : null;
        _ready.Clear();
        LastError = _renderer is null ? "FFLResHigh.dat not found" : null;
        HeadReady?.Invoke();
    }

    public static string Key(string studioData, MiiExpression expression) => $"{studioData}|{(int)expression}";

    /// <summary>Returns the head if built; otherwise starts building it and returns null.</summary>
    public IReadOnlyList<HeadMeshData>? Get(string studioData, MiiExpression expression)
    {
        if (_renderer is null || string.IsNullOrEmpty(studioData))
            return null;
        var key = Key(studioData, expression);
        if (_ready.TryGetValue(key, out var head))
            return head;
        if (_building.TryAdd(key, 0))
        {
            var renderer = _renderer;
            Task.Run(() =>
            {
                var result = renderer.BuildHeadModel(studioData, (int)expression);
                if (result.IsSuccess)
                    _ready[key] = result.Value;
                else
                    LastError = result.Error!.Message;
                _building.TryRemove(key, out _);
                Dispatcher.UIThread.Post(() => HeadReady?.Invoke());
            });
        }

        return null;
    }

    /// <summary>Drops heads for Miis other than these (the animation's actors).</summary>
    public IReadOnlySet<string> Retain(IEnumerable<string> studioData)
    {
        var prefixes = studioData.Select(s => s + "|").ToList();
        foreach (var key in _ready.Keys.Where(k => !prefixes.Any(p => k.StartsWith(p, StringComparison.Ordinal))).ToList())
            _ready.TryRemove(key, out _);
        return _ready.Keys.ToHashSet();
    }

    /// <summary>Prebuilds every expression so switching expressions while animating never stalls.</summary>
    public void Prewarm(string studioData)
    {
        foreach (var expression in MiiExpressionInfo.All)
            Get(studioData, expression);
    }
}
