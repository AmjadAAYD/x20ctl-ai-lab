using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using X20Ctl.Zone.Lighting;
namespace X20Ctl.Product;

internal sealed class LightingWindow : Window
{
    readonly TextBox hex=new(){MaxLength=7,Padding=new(12,8,12,8)};
    readonly TextBlock heroHexText=new(){FontSize=20,FontWeight=FontWeights.SemiBold},heroRgbText=new(){FontSize=12.5,Margin=new(0,2,0,0)},status=new(){Text="Instant Apply is ON — pick any colour or mode to update immediately.",TextWrapping=TextWrapping.Wrap,Margin=new(0,12,0,0),MinHeight=38};
    readonly Border preview=new(){BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),Height=96,CornerRadius=new(14),Padding=new(18,14,18,14),Margin=new(0,0,0,16)};
    readonly Button read=ActionButton("Read controller"),apply=ActionButton("Apply");
    readonly ComboBox modes=new(){ItemsSource=new[]{"Solid","Rainbow","Breathe","Cycle","Off","Battery"},SelectedIndex=0,MinWidth=115,Margin=new(0,0,8,0),Visibility=Visibility.Collapsed};
    readonly Button[] modePills=new Button[6];
    readonly Button instantToggleBtn=new(){Content="Instant Apply: ON",Padding=new(12,6,12,6),FontSize=12,Margin=new(12,0,0,0),Style=DS.Style("DS.Primary"),ToolTip="Immediately writes colour and mode changes to the controller over BLE."};
    readonly System.Windows.Threading.DispatcherTimer instantDebounce=new(){Interval=TimeSpan.FromMilliseconds(75)};
    bool instantApply=true;
    readonly Slider brightness=new(){Minimum=0,Maximum=100,Value=100,SmallChange=1,LargeChange=10,TickFrequency=1,IsSnapToTickEnabled=true,MinHeight=28,Margin=new(0,4,0,4),ToolTip="Controller brightness (0x00 Off .. 0x01 Moonlight .. 0xFF Max)."};
    readonly TextBlock brightnessValue=new(){Text="100% (0xFF)",HorizontalAlignment=HorizontalAlignment.Right};
    readonly WrapPanel viewTabs=new();
    readonly Border leftLedBar=new(){Width=56,Height=11,CornerRadius=new(5.5),BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),Margin=new(0,0,6,0)};
    readonly Border rightLedBar=new(){Width=56,Height=11,CornerRadius=new(5.5),BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),Margin=new(0,0,10,0)};
    readonly TextBlock ledBarLabel=new(){FontSize=12,VerticalAlignment=VerticalAlignment.Center};
    readonly Button[] sectionBtns=new Button[3];
    FrameworkElement? staticView,effectsView,presetsView,actionFooter;
    LightingEffectsSection? effectsSection;
    int activeSection;
    bool syncingBrightness;
    readonly HueWheel wheel=new();
    readonly ValueStrip valueStrip=new();
    readonly List<(Button Button,int Rgb)> swatches=[];
    readonly StackPanel batteryPanel=new(){Visibility=Visibility.Collapsed,Margin=new(0,12,0,16)};
    readonly Button[] levelButtons=new Button[4];
    readonly int[] levelColours=(int[])BatteryLightingService.Load().Colours.Clone();
    static readonly byte[] ModeValues=[2,1,3,4,0];
    static readonly (string Name, int Rgb)[] CuratedSwatches =
    [
        ("Electric Cyan", 0x00E5FF), ("Sky Blue", 0x2979FF), ("Royal Indigo", 0x5E35B1), ("Neon Violet", 0xAA00FF),
        ("Hot Magenta", 0xFF00AA), ("Crimson Red", 0xFF1744), ("Lava Orange", 0xFF6D00), ("Amber Gold", 0xFFAB00),
        ("Solar Yellow", 0xFFD600), ("Lime Pulse", 0x76FF03), ("Emerald", 0x00E676), ("Pure White", 0xFFFFFF),
    ];
    const int BatteryMode=5;
    int selected=0xff0000,editingLevel=3; double hue,saturation=1,value=1; bool busy,loadingLevel,pendingInstantWrite; byte level=0x50;
    bool BatterySelected=>modes.SelectedIndex==BatteryMode;
    static Button ActionButton(string text)=>new(){Content=text,Style=DS.Style("ActionButton"),Padding=new(20,10,20,10),MinWidth=130};
    public LightingWindow()
    {
        Title="X20 Pro · RGB";Width=1100;Height=800;MinWidth=820;MinHeight=680;
        Background=DS.Brush("DS.Deep");Foreground=DS.Brush("DS.Text");FontFamily=DS.Body;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        status.Foreground=DS.Brush("DS.TextSoft");status.FontSize=13.5;status.Margin=new(0);status.MinHeight=0;
        var heroStack=new StackPanel{VerticalAlignment=VerticalAlignment.Center};heroStack.Children.Add(heroHexText);heroStack.Children.Add(heroRgbText);preview.Child=heroStack;
        var root=new Grid();root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new());root.RowDefinitions.Add(new(){Height=GridLength.Auto});Content=root;
        var columns=new Grid();columns.ColumnDefinitions.Add(new(){Width=new(1.15,GridUnitType.Star)});columns.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});staticView=columns;Grid.SetRow(columns,1);root.Children.Add(columns);
        var panel=new StackPanel();var detail=new StackPanel();
        var left=Kit.Panel("Colour Wheel", "Spin the wheel or tap a swatch for instant controller sync.",null,new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},new Thickness(22));left.Margin=new(0,0,16,0);columns.Children.Add(left);
        var right=Kit.Panel("Mode & Brightness",null,null,new ScrollViewer{Content=detail,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},new Thickness(22));Grid.SetColumn(right,1);columns.Children.Add(right);
        var picker=new Grid{Height=248,Margin=new(0,4,0,18)};
        picker.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});picker.ColumnDefinitions.Add(new(){Width=new(28)});
        wheel.Width=244;wheel.Height=244;wheel.HorizontalAlignment=HorizontalAlignment.Center;wheel.VerticalAlignment=VerticalAlignment.Center;wheel.ToolTip="Hue and saturation wheel";
        valueStrip.Margin=new(10,8,0,8);valueStrip.ToolTip="Lightness / intensity";
        Grid.SetColumn(valueStrip,1);picker.Children.Add(wheel);picker.Children.Add(valueStrip);panel.Children.Add(picker);
        detail.Children.Add(preview);
        var brightnessHeader=new Grid{Margin=new(0,4,0,2)};brightnessHeader.ColumnDefinitions.Add(new());brightnessHeader.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        brightnessHeader.Children.Add(Kit.Text("Brightness",13,"DS.TextSoft"));brightnessValue.Foreground=DS.Brush("DS.TextSoft");Grid.SetColumn(brightnessValue,1);brightnessHeader.Children.Add(brightnessValue);
        System.Windows.Automation.AutomationProperties.SetName(brightness,"RGB brightness");
        detail.Children.Add(brightnessHeader);detail.Children.Add(brightness);
        var quickBright=new WrapPanel{Margin=new(0,2,0,4)};
        var moonlightBtn=new Button{Content="Moonlight (1%)",Style=DS.Style("ActionButton"),Padding=new(12,5,12,5),FontSize=12,Margin=new(0,0,8,0),ToolTip="Ultra-faint 1% moonlight glow (0x01)"};
        moonlightBtn.Click+=(_,_)=>{if(!busy){SetBrightnessByte(LightingRecord.MoonlightBrightnessByte);TriggerInstantApply(immediate:true);}};
        var offBrightBtn=new Button{Content="0% Off",Style=DS.Style("ActionButton"),Padding=new(12,5,12,5),FontSize=12,ToolTip="Turn LEDs off while keeping colour/mode (0x00)"};
        offBrightBtn.Click+=(_,_)=>{if(!busy){SetBrightnessByte(LightingRecord.MinBrightnessByte);TriggerInstantApply(immediate:true);}};
        quickBright.Children.Add(moonlightBtn);quickBright.Children.Add(offBrightBtn);detail.Children.Add(quickBright);
        brightness.ValueChanged+=(_,_)=>{if(syncingBrightness||busy)return;level=LightingRecord.BrightnessByteFor(brightness.Value);brightnessValue.Text=$"{Math.Round(brightness.Value):0}% (0x{level:X2})";TriggerInstantApply();};
        wheel.Changed+=(h,s)=>{hue=h;saturation=s;Refresh();TriggerInstantApply();};valueStrip.Changed+=v=>{value=v;Refresh();TriggerInstantApply();};
        hex.LostFocus+=(_,_)=>CommitHex();hex.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){if(CommitHex())TriggerInstantApply(immediate:true);e.Handled=true;}};
        var palette=new System.Windows.Controls.Primitives.UniformGrid{Columns=6,Margin=new(-4,4,-4,0)};
        foreach(var (name,rgbVal) in CuratedSwatches)
        {
            var b=new Button{Height=40,Margin=new(4),Background=new SolidColorBrush(ToColour(rgbVal)),BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),ToolTip=$"{name} · #{rgbVal:X6}"};
            System.Windows.Automation.AutomationProperties.SetName(b,$"{name}, #{rgbVal:X6}");
            b.Template=SwatchTemplate();
            b.Click+=(_,_)=>{if(!busy){SetColour(rgbVal);TriggerInstantApply(immediate:true);}};palette.Children.Add(b);swatches.Add((b,rgbVal));
        }
        panel.Children.Add(palette);
        var kelvinWrap=new WrapPanel{Margin=new(0,12,0,0)};
        foreach(var (kLabel,kRgb) in new (string,int)[]{("2700K Warm",0xFFB46B),("4000K Neutral",0xFFE6CC),("6500K Cool",0xE3F0FF)})
        {
            var kb=new Button{Content=kLabel,Style=DS.Style("ActionButton"),Padding=new(10,5,10,5),FontSize=11.5,Margin=new(0,0,6,0)};
            kb.Click+=(_,_)=>{if(!busy){SetColour(kRgb);TriggerInstantApply(immediate:true);}};
            kelvinWrap.Children.Add(kb);
        }
        panel.Children.Add(kelvinWrap);
        detail.Children.Add(Kit.Text("Hardware Mode",13,"DS.TextSoft").Also(t=>t.Margin=new(0,18,0,8)));
        detail.Children.Add(modes);
        var modeGrid=new WrapPanel{Margin=new(0,0,0,8)};
        string[] modeNames=["Solid","Rainbow","Breathe","Cycle","Off","Battery"];
        for(int i=0;i<modeNames.Length;i++)
        {
            int idx=i;
            var mb=new Button{Content=modeNames[i],Padding=new(12,6,12,6),FontSize=12.5,Margin=new(0,0,6,6),Style=DS.Style(i==0?"DS.Primary":"ActionButton")};
            mb.Click+=(_,_)=>{if(!busy){modes.SelectedIndex=idx;TriggerInstantApply(immediate:true);}};
            modePills[i]=mb;modeGrid.Children.Add(mb);
        }
        detail.Children.Add(modeGrid);
        batteryPanel.Children.Add(Kit.Text("Battery level colours",13,"DS.TextSoft").Also(t=>t.Margin=new(0,0,0,8)));
        var gauge=new System.Windows.Controls.Primitives.UniformGrid{Columns=4,Margin=new(-3,0,-3,0)};
        for(int i=0;i<4;i++)
        {
            int index=i;var name=$"{i+1}/4";
            var b=new Button{Height=40,Margin=new(3),Template=SwatchTemplate(),ToolTip=$"Battery {name}"};
            b.Click+=(_,_)=>{if(!busy)EditLevel(index);};levelButtons[i]=b;
            var cell=new StackPanel();cell.Children.Add(b);cell.Children.Add(Kit.Text(name,13,"DS.TextSoft").Also(t=>{t.HorizontalAlignment=HorizontalAlignment.Center;t.Margin=new(0,4,0,0);}));gauge.Children.Add(cell);
        }
        batteryPanel.Children.Add(gauge);batteryPanel.Children.Add(Kit.Text("Updates while X20CTL is open.",13,"DS.TextMuted").Also(t=>t.Margin=new(0,10,0,0)));detail.Children.Add(batteryPanel);
        var fineTuneStack=new StackPanel{Margin=new(0,8,0,0)};
        fineTuneStack.Children.Add(Kit.Text("Custom Hex (#RRGGBB)",12.5,"DS.TextSoft").Also(t=>t.Margin=new(0,0,0,6)));
        fineTuneStack.Children.Add(hex);
        read.Margin=new(0,10,0,0);read.HorizontalAlignment=HorizontalAlignment.Left;
        fineTuneStack.Children.Add(read);
        detail.Children.Add(new Expander{Header=Kit.Text("Fine-tune Hex & read controller",12.5,"DS.TextSoft"),Foreground=DS.Brush("DS.TextSoft"),Margin=new(0,12,0,0),Content=fineTuneStack});
        var footer=new Grid();footer.ColumnDefinitions.Add(new());footer.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        status.VerticalAlignment=VerticalAlignment.Center;status.Margin=new(0,0,24,0);footer.Children.Add(status);
        apply.Style=DS.Style("DS.Primary");apply.MinWidth=160;Grid.SetColumn(apply,1);footer.Children.Add(apply);
        var actionPanel=Kit.Panel(null,null,null,footer,new Thickness(20,14,20,14));actionPanel.Margin=new(0,16,0,0);actionFooter=actionPanel;Grid.SetRow(actionPanel,2);root.Children.Add(actionPanel);
        BuildTopStudioBarAndSections(root);
        read.Click+=async(_,_)=>await Run(false);apply.Click+=async(_,_)=>await ApplyActiveSelectionAsync();
        instantDebounce.Tick+=async(_, _)=>{instantDebounce.Stop();await ApplyActiveSelectionAsync();};
        instantToggleBtn.Click+=(_,_)=>{instantApply=!instantApply;instantToggleBtn.Content=instantApply?"Instant Apply: ON":"Instant Apply: OFF";instantToggleBtn.Style=DS.Style(instantApply?"DS.Primary":"ActionButton");status.Text=instantApply?"Instant Apply is ON — changes sync immediately.":"Instant Apply is OFF — click Apply when ready.";};
        modes.SelectionChanged+=(_,_)=>{apply.IsEnabled=!busy&&modes.SelectedIndex>=0;batteryPanel.Visibility=BatterySelected?Visibility.Visible:Visibility.Collapsed;for(int i=0;i<modePills.Length;i++)if(modePills[i]!=null)modePills[i].Style=DS.Style(i==modes.SelectedIndex?"DS.Primary":"ActionButton");if(BatterySelected)EditLevel(editingLevel);};
        SetColour(0xff0000);ShowLevel(0x50);
        if(BatteryLightingService.Load().Enabled)modes.SelectedIndex=BatteryMode;
    }
    internal FrameworkElement DetachBody(){var body=(FrameworkElement)Content;Content=null;body.HorizontalAlignment=HorizontalAlignment.Stretch;return body;}
    /// <summary>Shows what the pad holds as soon as the page opens, so editing starts from the real colour. Read only; a failure just says so.</summary>
    internal void ReadOnOpen(){if(!ReviewSandbox.Active)_=Run(false);}
    internal void ShowBattery(){ShowSection(0);modes.SelectedIndex=BatteryMode;}
    internal void ShowSection(int index)
    {
        activeSection=Math.Clamp(index,0,2);
        if(staticView!=null)staticView.Visibility=activeSection==0?Visibility.Visible:Visibility.Collapsed;
        if(effectsView!=null)effectsView.Visibility=activeSection==1?Visibility.Visible:Visibility.Collapsed;
        if(presetsView!=null)presetsView.Visibility=activeSection==2?Visibility.Visible:Visibility.Collapsed;
        if(actionFooter!=null)actionFooter.Visibility=activeSection==0?Visibility.Visible:Visibility.Collapsed;
        for(int i=0;i<sectionBtns.Length;i++)
            if(sectionBtns[i]!=null)sectionBtns[i].Style=DS.Style(i==activeSection?"DS.Primary":"ActionButton");
    }
    void SetBrightnessByte(byte b)
    {
        ShowLevel(b);
        if(b==LightingRecord.MoonlightBrightnessByte)status.Text="Moonlight 1% (0x01) selected — click Apply to write to controller.";
        else if(b==LightingRecord.MinBrightnessByte)status.Text="0% Off (0x00) selected — click Apply to turn LEDs off while keeping colour.";
    }
    void BuildTopStudioBarAndSections(Grid root)
    {
        effectsSection=new LightingEffectsSection(()=>selected,SetColour,()=>level);
        effectsView=effectsSection.EffectsView;Grid.SetRow(effectsView,1);root.Children.Add(effectsView);
        presetsView=effectsSection.PresetsAndGamesView;Grid.SetRow(presetsView,1);root.Children.Add(presetsView);

        string[] labels=["Colours","Effects Studio","Presets & Auto"];
        for(int i=0;i<labels.Length;i++)
        {
            int idx=i;
            var btn=new Button{Content=labels[i],Padding=new(14,7,14,7),FontSize=12.5,Margin=new(0,0,8,0),Style=DS.Style(i==0?"DS.Primary":"ActionButton")};
            btn.Click+=(_,_)=>ShowSection(idx);
            sectionBtns[i]=btn;
            viewTabs.Children.Add(btn);
        }

        ledBarLabel.Foreground=DS.Brush("DS.TextSoft");
        var ledPreviewStrip=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,Margin=new(0,2,0,2)};
        ledPreviewStrip.Children.Add(Kit.Text("L ",11.5,"DS.TextMuted").Also(t=>t.VerticalAlignment=VerticalAlignment.Center));
        ledPreviewStrip.Children.Add(leftLedBar);
        ledPreviewStrip.Children.Add(Kit.Text("R ",11.5,"DS.TextMuted").Also(t=>t.VerticalAlignment=VerticalAlignment.Center));
        ledPreviewStrip.Children.Add(rightLedBar);
        ledPreviewStrip.Children.Add(ledBarLabel);
        ledPreviewStrip.Children.Add(instantToggleBtn);

        var headerWrap=new WrapPanel{VerticalAlignment=VerticalAlignment.Center};
        viewTabs.Margin=new(0,0,14,0);
        headerWrap.Children.Add(viewTabs);
        headerWrap.Children.Add(ledPreviewStrip);

        var headerCard=Kit.Panel(null,null,null,headerWrap,new Thickness(14,8,14,8));
        headerCard.Margin=new(0,0,0,12);
        Grid.SetRow(headerCard,0);
        root.Children.Add(headerCard);

        ShowSection(0);
        var previewTimer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(50)};
        previewTimer.Tick+=(_,_)=>UpdateDualLedPreview();
        root.Loaded+=(_,_)=>{UpdateDualLedPreview();previewTimer.Start();};
        root.Unloaded+=(_,_)=>previewTimer.Stop();
        UpdateDualLedPreview();
    }
    void UpdateDualLedPreview()
    {
        LightingFrame frame;
        if(LightingEffectsService.Status.IsRunning || activeSection!=0)
            frame=LightingEffectsService.EvaluatePreview();
        else
            frame=new((byte)((selected>>16)&0xff),(byte)((selected>>8)&0xff),(byte)(selected&0xff),level,ModeValues[Math.Clamp(modes.SelectedIndex,0,ModeValues.Length-1)]);

        double alpha=frame.Brightness==0?0.06:Math.Clamp(0.14+0.86*(frame.Brightness/255.0),0.06,1.0);
        var glowColor=Color.FromArgb((byte)Math.Round(alpha*255),frame.R,frame.G,frame.B);
        var brush=new SolidColorBrush(glowColor);
        leftLedBar.Background=rightLedBar.Background=brush;
        var shadow=new System.Windows.Media.Effects.DropShadowEffect{Color=Color.FromRgb(frame.R,frame.G,frame.B),BlurRadius=frame.Brightness>0?14:0,ShadowDepth=0,Opacity=frame.Brightness>0?0.85:0};
        leftLedBar.Effect=rightLedBar.Effect=shadow;
        ledBarLabel.Text=$"{frame.Hex} · {Math.Round(LightingRecord.BrightnessPercentFor(frame.Brightness))}%";
    }
    void TriggerInstantApply(bool immediate=false)
    {
        if(!instantApply||ReviewSandbox.Active)return;
        if(immediate){instantDebounce.Stop();_=ApplyActiveSelectionAsync();}
        else{instantDebounce.Stop();instantDebounce.Start();}
    }
    async Task ApplyActiveSelectionAsync()
    {
        if(busy){pendingInstantWrite=true;return;}
        if(!CommitHex()||modes.SelectedIndex<0)return;
        await LightingEffectsService.StopAsync(restoreBaseline:false);
        if(BatterySelected)await ApplyBattery();
        else await Run(true,ModeValues[modes.SelectedIndex]);
        if(pendingInstantWrite){pendingInstantWrite=false;TriggerInstantApply(immediate:true);}
    }
    static Color ToColour(int colour)=>Color.FromRgb((byte)(colour>>16),(byte)(colour>>8),(byte)colour);
    static ControlTemplate SwatchTemplate()
    {
        var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(6));
        foreach(var prop in new[]{Border.BackgroundProperty,Border.BorderBrushProperty,Border.BorderThicknessProperty})
            border.SetBinding(prop,new System.Windows.Data.Binding(prop.Name){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
        return new ControlTemplate(typeof(Button)){VisualTree=border};
    }
    void EditLevel(int index){editingLevel=index;loadingLevel=true;SetColour(levelColours[index]);loadingLevel=false;RefreshLevels();}
    void RefreshLevels()
    {
        for(int i=0;i<4;i++)
        {
            var b=levelButtons[i];b.Background=new SolidColorBrush(ToColour(levelColours[i]));
            b.BorderBrush=i==editingLevel?DS.Brush("DS.AccentHi"):DS.Brush("DS.Hairline");b.BorderThickness=new(i==editingLevel?3:1);
            System.Windows.Automation.AutomationProperties.SetName(b,$"Battery {i+1} of 4, #{levelColours[i]:X6}");
        }
    }
    bool CommitHex()
    {
        var text=hex.Text.Trim().TrimStart('#');
        if(text.Length!=6||!int.TryParse(text,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out int colour)){status.Text="Enter a six-digit colour such as #FF8000.";return false;}
        SetColour(colour);return true;
    }
    void SetColour(int colour)
    {
        var c=ToColour(colour);double r=c.R/255d,g=c.G/255d,b=c.B/255d,max=Math.Max(r,Math.Max(g,b)),min=Math.Min(r,Math.Min(g,b)),delta=max-min;
        if(delta>0)hue=((max==r?(g-b)/delta:max==g?(b-r)/delta+2:(r-g)/delta+4)*60+360)%360;
        if(delta>0||max>0)saturation=max==0?saturation:delta/max;value=max;Refresh();
    }
    void Refresh()
    {
        selected=ColourPalette.FromHsv(hue,saturation,value);var c=ToColour(selected);preview.Background=new SolidColorBrush(c);
        preview.Effect=new System.Windows.Media.Effects.DropShadowEffect{Color=c,BlurRadius=22,ShadowDepth=0,Opacity=0.45};
        double luma=(0.299*c.R+0.587*c.G+0.114*c.B)/255d;
        var fg=new SolidColorBrush(luma>0.58?Color.FromRgb(10,18,38):Color.FromRgb(244,247,253));
        heroHexText.Foreground=heroRgbText.Foreground=fg;
        heroHexText.Text=$"#{selected:X6}";heroRgbText.Text=$"R {c.R}  ·  G {c.G}  ·  B {c.B}";
        hex.Text=$"#{selected:X6}";
        wheel.Set(hue,saturation,value);valueStrip.Set(hue,saturation,value);
        foreach(var(button,colour)in swatches){button.BorderBrush=colour==selected?DS.Brush("DS.AccentHi"):DS.Brush("DS.Hairline");button.BorderThickness=new(colour==selected?3:1);}
        if(BatterySelected&&!loadingLevel){levelColours[editingLevel]=selected;RefreshLevels();}
    }
    void Lock(bool locked)
    {
        busy=locked;read.IsEnabled=modes.IsEnabled=batteryPanel.IsEnabled=brightness.IsEnabled=!locked;apply.IsEnabled=!locked&&modes.SelectedIndex>=0;
        foreach(var(button,_)in swatches)button.IsEnabled=!locked;
    }
    async Task Run(bool write,byte? mode=null)
    {
        if(busy)return;
        Lock(true);
        var c=ToColour(selected);byte[] colour=[c.R,c.G,c.B];status.Text=write?"Applying lighting…":"Reading controller…";
        try
        {
            if(write)BatteryLightingService.Disable();
            var record=await LightingBleClient.RunAsync(write?colour:null,mode,write?level:null);
            ShowLevel(record[5]);if(!BatterySelected){SetColour((record[0]<<16)|(record[1]<<8)|record[2]);modes.SelectedIndex=Array.IndexOf(ModeValues,record[6]);}
            if(write)LightingEffectsService.RecordExternalNativeState(record[0],record[1],record[2],record[5],record[6]);
            status.Text=write||mode.HasValue?"Applied. Controller read-back matches.":"Stored colour read. The active effect may change its appearance.";
        }
        catch(Exception ex){status.Text=ex.Message;}
        finally{Lock(false);}
    }
    void ShowLevel(byte stored)
    {
        level=stored;syncingBrightness=true;try{brightness.Value=Math.Round(LightingRecord.BrightnessPercentFor(stored));}finally{syncingBrightness=false;}
        brightnessValue.Text=stored==1?"<1% (0x01 Moonlight)":$"{brightness.Value:0}% (0x{stored:X2})";
    }
    async Task ApplyBattery()
    {
        if(busy)return;
        Lock(true);status.Text="Applying lighting…";
        try{int level=await BatteryLightingService.ApplyAsync((int[])levelColours.Clone());status.Text=$"Battery {level}/4. Controller read-back matches.";}
        catch(Exception ex){status.Text=ex.Message;}
        finally{Lock(false);}
    }
    sealed class HueWheel:FrameworkElement
    {
        double hue,sat=1,val=1;
        static System.Windows.Media.Imaging.WriteableBitmap? cachedWheel;
        public event Action<double,double>? Changed;
        public void Set(double h,double s,double v){hue=h;sat=s;val=v;InvalidateVisual();}
        protected override void OnInitialized(EventArgs e){base.OnInitialized(e);Focusable=true;Cursor=Cursors.Cross;System.Windows.Automation.AutomationProperties.SetName(this,"Hue and saturation wheel");}
        static System.Windows.Media.Imaging.WriteableBitmap WheelBitmap()
        {
            if(cachedWheel!=null)return cachedWheel;
            const int size=240;const double r=size/2d;
            var wb=new System.Windows.Media.Imaging.WriteableBitmap(size,size,96,96,PixelFormats.Bgra32,null);
            byte[] buf=new byte[size*size*4];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                double dx=x+0.5-r,dy=y+0.5-r,dist=Math.Sqrt(dx*dx+dy*dy);
                int idx=(y*size+x)*4;
                if(dist>r){buf[idx+3]=0;continue;}
                double angle=(Math.Atan2(dy,dx)*180/Math.PI+360)%360;
                int rgb=ColourPalette.FromHsv(angle,Math.Clamp(dist/r,0,1),1);
                byte alpha=(byte)(dist>r-1.5?Math.Clamp((r-dist)/1.5,0,1)*255:255);
                buf[idx]=(byte)rgb;buf[idx+1]=(byte)(rgb>>8);buf[idx+2]=(byte)(rgb>>16);buf[idx+3]=alpha;
            }
            wb.WritePixels(new Int32Rect(0,0,size,size),buf,size*4,0);
            wb.Freeze();
            return cachedWheel=wb;
        }
        protected override void OnRender(DrawingContext dc)
        {
            double d=Math.Min(ActualWidth,ActualHeight);if(d<=4)return;
            var rect=new Rect((ActualWidth-d)/2,(ActualHeight-d)/2,d,d);
            dc.DrawImage(WheelBitmap(),rect);
            double rad=hue*Math.PI/180d,r=d/2d;
            var pt=new Point(rect.X+r+Math.Cos(rad)*sat*r,rect.Y+r+Math.Sin(rad)*sat*r);
            dc.DrawEllipse(null,new Pen(Brushes.Black,3.5),pt,7,7);
            dc.DrawEllipse(null,new Pen(Brushes.White,2),pt,7,7);
        }
        void Pick(Point p)
        {
            double d=Math.Min(ActualWidth,ActualHeight);if(d<=4)return;
            double cx=ActualWidth/2d,cy=ActualHeight/2d,r=d/2d;
            double dx=p.X-cx,dy=p.Y-cy;
            double h=(Math.Atan2(dy,dx)*180/Math.PI+360)%360;
            double s=Math.Clamp(Math.Sqrt(dx*dx+dy*dy)/r,0,1);
            Changed?.Invoke(h,s);
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){Focus();CaptureMouse();Pick(e.GetPosition(this));e.Handled=true;}
        protected override void OnMouseMove(MouseEventArgs e){if(IsMouseCaptured)Pick(e.GetPosition(this));}
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(IsMouseCaptured)ReleaseMouseCapture();}
    }
    sealed class ValueStrip:FrameworkElement
    {
        double hue,sat=1,val=1;public event Action<double>? Changed;
        public void Set(double h,double s,double v){hue=h;sat=s;val=v;InvalidateVisual();}
        protected override void OnInitialized(EventArgs e){base.OnInitialized(e);Focusable=true;Cursor=Cursors.SizeNS;System.Windows.Automation.AutomationProperties.SetName(this,"Lightness");}
        protected override void OnRender(DrawingContext dc)
        {
            var bounds=new Rect(0,0,ActualWidth,ActualHeight);
            var full=ToColour(ColourPalette.FromHsv(hue,sat,1));
            dc.DrawRoundedRectangle(new LinearGradientBrush(full,Colors.Black,90),new Pen(DS.Brush("DS.Hairline"),1),bounds,8,8);
            double y=(1-val)*ActualHeight;
            dc.DrawRoundedRectangle(null,new Pen(Brushes.White,2),new Rect(0,Math.Clamp(y-4,0,Math.Max(0,ActualHeight-8)),ActualWidth,8),4,4);
        }
        void Pick(Point p){Changed?.Invoke(1-Math.Clamp(p.Y/Math.Max(1,ActualHeight),0,1));}
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){Focus();CaptureMouse();Pick(e.GetPosition(this));e.Handled=true;}
        protected override void OnMouseMove(MouseEventArgs e){if(IsMouseCaptured)Pick(e.GetPosition(this));}
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(IsMouseCaptured)ReleaseMouseCapture();}
    }
}