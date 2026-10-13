using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using X20Ctl.Product.Scanner;

namespace X20Ctl.Product;

public enum VerifyOutcome { Match, Mismatch, Unconfirmed, NoController }

/// <summary>What the scan on entering a controller found. Detected is a model id when Windows named the pad as one.</summary>
public sealed record VerifyResult(VerifyOutcome Outcome, string? DetectedId, string? DetectedName, string Detail);

public static class ControllerVerify
{
    // model ids by the name ScanFlow.DetectModel returns
    private static readonly Dictionary<string, string> ByName = new()
    {
        ["Dune / D15"] = "dune", ["X20 Pro"] = "x20_pro", ["X20"] = "x20", ["X15"] = "x15", ["X10"] = "x10", ["X05 Pro"] = "x05_pro", ["X05"] = "x05", ["D10"] = "d10",
    };
    // identities seen on real hardware that name a model outright (docs/00-findings.md); the shared Xbox identities never do
    private static readonly Dictionary<(int, int), string> ById = new() { [(0x0079, 0x181C)] = "x20", [(0x1D57, 0xFA60)] = "x20", [(0x413D, 0x2131)] = "dune" };

    /// <summary>Asks Windows what is plugged in and whether it names itself as the model the player clicked. A name or ID is
    /// the pad's own claim; the shared Xbox identities prove nothing, so such a pad comes back Unconfirmed, never Match.</summary>
    public static async Task<VerifyResult> Run(string modelId)
    {
        Inventory inventory;
        try { inventory = await Inventory.Bound(Task.Run(() => WindowsInventory.Read(false)), 8000); }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or System.IO.IOException)
        { return new(VerifyOutcome.NoController, null, null, "Windows would not list devices."); }
        string? id = null, name = null;
        if (ScanFlow.DetectModel(inventory) is { } named && ByName.TryGetValue(named.Model, out var byName)) { id = byName; name = named.Name; }
        else foreach (var i in inventory.interfaces) if (ById.TryGetValue((i.vid, i.pid), out var byId)) { id = byId; name = $"{i.vid:X4}:{i.pid:X4}"; break; }
        if (id == modelId) return new(VerifyOutcome.Match, id, name, name ?? "");
        if (id != null) return new(VerifyOutcome.Mismatch, id, name, name ?? "");
        bool pad = inventory.interfaces.Any(i => (i.kind == "HID" && i.usagePage == 1 && i.usage is 4 or 5) || i.vid == 0x045E);
        return pad ? new(VerifyOutcome.Unconfirmed, null, null, "A gamepad is connected but doesn't name its model.")
                   : new(VerifyOutcome.NoController, null, null, "No gamepad was listed.");
    }
}

/// <summary>The popup shown when a controller is opened: a glowing gamepad outline that a light runs round while Windows is
/// asked who is connected, then the verdict. Decisions are the host's; this only shows and reports which button was pressed.</summary>
public sealed class ControllerVerifyView : Grid
{
    private readonly TextBlock status = Kit.Text("", 15, "DS.TextMuted");
    private readonly TextBlock title = Kit.Text("", 20, "DS.Text", FontWeights.SemiBold);
    private readonly StackPanel buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 20, 0, 0) };
    private readonly Path rim = new() { Data = GamepadGlyph.Outline, StrokeThickness = 8.5, StrokeLineJoin = PenLineJoin.Round, Stroke = new SolidColorBrush(Color.FromRgb(240, 245, 255)) };
    private readonly Path runner = new() { Data = GamepadGlyph.Outline, StrokeThickness = 8.5, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
    private readonly SolidColorBrush runnerBrush = new(Color.FromRgb(120, 215, 255));
    private readonly System.Windows.Media.Effects.DropShadowEffect rimGlow = new() { Color = Colors.White, BlurRadius = 20, ShadowDepth = 0, Opacity = .6 };
    private readonly Ellipse[] dots = new Ellipse[3];
    private readonly TextBlock verdict = new() { FontFamily = DS.Display, FontSize = 54, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0 };
    private readonly Border card = new();
    private readonly DispatcherTimer steps = new() { Interval = TimeSpan.FromMilliseconds(750) };
    private readonly string[] lines;
    private int step;

    public event Action? Cancelled;
    public void RaiseCancel() => Cancelled?.Invoke();

    public ControllerVerifyView(string modelId, string modelName, bool animate)
    {
        lines = ["Listening for the controller…", "Reading its identity…", $"Comparing with the {modelName}…"];
        Background = new SolidColorBrush(Color.FromArgb(170, 2, 6, 18));
        var accent = Color.FromRgb(86, 190, 255);
        var size = GamepadGlyph.Size;
        // dark glass body, white tube rim with a soft glow, a light running round it
        rim.Fill = new LinearGradientBrush(Color.FromRgb(46, 54, 76), Color.FromRgb(22, 27, 42), 90);
        rim.Effect = rimGlow;
        runner.Stroke = runnerBrush;
        double dash = GamepadGlyph.Length * .16 / runner.StrokeThickness, gap = GamepadGlyph.Length * .84 / runner.StrokeThickness;
        runner.StrokeDashArray = [dash, gap];
        runner.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = accent, BlurRadius = 22, ShadowDepth = 0, Opacity = 1 };
        double pad = GamepadGlyph.Pad;
        var art = new Grid { Width = size.Width + pad * 2, Height = size.Height + pad * 2 };
        var details = new Path { Data = GamepadGlyph.Details, StrokeThickness = 2.4, Stroke = new SolidColorBrush(Color.FromArgb(105, 200, 218, 250)), StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        foreach (var layer in new Path[] { rim, details, runner }) { layer.Margin = new(pad); layer.HorizontalAlignment = HorizontalAlignment.Left; layer.VerticalAlignment = VerticalAlignment.Top; art.Children.Add(layer); }
        var dotRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new(0, -56, 0, 0) };
        for (int i = 0; i < 3; i++)
        {
            dots[i] = new Ellipse { Width = 9, Height = 9, Margin = new(5, 0, 5, 0), Fill = Brushes.White, Opacity = .35,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.White, BlurRadius = 10, ShadowDepth = 0, Opacity = 1 } };
            dotRow.Children.Add(dots[i]);
        }
        art.Children.Add(dotRow); art.Children.Add(verdict);
        var stage = new Viewbox { Width = 400, Height = 300, Child = art, Margin = new(0, 6, 0, 6) };
        title.Text = $"Checking this is a {modelName}";
        title.HorizontalAlignment = status.HorizontalAlignment = HorizontalAlignment.Center;
        card.CornerRadius = new(18); card.Padding = new(34, 28, 34, 26); card.MinWidth = 500;
        card.Background = new SolidColorBrush(Color.FromRgb(12, 21, 46));
        card.BorderBrush = new SolidColorBrush(Color.FromArgb(130, accent.R, accent.G, accent.B)); card.BorderThickness = new(1);
        card.HorizontalAlignment = HorizontalAlignment.Center; card.VerticalAlignment = VerticalAlignment.Center;
        card.Child = new StackPanel { Children = { title, stage, status, buttons } };
        Children.Add(card);
        status.Text = lines[0];
        if (animate) Start();
        steps.Tick += (_, _) => { if (step < lines.Length - 1) status.Text = lines[++step]; };
        steps.Start();
        Unloaded += (_, _) => steps.Stop();
    }

    private void Start()
    {
        var forever = RepeatBehavior.Forever;
        // the light runs the whole way round: the dash and its gap add up to one lap
        double lap = GamepadGlyph.Length / runner.StrokeThickness;
        runner.BeginAnimation(Shape.StrokeDashOffsetProperty, new DoubleAnimation(0, -lap, TimeSpan.FromSeconds(2.4)) { RepeatBehavior = forever });
        runnerBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Color.FromRgb(120, 215, 255), Color.FromRgb(190, 130, 255), TimeSpan.FromSeconds(2.4)) { RepeatBehavior = forever, AutoReverse = true });
        rimGlow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, new DoubleAnimation(.3, .75, TimeSpan.FromSeconds(1.2)) { RepeatBehavior = forever, AutoReverse = true, EasingFunction = new SineEase() });
        for (int i = 0; i < 3; i++)
            dots[i].BeginAnimation(OpacityProperty, new DoubleAnimationUsingKeyFrames
            {
                RepeatBehavior = forever, BeginTime = TimeSpan.FromMilliseconds(i * 220), Duration = TimeSpan.FromSeconds(1.1),
                KeyFrames = { new LinearDoubleKeyFrame(1, KeyTime.FromPercent(.3)), new LinearDoubleKeyFrame(.3, KeyTime.FromPercent(.7)) },
            });
    }

    private Button Action(string text, bool primary, Action click)
    {
        var b = new Button { Style = DS.Style(primary ? "DS.ActionPrimary" : "DS.ActionSecondary"), Height = 42, MinHeight = 0, Padding = new(22, 0, 22, 0), Margin = new(5, 0, 5, 0), Content = text };
        b.Click += (_, _) => click(); return b;
    }

    /// <summary>Shows the verdict. enter = go on into the Studio, switchTo = open it as the model Windows named, rescan = ask again.</summary>
    public void ShowResult(VerifyResult r, string modelName, string? detectedName, Action enter, Action<string> switchTo, Action rescan)
    {
        steps.Stop();
        runner.BeginAnimation(Shape.StrokeDashOffsetProperty, null); runnerBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
        rimGlow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, null);
        foreach (var d in dots) { d.BeginAnimation(OpacityProperty, null); d.Visibility = Visibility.Collapsed; }
        Color tone = r.Outcome switch { VerifyOutcome.Match => Color.FromRgb(90, 220, 150), VerifyOutcome.Mismatch => Color.FromRgb(255, 105, 95), VerifyOutcome.Unconfirmed => Color.FromRgb(255, 190, 80), _ => Color.FromRgb(160, 175, 210) };
        // the whole outline takes the verdict's colour, and the running light is put away
        rim.Stroke = new SolidColorBrush(tone);
        rimGlow.Color = tone; rimGlow.Opacity = .8;
        runner.Visibility = Visibility.Collapsed;
        verdict.Text = r.Outcome switch { VerifyOutcome.Match => "✓", VerifyOutcome.Mismatch => "✕", VerifyOutcome.Unconfirmed => "?", _ => "…" };
        verdict.Foreground = new SolidColorBrush(tone); verdict.Margin = new(0, -56, 0, 0); verdict.Opacity = 1;
        card.BorderBrush = new SolidColorBrush(Color.FromArgb(190, tone.R, tone.G, tone.B));
        buttons.Children.Clear();
        switch (r.Outcome)
        {
            case VerifyOutcome.Match:
                title.Text = $"Verified · {modelName}"; status.Text = (r.DetectedName ?? "") + " · opening";
                break;
            case VerifyOutcome.Mismatch:
                string other = ModelLabel(r.DetectedId!, detectedName);
                title.Text = $"This is a {other}, not a {modelName}"; status.Text = $"The controller names itself \"{r.DetectedName}\". {modelName} settings stay off it.";
                buttons.Children.Add(Action($"Open as {other}", true, () => switchTo(r.DetectedId!)));
                buttons.Children.Add(Action("Cancel", false, () => Cancelled?.Invoke()));
                break;
            case VerifyOutcome.Unconfirmed:
                title.Text = "Controller found"; status.Text = $"It doesn't name its model, so it can't be confirmed as a {modelName}.";
                buttons.Children.Add(Action("Continue", true, enter));
                buttons.Children.Add(Action("Cancel", false, () => Cancelled?.Invoke()));
                break;
            default:
                title.Text = "No controller found"; status.Text = $"Connect your {modelName}, or edit offline.";
                buttons.Children.Add(Action("Continue offline", true, enter));
                buttons.Children.Add(Action("Scan again", false, rescan));
                break;
        }
        if (buttons.Children.Count > 0) buttons.Children[0].Focus();
    }

    private static string ModelLabel(string id, string? fallback) => id switch
    {
        "x20" => "X20", "x20_pro" => "X20 Pro", "x15" => "X15", "x10" => "X10", "x05" => "X05", "x05_pro" => "X05 Pro", "d10" => "D10", "dune" => "Dune 8K", _ => fallback ?? id,
    };
}
