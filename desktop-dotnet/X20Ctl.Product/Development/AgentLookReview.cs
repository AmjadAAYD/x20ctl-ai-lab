using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using X20Ctl.Product.Agent;
namespace X20Ctl.Product.Development;

/// <summary>
/// --review-agent-look DIRECTORY: how the agent looks, without the model. Offline mode answers, so it is quick and the
/// same every run: the window open, a search as it's typed and as it's sent, a change with its walk-through, and the window after. Captures are
/// rendered, never the screen.
/// </summary>
internal static class AgentLookReview
{
    public static async Task Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var window = new ProductWindow(false, Path.Combine(directory, "assignments.json"));
        try
        {
            window.Show(); window.WindowState = WindowState.Maximized; await Task.Delay(500);
            window.Scene.AssignController(0, window.Host.Catalog.Get("x20_pro")); window.Host.EnterStudio(0); await Task.Delay(1400);
            var studio = window.Host.Studio!; studio.ShowButtons();
            AgentPanel.UseOffline(true);
            await Shot("0-closed");
            int stop = 0;
            studio.AgentTourStarted += tour => tour.Held += step => _ = Shot($"3-tour-{++stop}-{step.Target}");
            studio.OpenAgent(); var talk = studio.AgentWindow!;
            await Shot("1-open");
            talk.Input.Text = "sti"; await Shot("2-typing-sti"); talk.Input.Text = "";
            await talk.Ask("Sticks"); await Shot("2-searched-sticks");
            await talk.Ask("Turbo on X, 12 a second"); await Shot("4-after");
            await talk.Ask("Make my right stick good for sniping"); await Shot("5-after-curve");
        }
        finally { AgentPanel.UseOffline(false); window.Close(); }

        async Task Shot(string name)
        {
            await Task.Delay(900); window.UpdateLayout();
            var bmp = new RenderTargetBitmap((int)window.Stage.ActualWidth, (int)window.Stage.ActualHeight, 96, 96, PixelFormats.Pbgra32); bmp.Render(window.Stage);
            var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(bmp)); using var f = File.Create(Path.Combine(directory, name + ".png")); enc.Save(f);
        }
    }
}
