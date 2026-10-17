using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using X20Ctl.Zone;
namespace X20Ctl.Product;

/// <summary>
/// The Buttons page in the console design (approved by the owner on 10 Oct 2026): controller visualizer
/// (layered photo + exact-shape hotspots) on the left, the remap workspace on the right, and the
/// full-width outputs shelf underneath. It only edits the local ButtonDraft; it never writes hardware.
/// </summary>
public sealed class ButtonsConsolePage : Grid
{
    private readonly ControllerModel model; private readonly ControllerLayout layout; private readonly ButtonDraft draft;
    private readonly Func<string, string> label; private readonly bool animate;
    private readonly Action<string> select; private readonly Func<string, bool> openMacro; private readonly Action toggleTry;
    private readonly Grid stage = new() { Width = 1536, Height = 1024 };
    private readonly Canvas hotspots = new() { Width = 1536, Height = 1024 };
    private readonly Dictionary<string, PhysicalHotspot> regions = new();
    private readonly Dictionary<string, Button> outputs = new();
    private readonly Dictionary<string, RadioButton> groups = new();
    private readonly StackPanel workspace = new(), overview = new() { Visibility = Visibility.Collapsed };
    // the mapping card: "You press" (the physical control) → "Game gets" (its draft output), in plain words
    private readonly Border pressedFace = new(), sendsFace = new(), mappingCard = new();
    private readonly TextBlock pressedName = Kit.Text("", 13, "DS.TextSoft"), sendsName = Kit.Text("", 13, "DS.TextSoft"), sentence = Kit.Text("", 15, "DS.Text"), readOnly = Kit.Text("", 14, "DS.TextSoft");
    private readonly Button restore = new() { Content = "Restore default", ToolTip = "Make this button send itself again" };
    private readonly TextBlock outputsHint = Kit.Text("", 14, "DS.TextSoft"), stepTwoHint = Kit.Text("Choose any button in Outputs below", 13, "DS.TextSoft");
    private readonly RadioButton front = new(), back = new();
    private readonly Button tryButton, allButton;
    private string selected = "",outputGroup="All";
    private readonly TextBlock showcaseTitle=Kit.Text("Buttons showcase",16,"DS.Text",FontWeights.SemiBold);
    private readonly StackPanel outputPicker=new(){Visibility=Visibility.Collapsed};
    private readonly TextBlock selectionTitle=Kit.Text("Select a control",24,"DS.Text",FontWeights.SemiBold);
    private readonly Canvas markers=new(){Width=1536,Height=1024,IsHitTestVisible=false};
    private readonly TextBlock mappingLabel=Kit.Text("",32,"DS.Text",FontWeights.SemiBold);
    private bool connected;
    public void SetConnected(bool value){connected=value;RefreshMapping();}
    public void SelectPhysical(string key){if(Listening!=Listen.None){Press(key);return;}if(layout.Controls.Any(r=>r.Key==key))Pick(key);}
    private bool TargetAllowed(string key)=>ButtonDraft.Targets.Contains(key)||key=="NONE"&&draft.PcMode;
    private string TargetName(string key)=>key=="NONE"?"Disabled":label(key);
    public PhotoController Controller { get; private set; }
    public LiveController Live => Controller.Live;
    public bool IsRear { get; private set; }

    private static readonly (string Name, string First, string[] Keys)[] Groups =
    [
        ("Face", "A", ["A", "B", "X", "Y"]), ("D-pad", "DPAD_UP", ["DPAD_UP", "DPAD_DOWN", "DPAD_LEFT", "DPAD_RIGHT"]),
        ("Shoulders", "LB", ["LB", "RB", "LT", "RT"]), ("Sticks", "L3", ["L3", "R3", "LSTICK_ANALOG", "RSTICK_ANALOG"]), ("System", "SELECT", ["SELECT", "START", "HOME"])
    ];

    public ButtonsConsolePage(ControllerModel model, ControllerLayout layout, ButtonDraft draft, Func<string, string> label, bool animate,
        Action<string> select, Func<string, bool> openMacro, Action toggleTry)
    {
        this.model = model; this.layout = layout; this.draft = draft; this.label = label; this.animate = animate; this.select = select; this.openMacro = openMacro; this.toggleTry = toggleTry;
        Controller = new PhotoController(model.Id); stage.Children.Add(Controller); stage.Children.Add(hotspots);stage.Children.Add(markers);
        RowDefinitions.Add(new()); RowDefinitions.Add(new() { Height = new(14) }); RowDefinitions.Add(new() { Height = GridLength.Auto });
        var top = new Grid(); top.ColumnDefinitions.Add(new() { Width = new(1.12, GridUnitType.Star) }); top.ColumnDefinitions.Add(new() { Width = new(16) }); top.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 390 });
        Children.Add(top);

        // left: visualizer with a Front / Back segmented switch
        string g = "view" + Guid.NewGuid().ToString("N");
        front.Content = "Front"; back.Content = "Back";
        foreach (var r in new[] { front, back }) { r.Style = DS.Style("DS.Segment"); r.GroupName = g; r.Width = 86; r.Height = 32; }
        front.IsChecked = true; back.IsEnabled = layout.Controls.Any(c => c.View is "back" or "shoulder");
        front.Checked += (_, _) => ShowView(false); back.Checked += (_, _) => ShowView(true);
        var viewSwitch = new Border { CornerRadius = new(11), Background = new SolidColorBrush(Color.FromRgb(29,44,67)), BorderBrush = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255)), BorderThickness = new(1), Padding = new(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { front, back } }, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new(0, -60, 0, 0) };
        var visual = new Grid { Children = { new Viewbox { Child = stage }, viewSwitch } };
        top.Children.Add(Kit.Panel("Button Visualizer", "Select a control on the controller to remap it.", null, visual));

        // right: two steps around a "you press → game gets" card, then the actions
        var seg = new UniformGrid { Rows = 1 }; string sg = "grp" + Guid.NewGuid().ToString("N");
        var allCategory=new RadioButton{Content="All",Style=DS.Style("DS.Segment"),GroupName=sg};allCategory.Click+=(_,_)=>{outputGroup="All";RefreshMapping();};groups["All"]=allCategory;seg.Children.Add(allCategory);
        foreach (var (name, first, keys) in Groups)
        {
            var r = new RadioButton { Content = name, Style = DS.Style("DS.Segment"), GroupName = sg, IsEnabled = keys.Any(ButtonDraft.Targets.Contains) };
            string groupName=name;r.Click+=(_,_)=>{outputGroup=groupName;RefreshMapping();}; groups[name] = r; seg.Children.Add(r);
        }
        workspace.Children.Add(selectionTitle);
        workspace.Children.Add(BuildMappingCard());
        workspace.Children.Add(Kit.Text(draft.PcMode?"PC MODE · Processed by X20CTL on this PC. Controller memory is unchanged.":"LOCAL DRAFT · Not written to controller memory.",13,"DS.TextSoft").Also(t=>{t.TextWrapping=TextWrapping.Wrap;t.Margin=new(0,10,0,12);}));
        outputPicker.Children.Add(outputsHint);
        outputPicker.Children.Add(new Border{CornerRadius=new(12),Background=new SolidColorBrush(Color.FromArgb(20,255,255,255)),Padding=new(2),Child=seg,Margin=new(0,8,0,10)});
        workspace.Children.Add(outputPicker);
        tryButton = ActionButton("", "Test mapping", "ActionButton"); tryButton.Click += (_, _) => this.toggleTry();
        tryButton.Width=160;tryButton.HorizontalAlignment=HorizontalAlignment.Left;
        allButton = ActionButton("", "All assignments", "ActionButton"); allButton.Click += (_, _) => ShowOverview(overview.Visibility != Visibility.Visible);
        var more = new Button { Content = "", Style = DS.Style("DS.ActionRound"), ToolTip = "Reset options" };
        more.ContextMenu = new ContextMenu { Items = { Menu("Reset this control", () => { if (Source.Length > 0 && draft.CanEdit(Source)) { draft.Reset(Source); Changed?.Invoke(); } }), Menu("Reset all controls", () => { draft.ResetAll(); Changed?.Invoke(); }) } };
        more.Click += (_, _) => { more.ContextMenu.PlacementTarget = more; more.ContextMenu.IsOpen = true; };
        var actions = new Grid { Margin = new(0, 14, 0, 0) }; actions.ColumnDefinitions.Add(new()); actions.ColumnDefinitions.Add(new() { Width = new(12) }); actions.ColumnDefinitions.Add(new()); actions.ColumnDefinitions.Add(new() { Width = new(12) }); actions.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        actions.Children.Add(tryButton); Grid.SetColumn(allButton, 2); actions.Children.Add(allButton); Grid.SetColumn(more, 4); actions.Children.Add(more);
        var body = new Grid { Children = { workspace, overview } };
        var right = Kit.Panel(null, null, null, new ScrollViewer{Content=new StackPanel{Children={body,actions}},VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}); Grid.SetColumn(right, 2); top.Children.Add(right);

        // bottom: outputs shelf (interactive)

        var showcase=BuildShowcase();Grid.SetRow(showcase,2);Children.Add(showcase);
        ShowView(false);
    }

    public event Action? Changed;
    public IReadOnlyCollection<PhysicalHotspot> Hotspots => regions.Values;
    public PhysicalHotspot? Hotspot(string key) => regions.GetValueOrDefault(key);

    public void ShowView(bool rear)
    {
        if (rear == IsRear && regions.Count > 0) return;
        IsRear = rear; front.IsChecked = !rear; back.IsChecked = rear;
        stage.Children.Remove(Controller); bool smooth = Controller.Live.Smooth; Controller = new PhotoController(model.Id, rear); Controller.Live.Smooth = smooth; stage.Children.Insert(0, Controller);
        hotspots.Children.Clear(); regions.Clear();
        foreach (var region in layout.Controls.Where(r => r.Role != "axis" && r.Key is not ("CAPTURE" or "TURBO") && (r.View == (rear ? "back" : "front") || rear && r.View == "shoulder")))
        {
            var h = new PhysicalHotspot(region, animate) { ToolTip = label(region.Key), Tag = region.Key };
            var at = LiveController.Place(model.Id, region); Canvas.SetLeft(h, at.X); Canvas.SetTop(h, at.Y);
            string key = region.Key; h.Click += (_, _) => Pick(key);
            hotspots.Children.Add(h); regions[key] = h;
        }
        if (animate) { Controller.Opacity = 0; Controller.BeginAnimation(OpacityProperty, DS.To(1, 220)); }
        Refresh(selected);
    }

    /// <summary>Re-reads the draft and the selection; called by the Studio after any change.</summary>
    public void Refresh(string key)
    {
        // the view follows a newly picked control (a paddle shows the back); a refresh for the same control keeps the
        // view the player chose, so Back stays on the back even with a front button selected
        bool picked = key != selected; selected = key;Source=layout.Controls.Any(r=>r.Key==key)?key:"";
        var region = layout.Controls.FirstOrDefault(r => r.Key == key);
        if (region != null && picked)
        {
            bool needsRear = region.View is "back" or "shoulder";
            if (needsRear != IsRear && (needsRear ? back.IsEnabled : true)) { ShowView(needsRear); return; }
        }
        foreach (var (k, h) in regions) h.Illuminate(k == key || region?.Role == "axis" && k == (key.StartsWith('L') ? "L3" : "R3"), false);

        RefreshMapping();RefreshMarkers();
        if (overview.Visibility == Visibility.Visible) BuildOverview();
    }

    // ----- remapping, the way games do it: click a slot, press a button, it fills in -----
    public enum Listen { None, Source, Target }
    /// <summary>Which side of the card is waiting for a press. While it waits, every controller button is captured.</summary>
    public Listen Listening { get; private set; }
    /// <summary>The physical button on the left of the card ("you press"); empty until the player picks one.</summary>
    public string Source { get; private set; } = "";
    public void BeginListening(Listen side) { if(!connected && side!=Listen.None){Listening=Listen.None;RefreshMapping();return;}Listening = side == Listen.Target && Source.Length == 0 ? Listen.Source : side; RefreshMapping(); }
    public void CancelListening() { if (Listening == Listen.None) return; Listening = Listen.None; RefreshMapping(); }
    /// <summary>A press while listening: a controller button (from the controller or its keyboard mirror), a click on
    /// the picture, or a click in Outputs. The first fills "you press" and moves on; the second sets what it does.</summary>
    public void Press(string button)
    {
        if (Listening == Listen.Source)
        {
            if (!layout.Controls.Any(r => r.Key == button)) return;
            Source = button; select(button);
            Listening = draft.CanEdit(button) ? Listen.Target : Listen.None; RefreshMapping(); return;
        }
        if (Listening == Listen.Target && TargetAllowed(button) && draft.CanEdit(Source))
        {
            Listening = Listen.None; draft.Set(Source, button); Changed?.Invoke(); RefreshMapping();
        }
    }
    private void Pick(string key)
    {
        if (openMacro(key)) return;
        Source = key; select(key); Listening = Listen.None; RefreshMapping();
    }
    private void RefreshMapping()
    {
        bool has = Source.Length > 0 && layout.Controls.Any(r => r.Key == Source), editable = has && draft.CanEdit(Source);
        string target = editable ? draft.Target(Source) : "", name = has ? label(Source) : "";
        bool changed = editable && target != Source, waitSource = Listening == Listen.Source, waitTarget = Listening == Listen.Target;
        FillFace(pressedFace, waitSource ? "…" : has ? Short(Source) : "+", waitSource || !has ? null : Source, false, waitSource);
        FillFace(sendsFace, editable ? Short(target) : has ? "—" : "+", !editable ? null : target, changed, waitTarget);
        Pulse(pressedRing, waitSource); Pulse(sendsRing, waitTarget);
        pressedName.Text = waitSource ? "Press a button…" : has ? Kind(Source) : "Click to choose";
        sendsName.Text = waitTarget ? "Choose output below" : editable ? target=="NONE"?"Disabled":Kind(target) : has ? "Can't change" : "Then press its new job";
        selectionTitle.Text=has?label(Source):"Select a control";
        sentence.Text=!has?(connected?"Select a control on the picture or press it on the controller.":"Select a control on the picture. Physical detection is unavailable while offline."):editable?$"Current mapping: {Short(Source)} → {Short(target)}":"This input can be inspected; remapping is unavailable.";
        readOnly.Text = !has ? "" : model.Id != "x20" && !draft.PcMode ? "Remapping is not verified for this model yet, so it can only be inspected." : editable ? "" : $"{name} can't be remapped. It has no button role to swap.";
        readOnly.Visibility = readOnly.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed; sentence.Visibility = sentence.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        restore.Visibility = changed ? Visibility.Visible : Visibility.Collapsed;restore.Content=$"Restore {Short(Source)} → {Short(Source)}";
        outputPicker.Visibility=editable?Visibility.Visible:Visibility.Collapsed;tryButton.Visibility=changed?Visibility.Visible:Visibility.Collapsed;
        stepTwoHint.Text = waitTarget ? "Waiting for a press" : editable ? "Click GAME GETS, then press a button" : has ? "Not available for this button" : "Pick a button first";
        mappingCard.BorderBrush = new SolidColorBrush(waitSource || waitTarget ? Color.FromArgb(200, 125, 182, 255) : changed ? Color.FromArgb(120, 125, 182, 255) : Color.FromArgb(38, 255, 255, 255));
        foreach (var pair in groups)pair.Value.IsChecked=pair.Key==outputGroup;showcaseTitle.Text=editable?$"Assign {Short(Source)} · Buttons showcase":"Buttons showcase · Select a physical input";
        foreach (var (k,b) in outputs){Paint(b,k,editable&&k==target,editable||!has);b.Opacity=outputGroup=="All"||k=="NONE"||Groups.First(g=>g.Name==outputGroup).Keys.Contains(k)?1:.3;}
        RefreshMarkers();
        outputsHint.Text=editable?$"Assign {Short(Source)} to:":"Select a control first";
    }

    public void ToggleOverview() => ShowOverview(overview.Visibility != Visibility.Visible);
    /// <summary>A on a control jumps here: focus the tile of its current output.</summary>
    public void FocusOutput(string target) { if (outputs.TryGetValue(target, out var b) && b.IsEnabled) b.Focus(); }
    public void SetTrying(bool on) => ((TextBlock)((StackPanel)tryButton.Content).Children[1]).Text = on ? "Stop test" : "Test mapping";

    // ----- pieces -----
    private static FrameworkElement Step(int n, string title, TextBlock h, double top)
    {
        var badge = new Grid { Width = 28, Height = 28, Margin = new(0, 0, 12, 0), Children = { new System.Windows.Shapes.Ellipse { Fill = new SolidColorBrush(Kit.Blue) }, Kit.Text(n.ToString(), 14, "DS.Text", FontWeights.SemiBold).Also(t => t.HorizontalAlignment = HorizontalAlignment.Center) } };
        var row = new DockPanel { Margin = new(0, top, 0, 8) };
        h.HorizontalAlignment = HorizontalAlignment.Right; DockPanel.SetDock(h, Dock.Right);
        DockPanel.SetDock(badge, Dock.Left); row.Children.Add(badge); row.Children.Add(h); row.Children.Add(Kit.Text(title, 16, "DS.Text", FontWeights.SemiBold));
        return row;
    }
    private readonly System.Windows.Shapes.Ellipse pressedRing = Ring(), sendsRing = Ring();
    private static System.Windows.Shapes.Ellipse Ring() => new() { Width = 84, Height = 84, Stroke = new SolidColorBrush(Color.FromRgb(125, 182, 255)), StrokeThickness = 2, Opacity = 0, IsHitTestVisible = false, RenderTransformOrigin = new(.5, .5), RenderTransform = new ScaleTransform(1, 1) };
    private void Pulse(System.Windows.Shapes.Ellipse ring, bool on)
    {
        var s = (ScaleTransform)ring.RenderTransform;
        ring.BeginAnimation(OpacityProperty, null); s.BeginAnimation(ScaleTransform.ScaleXProperty, null); s.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        if (!on) { ring.Opacity = 0; return; }
        if (!animate) { ring.Opacity = .9; return; }
        var grow = Ambient.Rate(new System.Windows.Media.Animation.DoubleAnimation(.86, 1.18, TimeSpan.FromMilliseconds(1100)) { RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever, EasingFunction = DS.EaseOut }, 30);
        s.BeginAnimation(ScaleTransform.ScaleXProperty, grow); s.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        ring.BeginAnimation(OpacityProperty, Ambient.Rate(new System.Windows.Media.Animation.DoubleAnimation(.95, 0, TimeSpan.FromMilliseconds(1100)) { RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever }, 30));
    }
    private Border BuildMappingCard()
    {
        var g = new Grid(); g.ColumnDefinitions.Add(new()); g.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); g.ColumnDefinitions.Add(new());
        g.RowDefinitions.Add(new()); g.RowDefinitions.Add(new() { Height = GridLength.Auto });
        StackPanel Side(string caption, Border face, System.Windows.Shapes.Ellipse ring, TextBlock name, Listen side, string tip)
        {
            face.Width = face.Height = 50; face.CornerRadius = new(25); face.BorderThickness = new(1.4); face.HorizontalAlignment = HorizontalAlignment.Center; face.VerticalAlignment = VerticalAlignment.Center;
            var slot = new Button { Template = Bare(), Cursor = System.Windows.Input.Cursors.Hand, FocusVisualStyle = null, ToolTip = tip, Margin = new(0, 2, 0, 2), Content = new Grid { Width = 66, Height = 66, Background = Brushes.Transparent, Children = { ring, face } } };
            slot.Click += (_, _) => BeginListening(Listening == side ? Listen.None : side);
            slot.MouseEnter += (_, _) => face.BorderThickness = new(2.4); slot.MouseLeave += (_, _) => face.BorderThickness = new(1.4);
            System.Windows.Automation.AutomationProperties.SetName(slot, caption);
            return new() { HorizontalAlignment = HorizontalAlignment.Center, Children = {
                Kit.Text(caption, 11.5, "DS.TextMuted", FontWeights.SemiBold).Also(t => t.HorizontalAlignment = HorizontalAlignment.Center), slot,
                name.Also(t => t.HorizontalAlignment = HorizontalAlignment.Center) } };
        }
        g.Children.Add(Side("YOU PRESS", pressedFace, pressedRing, pressedName, Listen.Source, "Choose a physical input on the artwork, or capture it when connected"));
        var arrow = Kit.Icon("\uE72A", 22, "DS.AccentHi"); arrow.Margin = new(18, 0, 18, 8); Grid.SetColumn(arrow, 1); g.Children.Add(arrow);
        var right = Side("GAME GETS", sendsFace, sendsRing, sendsName, Listen.Target, "Choose an output below, or capture a button when connected"); Grid.SetColumn(right, 2); g.Children.Add(right);
        restore.Style = DS.Style("ActionButton"); restore.Height = 42; restore.MinHeight = 42; restore.Padding = new(22, 0, 22, 0); restore.FontSize = 13.5;
        restore.Click += (_, _) => { if (Source.Length > 0 && draft.CanEdit(Source)) { draft.Reset(Source); Changed?.Invoke(); RefreshMapping(); } };
        var foot = new DockPanel { Margin = new(2, 8, 0, 0) }; DockPanel.SetDock(restore, Dock.Right); foot.Children.Add(restore);
        readOnly.TextWrapping = TextWrapping.Wrap; readOnly.TextTrimming = TextTrimming.None; sentence.TextWrapping = TextWrapping.Wrap; sentence.TextTrimming = TextTrimming.None;
        foot.Children.Add(new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { sentence, readOnly } });
        Grid.SetRow(foot, 1); Grid.SetColumnSpan(foot, 3); g.Children.Add(foot);
        mappingCard.CornerRadius = new(14); mappingCard.BorderThickness = new(1); mappingCard.Padding = new(16, 10, 16, 12); mappingCard.Margin = new(0, 12, 0, 0);
        mappingCard.Background = new SolidColorBrush(Color.FromArgb(18, 255, 255, 255)); mappingCard.Child = g;
        return mappingCard;
    }
    private static void FillFace(Border face, string text, string? key, bool on, bool waiting = false)
    {
        var color = key switch { "Y" => Color.FromRgb(245, 205, 80), "X" => Color.FromRgb(70, 170, 255), "B" => Color.FromRgb(255, 100, 125), "A" => Color.FromRgb(70, 220, 150), _ => (Color?)null };
        bool empty = key == null && !waiting && text == "+";
        var t = Kit.Text(text, text.Length > 2 ? 17 : empty ? 30 : 26, empty ? "DS.TextMuted" : "DS.Text", FontWeights.SemiBold); t.HorizontalAlignment = HorizontalAlignment.Center;
        if (!on && color != null) t.Foreground = new SolidColorBrush(color.Value);
        face.Child = t;
        face.Background = on ? new SolidColorBrush(Color.FromRgb(43,115,222)) : waiting ? new SolidColorBrush(Color.FromArgb(60, 61, 139, 255)) : new SolidColorBrush(Color.FromArgb(empty ? (byte)14 : (byte)30, 255, 255, 255));
        face.BorderBrush = on || waiting ? Brushes.White : color != null ? new SolidColorBrush(Color.FromArgb(150, color.Value.R, color.Value.G, color.Value.B)) : new SolidColorBrush(Color.FromArgb(empty ? (byte)90 : (byte)46, 255, 255, 255));
        face.Effect = on || waiting ? new DropShadowEffect { Color = Kit.Blue, BlurRadius = 22, ShadowDepth = 0, Opacity = .9 } : null;
    }
    private static string Kind(string key) => key switch { "A" or "B" or "X" or "Y" => $"{key} button", "LB" => "Left bumper", "RB" => "Right bumper", "LT" => "Left trigger", "RT" => "Right trigger", "L3" => "Left stick click", "R3" => "Right stick click", "SELECT" => "View button", "START" => "Menu button", "HOME" => "Guide button", "LSTICK_ANALOG" => "Left stick", "RSTICK_ANALOG" => "Right stick", _ when key.StartsWith("DPAD_") => "D-pad " + key[5..].ToLowerInvariant(), _ => key };
    private static string Short(string key) => key switch { "DPAD_UP" => "↑", "DPAD_DOWN" => "↓", "DPAD_LEFT" => "←", "DPAD_RIGHT" => "→", "SELECT" => "View", "START" => "Menu", "HOME" => "Guide", "NONE"=>"Disabled", "LSTICK_ANALOG" => "LS", "RSTICK_ANALOG" => "RS", _ => key };
    private void SetTarget(string target)
    {
        if (Listening == Listen.Target) { Press(target); return; }
        if(Source.Length==0){if(layout.Controls.Any(r=>r.Key==target))Pick(target);return;}
        if (!draft.CanEdit(Source)) return;
        draft.Set(Source, target); Changed?.Invoke();
    }
    private Border BuildShowcase()
    {
        var grid=new Grid{Width=1296};for(int i=0;i<6;i++)grid.ColumnDefinitions.Add(new());
        string[] headings=["FACE","D-PAD","SHOULDERS","STICKS","SYSTEM","SPECIAL"];
        for(int i=0;i<headings.Length;i++)
        {
            var column=new StackPanel{Margin=new(8,10,8,10)};var family=new Border{CornerRadius=new(10),Background=new SolidColorBrush(Color.FromRgb(18,30,50)),BorderBrush=new SolidColorBrush(Color.FromRgb(40,57,82)),BorderThickness=new(1),Margin=new(5,0,5,0),Child=column};Grid.SetColumn(family,i);grid.Children.Add(family);
            column.Children.Add(Kit.Text(headings[i],12,"DS.TextSoft",FontWeights.SemiBold).Also(t=>{t.HorizontalAlignment=HorizontalAlignment.Center;t.Margin=new(0,0,0,8);}));
            if(i==0)column.Children.Add(Diamond(true,("Y","Y"),("X","X"),("B","B"),("A","A")));
            else if(i==1)column.Children.Add(Diamond(false,("DPAD_UP","↑"),("DPAD_LEFT","←"),("DPAD_RIGHT","→"),("DPAD_DOWN","↓")));
            else if(i==2){var shoulders=Pairs(("LB","LB"),("RB","RB"),("LT","LT"),("RT","RT"));shoulders.Width=180;column.Children.Add(shoulders);}
            else if(i==3 || i==4){var pair=new UniformGrid{Columns=2,Rows=1,Height=56,Margin=new(0,40,0,0)};foreach(string key in i==3?new[]{"L3","R3"}:new[]{"SELECT","START"}){var output=Output(key,Short(key),false,0,0,true);output.Margin=new(4);pair.Children.Add(output);}column.Children.Add(pair);}
            else{var special=new StackPanel{Margin=new(0,24,0,0)};var defaults=new Button{Content="Default",Style=DS.Style("ActionButton"),Height=42,MinHeight=42,Margin=new(0,0,0,8)};defaults.Click+=(_,_)=>{if(Source.Length>0&&draft.CanEdit(Source)){draft.Reset(Source);Changed?.Invoke();RefreshMapping();}};special.Children.Add(defaults);if(draft.PcMode){var disabled=Output("NONE","Disabled",false,0,0,true);disabled.Height=40;special.Children.Add(disabled);}column.Children.Add(special);}
        }
        var header=new DockPanel{Margin=new(0,0,0,10)};header.Children.Add(showcaseTitle);
        return new Border{CornerRadius=new(16),Padding=new(16,12,16,10),BorderThickness=new(1),BorderBrush=new SolidColorBrush(Color.FromArgb(60,125,170,230)),Background=new SolidColorBrush(Color.FromArgb(80,8,17,38)),Child=new StackPanel{Children={header,new Viewbox{Child=grid,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,MaxHeight=176}}}};
    }
    private void RefreshMarkers()
    {
        markers.Children.Clear();
        foreach(var (key,hot) in regions)
        {
            bool modified=draft.CanEdit(key)&&draft.Target(key)!=key;
            hot.ToolTip=modified?$"{label(key)} → {TargetName(draft.Target(key))}":label(key);
            if(!modified)continue;
            var dot=new System.Windows.Shapes.Ellipse{Width=16,Height=16,Fill=Brushes.LightSkyBlue,Stroke=Brushes.White,StrokeThickness=2};
            Canvas.SetLeft(dot,Canvas.GetLeft(hot)+hot.Width-10);Canvas.SetTop(dot,Canvas.GetTop(hot)-5);markers.Children.Add(dot);
        }
        if(regions.TryGetValue(Source,out var active)&&draft.CanEdit(Source))
        {
            mappingLabel.Text=$"{Short(Source)} → {Short(draft.Target(Source))}";mappingLabel.Background=new SolidColorBrush(Color.FromArgb(210,9,22,45));mappingLabel.Padding=new(12,5,12,5);
            Canvas.SetLeft(mappingLabel,Math.Clamp(Canvas.GetLeft(active)+active.Width+16,12,1280));Canvas.SetTop(mappingLabel,Math.Max(12,Canvas.GetTop(active)-20));markers.Children.Add(mappingLabel);
        }
    }
    private Canvas Diamond(bool round, (string Key, string Label) t, (string Key, string Label) l, (string Key, string Label) r, (string Key, string Label) b)
    {
        double s = round ? 50 : 48, w = 172, h = 150; var c = new Canvas { Width = w, Height = h, HorizontalAlignment = HorizontalAlignment.Center };
        void Put((string Key, string Label) k, double x, double y) { var o = Output(k.Key, k.Label, round, s, s, true); Canvas.SetLeft(o, x); Canvas.SetTop(o, y); c.Children.Add(o); }
        Put(t, (w - s) / 2, 0); Put(l, 0, (h - s) / 2); Put(r, w - s, (h - s) / 2); Put(b, (w - s) / 2, h - s);
        return c;
    }
    private UniformGrid Pairs(params (string Key, string Label)[] items)
    {
        var u = new UniformGrid { Columns = 2, Rows = 2, Width = 220, Height = 124, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        foreach (var (k, l) in items) { var o = Output(k, l, false, 0, 0, true); o.Margin = new(5); u.Children.Add(o); }
        return u;
    }
    private Button Output(string key, string text, bool round, double w, double h, bool target)
    {
        var face = new TextBlock { Text = text, FontFamily = DS.Display, FontSize = round ? 20 : 18, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var border = new Border { Child = face, CornerRadius = new(round ? w / 2 : 18), BorderThickness = new(1.2) };
        border.SetBinding(Border.CornerRadiusProperty,new System.Windows.Data.Binding("ActualHeight"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self),Converter=new HalfHeightRadius()});
        var b = new Button { Content = border, Template = Bare(), Cursor = System.Windows.Input.Cursors.Hand, Tag = key, ToolTip = label(key), FocusVisualStyle = null };
        if (w > 0) { b.Width = w; b.Height = h; }
        if (target && TargetAllowed(key)) { b.Click += (_, _) => SetTarget(key); outputs[key] = b; }
        else b.IsEnabled = false;
        b.GotKeyboardFocus += (_, _) => border.BorderBrush = Brushes.White; b.LostKeyboardFocus += (_, _) => Paint(b, key, false, b.IsEnabled);
        Paint(b, key, false, b.IsEnabled);
        return b;
    }
    private static void Paint(Button b, string key, bool on, bool enabled)
    {
        var border = (Border)b.Content; var face = (TextBlock)border.Child;
        var color = key switch { "Y" => Color.FromRgb(245, 205, 80), "X" => Color.FromRgb(70, 170, 255), "B" => Color.FromRgb(255, 100, 125), "A" => Color.FromRgb(70, 220, 150), _ => (Color?)null };
        b.IsEnabled = enabled;
        border.Opacity = enabled ? 1 : .45;
        border.BorderThickness = new(on ? 2 : 1.2);
        border.Background = on ? new SolidColorBrush(Color.FromRgb(43,115,222)) : new SolidColorBrush(Color.FromRgb(29,44,67));
        border.BorderBrush = on ? Brushes.White : color != null ? new SolidColorBrush(Color.FromArgb(150, color.Value.R, color.Value.G, color.Value.B)) : new SolidColorBrush(Color.FromArgb(46, 255, 255, 255));
        face.Foreground = on || color == null ? DS.Brush("DS.Text") : new SolidColorBrush(color.Value);
        border.Effect = null;
    }
    private void ShowOverview(bool on)
    {
        overview.Visibility = on ? Visibility.Visible : Visibility.Collapsed; workspace.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
        ((TextBlock)((StackPanel)allButton.Content).Children[1]).Text = on ? "Back to control" : "All assignments";
        if (on) BuildOverview();
    }
    private void BuildOverview()
    {
        overview.Children.Clear(); overview.Children.Add(Kit.Section("All assignments", 0));
        if (model.Id != "x20" && !draft.PcMode) { overview.Children.Add(Kit.Info("No verified mappings for this model yet.")); return; }
        foreach (var (name, _, keys) in Groups.Take(4))
        {
            var rows = keys.Where(ButtonDraft.Sources.Contains).Select(k => (label(k), draft.Target(k) == k ? "Default" : "→ " + label(draft.Target(k)), draft.Target(k) == k ? (string?)null : "DS.AccentHi")).ToArray();
            if (rows.Length == 0) continue;
            overview.Children.Add(Kit.Text(name.ToUpperInvariant(), 12, "DS.TextSoft", FontWeights.SemiBold).Also(t => t.Margin = new(0, 8, 0, 4)));
            overview.Children.Add(Kit.Table(rows));
        }
    }
    private static Button ActionButton(string icon, string text, string style) => new()
    {
        Style = DS.Style(style),
        Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = icon, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 17, Foreground = DS.Brush("DS.Text"), VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 0, 12, 0) }, new TextBlock { Text = text, FontFamily = DS.Display, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = DS.Brush("DS.Text"), VerticalAlignment = VerticalAlignment.Center } } }
    };
    private static MenuItem Menu(string header, Action act) { var m = new MenuItem { Header = header }; m.Click += (_, _) => act(); return m; }
    private static ControlTemplate Bare() { var t = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) }; t.Seal(); return t; }
    private static T? FindLast<T>(DependencyObject root) where T : DependencyObject
    {
        T? found = null;
        void Walk(DependencyObject d) { foreach (var c in LogicalTreeHelper.GetChildren(d)) if (c is DependencyObject o) { if (o is T t) found = t; Walk(o); } }
        Walk(root); return found;
    }
}

internal static class FluentExtensions
{
    public static T Also<T>(this T value, Action<T> act) { act(value); return value; }
}
