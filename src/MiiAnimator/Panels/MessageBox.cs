using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MiiAnimator.Panels;

public static class MessageBox
{
    public static Task Show(TopLevel owner, string title, string message) => Ask(owner, title, message, "OK");

    /// <summary>Shows a dialog with the given buttons and returns the index of the clicked one (-1 if closed).</summary>
    public static async Task<int> Ask(TopLevel owner, string title, string message, params string[] buttons)
    {
        var result = -1;
        var window = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false,
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            var button = new Button { Content = buttons[i], MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            if (i == 0)
                button.Classes.Add("accent");
            button.Click += (_, _) =>
            {
                result = index;
                window.Close();
            };
            row.Children.Add(button);
        }

        window.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                row,
            },
        };

        if (owner is Window ownerWindow)
            await window.ShowDialog(ownerWindow);
        else
            window.Show();
        return result;
    }
}
