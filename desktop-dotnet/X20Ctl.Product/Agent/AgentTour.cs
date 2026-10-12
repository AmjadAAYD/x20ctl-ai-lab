using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using X20Ctl.Zone;
using X20Ctl.Zone.Agent;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Product.Agent;

/// <summary>
/// The walk-through after the agent changes something (owner request, 12 Oct 2026): the Studio goes to the page where each
/// change lives, and this card shows it happening, slowly, with what the setting means. Sticks and triggers are drawn as
/// their real response (<see cref="PadProcessor.Response"/>, what PC mode runs) bending from before to after; buttons as
/// caps that change what they send, or pulse at their turbo rate. Skip ends it at once.
/// </summary>
public sealed class AgentTour
{
    const double W = 400, H = 190;
    public Border Card { get; }
    /// <summary>A step finished animating and is being held: for review captures.</summary>
    public event Action<TourStep>? Held;
    readonly CancellationTokenSource skip = new();
    readonly StackPanel body = new();
    readonly TextBlock counter = Kit.Text("", 13, "DS.TextMuted");
    readonly Border progress = new() { Height = 3, CornerRadius = new(1.5), HorizontalAlignment = HorizontalAlignment.Left, Width = 0, Background = DS.Brush("DS.AccentHi") };

    public AgentTour()
    {
        var mark = new Grid { Width = 30, Height = 30, Margin = new(0, 0, 10, 0), Children = { new Ellipse { Fill = AgentMark.Field() }, AgentMark.Glyph(20) } };
        var skipButton = new Button { Style = DS.Style("DS.ActionSecondary"), Height = 30, MinHeight = 0, Padding = new(12, 0, 12, 0), Content = Kit.Text("Skip", 13, "DS.TextSoft") };
        skipButton.Click += (_, _) => Skip();
        var head = new DockPanel { Margin = new(0, 0, 0, 14) };
        DockPanel.SetDock(skipButton, Dock.Right); head.Children.Add(skipButton);
        DockPanel.SetDock(mark, Dock.Left); head.Children.Add(mark);
        head.Children.Add(new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { Kit.Text("Agent XCTL", 15, "DS.Text", FontWeights.SemiBold), counter } });
        var track = new Border { Height = 3, CornerRadius = new(1.5), Margin = new(0, 16, 0, 0), Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Child = progress };
        Card = new Border
        {
            Width = 470, CornerRadius = new(22), BorderThickness = new(1), Padding = new(22, 18, 22, 18),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 40, 30, 0),
            Background = new LinearGradientBrush(Color.FromArgb(250, 14, 24, 52), Color.FromArgb(250, 7, 12, 30), 90),
            BorderBrush = new LinearGradientBrush(Color.FromArgb(150, 120, 170, 255), Color.FromArgb(40, 120, 170, 255), 90),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 40, ShadowDepth = 10, Opacity = .55 },
            Child = new StackPanel { Children = { head, body, track } },
        };
    }

    public void Skip() => skip.Cancel();

    /// <summary>Every step in turn: visit its page, show the change, hold so it can be read. Returns when done or skipped.</summary>
    public async Task Play(IReadOnlyList<TourStep> steps, PcModeProfile before, PcModeProfile after, Action<TourStep> visit)
    {
        try
        {
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                visit(step);
                counter.Text = steps.Count == 1 ? "What changed" : $"{i + 1} of {steps.Count}";
                var show = Build(step, before, after, out var play);
                body.Children.Clear(); body.Children.Add(show);
                if (i > 0) Fx.Appear(show, .98, 260);
                int morph = DS.Motion ? 2000 : 0, hold = 2600 + 700 * step.Rows.Count;
                progress.BeginAnimation(FrameworkElement.WidthProperty, null); progress.Width = 0;
                progress.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(0, 426, TimeSpan.FromMilliseconds(600 + morph + hold)));
                await Task.Delay(600, skip.Token);
                play(morph);
                await Task.Delay(morph, skip.Token);
                Held?.Invoke(step);
                await Task.Delay(hold, skip.Token);
            }
        }
        catch (TaskCanceledException) { }
    }

    /// <summary>One step's content, and the action that animates it from before to after.</summary>
    FrameworkElement Build(TourStep step, PcModeProfile before, PcModeProfile after, out Action<int> play)
    {
        var stack = new StackPanel();
        stack.Children.Add(Kit.Text(AgentApplier.Name(step.Target), 24, "DS.Text", FontWeights.SemiBold).Also(t => t.Margin = new(0, 0, 0, 12)));
        var picture = step.Kind == TourKind.Button ? Caps(step, before, after, out play) : Response(step, before, after, out play);
        stack.Children.Add(picture);
        foreach (var row in step.Rows)
        {
            var line = new DockPanel { Margin = new(0, 14, 0, 0) };
            var change = Kit.Text($"{row.Change.Before}  →  {row.Change.After}", 15, "DS.AccentHi", FontWeights.SemiBold); DockPanel.SetDock(change, Dock.Right); line.Children.Add(change);
            line.Children.Add(Kit.Text(row.Meaning.Name, 15, "DS.Text", FontWeights.SemiBold));
            stack.Children.Add(line);
            stack.Children.Add(Wrap(char.ToUpper(row.Meaning.What[0]) + row.Meaning.What[1..] + ".", 13.5, "DS.TextSoft", 4));
            if (Direction(row) is { Length: > 0 } direction) stack.Children.Add(Wrap(direction, 13.5, "DS.TextSoft", 2));
            stack.Children.Add(Wrap("Good for " + row.Meaning.Good + ".", 13.5, "DS.Success", 4));
            stack.Children.Add(Wrap("Watch out: " + row.Meaning.Careful + ".", 13.5, "DS.Warn", 2));
        }
        return stack;
    }

    /// <summary>Which way it moved and what that does: "Higher: …", or the curve's own shape.</summary>
    static string Direction(TourRow row)
    {
        var m = row.Meaning;
        if (m.Action is AgentVocabulary.StickCurve or AgentVocabulary.TriggerCurve) return CurveDraft.Presets.ContainsKey(row.Change.After) ? "Now " + AgentKnowledge.Preset(row.Change.After) + "." : "";
        if (m.More.Length == 0) return "";
        static double N(string s) => double.TryParse(s.TrimEnd('%', 's', '/').Replace("off", "0"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0;
        return N(row.Change.After) > N(row.Change.Before) ? "Higher: " + m.More + "." : "Lower: " + m.Less + ".";
    }

    // ---- sticks and triggers: the response, bending ----
    UIElement Response(TourStep step, PcModeProfile before, PcModeProfile after, out Action<int> play)
    {
        var canvas = new Canvas { Width = W, Height = H, ClipToBounds = false, Margin = new(0, 0, 0, 22) };
        var grid = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
        for (int i = 0; i <= 4; i++)
        {
            canvas.Children.Add(new Line { X1 = i * W / 4, X2 = i * W / 4, Y1 = 0, Y2 = H, Stroke = grid, StrokeThickness = 1 });
            canvas.Children.Add(new Line { X1 = 0, X2 = W, Y1 = i * H / 4, Y2 = i * H / 4, Stroke = grid, StrokeThickness = 1 });
        }
        var (fromCurve, fromTuning) = Shape(before, step); var (toCurve, toTuning) = Shape(after, step);
        double fromInner = fromTuning?.Inner ?? fromCurve?.Inner ?? 0, toInner = toTuning?.Inner ?? toCurve?.Inner ?? 0;
        double fromOuter = fromTuning?.Outer ?? fromCurve?.Outer ?? 100, toOuter = toTuning?.Outer ?? toCurve?.Outer ?? 100;
        var dead = new Rectangle { Height = H, Width = fromInner / 100 * W, Fill = new SolidColorBrush(Color.FromArgb(46, 232, 54, 79)) };
        var full = new Rectangle { Height = H, Width = (100 - fromOuter) / 100 * W, Fill = new SolidColorBrush(Color.FromArgb(40, 61, 214, 140)) };
        Canvas.SetLeft(full, fromOuter / 100 * W);
        canvas.Children.Add(dead); canvas.Children.Add(full);
        var was = Points(fromCurve, fromTuning); var now = Points(toCurve, toTuning);
        canvas.Children.Add(new Polyline { Points = was, Stroke = new SolidColorBrush(Color.FromArgb(120, 200, 215, 240)), StrokeThickness = 2, StrokeDashArray = [3, 3] });
        var line = new Polyline { Points = was, Stroke = DS.Brush("DS.AccentHi"), StrokeThickness = 3.5, StrokeLineJoin = PenLineJoin.Round, Effect = new DropShadowEffect { Color = Color.FromRgb(70, 150, 255), BlurRadius = 14, ShadowDepth = 0, Opacity = .8 } };
        canvas.Children.Add(line);
        // a marker at half a push, riding the curve, with what it gives
        double fromHalf = PadProcessor.Response(fromCurve, fromTuning, .5) * 100, toHalf = PadProcessor.Response(toCurve, toTuning, .5) * 100;
        var dot = new Ellipse { Width = 12, Height = 12, Fill = Brushes.White, Effect = new DropShadowEffect { Color = AgentMark.Ice, BlurRadius = 12, ShadowDepth = 0, Opacity = 1 } };
        var tag = Kit.Text("", 13, "DS.Text", FontWeights.SemiBold);
        void Mark(double half) { Canvas.SetLeft(dot, W / 2 - 6); Canvas.SetTop(dot, H - half / 100 * H - 6); Canvas.SetLeft(tag, W / 2 + 12); Canvas.SetTop(tag, H - half / 100 * H - 22); tag.Text = $"half a push → {half:0}%"; }
        Mark(fromHalf);
        canvas.Children.Add(new Line { X1 = W / 2, X2 = W / 2, Y1 = 0, Y2 = H, Stroke = new SolidColorBrush(Color.FromArgb(70, 169, 207, 255)), StrokeThickness = 1, StrokeDashArray = [2, 4] });
        canvas.Children.Add(dot); canvas.Children.Add(tag);
        var axisX = Kit.Text(step.Kind == TourKind.Stick ? "PUSH →" : "PULL →", 11, "DS.TextMuted"); Canvas.SetLeft(axisX, W - 50); Canvas.SetTop(axisX, H + 4); canvas.Children.Add(axisX);
        var axisY = Kit.Text("↑ OUTPUT", 11, "DS.TextMuted"); Canvas.SetLeft(axisY, 4); Canvas.SetTop(axisY, 2); canvas.Children.Add(axisY);
        play = ms =>
        {
            line.Points = now; dead.Width = toInner / 100 * W; full.Width = (100 - toOuter) / 100 * W; Canvas.SetLeft(full, toOuter / 100 * W);
            if (ms == 0) { Mark(toHalf); return; }
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            line.BeginAnimation(Polyline.PointsProperty, new CurvePointMorph(was, now) { Duration = TimeSpan.FromMilliseconds(ms) });
            dead.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(fromInner / 100 * W, toInner / 100 * W, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
            full.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation((100 - fromOuter) / 100 * W, (100 - toOuter) / 100 * W, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
            full.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(fromOuter / 100 * W, toOuter / 100 * W, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
            _ = Count(fromHalf, toHalf, ms, Mark);
        };
        return canvas;
    }

    async Task Count(double from, double to, int ms, Action<double> show)
    {
        // a short-lived count, not a standing Rendering handler (idle CPU is frame rate)
        const int frames = 40;
        try { for (int f = 1; f <= frames; f++) { await Task.Delay(ms / frames, skip.Token); double t = f / (double)frames; t = t < .5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2; show(from + (to - from) * t); } }
        catch (TaskCanceledException) { }
    }

    static (SetupCurve?, StickTuning?) Shape(PcModeProfile p, TourStep step) =>
        (p.Curves.GetValueOrDefault(step.Target), step.Kind == TourKind.Stick ? p.Sticks.GetValueOrDefault(step.Target) : null);

    static PointCollection Points(SetupCurve? curve, StickTuning? tuning) =>
        new(Enumerable.Range(0, 101).Select(i => new Point(i / 100.0 * W, H - PadProcessor.Response(curve, tuning, i / 100.0) * H)));

    // ---- buttons: what it sends, and its turbo ----
    UIElement Caps(TourStep step, PcModeProfile before, PcModeProfile after, out Action<int> play)
    {
        string from = before.Remap.GetValueOrDefault(step.Target, step.Target), to = after.Remap.GetValueOrDefault(step.Target, step.Target);
        int hz = after.Turbo.GetValueOrDefault(step.Target);
        var source = Cap(step.Target, false); var sent = Cap(from, true);
        var sentLabel = (TextBlock)((Border)sent.Children[0]).Child;
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 6, 0, 10) };
        row.Children.Add(Labelled(source, "YOU PRESS")); row.Children.Add(Kit.Icon("", 22, "DS.AccentHi").Also(t => t.Margin = new(26, 0, 26, 18))); row.Children.Add(Labelled(sent, "GAME GETS"));
        var rate = Kit.Text(hz > 0 ? $"{hz} presses a second while held" : before.Turbo.ContainsKey(step.Target) ? "Turbo off" : "", 14, "DS.TextSoft").Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Opacity = 0; });
        var panel = new StackPanel { Children = { row, rate } };
        var glow = (Border)source.Children[0];
        play = ms =>
        {
            if (from != to)
            {
                if (ms == 0) sentLabel.Text = to;
                else
                {
                    var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(ms / 2)) { FillBehavior = FillBehavior.Stop };
                    fade.Completed += (_, _) => { sentLabel.Text = to; sentLabel.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ms / 2))); };
                    sentLabel.BeginAnimation(UIElement.OpacityProperty, fade);
                }
            }
            rate.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Math.Max(ms, 1))));
            // the cap flashes at the real rate, for as long as the step is shown
            if (hz > 0 && DS.Motion) glow.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, .25, TimeSpan.FromSeconds(.5 / hz)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(6)) });
        };
        return panel;
    }

    static Grid Cap(string text, bool output) => new()
    {
        Width = 92, Height = 92,
        Children =
        {
            new Border
            {
                CornerRadius = new(46), BorderThickness = new(2),
                Background = new RadialGradientBrush(Color.FromArgb(255, 40, 70, 130), Color.FromArgb(255, 14, 24, 52)),
                BorderBrush = output ? DS.Brush("DS.AccentHi") : new SolidColorBrush(Color.FromArgb(160, 200, 215, 240)),
                Effect = new DropShadowEffect { Color = output ? Color.FromRgb(70, 150, 255) : Colors.Black, BlurRadius = 18, ShadowDepth = 0, Opacity = .7 },
                Child = Kit.Text(text, text.Length > 3 ? 15 : 24, "DS.Text", FontWeights.SemiBold).Also(t => t.HorizontalAlignment = HorizontalAlignment.Center),
            },
        },
    };

    static StackPanel Labelled(UIElement cap, string label) => new() { Children = { cap, Kit.Text(label, 11, "DS.TextMuted").Also(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new(0, 8, 0, 0); }) } };
    static TextBlock Wrap(string text, double size, string brush, double top) => Kit.Text(text, size, brush).Also(t => { t.TextWrapping = TextWrapping.Wrap; t.TextTrimming = TextTrimming.None; t.Margin = new(0, top, 0, 0); });
}
