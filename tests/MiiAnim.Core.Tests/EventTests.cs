using MiiAnim.Core.Animation;
using MiiAnim.Core.Format;

namespace MiiAnim.Core.Tests;

public class EventTests
{
    private static MiiAnimation WithEvents(params (int Frame, string Name)[] events)
    {
        var animation = new MiiAnimation { Length = 100 };
        foreach (var (frame, name) in events)
            animation.AddEvent(frame, name);
        return animation;
    }

    [Fact]
    public void Events_RoundTripThroughTheFormat()
    {
        var animation = WithEvents((40, "swap_gender"), (0, "start"), (40, "sound:pop"), (100, "done"));
        var read = MiiAnimFormat.Read(MiiAnimFormat.Write(animation));
        Assert.Equal(animation.Events, read.Events);
        Assert.Equal([0, 40, 40, 100], read.Events.Select(e => e.Frame));
    }

    [Fact]
    public void FilesWithoutEvents_AreUnchanged()
    {
        var animation = WithEvents();
        Assert.Equal(0, MiiAnimFormat.Write(animation)[5] & 2);
        Assert.Empty(MiiAnimFormat.Read(MiiAnimFormat.Write(animation)).Events);
    }

    [Fact]
    public void EventsBetween_FiresEachEventOncePerPass()
    {
        var animation = WithEvents((0, "a"), (50, "b"), (99, "c"));
        Assert.Equal(["a"], animation.EventsBetween(-1, 0).Select(e => e.Name));
        Assert.Equal(["b"], animation.EventsBetween(10, 50).Select(e => e.Name));
        Assert.Empty(animation.EventsBetween(50, 60));
        // Crossing the loop point fires the end of this loop and the start of the next.
        Assert.Equal(["c", "a", "b"], animation.EventsBetween(98, 160).Select(e => e.Name));
        // Played once: nothing after the end.
        Assert.Equal(["a", "b", "c"], animation.EventsBetween(-1, 250, looping: false).Select(e => e.Name));
    }
}
