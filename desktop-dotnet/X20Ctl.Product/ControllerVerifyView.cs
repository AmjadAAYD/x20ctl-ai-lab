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

/// <summary>The popup shown when a controller is opened: a radar sweep over the model's photo while Windows is asked who is
/// connected, then the verdict. Decisions are the host's; this only shows and reports which button was pressed.</summary>
public sealed class ControllerVerifyView : Grid
{
    private readonly TextBlock status = Kit.Text("Listening for the controller…", 15, "DS.Text");
    private readonly TextBlock title = Kit.Text("", 20, "DS.Text", FontWeights.SemiBold);
    private readonly StackPanel buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 20, 0, 0) };
    private readonly List<Shape> pulse = new();
    private readonly Ellipse sweepRing = new() { Width = 300, Height = 300, StrokeThickness = 3, RenderTransformOrigin = new(.5, .5), RenderTransform = new RotateTransform() };
    private readonly Rectangle beam = new() { Height = 70, RenderTransform = new TranslateTransform(), IsHitTestVisible = false };
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
        var photo = new Viewbox { Width = 360, Height = 240, Child = new PhotoController(modelId), Opacity = .92, IsHitTestVisible = false };
        // expanding rings and a turning dashed ring behind the photo
        var stage = new Grid { Width = 420, Height = 300, ClipToBounds = false };
        for (int i = 0; i < 3; i++)
        {
            var ring = new Ellipse { Width = 300, Height = 300, Stroke = new SolidColorBrush(accent), StrokeThickness = 2, Opacity = 0, RenderTransformOrigin = new(.5, .5), RenderTransform = new ScaleTransform(.6, .6) };
            pulse.Add(ring); stage.Children.Add(ring);
        }
        sweepRing.Stroke = new LinearGradientBrush(Color.FromArgb(230, accent.R, accent.G, accent.B), Color.FromArgb(0, accent.R, accent.G, accent.B), 0);
        sweepRing.StrokeDashArray = [14, 7];
        stage.Children.Add(sweepRing);
        stage.Children.Add(photo);
        beam.Fill = new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromArgb(0, accent.R, accent.G, accent.B), 0), new(Color.FromArgb(150, accent.R, accent.G, accent.B), .85), new(Color.FromArgb(255, 220, 245, 255), 1),
        }, 90);
        beam.Width = 360; beam.VerticalAlignment = VerticalAlignment.Top; beam.Margin = new(0, 30, 0, 0);
        var beamHost = new Grid { Width = 360, Height = 240, ClipToBounds = true, IsHitTestVisible = false, Children = { beam } };
        stage.Children.Add(beamHost);
        title.Text = $"Checking this is a {modelName}";
        status.Foreground = DS.Brush("DS.TextMuted");
        card.CornerRadius = new(18); card.Padding = new(34, 28, 34, 26); card.MinWidth = 500;
        card.Background = new SolidColorBrush(Color.FromRgb(12, 21, 46));
        card.BorderBrush = new SolidColorBrush(Color.FromArgb(130, accent.R, accent.G, accent.B)); card.BorderThickness = new(1);
        card.HorizontalAlignment = HorizontalAlignment.Center; card.VerticalAlignment = VerticalAlignment.Center;
        title.HorizontalAlignment = status.HorizontalAlignment = HorizontalAlignment.Center;
        card.Child = new StackPanel { Children = { title, new Border { Height = 4 }, stage, status, buttons } };
        Children.Add(card);
        if (animate) Start(); else status.Text = lines[0];
        steps.Tick += (_, _) => { if (step < lines.Length - 1) status.Text = lines[++step]; };
        steps.Start();
        Unloaded += (_, _) => steps.Stop();
    }

    private void Start()
    {
        var forever = RepeatBehavior.Forever;
        sweepRing.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(3.2)) { RepeatBehavior = forever });
        ((TranslateTransform)beam.RenderTransform).BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(-10, 190, TimeSpan.FromSeconds(1.5)) { RepeatBehavior = forever, AutoReverse = true, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        for (int i = 0; i < pulse.Count; i++)
        {
            var begin = TimeSpan.FromMilliseconds(i * 700);
            var scale = (ScaleTransform)pulse[i].RenderTransform;
            var grow = new DoubleAnimation(.55, 1.35, TimeSpan.FromSeconds(2.1)) { RepeatBehavior = forever, BeginTime = begin };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow); scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            pulse[i].BeginAnimation(OpacityProperty, new DoubleAnimationUsingKeyFrames
            {
                RepeatBehavior = forever, BeginTime = begin, Duration = TimeSpan.FromSeconds(2.1),
                KeyFrames = { new LinearDoubleKeyFrame(.55, KeyTime.FromPercent(.15)), new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)) },
            });
        }
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
        foreach (var p in pulse) { p.BeginAnimation(OpacityProperty, null); p.Opacity = 0; }
        sweepRing.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null);
        ((TranslateTransform)beam.RenderTransform).BeginAnimation(TranslateTransform.YProperty, null);
        beam.Visibility = Visibility.Collapsed;
        Color tone = r.Outcome switch { VerifyOutcome.Match => Color.FromRgb(90, 220, 150), VerifyOutcome.Mismatch => Color.FromRgb(255, 105, 95), VerifyOutcome.Unconfirmed => Color.FromRgb(255, 190, 80), _ => Color.FromRgb(140, 160, 200) };
        sweepRing.Stroke = new SolidColorBrush(tone); sweepRing.StrokeDashArray = null;
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
