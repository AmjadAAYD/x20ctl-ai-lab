using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
namespace X20Ctl.Product.Agent;

/// <summary>
/// The agent's mark (owner request, 12 Oct 2026): a round head with two lit eyes and an antenna tipped in the brand red,
/// on the X20CTL blue. Drawn as vectors, so it is sharp at any size: the orb, the window's title and anywhere else.
/// </summary>
public static class AgentMark
{
    public static readonly Color Red = Color.FromRgb(232, 54, 79), Ice = Color.FromRgb(169, 207, 255);
    public static Viewbox Glyph(double size)
    {
        var canvas = new Canvas { Width = 64, Height = 64 };
        var white = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255));
        canvas.Children.Add(new Line { X1 = 32, Y1 = 22, X2 = 32, Y2 = 12, Stroke = white, StrokeThickness = 3, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        canvas.Children.Add(Place(new Ellipse { Width = 9, Height = 9, Fill = new SolidColorBrush(Red), Effect = new DropShadowEffect { Color = Red, BlurRadius = 10, ShadowDepth = 0, Opacity = .9 } }, 27.5, 6));
        canvas.Children.Add(Place(new Rectangle { Width = 38, Height = 28, RadiusX = 11, RadiusY = 11, Stroke = white, StrokeThickness = 3.2, Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) }, 13, 22));
        foreach (double x in new[] { 22.5, 36.5 })
            canvas.Children.Add(Place(new Rectangle { Width = 5, Height = 9, RadiusX = 2.5, RadiusY = 2.5, Fill = new SolidColorBrush(Ice), Effect = new DropShadowEffect { Color = Ice, BlurRadius = 8, ShadowDepth = 0, Opacity = 1 } }, x, 31.5));
        // the "ears": two short grips, so the head reads as a controller as much as a robot
        foreach (double x in new[] { 8, 52 })
            canvas.Children.Add(Place(new Rectangle { Width = 4, Height = 12, RadiusX = 2, RadiusY = 2, Fill = white }, x, 30));
        return new Viewbox { Width = size, Height = size, Child = canvas };
    }
    static UIElement Place(UIElement e, double x, double y) { Canvas.SetLeft(e, x); Canvas.SetTop(e, y); return e; }
    public static Brush Field() => new RadialGradientBrush(new GradientStopCollection { new(Color.FromRgb(92, 160, 255), 0), new(Color.FromRgb(40, 104, 228), .55), new(Color.FromRgb(16, 42, 112), 1) }) { GradientOrigin = new(.32, .25), Center = new(.4, .35), RadiusX = .8, RadiusY = .8 };
}

/// <summary>
/// The round button that opens the agent. It floats over the Studio; a dot on its edge says whether the model is ready,
/// and a ring turns only while the agent is thinking (no animation runs while idle).
/// </summary>
public sealed class AgentOrb : Button
{
    readonly Ellipse dot = new() { Width = 15, Height = 15, StrokeThickness = 2.5, Stroke = new SolidColorBrush(Color.FromRgb(6, 10, 22)), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new(0, 0, 3, 3) };
    readonly Ellipse ring = new() { StrokeThickness = 3, Visibility = Visibility.Collapsed, RenderTransformOrigin = new(.5, .5), Margin = new(-6) };
    readonly ScaleTransform scale = new(1, 1);

    public AgentOrb()
    {
        Width = Height = 68; Cursor = Cursors.Hand; FocusVisualStyle = null; ToolTip = "Agent XCTL";
        System.Windows.Automation.AutomationProperties.SetName(this, "Open Agent XCTL");
        Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) };
        ring.Stroke = new LinearGradientBrush(Color.FromArgb(0, 169, 207, 255), Color.FromArgb(255, 169, 207, 255), 0);
        ring.RenderTransform = new RotateTransform();
        var face = new Grid
        {
            Children =
            {
                ring,
                new Ellipse { Fill = AgentMark.Field(), Stroke = new SolidColorBrush(Color.FromArgb(150, 190, 220, 255)), StrokeThickness = 1.5,
                    Effect = new DropShadowEffect { Color = Color.FromRgb(52, 126, 241), BlurRadius = 26, ShadowDepth = 0, Opacity = .65 } },
                AgentMark.Glyph(40),
                dot,
            },
        };
        RenderTransformOrigin = new(.5, .5); RenderTransform = scale; Content = face;
        MouseEnter += (_, _) => Grow(1.07); MouseLeave += (_, _) => Grow(1);
        PreviewMouseLeftButtonDown += (_, _) => Grow(.95); PreviewMouseLeftButtonUp += (_, _) => Grow(IsMouseOver ? 1.07 : 1);
        SetState(ModelState.Starting);
    }

    void Grow(double to)
    {
        if (!DS.Motion) { scale.ScaleX = scale.ScaleY = to; return; }
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, DS.To(to, 140)); scale.BeginAnimation(ScaleTransform.ScaleYProperty, DS.To(to, 140));
    }

    public void SetState(ModelState state) =>
        dot.Fill = DS.Brush(state switch { ModelState.Ready => "DS.Success", ModelState.Starting or ModelState.Loading => "DS.Warn", _ => "DS.Danger" });

    public void SetThinking(bool on)
    {
        var turn = (RotateTransform)ring.RenderTransform;
        ring.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (on && DS.Motion) turn.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1.1)) { RepeatBehavior = RepeatBehavior.Forever });
        else turn.BeginAnimation(RotateTransform.AngleProperty, null);
    }
}
