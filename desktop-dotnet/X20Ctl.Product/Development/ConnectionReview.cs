using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace X20Ctl.Product.Development;

internal static class ConnectionReview
{
    public static async Task Run(string directory)
    {
        foreach (string theme in new[] { "default", "nocturne" })
        {
            ThemeCatalog.Apply(ThemeCatalog.Find(theme));
            await RunTheme(Path.Combine(directory, theme));
        }
    }
    private static async Task RunTheme(string directory)
    {
        ReviewSandbox.Active = true;
        ReviewSandbox.HideLibrary = true;
        Directory.CreateDirectory(directory);
        var host = new ProductHost(new ZoneScene(true), Path.Combine(directory, "assignments.json"));
        var stage = new DesignStage { Child = host };
        using var source = new HwndSource(new HwndSourceParameters("Connection regression offscreen")
        { WindowStyle = unchecked((int)0x80000000), Width = 1600, Height = 900, PositionX = -32000, PositionY = -32000 }) { RootVisual = stage };
        host.Zone.AssignController(0, host.Catalog.Get("x20_pro"));
        host.EnterStudio(0);
        host.UpdateBatteryLevels(["receiver", null, null, null], "2.4 GHz receiver");
        Set("nativeEpoch", "connection-review");
        Set("nativeSubscription", "review-input");
        long sequence = 0;
        await Input(true);
        CheckDetected("initial input");
        for (int i = 0; i < 3; i++)
        {
            host.ReturnToZone();
            await Input(true);
            host.EnterStudio(0);
            CheckDetected("return from Controller Zone");
        }
        // Use the real menu entry path: a still-connected session must not open another scan.
        host.ReturnToZone();
        ReviewSandbox.Active = false;
        try { Call("VerifyThenEnter", 0); }
        finally { ReviewSandbox.Active = true; }
        if (host.Studio == null) throw new InvalidOperationException("Reopening a connected controller started verification again.");
        CheckDetected("menu entry");
        host.ReturnToZone();
        await Input(false);
        host.EnterStudio(0);
        if (host.Studio!.Dashboard!.RebelConnection.Text.StartsWith("Detected"))
            throw new InvalidOperationException("A real disconnect was remembered as connected.");
        await Input(true);
        CheckDetected("reconnect");
        host.ReturnToZone();
        host.OpenSelector(0);
        host.AssignSelection(host.Catalog.Get("x20_pro"));
        host.EnterStudio(0);
        if (host.Studio!.Dashboard!.RebelConnection.Text.StartsWith("Detected"))
            throw new InvalidOperationException("Replacing an assignment retained its old connection.");
        await Input(true);
        host.ReturnToZone();
        host.OpenSelector(0);
        host.AssignSelection(host.Catalog.Get("x20"));
        host.EnterStudio(0);
        if (host.Studio!.Dashboard!.RebelConnection.Text.StartsWith("Detected"))
            throw new InvalidOperationException("A different model inherited the previous connection.");
        await Input(true);
        await host.StopNativeEngineAsync();
        host.ReturnToZone(); host.EnterStudio(0);
        if (host.Studio!.Dashboard!.RebelConnection.Text.StartsWith("Detected"))
            throw new InvalidOperationException("Engine shutdown retained a stale connection.");
        host.MouseMode?.Dispose(); host.PcMode?.Dispose();
        File.WriteAllText(Path.Combine(directory, "result.txt"), "PASS: live connection survives three Zone round trips and menu entry; disconnect, reconnect, assignment replacement, model change, and engine shutdown handled.");

        void CheckDetected(string step)
        {
            string text = host.Studio!.Dashboard!.RebelConnection.Text;
            if (text != "Detected · 2.4 GHz receiver") throw new InvalidOperationException(step + ": " + text);
        }
        void Set(string name, object value) => typeof(ProductHost).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(host, value);
        void Call(string name, object value) => typeof(ProductHost).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(host, [value]);
        async Task Input(bool connected)
        {
            var message = JsonSerializer.SerializeToElement(new
            {
                engineEpoch = "connection-review", subscriptionId = "review-input", inputContext = "UI_NAVIGATION",
                sequence = ++sequence, timestampUs = sequence * 10000,
                payload = new { connected, slot = 0, buttons = Array.Empty<string>(), leftX = 0, leftY = 0, rightX = 0, rightY = 0, lt = 0, rt = 0 }
            });
            Call("QueueNativeEvent", message);
            await host.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
        }
    }
}
