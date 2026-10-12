using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using X20Ctl.Product.Agent;
using X20Ctl.Zone.Agent;
namespace X20Ctl.Product.Development;

/// <summary>
/// --review-talk DIRECTORY: the Talk tab against the real local model. Opens an X20 Pro, sends sentences through the tab
/// exactly as typed, then an X20 for the paddle it doesn't have, and writes captures (rendered, never the screen) and a log
/// of every turn: the model's raw output, each check, what changed, and the token counts and times the model reported.
/// Settings go to a fixture file in DIRECTORY; PC mode stays off.
/// </summary>
internal static class TalkReview
{
    public static async Task Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var log = new StringBuilder();
        var window = new ProductWindow(false, Path.Combine(directory, "assignments.json"));
        try
        {
            window.Show(); window.WindowState = WindowState.Maximized; await Task.Delay(500);
            await Controller("x20_pro", 0, "1-pro", ["Make my right stick more precise for sniping", "I want faster triggers for shooting", "Put jump on M5", "Set my left stick dead zone to 260", "Turbo on X, 12 presses a second", "Can you change my RGB to red?"]);
            window.Host.ReturnToZone(); await Task.Delay(700);
            await Controller("x20", 1, "2-x20", ["Put jump on M5"]);
            log.AppendLine("== fixture file after the run");
            string fixture = Path.Combine(directory, "pc-mode-fixture.json");
            log.AppendLine(File.Exists(fixture) ? File.ReadAllText(fixture) : "(none written)");
        }
        catch (Exception failure) { log.AppendLine("FAILED: " + failure); }
        finally { File.WriteAllText(Path.Combine(directory, "talk-log.txt"), log.ToString()); window.Close(); }

        async Task Controller(string model, int player, string prefix, string[] sentences)
        {
            window.Scene.AssignController(player, window.Host.Catalog.Get(model)); window.Host.EnterStudio(player); await Task.Delay(1400);
            var studio = window.Host.Studio!; studio.ShowManagement("Talk"); var talk = studio.Talk!;
            for (int i = 0; i < 600 && TalkPage.State is ModelState.Starting or ModelState.Loading; i++) await Task.Delay(250);
            log.AppendLine($"#### {model} · model state {TalkPage.State}");
            await Shot(prefix + "-0-ready");
            int n = 0;
            foreach (var sentence in sentences)
            {
                var turn = await talk.Ask(sentence); n++;
                log.AppendLine($"\n== \"{sentence}\"");
                if (turn == null) { log.AppendLine("no turn (model unreachable or not ready)"); continue; }
                for (int a = 0; a < turn.Attempts.Count; a++) log.AppendLine($"output {a + 1}: {turn.Attempts[a].Content.ReplaceLineEndings(" ")}");
                if (turn.Verdict.StructureError != null) log.AppendLine("structure: " + turn.Verdict.StructureError);
                foreach (var c in turn.Verdict.Checks) log.AppendLine($"  {c.State,-11} {c.Action.Action} {c.Action.Target} {c.Action.ValueText} · {c.Message}");
                log.AppendLine("valid: " + turn.Verdict.Valid + (turn.Outcome.Error != null ? " · " + turn.Outcome.Error : ""));
                foreach (var c in turn.Outcome.Changes) log.AppendLine($"  changed {c.What}: {c.Before} -> {c.After}");
                log.AppendLine("stats: " + AgentSession.Stats(turn));
            }
            await Shot($"{prefix}-1-conversation");
            talk.ExpandDetails(); talk.ScrollTo(0); await Shot($"{prefix}-2-details-top");
        }

        async Task Shot(string name)
        {
            await Task.Delay(900); window.UpdateLayout();
            var bmp = new RenderTargetBitmap((int)window.Stage.ActualWidth, (int)window.Stage.ActualHeight, 96, 96, PixelFormats.Pbgra32); bmp.Render(window.Stage);
            var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(bmp)); using var f = File.Create(Path.Combine(directory, name + ".png")); enc.Save(f);
        }
    }
}
