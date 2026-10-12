using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using X20Ctl.Zone.Agent;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Product.Agent;

/// <summary>
/// Talk (class project, 12 Oct 2026): the controller talks back. A sentence goes to the local model, its reply is checked
/// against the agent's fixed rules, and only a valid one changes the settings, straight away, with an Undo. The model's
/// words are shown as what it proposed; the line under them is the app saying what really happened.
/// </summary>
public sealed class TalkPage
{
    static readonly string[] Suggestions = ["Make my sticks smooth", "Faster triggers for shooting", "My right stick drifts a little", "Turbo on X, 12 presses a second", "Put jump on M1", "Reset everything"];
    // one model server and one loaded model for the whole app, however often the tab is opened
    static readonly OllamaClient model = new();
    static Task<ModelState>? preparing;
    static ModelState state = ModelState.Starting;
    static string stateText = "";
    static event Action? StateChanged;

    public Grid Body { get; } = new();
    public TextBox Input { get; }
    readonly string modelId;
    readonly PcModeLink? link;
    readonly AgentSession session;
    readonly StackPanel transcript = new() { Margin = new(0, 0, 14, 0) };
    readonly ScrollViewer scroller;
    readonly Button send;
    readonly TextBlock status = Kit.Text("", 14, "DS.TextSoft");
    readonly Border statusDot = new() { Width = 9, Height = 9, CornerRadius = new(5), Margin = new(0, 0, 9, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly StackPanel settings = new();
    readonly TextBlock target = Kit.Text("", 14, "DS.TextMuted").Also(t => { t.TextWrapping = TextWrapping.Wrap; t.TextTrimming = TextTrimming.None; });
    PcModeProfile preview = new(); // the settings when PC mode isn't reachable from this window
    bool busy;

    public TalkPage(string modelId, string controllerName, PcModeLink? link, Action<Button> register)
    {
        this.modelId = modelId; this.link = link;
        session = new AgentSession(model, modelId, controllerName.Replace("EasySMX ", ""));
        Body.ColumnDefinitions.Add(new() { Width = new(1.55, GridUnitType.Star) }); Body.ColumnDefinitions.Add(new() { Width = new(24) }); Body.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 320 });
        Body.Margin = new(30, 6, 30, 18);

        // left: the conversation
        var left = new DockPanel();
        var head = new DockPanel { Margin = new(0, 0, 0, 12) };
        var dot = new StackPanel { Orientation = Orientation.Horizontal, Children = { statusDot, status } }; DockPanel.SetDock(dot, Dock.Right); head.Children.Add(dot);
        head.Children.Add(Kit.Text("Talk", 30, "DS.Text", FontWeights.Bold));
        DockPanel.SetDock(head, Dock.Top); left.Children.Add(head);

        Input = new TextBox { AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, MinHeight = 52, MaxHeight = 120, MaxLength = 600, FontSize = 16, Padding = new(14, 13, 14, 12), VerticalContentAlignment = VerticalAlignment.Center, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Input.KeyDown += (_, e) => { if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None) { e.Handled = true; Send(); } };
        send = new Button { Style = DS.Style("DS.ActionPrimary"), Height = 52, MinHeight = 0, Padding = new(26, 0, 26, 0), Margin = new(10, 0, 0, 0), Content = Kit.Text("Send", 16, "DS.Text", FontWeights.SemiBold) };
        send.Click += (_, _) => Send(); register(send);
        var row = new DockPanel { Margin = new(0, 12, 0, 0) }; DockPanel.SetDock(send, Dock.Right); row.Children.Add(send); row.Children.Add(Input);
        var chips = new WrapPanel { Margin = new(0, 12, 0, 0) };
        foreach (var s in Suggestions)
        {
            var chip = new Button { Style = DS.Style("DS.ActionSecondary"), Height = 34, MinHeight = 0, Padding = new(14, 0, 14, 0), Margin = new(0, 0, 8, 8), Content = Kit.Text(s, 14, "DS.TextSoft") };
            chip.Click += (_, _) => { Input.Text = s; Send(); }; register(chip); chips.Children.Add(chip);
        }
        var bottom = new StackPanel { Children = { chips, row } }; DockPanel.SetDock(bottom, Dock.Bottom); left.Children.Add(bottom);
        scroller = new ScrollViewer { Content = transcript, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        left.Children.Add(scroller);
        Body.Children.Add(left);

        // right: what the settings are now
        var right = new StackPanel { Children = { Kit.Text("Settings now", 18.5, "DS.Text", FontWeights.SemiBold), target.Also(t => t.Margin = new(0, 4, 0, 12)), settings } };
        var panel = Kit.Panel(null, null, null, new ScrollViewer { Content = right, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); Grid.SetColumn(panel, 2); Body.Children.Add(panel);

        Say(controllerName.Replace("EasySMX ", "") + " here. Tell me how you play, or what feels wrong, and I'll set myself up.", null);
        ShowSettings(new HashSet<string>());
        StateChanged += ShowState; Body.Unloaded += (_, _) => StateChanged -= ShowState; Body.Loaded += (_, _) => { StateChanged -= ShowState; StateChanged += ShowState; };
        ShowState();
        preparing ??= model.Prepare((s, text) => Application.Current.Dispatcher.BeginInvoke(() => { state = s; stateText = text; StateChanged?.Invoke(); }));
    }

    PcModeProfile Current => link == null ? preview : link.Store.Get(PcModePage.StoreKey);

    void Store(PcModeProfile next)
    {
        if (link == null) { preview = next; return; }
        link.Store.Save(PcModePage.StoreKey, next); link.Service.Reload(); PcModePage.Replaced();
    }

    void ShowState()
    {
        status.Text = state == ModelState.Starting && stateText.Length == 0 ? "Starting…" : stateText;
        statusDot.Background = DS.Brush(state switch { ModelState.Ready => "DS.Success", ModelState.Starting or ModelState.Loading => "DS.Warn", _ => "DS.Danger" });
        send.IsEnabled = !busy && state == ModelState.Ready;
    }

    async void Send() { if (Input.Text.Trim() is { Length: > 0 } sentence && !busy) { Input.Text = ""; await Ask(sentence); } }

    /// <summary>The whole round: the sentence, the model, the checks, and (when valid) the settings changed. Returns the
    /// turn, or null when the model couldn't be reached.</summary>
    public async Task<AgentTurn?> Ask(string sentence)
    {
        if (busy) return null;
        if (state != ModelState.Ready) { Bubble(sentence, mine: true); Say(state == ModelState.Missing ? stateText : "Not ready yet: " + stateText, null); return null; }
        busy = true; ShowState();
        Bubble(sentence, mine: true);
        var thinking = Bubble("…", mine: false);
        var before = Current;
        try
        {
            var turn = await session.Ask(sentence, before);
            transcript.Children.Remove(thinking);
            if (turn.Outcome.Next is { } next && turn.Outcome.Changes.Count > 0)
            {
                try { Store(next); Say(turn, before); ShowSettings(turn.Outcome.Changes.Select(c => c.What).ToHashSet()); }
                catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException) { Say(turn, null, "Couldn't save: " + error.Message); }
            }
            else Say(turn, null);
            return turn;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            transcript.Children.Remove(thinking);
            Say(error is TaskCanceledException ? "I took too long to answer. Try again." : "I couldn't reach the model: " + error.Message, null);
            return null;
        }
        finally { busy = false; ShowState(); Input.Focus(); }
    }
    public static ModelState State => state;
    public PcModeProfile Settings => Current;
    /// <summary>Opens every answer's details (validation and model output), for review captures.</summary>
    public void ExpandDetails() { foreach (var b in transcript.Children.OfType<Border>()) if (b.Child is StackPanel { Children.Count: > 1 } s && s.Children[^1] is StackPanel d) d.Visibility = Visibility.Visible; }
    public void ScrollTo(double offset) { Body.UpdateLayout(); scroller.ScrollToVerticalOffset(offset); }

    // ---- the conversation ----
    Border Bubble(string text, bool mine)
    {
        var bubble = new Border
        {
            CornerRadius = new(16), Padding = new(16, 12, 16, 12), Margin = mine ? new(80, 6, 0, 6) : new(0, 6, 60, 6), MaxWidth = 760,
            HorizontalAlignment = mine ? HorizontalAlignment.Right : HorizontalAlignment.Left, BorderThickness = new(1),
            Background = new SolidColorBrush(mine ? Color.FromArgb(150, 36, 82, 160) : Color.FromArgb(140, 14, 24, 48)),
            BorderBrush = new SolidColorBrush(mine ? Color.FromArgb(120, 125, 182, 255) : Color.FromArgb(60, 120, 170, 255)),
            Child = Wrap(text, 16, "DS.Text"),
        };
        transcript.Children.Add(bubble); Fx.Appear(bubble, .97, 200); scroller.ScrollToEnd(); return bubble;
    }

    void Say(string text, string? detail) { var b = Bubble(text, false); if (detail != null) ((TextBlock)b.Child).Text += "\n" + detail; }

    /// <summary>One answer: the model's words, then what the app did, then the details on request.</summary>
    void Say(AgentTurn turn, PcModeProfile? undo, string? failure = null)
    {
        var stack = new StackPanel();
        var v = turn.Verdict;
        stack.Children.Add(Wrap(v.Proposal?.Speech is { Length: > 0 } speech ? speech : "I didn't give a usable answer.", 16, "DS.Text"));
        foreach (var c in turn.Outcome.Changes)
            stack.Children.Add(Wrap($"{c.What}   {c.Before} → {c.After}", 14.5, "DS.AccentHi", 6));
        foreach (var skipped in v.Checks.Where(c => c.State == CheckState.Unavailable))
            stack.Children.Add(Wrap($"{skipped.Action.Target}: {skipped.Message}", 14, "DS.Warn", 6));
        // the app's own line: the truth about what happened, whatever the model said
        string verdict = failure ?? (turn.Outcome.Next != null && turn.Outcome.Changes.Count > 0 ? $"Applied · {turn.Outcome.Changes.Count} change{(turn.Outcome.Changes.Count == 1 ? "" : "s")}{(link == null ? " · preview only" : "")}"
            : !v.Valid ? "Rejected · nothing changed · " + AgentSession.Problems(v)
            : turn.Outcome.Error != null ? "Rejected · " + turn.Outcome.Error : "No changes");
        stack.Children.Add(Wrap(verdict, 14, failure != null || !v.Valid || turn.Outcome.Error != null ? "DS.Danger" : turn.Outcome.Changes.Count > 0 ? "DS.Success" : "DS.TextMuted", 10).Also(t => t.FontWeight = FontWeights.SemiBold));

        var details = new StackPanel { Visibility = Visibility.Collapsed, Margin = new(0, 10, 0, 0) };
        details.Children.Add(Kit.Text("VALIDATION", 12, "DS.TextMuted", FontWeights.SemiBold));
        if (v.StructureError != null) details.Children.Add(Wrap("✕ " + v.StructureError, 13.5, "DS.Danger", 4));
        foreach (var c in v.Checks)
            details.Children.Add(Wrap($"{(c.State == CheckState.Ok ? "✓" : c.State == CheckState.Rejected ? "✕" : "–")} {c.Action.Action} · {c.Action.Target} · {c.Action.ValueText} — {c.Message}", 13.5, c.State == CheckState.Ok ? "DS.TextSoft" : c.State == CheckState.Rejected ? "DS.Danger" : "DS.Warn", 4));
        if (v.StructureError == null && v.Checks.Count == 0) details.Children.Add(Wrap("No actions proposed.", 13.5, "DS.TextSoft", 4));
        foreach (var a in v.Proposal?.Assumptions ?? []) details.Children.Add(Wrap("Assumed: " + a, 13.5, "DS.TextSoft", 4));
        for (int i = 0; i < turn.Attempts.Count; i++)
        {
            details.Children.Add(Kit.Text(turn.Attempts.Count > 1 ? $"MODEL OUTPUT · ATTEMPT {i + 1}" : "MODEL OUTPUT", 12, "DS.TextMuted", FontWeights.SemiBold).Also(t => t.Margin = new(0, 10, 0, 4)));
            details.Children.Add(new TextBox { Text = turn.Attempts[i].Content, IsReadOnly = true, FontFamily = new FontFamily("Consolas"), FontSize = 12.5, TextWrapping = TextWrapping.Wrap, MaxHeight = 220, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new(10) });
        }

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 10, 0, 0) };
        actions.Children.Add(Kit.Text(AgentSession.Stats(turn), 13, "DS.TextMuted").Also(t => t.Margin = new(0, 0, 14, 0)));
        var more = Small("Details"); more.Click += (_, _) => { bool show = details.Visibility != Visibility.Visible; details.Visibility = show ? Visibility.Visible : Visibility.Collapsed; ((TextBlock)more.Content).Text = show ? "Hide details" : "Details"; };
        actions.Children.Add(more);
        if (undo != null)
        {
            var back = Small("Undo"); back.Margin = new(8, 0, 0, 0);
            back.Click += (_, _) =>
            {
                try { var now = Current; Store(undo); back.IsEnabled = false; ((TextBlock)back.Content).Text = "Undone"; ShowSettings(AgentApplier.Diff(now, undo).Select(c => c.What).ToHashSet()); }
                catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException) { ((TextBlock)back.Content).Text = "Couldn't undo"; }
            };
            actions.Children.Add(back);
        }
        stack.Children.Add(actions); stack.Children.Add(details);
        var bubble = Bubble("", false); bubble.Child = stack; scroller.ScrollToEnd();
    }

    // ---- the settings panel ----
    void ShowSettings(IReadOnlySet<string> changed)
    {
        target.Text = link == null ? "Preview · PC mode isn't available in this window"
            : $"PC mode · Player {PcModePage.Player + 1}, {(PcModePage.ProfileKey == PcModeStore.DefaultKey ? "every game" : PcModePage.ProfileKey[PcModeStore.GamePrefix.Length..])}{(link.Service.Enabled ? "" : " · PC mode is off, so games don't see these yet")}";
        settings.Children.Clear();
        var rows = AgentApplier.Diff(new PcModeProfile(), Current);
        if (rows.Count == 0) settings.Children.Add(Wrap("Everything is at its default.", 15, "DS.TextSoft"));
        foreach (var r in rows)
        {
            bool fresh = changed.Contains(r.What);
            var line = new DockPanel { Margin = new(0, 0, 0, 8) };
            var value = Kit.Text(r.After, 15, fresh ? "DS.AccentHi" : "DS.Text", FontWeights.SemiBold); DockPanel.SetDock(value, Dock.Right); line.Children.Add(value);
            line.Children.Add(Kit.Text(r.What, 15, "DS.TextSoft"));
            settings.Children.Add(line);
            if (fresh) Fx.Appear(line, 1.04, 320);
        }
        var paddles = AgentVocabulary.Paddles(modelId);
        if (paddles.Length > 0) settings.Children.Add(Wrap($"Paddles {paddles[0]}–{paddles[^1]} are programmed on the controller, which isn't supported yet.", 13.5, "DS.TextMuted", 14));
    }

    static Button Small(string text) => new() { Style = DS.Style("DS.ActionSecondary"), Height = 30, MinHeight = 0, Padding = new(12, 0, 12, 0), Content = Kit.Text(text, 13, "DS.TextSoft") };
    static TextBlock Wrap(string text, double size, string brush, double top = 0) => Kit.Text(text, size, brush).Also(t => { t.TextWrapping = TextWrapping.Wrap; t.TextTrimming = TextTrimming.None; t.Margin = new(0, top, 0, 0); });
}
