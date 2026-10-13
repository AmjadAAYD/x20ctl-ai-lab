using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace X20Ctl.Product.Agent;

/// <summary>A speech bubble inviting the owner to talk to the agent (owner request, 14 Oct 2026). Clicking it opens the agent.</summary>
public static class AgentInvite
{
    public const string Text = "Need help ? I will assist you !";

    /// <summary>The bubble, with its tail on the right (pointing at the title-bar pill) or at the bottom left.</summary>
    public static Button Make(Action open, bool tailRight, double size = 14)
    {
        var fill = new LinearGradientBrush(Color.FromArgb(235, 30, 52, 104), Color.FromArgb(235, 18, 30, 66), 90); fill.Freeze();
        var edge = new SolidColorBrush(Color.FromArgb(170, 125, 182, 255)); edge.Freeze();
        var face = new Border
        {
            CornerRadius = new(16), Padding = new(14, 7, 16, 8), Background = fill, BorderBrush = edge, BorderThickness = new(1),
            Effect = new DropShadowEffect { Color = Color.FromRgb(58, 110, 220), BlurRadius = 16, ShadowDepth = 0, Opacity = .45 },
            Child = new StackPanel { Orientation = Orientation.Horizontal, Children = {
                new TextBlock { Text = "", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = size, Foreground = DS.Brush("DS.AccentHi"), VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 1, 9, 0) },
                new TextBlock { Text = Text, FontFamily = DS.Display, FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center } } },
        };
        // the tail: a small triangle in the bubble's colours, pointing at what it speaks for
        var tail = new Path
        {
            Data = Geometry.Parse(tailRight ? "M 0,0 L 10,6 L 0,12 Z" : "M 0,0 L 14,0 L 2,11 Z"), Fill = new SolidColorBrush(Color.FromArgb(235, 24, 40, 84)), Stroke = edge, StrokeThickness = 1,
            HorizontalAlignment = tailRight ? HorizontalAlignment.Right : HorizontalAlignment.Left, VerticalAlignment = tailRight ? VerticalAlignment.Center : VerticalAlignment.Bottom,
            Margin = tailRight ? new(0, 0, -9, 0) : new(22, 0, 0, -10),
        };
        var button = new Button
        {
            Cursor = Cursors.Hand, FocusVisualStyle = null, ToolTip = "Agent XCTL", Focusable = false,
            Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) },
            Content = new Grid { Children = { face, tail } },
        };
        button.Click += (_, _) => open();
        button.MouseEnter += (_, _) => face.BorderBrush = Brushes.White; button.MouseLeave += (_, _) => face.BorderBrush = edge;
        return button;
    }
}
