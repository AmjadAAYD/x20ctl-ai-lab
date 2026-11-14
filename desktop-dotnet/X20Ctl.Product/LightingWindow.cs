using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using X20Ctl.Zone.Lighting;
namespace X20Ctl.Product;

internal sealed class LightingWindow : Window
{
    readonly TextBox hex=new(){MaxLength=7,Padding=new(12,9,12,9)};
    readonly TextBlock rgb=new(){Margin=new(0,8,0,0)},status=new(){Text="Choose a colour and effect, then Apply.",TextWrapping=TextWrapping.Wrap,Margin=new(0,12,0,0),MinHeight=42};
    readonly Border preview=new(){BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),Height=180,CornerRadius=new(12),Margin=new(0,0,0,18)};
    readonly Button read=ActionButton("Read controller"),apply=ActionButton("Apply");
    readonly ComboBox modes=new(){ItemsSource=new[]{"Solid","Rainbow","Breathe","Cycle","Off","Battery"},SelectedIndex=0,MinWidth=115,Margin=new(0,0,8,0)};
    readonly Slider brightness=new(){Minimum=0,Maximum=100,Value=100,SmallChange=1,LargeChange=10,TickFrequency=1,IsSnapToTickEnabled=true,MinHeight=28,Margin=new(0,4,0,4),ToolTip="Controller brightness (0x00 Off .. 0x01 Moonlight .. 0xFF Max)."};
    readonly TextBlock brightnessValue=new(){Text="100% (0xFF)",HorizontalAlignment=HorizontalAlignment.Right};
    readonly WrapPanel viewTabs=new();
    readonly Border leftLedBar=new(){Width=48,Height=10,CornerRadius=new(5),BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),Margin=new(0,0,6,0)};
    readonly Border rightLedBar=new(){Width=48,Height=10,CornerRadius=new(5),BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),Margin=new(0,0,8,0)};
    readonly TextBlock ledBarLabel=new(){FontSize=12,VerticalAlignment=VerticalAlignment.Center};
    readonly Button[] sectionBtns=new Button[3];
    FrameworkElement? staticView,effectsView,presetsView,actionFooter;
    LightingEffectsSection? effectsSection;
    int activeSection;
    bool syncingBrightness;
    readonly ColourField square=new(false),hueStrip=new(true);
    readonly List<(Button Button,int Rgb)> swatches=[];
    readonly StackPanel batteryPanel=new(){Visibility=Visibility.Collapsed,Margin=new(0,0,0,24)};
    readonly Button[] levelButtons=new Button[4];
    readonly int[] levelColours=(int[])BatteryLightingService.Load().Colours.Clone();
    static readonly byte[] ModeValues=[2,1,3,4,0];
    const int BatteryMode=5;
    int selected=0xff0000,editingLevel=3; double hue,saturation=1,value=1; bool busy,loadingLevel; byte level=0x50;
    bool BatterySelected=>modes.SelectedIndex==BatteryMode;
    static Button ActionButton(string text)=>new(){Content=text,Style=DS.Style("ActionButton"),Padding=new(20,10,20,10),MinWidth=130};
    public LightingWindow()
    {
        Title="X20 Pro · RGB";Width=1100;Height=800;MinWidth=820;MinHeight=680;
        Background=DS.Brush("DS.Deep");Foreground=DS.Brush("DS.Text");FontFamily=DS.Body;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        rgb.Foreground=DS.Brush("DS.TextSoft");rgb.FontSize=15;
        status.Foreground=DS.Brush("DS.TextSoft");status.FontSize=14;status.Margin=new(0);status.MinHeight=0;
        preview.BorderBrush=DS.Brush("DS.Hairline");
        var root=new Grid();root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new());root.RowDefinitions.Add(new(){Height=GridLength.Auto});Content=root;
        var columns=new Grid();columns.ColumnDefinitions.Add(new(){Width=new(1.6,GridUnitType.Star)});columns.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});staticView=columns;Grid.SetRow(columns,1);root.Children.Add(columns);
        var panel=new StackPanel();var detail=new StackPanel();
        var left=Kit.Panel("Colour", "Choose from the palette or create a custom colour.",null,new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},new Thickness(24));left.Margin=new(0,0,16,0);columns.Children.Add(left);
        var right=Kit.Panel("Selected colour",null,null,new ScrollViewer{Content=detail,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},new Thickness(24));Grid.SetColumn(right,1);columns.Children.Add(right);
        var picker=new Grid{Height=260,Margin=new(0,8,0,20)};
        picker.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});picker.ColumnDefinitions.Add(new(){Width=new(26)});
        square.Margin=new(0,0,14,0);square.ToolTip="Saturation and brightness · arrow keys adjust";hueStrip.ToolTip="Hue · up/down keys adjust";
        Grid.SetColumn(hueStrip,1);picker.Children.Add(square);picker.Children.Add(hueStrip);panel.Children.Add(picker);
        detail.Children.Add(preview);detail.Children.Add(Kit.Text("Hex colour",13,"DS.TextSoft").Also(t=>t.Margin=new(0,0,0,8)));detail.Children.Add(hex);detail.Children.Add(rgb);
        var brightnessHeader=new Grid{Margin=new(0,20,0,2)};brightnessHeader.ColumnDefinitions.Add(new());brightnessHeader.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        brightnessHeader.Children.Add(Kit.Text("Brightness",13,"DS.TextSoft"));brightnessValue.Foreground=DS.Brush("DS.TextSoft");Grid.SetColumn(brightnessValue,1);brightnessHeader.Children.Add(brightnessValue);
        System.Windows.Automation.AutomationProperties.SetName(brightness,"RGB brightness");
        detail.Children.Add(brightnessHeader);detail.Children.Add(brightness);
        var quickBright=new WrapPanel{Margin=new(0,2,0,4)};
        var moonlightBtn=new Button{Content="Moonlight (0x01)",Style=DS.Style("ActionButton"),Padding=new(10,4,10,4),FontSize=12,Margin=new(0,0,8,0),ToolTip="Ultra-faint 1% moonlight glow (0x01)"};
        moonlightBtn.Click+=(_,_)=>{if(!busy)SetBrightnessByte(LightingRecord.MoonlightBrightnessByte);};
        var offBrightBtn=new Button{Content="0% Off (0x00)",Style=DS.Style("ActionButton"),Padding=new(10,4,10,4),FontSize=12,ToolTip="Turn LEDs off while keeping colour/mode (0x00)"};
        offBrightBtn.Click+=(_,_)=>{if(!busy)SetBrightnessByte(LightingRecord.MinBrightnessByte);};
        quickBright.Children.Add(moonlightBtn);quickBright.Children.Add(offBrightBtn);detail.Children.Add(quickBright);
        brightness.ValueChanged+=(_,_)=>{if(syncingBrightness||busy)return;level=LightingRecord.BrightnessByteFor(brightness.Value);brightnessValue.Text=$"{Math.Round(brightness.Value):0}% (0x{level:X2})";};
        square.Changed+=(x,y)=>{saturation=x;value=1-y;Refresh();};hueStrip.Changed+=(_,y)=>{hue=y*360;Refresh();};
        hex.LostFocus+=(_,_)=>CommitHex();hex.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){CommitHex();e.Handled=true;}};
        var palette=new System.Windows.Controls.Primitives.UniformGrid{Columns=12,Margin=new(-3,10,-3,0)};
        foreach(var preset in ColourPalette.Presets)
        {
            var b=new Button{Height=40,Margin=new(3),Background=new SolidColorBrush(ToColour(preset.Rgb)),BorderBrush=DS.Brush("DS.Hairline"),BorderThickness=new(1),ToolTip=$"{preset.Name} · #{preset.Rgb:X6}"};
            System.Windows.Automation.AutomationProperties.SetName(b,$"{preset.Name}, #{preset.Rgb:X6}");
            b.Template=SwatchTemplate();
            b.Click+=(_,_)=>{if(!busy)SetColour(preset.Rgb);};palette.Children.Add(b);swatches.Add((b,preset.Rgb));
        }
        panel.Children.Add(palette);panel.Children.Add(Kit.Text("48 prepared colours · hover for names",13,"DS.TextSoft").Also(t=>t.Margin=new(0,16,0,8)));
        detail.Children.Add(Kit.Text("Effect",13,"DS.TextSoft").Also(t=>t.Margin=new(0,24,0,8)));
        modes.HorizontalAlignment=HorizontalAlignment.Stretch;modes.Margin=new(0);modes.MinHeight=44;detail.Children.Add(modes);
        detail.Children.Add(Kit.Text("Solid · Rainbow (0x01) · Breathe · Cycle · Off · Battery",13,"DS.TextMuted").Also(t=>t.Margin=new(0,10,0,24)));
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
        read.HorizontalAlignment=HorizontalAlignment.Left;detail.Children.Add(read);
        var note=Kit.Text("Keep the controller awake and Windows Bluetooth enabled.",13,"DS.TextSoft");note.TextWrapping=TextWrapping.Wrap;note.TextTrimming=TextTrimming.None;note.Margin=new(0,16,0,0);detail.Children.Add(note);
        var footer=new Grid();footer.ColumnDefinitions.Add(new());footer.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        status.VerticalAlignment=VerticalAlignment.Center;status.Margin=new(0,0,24,0);footer.Children.Add(status);
        apply.Style=DS.Style("DS.Primary");apply.MinWidth=160;Grid.SetColumn(apply,1);footer.Children.Add(apply);
        var actionPanel=Kit.Panel(null,null,null,footer,new Thickness(20,14,20,14));actionPanel.Margin=new(0,16,0,0);actionFooter=actionPanel;Grid.SetRow(actionPanel,2);root.Children.Add(actionPanel);
        BuildTopStudioBarAndSections(root);
        read.Click+=async(_,_)=>await Run(false);apply.Click+=async(_,_)=>{if(!CommitHex()||modes.SelectedIndex<0)return;await LightingEffectsService.StopAsync(restoreBaseline:false);if(BatterySelected)await ApplyBattery();else await Run(true,ModeValues[modes.SelectedIndex]);};
        modes.SelectionChanged+=(_,_)=>{apply.IsEnabled=!busy&&modes.SelectedIndex>=0;batteryPanel.Visibility=BatterySelected?Visibility.Visible:Visibility.Collapsed;if(BatterySelected)EditLevel(editingLevel);};
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

        string[] labels=["Colours & Battery","Effects Studio","Presets & Game Auto"];
        for(int i=0;i<labels.Length;i++)
        {
            int idx=i;
            var btn=new Button{Content=labels[i],Padding=new(12,6,12,6),FontSize=12.5,Margin=new(0,0,8,4),Style=DS.Style(i==0?"DS.Primary":"ActionButton")};
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
        var brush=new SolidColorBrush(Color.FromArgb((byte)Math.Round(alpha*255),frame.R,frame.G,frame.B));
        leftLedBar.Background=rightLedBar.Background=brush;
        ledBarLabel.Text=$"{frame.Hex} · 0x{frame.Brightness:X2} (2 bars linked)";
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
        selected=ColourPalette.FromHsv(hue,saturation,value);var c=ToColour(selected);preview.Background=new SolidColorBrush(c);hex.Text=$"#{selected:X6}";rgb.Text=$"R {c.R}   G {c.G}   B {c.B}";
        square.Set(hue,saturation,1-value);hueStrip.Set(hue,0,hue/360);
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
    sealed class ColourField(bool hueOnly):FrameworkElement
    {
        double hue,x,y;public event Action<double,double>? Changed;
        public void Set(double h,double px,double py){hue=h;x=px;y=py;InvalidateVisual();}
        protected override void OnInitialized(EventArgs e){base.OnInitialized(e);Focusable=true;Cursor=Cursors.Cross;System.Windows.Automation.AutomationProperties.SetName(this,hueOnly?"Hue":"Saturation and brightness");}
        protected override void OnRender(DrawingContext dc)
        {
            var bounds=new Rect(0,0,ActualWidth,ActualHeight);
            if(hueOnly){var brush=new LinearGradientBrush{StartPoint=new(0,0),EndPoint=new(0,1)};for(int i=0;i<=6;i++)brush.GradientStops.Add(new(ToColour(ColourPalette.FromHsv(i*60,1,1)),i/6d));dc.DrawRectangle(brush,null,bounds);dc.DrawRectangle(null,new Pen(Brushes.White,2),new Rect(0,Math.Clamp(y*ActualHeight-3,0,Math.Max(0,ActualHeight-6)),ActualWidth,6));}
            else{dc.DrawRectangle(new LinearGradientBrush(Colors.White,ToColour(ColourPalette.FromHsv(hue,1,1)),0),null,bounds);dc.DrawRectangle(new LinearGradientBrush(Colors.Transparent,Colors.Black,90),null,bounds);var point=new Point(x*ActualWidth,y*ActualHeight);dc.DrawEllipse(null,new Pen(Brushes.Black,3),point,5,5);dc.DrawEllipse(null,new Pen(Brushes.White,1.5),point,5,5);}
            if(IsKeyboardFocused)dc.DrawRectangle(null,new Pen(Brushes.White,1),bounds);
        }
        void Pick(Point p){x=Math.Clamp(p.X/Math.Max(1,ActualWidth),0,1);y=Math.Clamp(p.Y/Math.Max(1,ActualHeight),0,1);Changed?.Invoke(x,y);}
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){Focus();CaptureMouse();Pick(e.GetPosition(this));e.Handled=true;}
        protected override void OnMouseMove(MouseEventArgs e){if(IsMouseCaptured)Pick(e.GetPosition(this));}
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(IsMouseCaptured){Pick(e.GetPosition(this));ReleaseMouseCapture();}e.Handled=true;}
        protected override void OnKeyDown(KeyEventArgs e){double step=Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?.05:.005;if(e.Key==Key.Left&&!hueOnly)x-=step;else if(e.Key==Key.Right&&!hueOnly)x+=step;else if(e.Key==Key.Up)y-=step;else if(e.Key==Key.Down)y+=step;else{base.OnKeyDown(e);return;}x=Math.Clamp(x,0,1);y=Math.Clamp(y,0,1);Changed?.Invoke(x,y);e.Handled=true;}
    }
}
