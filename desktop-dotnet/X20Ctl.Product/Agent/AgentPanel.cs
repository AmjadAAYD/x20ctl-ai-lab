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
/// It is drawn as a controller (owner, 13 Oct 2026: "we are talking to a controller, it must look like one"): the
/// conversation is its screen, its sticks are eyes that follow what it's doing, the player lights chase while it thinks,
/// and its own buttons work it: LB Offline, RB the settings, A send, B away, X start over, Y HELP, the D-pad scrolls and
/// flips through ideas. A change it applies makes the real controller rumble.
/// </summary>
public sealed class AgentPanel
{
    // one model server and one loaded model for the whole app, however many windows open
    static readonly OllamaClient model = new();
    static Task<ModelState>? preparing;
    static ModelState state = ModelState.Starting;
    // offline mode: the phrasebook agent, chosen with the switch, or used when the model can't run on this PC at all
    static readonly OfflineAgent offlineAgent = new();
    static bool offlineChosen;
    /// <summary>The Offline switch, for review harnesses.</summary>
    public static void UseOffline(bool on) { offlineChosen = on; StateChanged?.Invoke(); }
    public static bool Offline => offlineChosen || state is ModelState.NotInstalled or ModelState.Missing or ModelState.Failed;
    /// <summary>Asks whichever agent is in use now, so the switch works mid-conversation.</summary>
    // the docs (M5): searched by meaning with the embedding model, or by words offline or when it can't be reached
    static readonly Lazy<IReadOnlyList<DocChunk>> docs = new(() => AgentDocs.Load(Path.Combine(AppContext.BaseDirectory, "Help")));
    static readonly Lazy<KeywordSearch> keywords = new(() => new KeywordSearch(docs.Value));
    static readonly Lazy<EmbeddingSearch> meanings = new(() => new EmbeddingSearch(docs.Value, new OllamaEmbedder(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "x20ctl", "agent", "doc-embeddings.json")));
    sealed class EitherDocs : IDocSearch
    {
        bool fellBack;
        IDocSearch Now => Offline || fellBack ? keywords.Value : meanings.Value;
        public string Method => Now.Method;
        public double Enough => Now.Enough;
        public async Task<IReadOnlyList<DocHit>> Search(string question, int count, CancellationToken cancel)
        {
            if (Offline || fellBack) return await keywords.Value.Search(question, count, cancel);
            try { return await meanings.Value.Search(question, count, cancel); }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) { fellBack = true; return await keywords.Value.Search(question, count, cancel); }
        }
    }
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
    readonly Button send = null!;
    readonly Action<Button> register;
    readonly StackPanel ideasList = new();
    readonly TextBlock ideasFor = Kit.Text("", 12.5, "DS.TextMuted").Also(t => { t.TextWrapping = TextWrapping.Wrap; t.TextTrimming = TextTrimming.None; t.Margin = new(0, 2, 0, 0); });
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
    readonly int player;
    readonly Border offlineCap; readonly TextBlock offlineLabel;
    readonly Border[] leds = new Border[4];
    readonly List<(TranslateTransform Look, ScaleTransform Lid, int Inward)> eyes = [];
    readonly TranslateTransform shake = new();
    readonly System.Windows.Threading.DispatcherTimer blink = new() { Interval = TimeSpan.FromSeconds(4) };

    /// <summary>The controller is drawn at this size and scaled as a whole to the room it's given: wide, across the Studio
    /// (owner's sketch, 13 Oct), with ideas on the left of its screen and the conversation on the right.</summary>
    public const double DesignWidth = 1400, DesignHeight = 580;
    static readonly Geometry ShellShape = Geometry.Parse("M 140,46 H 1260 C 1350,46 1400,100 1400,180 L 1400,470 C 1400,550 1345,580 1290,580 C 1240,580 1212,552 1196,522 Q 1180,500 1140,500 H 260 Q 220,500 204,522 C 188,552 160,580 110,580 C 55,580 0,550 0,470 L 0,180 C 0,100 50,46 140,46 Z");
    static readonly Color CapLight = Color.FromRgb(46, 58, 84), CapDark = Color.FromRgb(20, 27, 44), Edge = Color.FromArgb(150, 90, 112, 156);
    const double LeftX = 100, RightX = 1300, EyeY = 165, PadY = 318;

    public AgentPanel(string modelId, string controllerName, PcModeLink? link, Action<Button> register, int player = 0, Func<Action<string, int>, IDriftMeter>? meter = null)
    {
        this.modelId = modelId; this.link = link; this.player = player; this.register = register;
        string name = controllerName.Replace("EasySMX ", "");
        // drift is read from the real controller; a review harness can hand in its own reading instead
        session = new AgentSession(new Either(), modelId, name) { Docs = new EitherDocs(), Meter = (meter ?? (progress => new XInputDriftMeter(player, progress)))(Measuring) };
        greeting = "Agent XCTL, in your " + name + ". Tell me how you play, or what feels wrong, and I'll set myself up. Search the ideas on the left, or say HELP.";

        // ---- the screen, right: the conversation ----
        var heading = new DockPanel { Margin = new(0, 0, 0, 8) };
        DockPanel.SetDock(settingsToggle, Dock.Right); heading.Children.Add(settingsToggle);
        statusDot.Margin = new(12, 0, 6, 0);
        heading.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { Kit.Text("Agent XCTL", 15, "DS.Text", FontWeights.SemiBold), statusDot, status } });
        settingsBox = new Border { Visibility = Visibility.Collapsed, Margin = new(0, 0, 0, 8), Padding = new(14, 10, 14, 6), CornerRadius = new(12), Background = new SolidColorBrush(Color.FromArgb(120, 20, 30, 56)), BorderBrush = new SolidColorBrush(Color.FromArgb(50, 120, 170, 255)), BorderThickness = new(1),
            Child = new ScrollViewer { MaxHeight = 170, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new StackPanel { Children = { target.Also(t => t.Margin = new(0, 0, 0, 8)), settings } } } };
        var top = new StackPanel { Children = { heading, settingsBox } };
        scroller = new ScrollViewer { Content = transcript, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var chat = new DockPanel(); DockPanel.SetDock(top, Dock.Top); chat.Children.Add(top); chat.Children.Add(scroller);

        // ---- the screen, left: ideas, narrowed by what's typed ----
        var ideasHead = new StackPanel { Margin = new(0, 0, 0, 10), Children = { Kit.Text("Ideas", 15, "DS.Text", FontWeights.SemiBold), ideasFor } };
        var ideas = new DockPanel(); DockPanel.SetDock(ideasHead, Dock.Top); ideas.Children.Add(ideasHead);
        ideas.Children.Add(new ScrollViewer { Content = ideasList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new(0, 0, 8, 0) });

        // ---- one bar under both: it searches the ideas as you type, and sends to the agent on Enter ----
        Input = new TextBox { AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, MinHeight = 42, MaxHeight = 84, MaxLength = 600, FontSize = 14, Padding = new(24, 9, 12, 8), VerticalContentAlignment = VerticalAlignment.Center, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ToolTip = "Type to search the ideas; Enter or A to send" };
        Input.KeyDown += (_, e) => { if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None) { e.Handled = true; Send(); } };
        Input.TextChanged += (_, _) => { ShowIdeas(Input.Text); if (!busy) Look(Input.Text.Length > 0 ? Gaze.Typing : Gaze.Idle); };
        var bar = new Grid { Margin = new(0, 12, 0, 0), Children = { Input, Kit.Icon("", 14, "DS.TextMuted").Also(i => { i.Margin = new(14, 0, 0, 0); i.HorizontalAlignment = HorizontalAlignment.Left; i.IsHitTestVisible = false; }) } };

        var split = new Grid { ColumnDefinitions = { new() { Width = new GridLength(400) }, new() { Width = new GridLength(25) }, new() }, RowDefinitions = { new(), new() { Height = GridLength.Auto } } };
        split.Children.Add(ideas);
        var divider = new Border { Width = 1, Background = new SolidColorBrush(Color.FromArgb(36, 255, 255, 255)) }; Grid.SetColumn(divider, 1); split.Children.Add(divider);
        Grid.SetColumn(chat, 2); split.Children.Add(chat);
        Grid.SetRow(bar, 1); Grid.SetColumnSpan(bar, 3); split.Children.Add(bar);
        var screen = new Border
        {
            Width = 990, Height = 418, CornerRadius = new(18), Padding = new(18, 14, 16, 14), BorderThickness = new(1.5), Child = split,
            Background = new LinearGradientBrush(Color.FromRgb(8, 13, 28), Color.FromRgb(4, 7, 16), 90),
            BorderBrush = new SolidColorBrush(Color.FromArgb(200, 4, 6, 12)),
        };

        // ---- the controller around it ----
        var pad = new Canvas { Width = DesignWidth, Height = DesignHeight, RenderTransform = shake };
        // the bumpers, behind the shell's top edge: LB is Offline, RB the settings now
        (offlineSwitch, offlineCap, offlineLabel) = Bumper("LB", "Offline", 70, new CornerRadius(26, 18, 6, 6));
        offlineSwitch.Click += (_, _) => { offlineChosen = !offlineChosen; StateChanged?.Invoke(); };
        var (settingsButton, _, _) = Bumper("RB", "Settings", DesignWidth - 70 - 220, new CornerRadius(18, 26, 6, 6));
        settingsButton.Click += (_, _) => { settingsBox.Visibility = settingsBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; ShowSettings(new HashSet<string>()); };
        register(offlineSwitch); register(settingsButton);
        pad.Children.Add(offlineSwitch); pad.Children.Add(settingsButton);
        var shell = new System.Windows.Shapes.Path
        {
            Data = ShellShape, StrokeThickness = 1.5, Stroke = new LinearGradientBrush(Color.FromArgb(170, 120, 150, 210), Color.FromArgb(50, 120, 150, 210), 90),
            Fill = new LinearGradientBrush(new GradientStopCollection { new(Color.FromRgb(30, 39, 60), 0), new(Color.FromRgb(19, 25, 41), .5), new(Color.FromRgb(12, 16, 28), 1) }, 90),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 44, ShadowDepth = 12, Opacity = .6 },
        };
        pad.Children.Add(shell); Drag(shell);
        // grip texture, so the lower corners read as handles
        foreach (var (x, dir) in new[] { (75.0, 1), (DesignWidth - 75, -1) })
            for (int i = 0; i < 4; i++)
                pad.Children.Add(new System.Windows.Shapes.Line { X1 = x - 22 * dir + i * 12 * dir, Y1 = 498 + i * 8, X2 = x - 8 * dir + i * 12 * dir, Y2 = 534 + i * 8, Stroke = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)), StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, IsHitTestVisible = false });
        // the player lights: this player's is lit in the model's state; they chase while it thinks
        for (int i = 0; i < 4; i++)
        {
            leds[i] = new Border { Width = 16, Height = 5, CornerRadius = new(2.5), IsHitTestVisible = false };
            Canvas.SetLeft(leds[i], DesignWidth / 2 - 41 + i * 22); Canvas.SetTop(leds[i], 58); pad.Children.Add(leds[i]);
        }
        Canvas.SetLeft(screen, (DesignWidth - 990) / 2); Canvas.SetTop(screen, 72); pad.Children.Add(screen);

        // the sticks are its eyes
        Eye(pad, LeftX, EyeY); Eye(pad, RightX, EyeY);

        // left: the D-pad scrolls the conversation, and steps through the ideas
        var dpad = new (string Glyph, double X, double Y, string Tip, Action Act)[]
        {
            ("▲", 0, -1, "Scroll up", () => scroller.ScrollToVerticalOffset(scroller.VerticalOffset - 140)),
            ("▼", 0, 1, "Scroll down", () => scroller.ScrollToVerticalOffset(scroller.VerticalOffset + 140)),
            ("◀", -1, 0, "Previous idea", () => Idea(-1)),
            ("▶", 1, 0, "Next idea", () => Idea(1)),
        };
        var hub = new Border { Width = 34, Height = 34, Background = new LinearGradientBrush(CapLight, CapDark, 90), IsHitTestVisible = false };
        Canvas.SetLeft(hub, LeftX - 17); Canvas.SetTop(hub, PadY - 17); pad.Children.Add(hub);
        foreach (var d in dpad)
        {
            var arm = PadKey(new Border
            {
                Width = 34, Height = 36, BorderThickness = new(1), BorderBrush = new SolidColorBrush(Edge), Background = new LinearGradientBrush(CapLight, CapDark, 90),
                CornerRadius = d.Y < 0 ? new(8, 8, 0, 0) : d.Y > 0 ? new(0, 0, 8, 8) : d.X < 0 ? new(8, 0, 0, 8) : new(0, 8, 8, 0),
                Child = Kit.Text(d.Glyph, 11, "DS.TextMuted").Also(t => t.HorizontalAlignment = HorizontalAlignment.Center),
            }, d.Tip, d.Act);
            if (d.X != 0) { ((Border)arm.Content).Width = 36; ((Border)arm.Content).Height = 34; }
            Canvas.SetLeft(arm, LeftX - 17 + d.X * 35 - (d.X < 0 ? 2 : 0)); Canvas.SetTop(arm, PadY - 17 + d.Y * 35 - (d.Y < 0 ? 2 : 0));
            register(arm); pad.Children.Add(arm);
        }
        Legend(pad, LeftX, PadY + 74, "▲▼ scroll\n◀▶ ideas");

        // right: the face buttons. A sends, B puts it away, X starts over, Y shows every idea
        var face = new (string Letter, double X, double Y, string Tip, Action Act)[]
        {
            ("Y", 0, -1, "All ideas", () => { Input.Text = ""; ShowIdeas(""); Look(Gaze.Ideas); }),
            ("X", -1, 0, "New conversation", Restart),
            ("B", 1, 0, "Put away", () => CloseRequested?.Invoke()),
            ("A", 0, 1, "Send", Send),
        };
        foreach (var f in face)
        {
            bool primary = f.Letter == "A";
            var cap = PadKey(new Border
            {
                Width = 42, Height = 42, CornerRadius = new(21), BorderThickness = new(primary ? 2 : 1),
                BorderBrush = primary ? DS.Brush("DS.AccentHi") : new SolidColorBrush(Edge),
                Background = primary ? new RadialGradientBrush(Color.FromRgb(70, 140, 250), Color.FromRgb(28, 70, 160)) : new RadialGradientBrush(CapLight, CapDark),
                Effect = primary ? new DropShadowEffect { Color = Color.FromRgb(70, 150, 255), BlurRadius = 16, ShadowDepth = 0, Opacity = .7 } : null,
                Child = Kit.Text(f.Letter, 17, primary ? "DS.Text" : "DS.TextSoft", FontWeights.SemiBold).Also(t => t.HorizontalAlignment = HorizontalAlignment.Center),
            }, f.Tip, f.Act);
            Canvas.SetLeft(cap, RightX - 21 + f.X * 42); Canvas.SetTop(cap, PadY - 21 + f.Y * 42);
            register(cap); pad.Children.Add(cap);
            if (primary) send = cap;
        }
        Legend(pad, RightX, PadY + 74, "A send · B away\nX new · Y ideas");
        send ??= new Button();
        ShowIdeas("");

        Body = new Border
        {
            Background = Brushes.Transparent, Child = new Viewbox { Stretch = Stretch.Uniform, Child = pad },
            RenderTransform = new TransformGroup { Children = { pop, offset } }, RenderTransformOrigin = new(1, 1),
        };
        blink.Tick += (_, _) => Blink();
        Say(greeting, null);
        ShowSettings(new HashSet<string>());
        StateChanged += ShowState; Body.Unloaded += (_, _) => StateChanged -= ShowState; Body.Loaded += (_, _) => { StateChanged -= ShowState; StateChanged += ShowState; };
        ShowState(); Look(Gaze.Idle); Prepare();
    }

    (Button, Border, TextBlock) Bumper(string key, string label, double x, CornerRadius corners)
    {
        var text = Kit.Text(label, 13, "DS.TextSoft");
        var cap = new Border
        {
            Width = 220, Height = 50, CornerRadius = corners, BorderThickness = new(1), BorderBrush = new SolidColorBrush(Edge), Background = new LinearGradientBrush(CapLight, CapDark, 90),
            Child = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new(0, 9, 0, 0), Children = { Kit.Text(key, 12, "DS.TextMuted", FontWeights.SemiBold).Also(t => t.Margin = new(0, 0, 8, 0)), text } },
        };
        var button = PadKey(cap, key + " · " + label, null);
        Canvas.SetLeft(button, x); Canvas.SetTop(button, 10);
        return (button, cap, text);
    }

    /// <summary>A control on the controller: its own drawing, a lit edge when hovered or focused.</summary>
    static Button PadKey(Border look, string tip, Action? act)
    {
        var button = new Button { Content = look, ToolTip = tip, Cursor = Cursors.Hand, FocusVisualStyle = null, RenderTransformOrigin = new(.5, .5), RenderTransform = new ScaleTransform(1, 1),
            Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) } };
        System.Windows.Automation.AutomationProperties.SetName(button, tip);
        var rest = look.BorderBrush;
        void Lit(bool on) => look.BorderBrush = on ? DS.Brush("DS.AccentHi") : rest;
        button.MouseEnter += (_, _) => Lit(true); button.MouseLeave += (_, _) => Lit(button.IsKeyboardFocused);
        button.GotKeyboardFocus += (_, _) => Lit(true); button.LostKeyboardFocus += (_, _) => Lit(button.IsMouseOver);
        if (act != null) button.Click += (_, _) => act();
        return button;
    }

    static void Legend(Canvas pad, double x, double y, string text)
    {
        var t = Kit.Text(text, 11.5, "DS.TextMuted").Also(t => { t.TextAlignment = TextAlignment.Center; t.Width = 150; t.TextTrimming = TextTrimming.None; t.IsHitTestVisible = false; });
        Canvas.SetLeft(t, x - 75); Canvas.SetTop(t, y); pad.Children.Add(t);
    }

    void Idea(int step)
    {
        var listed = ideasList.Children.OfType<Button>().ToList();
        if (listed.Count == 0) return;
        int at = listed.FindIndex(b => b.IsKeyboardFocusWithin);
        var next = listed[at < 0 ? (step > 0 ? 0 : listed.Count - 1) : Math.Clamp(at + step, 0, listed.Count - 1)];
        next.Focus(); next.BringIntoView(); Look(Gaze.Ideas);
    }

    /// <summary>The ideas half: what matches the words typed so far, or everything (the HELP list first).</summary>
    void ShowIdeas(string query)
    {
        query = query.Trim();
        var found = AgentIdeas.Search(query, 10);
        ideasFor.Text = query.Length == 0 ? "What you can ask. Type a word to search." : found.Count == 0 ? $"Nothing for “{query}”. Try sticks, triggers, turbo, buttons, paddles or PC mode." : $"For “{query}”";
        ideasList.Children.Clear();
        foreach (var idea in found)
        {
            var card = PadKey(new Border
            {
                CornerRadius = new(12), Padding = new(12, 8, 12, 9), Margin = new(0, 0, 0, 6), BorderThickness = new(1),
                Background = new SolidColorBrush(Color.FromArgb(70, 30, 44, 76)), BorderBrush = new SolidColorBrush(Color.FromArgb(40, 120, 170, 255)),
                Child = new StackPanel { Children = { Wrap(idea.Have, 12.5, "DS.TextMuted"), Wrap("“" + idea.Say + "”", 14, "DS.AccentHi", 3) } },
            }, "Say it", null);
            string say = idea.Say;
            card.Click += (_, _) => { if (!busy) { Input.Text = say; Send(); } };
            register(card); ideasList.Children.Add(card);
        }
    }

    // ---- the eyes: where it looks says what it's doing ----
    enum Gaze { Idle, Typing, Thinking, Down, Ideas }
    void Eye(Canvas pad, double cx, double cy)
    {
        var well = new System.Windows.Shapes.Ellipse { Width = 100, Height = 100, Fill = new RadialGradientBrush(Color.FromRgb(5, 8, 16), Color.FromRgb(12, 17, 30)), Stroke = new SolidColorBrush(Edge), StrokeThickness = 1.2, IsHitTestVisible = false };
        Canvas.SetLeft(well, cx - 50); Canvas.SetTop(well, cy - 50); pad.Children.Add(well);
        var look = new TranslateTransform(); var lid = new ScaleTransform(1, 1);
        var pupil = new System.Windows.Shapes.Rectangle { Width = 13, Height = 24, RadiusX = 6.5, RadiusY = 6.5, Fill = new SolidColorBrush(AgentMark.Ice), RenderTransformOrigin = new(.5, .5), RenderTransform = lid,
            Effect = new DropShadowEffect { Color = AgentMark.Ice, BlurRadius = 18, ShadowDepth = 0, Opacity = 1 } };
        var cap = new Grid
        {
            Width = 74, Height = 74, IsHitTestVisible = false, RenderTransform = look,
            Children =
            {
                new System.Windows.Shapes.Ellipse { Fill = new RadialGradientBrush(new GradientStopCollection { new(Color.FromRgb(58, 72, 102), 0), new(CapDark, 1) }) { GradientOrigin = new(.4, .35) }, Stroke = new SolidColorBrush(Edge), StrokeThickness = 1.5 },
                new System.Windows.Shapes.Ellipse { Margin = new(9), Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), StrokeThickness = 1 },
                pupil,
            },
        };
        Canvas.SetLeft(cap, cx - 37); Canvas.SetTop(cap, cy - 37); pad.Children.Add(cap);
        eyes.Add((look, lid, cx < DesignWidth / 2 ? 1 : -1));
    }

    void Look(Gaze gaze)
    {
        foreach (var (look, _, inward) in eyes)
        {
            var (x, y) = gaze switch { Gaze.Typing => (5.0 * inward, 9.0), Gaze.Thinking => (0.0, -10.0), Gaze.Down => (0.0, 10.0), Gaze.Ideas => (-9.0, 3.0), _ => (6.0 * inward, 0.0) };
            if (!DS.Motion) { look.X = x; look.Y = y; continue; }
            look.BeginAnimation(TranslateTransform.XProperty, DS.To(x, 260)); look.BeginAnimation(TranslateTransform.YProperty, DS.To(y, 260));
        }
    }

    void Blink()
    {
        blink.Interval = TimeSpan.FromSeconds(3.5 + Random.Shared.NextDouble() * 3);
        if (!DS.Motion) return;
        foreach (var (_, lid, _) in eyes)
            lid.BeginAnimation(ScaleTransform.ScaleYProperty, new System.Windows.Media.Animation.DoubleAnimation(1, .1, TimeSpan.FromMilliseconds(80)) { AutoReverse = true });
    }

    /// <summary>A change landed: the eyes smile, the controller shakes on screen and rumbles in the hand.</summary>
    void Applied()
    {
        _ = AgentRumble.Pulse(player);
        if (!DS.Motion) return;
        foreach (var (_, lid, _) in eyes)
            lid.BeginAnimation(ScaleTransform.ScaleYProperty, new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames { KeyFrames = { new System.Windows.Media.Animation.LinearDoubleKeyFrame(.35, TimeSpan.FromMilliseconds(120)), new System.Windows.Media.Animation.LinearDoubleKeyFrame(.35, TimeSpan.FromMilliseconds(900)), new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(1050)) } });
        var jolt = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
        double t = 0; foreach (double x in new[] { -6.0, 6, -5, 4, -2, 0 }) jolt.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(x, TimeSpan.FromMilliseconds(t += 45)));
        shake.BeginAnimation(TranslateTransform.XProperty, jolt);
    }

    void Chase(bool on)
    {
        for (int i = 0; i < 4; i++)
        {
            if (!on || !DS.Motion) { leds[i].BeginAnimation(UIElement.OpacityProperty, null); leds[i].Opacity = 1; continue; }
            leds[i].BeginAnimation(UIElement.OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(1, .15, TimeSpan.FromMilliseconds(300)) { AutoReverse = true, BeginTime = TimeSpan.FromMilliseconds(150 * i), RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever });
        }
    }

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
        Body.Visibility = Visibility.Visible; Body.IsHitTestVisible = true; blink.Start();
        if (!DS.Motion) { Body.Opacity = 1; pop.ScaleX = pop.ScaleY = 1; return; }
        Body.BeginAnimation(UIElement.OpacityProperty, DS.To(1, 180));
        pop.BeginAnimation(ScaleTransform.ScaleXProperty, new System.Windows.Media.Animation.DoubleAnimation(.6, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = DS.Spring });
        pop.BeginAnimation(ScaleTransform.ScaleYProperty, new System.Windows.Media.Animation.DoubleAnimation(.6, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = DS.Spring });
    }
    public void Hide()
    {
        Body.IsHitTestVisible = false; blink.Stop();
        if (!DS.Motion) { Body.Visibility = Visibility.Collapsed; return; }
        var fade = DS.To(0, 160); fade.Completed += (_, _) => { if (!Body.IsHitTestVisible) Body.Visibility = Visibility.Collapsed; };
        Body.BeginAnimation(UIElement.OpacityProperty, fade);
        pop.BeginAnimation(ScaleTransform.ScaleXProperty, DS.To(.6, 160)); pop.BeginAnimation(ScaleTransform.ScaleYProperty, DS.To(.6, 160));
    }
    public bool IsShown => Body.Visibility == Visibility.Visible && Body.IsHitTestVisible;

    void Restart()
    {
        if (busy) return;
        session.Forget(); transcript.Children.Clear(); Input.Text = ""; ShowIdeas(""); Say(greeting, null); Input.Focus();
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
        for (int i = 0; i < 4; i++) leds[i].Background = i == Math.Clamp(player, 0, 3) || busy ? statusDot.Background : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        send.IsEnabled = !busy && (Offline || state == ModelState.Ready);
        offlineCap.Background = offlineChosen ? new LinearGradientBrush(Color.FromRgb(52, 104, 196), Color.FromRgb(28, 62, 132), 90) : new LinearGradientBrush(CapLight, CapDark, 90);
        offlineLabel.Foreground = DS.Brush(offlineChosen ? "DS.Text" : "DS.TextSoft");
        offlineSwitch.ToolTip = offlineChosen ? "Back to the model (Qwen)" : "Offline: answers from a phrasebook, no model";
    }
    public static string StateText => stateText;

    async void Send() { if (Input.Text.Trim() is { Length: > 0 } sentence && !busy) { Input.Text = ""; await Ask(sentence); } }

    /// <summary>The whole round: the sentence, the model, the checks, and (when valid) the settings changed. Returns the
    /// turn, or null when the model couldn't be reached.</summary>
    public async Task<AgentTurn?> Ask(string sentence)
    {
        if (busy) return null;
        // the help list is the app's own answer: instant, the same online and offline
        if (AgentHelp.Asks(sentence)) { Bubble(sentence, mine: true); ShowIdeas(""); Look(Gaze.Ideas); Say("Have a look at the ideas on the left: pick one, or type a word to search them.", null); return null; }
        // a topic alone is a search: the ideas narrow to it, and the app answers itself
        if (AgentIdeas.Topic(sentence) is { } topic) { Bubble(sentence, mine: true); ShowIdeas(sentence); Look(Gaze.Ideas); Say(AgentIdeas.TopicReply(topic), null); return null; }
        if (!Offline && state != ModelState.Ready) { Bubble(sentence, mine: true); Say(state == ModelState.Missing ? stateText : "Not ready yet: " + stateText, null); return null; }
        busy = true; ShowState(); Thinking?.Invoke(true); Look(Gaze.Thinking); Chase(true);
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
                try { Store(next); Applied(); }
                catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException) { Say(turn, null, "Couldn't save: " + error.Message); return turn; }
                // the walk-through first, so the answer and its recap are waiting when the window comes back
                if (Walkthrough != null) await Walkthrough(before, next, turn.Measured);
                Say(turn, before, switched: switched); ShowSettings(turn.Outcome.Changes.Select(c => c.What).ToHashSet());
            }
            else { Say(turn, null, switched: switched); if (switched != null) ShowSettings(new HashSet<string>()); }
            if (!turn.Verdict.Valid || turn.Outcome.Error != null) { Look(Gaze.Down); await Task.Delay(1400); }
            return turn;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            transcript.Children.Remove(thinking);
            Say(error is TaskCanceledException ? "I took too long to answer. Try again." : "I couldn't reach the model: " + error.Message, null);
            return null;
        }
        finally { busy = false; if (measuring != null) { transcript.Children.Remove(measuring); measuring = null; } Chase(false); Look(Gaze.Idle); ShowState(); Thinking?.Invoke(false); Input.Focus(); }
    }

    // ---- the drift reading: a countdown while the sticks are read ----
    Border? measuring;
    void Measuring(string phase, int seconds)
    {
        if (measuring == null)
        {
            measuring = Bubble("", mine: false); measuring.BorderBrush = DS.Brush("DS.Warn"); Look(Gaze.Down);
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
            Child = Wrap(text, 14, "DS.Text"),
        };
        transcript.Children.Add(bubble); Fx.Appear(bubble, .97, 200); scroller.ScrollToEnd(); return bubble;
    }

    void Say(string text, string? detail) { var b = Bubble(text, false); if (detail != null) ((TextBlock)b.Child).Text += "\n" + detail; }

    /// <summary>One answer: the model's words, then what the app did, then the details on request.</summary>
    void Say(AgentTurn turn, PcModeProfile? undo, string? failure = null, string? switched = null)
    {
        var stack = new StackPanel();
        var v = turn.Verdict;
        stack.Children.Add(Wrap(v.Proposal?.Speech is { Length: > 0 } speech ? speech : "I didn't give a usable answer.", 14, "DS.Text"));
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
            stack.Children.Add(new Border { BorderBrush = DS.Brush("DS.AccentHi"), BorderThickness = new(2, 0, 0, 0), Padding = new(12, 2, 0, 2), Margin = new(0, 10, 0, 0), Child = Wrap(recap, 13.5, "DS.TextSoft") });
        // the switch: "" when it worked, otherwise why not
        if (switched != null && turn.Outcome.PcMode is { } pc)
            stack.Children.Add(Wrap(switched.Length == 0 ? $"PC mode   {(pc ? "on" : "off")}" : "PC mode · " + switched, 14, switched.Length == 0 ? "DS.AccentHi" : "DS.Warn", 6));
        // where the answer came from: the doc passages it cites, each opening to its full text
        foreach (var hit in turn.Cited) stack.Children.Add(Source(hit));
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

    UIElement Source(DocHit hit)
    {
        string text = hit.Chunk.Text, shortText = text.Length > 170 ? text[..170].TrimEnd() + "…" : text;
        var body = Wrap(shortText, 13.5, "DS.TextSoft", 4);
        var card = new Button { Style = DS.Style("DS.ActionSecondary"), MinHeight = 0, Padding = new(12, 8, 12, 8), Margin = new(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = new StackPanel { Children = { Wrap("Source · " + hit.Chunk.Where, 12.5, "DS.AccentHi").Also(t => t.FontWeight = FontWeights.SemiBold), body } } };
        card.Click += (_, _) => body.Text = body.Text == shortText ? text : shortText;
        return card;
    }

    static Button Small(string text) => new() { Style = DS.Style("DS.ActionSecondary"), Height = 30, MinHeight = 0, Padding = new(12, 0, 12, 0), Content = Kit.Text(text, 13, "DS.TextSoft") };
    static TextBlock Wrap(string text, double size, string brush, double top = 0) => Kit.Text(text, size, brush).Also(t => { t.TextWrapping = TextWrapping.Wrap; t.TextTrimming = TextTrimming.None; t.Margin = new(0, top, 0, 0); });
}
