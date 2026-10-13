using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using X20Ctl.Zone.Agent;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Product.Agent;

/// <summary>
/// The agent (class project, 12 Oct 2026): a small window that opens from the round button over the Studio. A sentence
/// goes to the local model, its reply is checked against the agent's fixed rules, and only a valid one changes the
/// settings, straight away, with an Undo. The model's words are what it proposed; the line under them is the app saying
/// what really happened. The window can be dragged by its title and put away without losing the conversation.
/// </summary>
public sealed class AgentPanel
{
    static readonly string[] Suggestions = ["Make my sticks smooth", "Faster triggers for shooting", "My right stick drifts a little", "Turbo on X, 12 a second", "Put jump on M1", "Reset everything"];
    // one model server and one loaded model for the whole app, however many windows open
    static readonly OllamaClient model = new();
    static Task<ModelState>? preparing;
    static ModelState state = ModelState.Starting;
    // offline mode: the phrasebook agent, chosen with the switch, or used when the model can't run on this PC at all
    static readonly OfflineAgent offlineAgent = new();
    static bool offlineChosen;
    public static bool Offline => offlineChosen || state is ModelState.NotInstalled or ModelState.Missing or ModelState.Failed;
    /// <summary>Asks whichever agent is in use now, so the switch works mid-conversation.</summary>
    sealed class Either : IAgentModel
    {
        public string Name => Offline ? offlineAgent.Name : model.Name;
        public Task<ModelReply> Chat(IReadOnlyList<ChatMessage> messages, string schema, CancellationToken cancel) => (Offline ? offlineAgent : (IAgentModel)model).Chat(messages, schema, cancel);
    }
    static string stateText = "";
    /// <summary>The model's readiness changed (starting, loading, ready, missing…).</summary>
    public static event Action? StateChanged;
    /// <summary>Starts Ollama and loads the model in the background, once, so the agent is ready when it's opened.</summary>
    public static void Prepare() => preparing ??= model.Prepare((s, text) => Application.Current.Dispatcher.BeginInvoke(() => { state = s; stateText = text; StateChanged?.Invoke(); }));

    public Border Body { get; }
    public TextBox Input { get; }
    /// <summary>Minimise or close was chosen: the host puts the window away (the conversation stays).</summary>
    public event Action? CloseRequested;
    /// <summary>Plays the walk-through of a change (before, after) on the host's pages, and returns when it is over.</summary>
    public Func<PcModeProfile, PcModeProfile, DriftReport?, Task>? Walkthrough { get; set; }
    /// <summary>The agent started or stopped thinking.</summary>
    public event Action<bool>? Thinking;
    readonly string modelId, greeting;
    readonly PcModeLink? link;
    readonly AgentSession session;
    readonly StackPanel transcript = new() { Margin = new(0, 0, 10, 0) };
    readonly ScrollViewer scroller;
    readonly Button send;
    readonly WrapPanel chips = new() { Margin = new(0, 10, 0, 0) };
    readonly TextBlock status = Kit.Text("", 13, "DS.TextSoft");
    readonly Border statusDot = new() { Width = 8, Height = 8, CornerRadius = new(4), Margin = new(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly StackPanel settings = new();
    readonly Border settingsBox;
    readonly TextBlock settingsToggle = Kit.Text("", 13.5, "DS.TextSoft");
    readonly TextBlock target = Kit.Text("", 13, "DS.TextMuted").Also(t => { t.TextWrapping = TextWrapping.Wrap; t.TextTrimming = TextTrimming.None; });
    readonly TranslateTransform offset = new();
    readonly Button offlineSwitch;
    readonly ScaleTransform pop = new(1, 1);
    PcModeProfile preview = new(); // the settings when PC mode isn't reachable from this window
    bool busy;

    public AgentPanel(string modelId, string controllerName, PcModeLink? link, Action<Button> register, int player = 0, Func<Action<string, int>, IDriftMeter>? meter = null)
    {
        this.modelId = modelId; this.link = link;
        string name = controllerName.Replace("EasySMX ", "");
        // drift is read from the real controller; a review harness can hand in its own reading instead
        session = new AgentSession(new Either(), modelId, name) { Meter = (meter ?? (progress => new XInputDriftMeter(player, progress)))(Measuring) };
        greeting = "Agent XCTL, in your " + name + ". Tell me how you play, or what feels wrong, and I'll set myself up.";

        // title bar: the mark, the name, the model's state; drag it to move the window
        var mark = new Grid { Width = 40, Height = 40, Margin = new(0, 0, 12, 0), Children = { new System.Windows.Shapes.Ellipse { Fill = AgentMark.Field() }, AgentMark.Glyph(26) } };
        var title = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { Kit.Text("Agent XCTL", 18, "DS.Text", FontWeights.SemiBold), new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 2, 0, 0), Children = { statusDot, status } } } };
        var fresh = Chrome("", "New conversation"); fresh.Click += (_, _) => Restart();
        offlineSwitch = new Button { Style = DS.Style("DS.ActionSecondary"), Height = 34, MinHeight = 0, Padding = new(12, 0, 12, 0), Margin = new(6, 0, 0, 0), Content = Kit.Text("Offline", 13, "DS.TextSoft") };
        offlineSwitch.Click += (_, _) => { offlineChosen = !offlineChosen; StateChanged?.Invoke(); };
        register(offlineSwitch);
        var away = Chrome("", "Minimise"); away.Click += (_, _) => CloseRequested?.Invoke();
        var close = Chrome("", "Close"); close.Click += (_, _) => CloseRequested?.Invoke();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Children = { offlineSwitch, fresh, away, close } };
        var bar = new DockPanel { Background = Brushes.Transparent, Cursor = Cursors.SizeAll, Margin = new(0, 0, 0, 12) };
        DockPanel.SetDock(buttons, Dock.Right); bar.Children.Add(buttons); DockPanel.SetDock(mark, Dock.Left); bar.Children.Add(mark); bar.Children.Add(title);
        Drag(bar);

        // what the settings are now, folded away until asked for
        var toggle = new Button { Style = DS.Style("DS.ActionSecondary"), Height = 34, MinHeight = 0, Padding = new(14, 0, 14, 0), HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Content = settingsToggle };
        settingsBox = new Border { Visibility = Visibility.Collapsed, Margin = new(0, 8, 0, 0), Padding = new(14, 10, 14, 6), CornerRadius = new(12), Background = new SolidColorBrush(Color.FromArgb(90, 4, 10, 26)), BorderBrush = new SolidColorBrush(Color.FromArgb(50, 120, 170, 255)), BorderThickness = new(1),
            Child = new ScrollViewer { MaxHeight = 210, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new StackPanel { Children = { target.Also(t => t.Margin = new(0, 0, 0, 8)), settings } } } };
        toggle.Click += (_, _) => { settingsBox.Visibility = settingsBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; ShowSettings(new HashSet<string>()); };
        register(toggle);
        var top = new StackPanel { Margin = new(0, 0, 0, 10), Children = { bar, toggle, settingsBox } };

        Input = new TextBox { AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, MinHeight = 48, MaxHeight = 110, MaxLength = 600, FontSize = 15.5, Padding = new(14, 12, 14, 11), VerticalContentAlignment = VerticalAlignment.Center, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Input.KeyDown += (_, e) => { if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None) { e.Handled = true; Send(); } };
        send = new Button { Style = DS.Style("DS.ActionPrimary"), Width = 48, Height = 48, MinHeight = 0, MinWidth = 0, Padding = new(0), Margin = new(8, 0, 0, 0), ToolTip = "Send", Content = Kit.Icon("", 17, "DS.Text") };
        send.Click += (_, _) => Send(); register(send);
        var row = new DockPanel { Margin = new(0, 10, 0, 0) }; DockPanel.SetDock(send, Dock.Right); row.Children.Add(send); row.Children.Add(Input);
        foreach (var s in Suggestions)
        {
            var chip = new Button { Style = DS.Style("DS.ActionSecondary"), Height = 30, MinHeight = 0, Padding = new(12, 0, 12, 0), Margin = new(0, 0, 6, 6), Content = Kit.Text(s, 13, "DS.TextSoft") };
            chip.Click += (_, _) => { Input.Text = s; Send(); }; register(chip); chips.Children.Add(chip);
        }
        var bottom = new StackPanel { Children = { chips, row } };

        scroller = new ScrollViewer { Content = transcript, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var layout = new DockPanel();
        DockPanel.SetDock(top, Dock.Top); layout.Children.Add(top); DockPanel.SetDock(bottom, Dock.Bottom); layout.Children.Add(bottom); layout.Children.Add(scroller);

        Body = new Border
        {
            Width = 520, CornerRadius = new(22), BorderThickness = new(1), Padding = new(20, 16, 20, 18), Child = layout,
            Background = new LinearGradientBrush(Color.FromArgb(250, 14, 24, 52), Color.FromArgb(250, 7, 12, 30), 90),
            BorderBrush = new LinearGradientBrush(Color.FromArgb(130, 120, 170, 255), Color.FromArgb(40, 120, 170, 255), 90),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 40, ShadowDepth = 10, Opacity = .55 },
            RenderTransform = new TransformGroup { Children = { pop, offset } }, RenderTransformOrigin = new(1, 1),
        };
        Say(greeting, null);
        ShowSettings(new HashSet<string>());
        StateChanged += ShowState; Body.Unloaded += (_, _) => StateChanged -= ShowState; Body.Loaded += (_, _) => { StateChanged -= ShowState; StateChanged += ShowState; };
        ShowState(); Prepare();
    }

    static Button Chrome(string glyph, string tip) => new() { Style = DS.Style("DS.ActionSecondary"), Width = 34, Height = 34, MinHeight = 0, MinWidth = 0, Padding = new(0), Margin = new(6, 0, 0, 0), ToolTip = tip, Content = Kit.Icon(glyph, 13, "DS.TextSoft") };

    /// <summary>Move the window by its title bar, kept inside its host.</summary>
    void Drag(FrameworkElement handle)
    {
        Point? from = null; double x0 = 0, y0 = 0;
        handle.MouseLeftButtonDown += (_, e) => { if (e.OriginalSource is DependencyObject d && OnButton(d)) return; from = e.GetPosition(null); x0 = offset.X; y0 = offset.Y; handle.CaptureMouse(); e.Handled = true; };
        handle.MouseMove += (_, e) =>
        {
            if (from == null || Body.Parent is not FrameworkElement host) return;
            var now = e.GetPosition(null);
            // the window sits at the host's bottom right; it may travel left and up until it meets the host's edge
            double maxLeft = host.ActualWidth - Body.ActualWidth - Body.Margin.Right, maxUp = host.ActualHeight - Body.ActualHeight - Body.Margin.Bottom;
            offset.X = Math.Clamp(x0 + now.X - from.Value.X, -Math.Max(0, maxLeft), Body.Margin.Right - 8);
            offset.Y = Math.Clamp(y0 + now.Y - from.Value.Y, -Math.Max(0, maxUp), Body.Margin.Bottom - 8);
        };
        handle.MouseLeftButtonUp += (_, _) => { from = null; handle.ReleaseMouseCapture(); };
        static bool OnButton(DependencyObject d) { for (var e = d; e != null; e = e is Visual ? VisualTreeHelper.GetParent(e) : LogicalTreeHelper.GetParent(e)) if (e is Button) return true; return false; }
    }

    /// <summary>Opens out of the round button (the window's bottom-right corner), and folds back into it.</summary>
    public void Show()
    {
        Body.Visibility = Visibility.Visible; Body.IsHitTestVisible = true;
        if (!DS.Motion) { Body.Opacity = 1; pop.ScaleX = pop.ScaleY = 1; return; }
        Body.BeginAnimation(UIElement.OpacityProperty, DS.To(1, 180));
        pop.BeginAnimation(ScaleTransform.ScaleXProperty, new System.Windows.Media.Animation.DoubleAnimation(.6, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = DS.Spring });
        pop.BeginAnimation(ScaleTransform.ScaleYProperty, new System.Windows.Media.Animation.DoubleAnimation(.6, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = DS.Spring });
    }
    public void Hide()
    {
        Body.IsHitTestVisible = false;
        if (!DS.Motion) { Body.Visibility = Visibility.Collapsed; return; }
        var fade = DS.To(0, 160); fade.Completed += (_, _) => { if (!Body.IsHitTestVisible) Body.Visibility = Visibility.Collapsed; };
        Body.BeginAnimation(UIElement.OpacityProperty, fade);
        pop.BeginAnimation(ScaleTransform.ScaleXProperty, DS.To(.6, 160)); pop.BeginAnimation(ScaleTransform.ScaleYProperty, DS.To(.6, 160));
    }
    public bool IsShown => Body.Visibility == Visibility.Visible && Body.IsHitTestVisible;

    void Restart()
    {
        if (busy) return;
        session.Forget(); transcript.Children.Clear(); chips.Visibility = Visibility.Visible; Say(greeting, null); Input.Focus();
    }

    PcModeProfile Current => link == null ? preview : link.Store.Get(PcModePage.StoreKey);

    void Store(PcModeProfile next)
    {
        if (link == null) { preview = next; return; }
        link.Store.Save(PcModePage.StoreKey, next); link.Service.Reload(); PcModePage.Replaced();
    }

    void ShowState()
    {
        status.Text = Offline ? (offlineChosen ? "Offline · phrasebook" : "Offline · phrasebook (the model isn't available)") : state == ModelState.Starting && stateText.Length == 0 ? "Starting…" : stateText;
        statusDot.Background = DS.Brush(Offline ? "DS.AccentHi" : state switch { ModelState.Ready => "DS.Success", ModelState.Starting or ModelState.Loading => "DS.Warn", _ => "DS.Danger" });
        send.IsEnabled = !busy && (Offline || state == ModelState.Ready);
        if (offlineChosen) offlineSwitch.Background = new SolidColorBrush(Color.FromArgb(150, 36, 82, 160)); else offlineSwitch.ClearValue(Control.BackgroundProperty);
        ((TextBlock)offlineSwitch.Content).Foreground = DS.Brush(offlineChosen ? "DS.Text" : "DS.TextSoft");
        offlineSwitch.ToolTip = offlineChosen ? "Back to the model (Qwen)" : "Offline: answers from a phrasebook, no model";
    }
    public static string StateText => stateText;

    async void Send() { if (Input.Text.Trim() is { Length: > 0 } sentence && !busy) { Input.Text = ""; await Ask(sentence); } }

    /// <summary>The whole round: the sentence, the model, the checks, and (when valid) the settings changed. Returns the
    /// turn, or null when the model couldn't be reached.</summary>
    public async Task<AgentTurn?> Ask(string sentence)
    {
        if (busy) return null;
        if (!Offline && state != ModelState.Ready) { Bubble(sentence, mine: true); Say(state == ModelState.Missing ? stateText : "Not ready yet: " + stateText, null); return null; }
        busy = true; ShowState(); Thinking?.Invoke(true); chips.Visibility = Visibility.Collapsed;
        Bubble(sentence, mine: true);
        var thinking = Bubble("…", mine: false);
        var before = Current;
        try
        {
            var turn = await session.Ask(sentence, before);
            transcript.Children.Remove(thinking);
            // PC mode on or off, when asked: the switch itself, not a setting
            string? switched = turn.Outcome.PcMode is { } on ? (link == null ? "PC mode isn't reachable from this window." : PcModePage.SwitchFromAgent(link, on) ?? "") : null;
            if (turn.Outcome.Next is { } next && turn.Outcome.Changes.Count > 0)
            {
                try { Store(next); }
                catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException) { Say(turn, null, "Couldn't save: " + error.Message); return turn; }
                // the walk-through first, so the answer and its recap are waiting when the window comes back
                if (Walkthrough != null) await Walkthrough(before, next, turn.Measured);
                Say(turn, before, switched: switched); ShowSettings(turn.Outcome.Changes.Select(c => c.What).ToHashSet());
            }
            else { Say(turn, null, switched: switched); if (switched != null) ShowSettings(new HashSet<string>()); }
            return turn;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            transcript.Children.Remove(thinking);
            Say(error is TaskCanceledException ? "I took too long to answer. Try again." : "I couldn't reach the model: " + error.Message, null);
            return null;
        }
        finally { busy = false; if (measuring != null) { transcript.Children.Remove(measuring); measuring = null; } ShowState(); Thinking?.Invoke(false); Input.Focus(); }
    }

    // ---- the drift reading: a countdown while the sticks are read ----
    Border? measuring;
    void Measuring(string phase, int seconds)
    {
        if (measuring == null)
        {
            measuring = Bubble("", mine: false); measuring.BorderBrush = DS.Brush("DS.Warn");
            measuring.Child = new StackPanel { Children = { Kit.Text("", 16, "DS.Text", FontWeights.SemiBold), Wrap("Hands off the controller: I'm reading where the sticks rest.", 13.5, "DS.TextSoft", 4) } };
        }
        ((TextBlock)((StackPanel)measuring.Child).Children[0]).Text = $"{phase} · {seconds}";
        scroller.ScrollToEnd();
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
            CornerRadius = new(16), Padding = new(14, 10, 14, 10), Margin = mine ? new(50, 5, 0, 5) : new(0, 5, 24, 5),
            HorizontalAlignment = mine ? HorizontalAlignment.Right : HorizontalAlignment.Left, BorderThickness = new(1),
            Background = new SolidColorBrush(mine ? Color.FromArgb(150, 36, 82, 160) : Color.FromArgb(140, 14, 24, 48)),
            BorderBrush = new SolidColorBrush(mine ? Color.FromArgb(120, 125, 182, 255) : Color.FromArgb(60, 120, 170, 255)),
            Child = Wrap(text, 15, "DS.Text"),
        };
        transcript.Children.Add(bubble); Fx.Appear(bubble, .97, 200); scroller.ScrollToEnd(); return bubble;
    }

    void Say(string text, string? detail) { var b = Bubble(text, false); if (detail != null) ((TextBlock)b.Child).Text += "\n" + detail; }

    /// <summary>One answer: the model's words, then what the app did, then the details on request.</summary>
    void Say(AgentTurn turn, PcModeProfile? undo, string? failure = null, string? switched = null)
    {
        var stack = new StackPanel();
        var v = turn.Verdict;
        stack.Children.Add(Wrap(v.Proposal?.Speech is { Length: > 0 } speech ? speech : "I didn't give a usable answer.", 15, "DS.Text"));
        // what the app itself measured, before what the model made of it
        if (turn.Measured is { } m)
        {
            if (m.Problem != null) stack.Children.Add(Wrap("Not measured · " + m.Problem, 14, "DS.Warn", 8));
            foreach (var d in m.Sticks)
                stack.Children.Add(Wrap(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Measured · {AgentApplier.Name(d.Stick)} rests at {d.RestX:+0.0;-0.0;0.0}%, {d.RestY:+0.0;-0.0;0.0}% and wanders up to {d.Peak:0.0}%"), 14, "DS.Warn", 8));
        }
        foreach (var c in turn.Outcome.Changes)
            stack.Children.Add(Wrap($"{c.What}   {c.Before} → {c.After}", 14, "DS.AccentHi", 6));
        // why it fits, or the answer to a question: the model's words, from the settings' meanings
        if (v.Valid && v.Proposal?.Recap is { Length: > 0 } recap)
            stack.Children.Add(new Border { BorderBrush = DS.Brush("DS.AccentHi"), BorderThickness = new(2, 0, 0, 0), Padding = new(12, 2, 0, 2), Margin = new(0, 10, 0, 0), Child = Wrap(recap, 14.5, "DS.TextSoft") });
        // the switch: "" when it worked, otherwise why not
        if (switched != null && turn.Outcome.PcMode is { } pc)
            stack.Children.Add(Wrap(switched.Length == 0 ? $"PC mode   {(pc ? "on" : "off")}" : "PC mode · " + switched, 14, switched.Length == 0 ? "DS.AccentHi" : "DS.Warn", 6));
        foreach (var skipped in v.Checks.Where(c => c.State == CheckState.Unavailable))
            stack.Children.Add(Wrap($"{skipped.Action.Target}: {skipped.Message}", 14, "DS.Warn", 6));
        // the app's own line: the truth about what happened, whatever the model said
        string verdict = failure ?? (turn.Outcome.Next != null && turn.Outcome.Changes.Count == 0 && switched == "" ? "Applied · PC mode switched " + (turn.Outcome.PcMode == true ? "on" : "off")
            : turn.Outcome.Next != null && turn.Outcome.Changes.Count > 0 ? $"Applied · {turn.Outcome.Changes.Count} change{(turn.Outcome.Changes.Count == 1 ? "" : "s")}{(link == null ? " · preview only" : "")}"
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
        settingsToggle.Text = (rows.Count == 0 ? "Settings now · all default" : $"Settings now · {rows.Count} changed") + (settingsBox.Visibility == Visibility.Visible ? "  ⌃" : "  ⌄");
        if (rows.Count == 0) settings.Children.Add(Wrap("Everything is at its default.", 14, "DS.TextSoft"));
        foreach (var r in rows)
        {
            bool fresh = changed.Contains(r.What);
            var line = new DockPanel { Margin = new(0, 0, 0, 6) };
            var value = Kit.Text(r.After, 14, fresh ? "DS.AccentHi" : "DS.Text", FontWeights.SemiBold); DockPanel.SetDock(value, Dock.Right); line.Children.Add(value);
            line.Children.Add(Kit.Text(r.What, 14, "DS.TextSoft"));
            settings.Children.Add(line);
            if (fresh) Fx.Appear(line, 1.04, 320);
        }
        var paddles = AgentVocabulary.Paddles(modelId);
        if (paddles.Length > 0) settings.Children.Add(Wrap($"Paddles {paddles[0]}–{paddles[^1]} are programmed on the controller, which isn't supported yet.", 12.5, "DS.TextMuted", 8));
    }

    static Button Small(string text) => new() { Style = DS.Style("DS.ActionSecondary"), Height = 30, MinHeight = 0, Padding = new(12, 0, 12, 0), Content = Kit.Text(text, 13, "DS.TextSoft") };
    static TextBlock Wrap(string text, double size, string brush, double top = 0) => Kit.Text(text, size, brush).Also(t => { t.TextWrapping = TextWrapping.Wrap; t.TextTrimming = TextTrimming.None; t.Margin = new(0, top, 0, 0); });
}
