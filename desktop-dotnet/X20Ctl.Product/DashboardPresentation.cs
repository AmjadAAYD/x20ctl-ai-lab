using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace X20Ctl.Product;

/// <summary>Local artwork overrides are keyed by launcher identity, independent of display name.</summary>
internal static class GameArtwork
{
    private static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "x20ctl", "game-art", "custom");
    private static string FileFor(GameEntry game) => Path.Combine(Root, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(game.Store + "\n" + game.Id + "\n" + game.LaunchUri))) + ".png");
    public static string? PathFor(GameEntry game) => File.Exists(FileFor(game)) ? FileFor(game) : game.Cover;
    public static void Save(GameEntry game, string source)
    {
        // Decode before touching the existing override. Canonical PNG avoids extension/codec ambiguity.
        using var input = File.OpenRead(source);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        if (frame.PixelWidth > 8192 || frame.PixelHeight > 8192) throw new ArgumentException("Image too large");
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(frame));
        Directory.CreateDirectory(Root); string target = FileFor(game), temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { using (var output = File.Create(temp)) encoder.Save(output); File.Move(temp, target, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

/// <summary>Front/rear artwork turns edge-on before swapping; layout bounds never change.</summary>
internal sealed class DashboardControllerPreview : Grid
{
    private readonly Grid body = new();
    private readonly PhotoController front, rear;
    private readonly TranslateTransform hover = new();
    private readonly ScaleTransform turn = new(1, 1);
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly bool animate;
    private bool started, back;
    private int generation;
    public DashboardControllerPreview(string model, bool animate)
    {
        Width = 1536; Height = 1024; this.animate = animate;
        front = new PhotoController(model); rear = new PhotoController(model, true) { Opacity = 0 };
        body.Children.Add(front); body.Children.Add(rear); body.RenderTransform = turn; body.RenderTransformOrigin = new(.5, .5);
        RenderTransform = hover; Children.Add(body);
        timer.Tick += (_, _) => Turn();
        Unloaded += (_, _) => Stop();
        IsVisibleChanged += (_, _) => { if (IsVisible && started) Resume(); else Pause(); };
    }
    public void Start() { if (!animate) return; started = true; if (IsVisible) Resume(); }
    private void Resume()
    {
        if (timer.IsEnabled) return;
        hover.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-14, 14, TimeSpan.FromSeconds(2.5)) {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        timer.Start();
    }
    private void Pause()
    {
        generation++; timer.Stop(); hover.BeginAnimation(TranslateTransform.YProperty, null); turn.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        turn.ScaleX = 1; hover.Y = 0;
    }
    public void Stop() { started = false; Pause(); }
    private async void Turn()
    {
        int token = generation;
        turn.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, .015, TimeSpan.FromMilliseconds(280)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } });
        await System.Threading.Tasks.Task.Delay(280);
        if (token != generation || !IsVisible) return;
        back = !back; front.Opacity = back ? 0 : 1; rear.Opacity = back ? 1 : 0;
        turn.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.015, 1, TimeSpan.FromMilliseconds(340)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
    }
}
