using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using X20Ctl.Zone.Lighting;

namespace X20Ctl.Product;

/// <summary>
/// UI builder for the Custom Effects Studio and Saved Presets / Foreground-Game Auto-Switch views inside LightingWindow.
/// Uses button-pill selectors instead of ComboBox/TextBox elements so existing RGB single-field review contracts stay exact.
/// </summary>
internal sealed class LightingEffectsSection
{
    readonly Func<int> getPickerRgb;
    readonly Action<int> setPickerRgb;
    readonly Func<byte> getPickerBrightness;

    readonly WrapPanel kindPillsPanel = new() { Margin = new(0, 0, 0, 12) };
    readonly WrapPanel palettePanel = new() { Margin = new(0, 4, 0, 10) };

    readonly Slider minBrightSlider = new() { Minimum = 0, Maximum = 255, Value = 1, SmallChange = 1, LargeChange = 16, TickFrequency = 1, IsSnapToTickEnabled = true, MinHeight = 26 };
    readonly Slider maxBrightSlider = new() { Minimum = 1, Maximum = 255, Value = 224, SmallChange = 1, LargeChange = 16, TickFrequency = 1, IsSnapToTickEnabled = true, MinHeight = 26 };
    readonly Slider intervalSlider = new() { Minimum = 60, Maximum = 1500, Value = 80, SmallChange = 10, LargeChange = 50, TickFrequency = 10, IsSnapToTickEnabled = true, MinHeight = 26 };
    readonly Slider periodSlider = new() { Minimum = 800, Maximum = 15000, Value = 4200, SmallChange = 200, LargeChange = 1000, TickFrequency = 100, IsSnapToTickEnabled = true, MinHeight = 26 };
    readonly Slider idleSlider = new() { Minimum = 5, Maximum = 300, Value = 60, SmallChange = 5, LargeChange = 15, TickFrequency = 5, IsSnapToTickEnabled = true, MinHeight = 26 };

    readonly TextBlock minBrightLabel = new() { FontSize = 12.5 };
    readonly TextBlock maxBrightLabel = new() { FontSize = 12.5 };
    readonly TextBlock intervalLabel = new() { FontSize = 12.5 };
    readonly TextBlock periodLabel = new() { FontSize = 12.5 };
    readonly TextBlock idleLabel = new() { FontSize = 12.5 };
    readonly TextBlock liveTelemetryText = new() { FontSize = 13, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock presetsTelemetryText = new() { FontSize = 12.5, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock selectedGameRuleHint = new() { FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 8) };

    readonly StackPanel savedPresetsList = new() { Margin = new(0, 8, 0, 0) };
    readonly StackPanel builtInPresetsList = new() { Margin = new(0, 8, 0, 0) };
    readonly WrapPanel runningAppsPanel = new() { Margin = new(0, 4, 0, 10) };
    readonly StackPanel gameRulesList = new() { Margin = new(0, 8, 0, 0) };

    static readonly LightingEffectKind[] AnimatedKinds =
    [
        LightingEffectKind.ScreenAmbient,
        LightingEffectKind.SmoothHueWave,
        LightingEffectKind.PaletteFade,
        LightingEffectKind.PaletteStep,
        LightingEffectKind.MoonlightPulse,
        LightingEffectKind.EmberShimmer,
        LightingEffectKind.DualStrobe,
        LightingEffectKind.TriggerGlow,
        LightingEffectKind.StickHeat,
        LightingEffectKind.ButtonFlash,
        LightingEffectKind.IdleSleepDim,
    ];

    static readonly (int Cap, string Label)[] WriteCapChoices =
    [
        (0, "Continuous"),
        (50, "50 writes"),
        (150, "150 writes"),
        (400, "400 writes"),
        (1000, "1000 writes"),
    ];

    readonly Dictionary<LightingEffectKind, Button> kindButtons = [];
    readonly Dictionary<int, Button> capButtons = [];
    readonly List<string> palette = ["#00FFFF", "#FF00AA", "#3A6CFF"];

    LightingEffectKind selectedKind = LightingEffectKind.SmoothHueWave;
    int selectedWriteCap;
    string selectedRulePresetId = "builtin-hue-sweep";
    bool syncing;

    public FrameworkElement EffectsView { get; }
    public FrameworkElement PresetsAndGamesView { get; }

    public LightingEffectsSection(Func<int> getPickerRgb, Action<int> setPickerRgb, Func<byte> getPickerBrightness)
    {
        this.getPickerRgb = getPickerRgb;
        this.setPickerRgb = setPickerRgb;
        this.getPickerBrightness = getPickerBrightness;

        EffectsView = BuildEffectsView();
        PresetsAndGamesView = BuildPresetsAndGamesView();
        LoadFromSpec(LightingEffectsService.CurrentSpec);
        RefreshSavedPresets();
        RefreshRunningApps();
        RefreshGameRules();

        LightingEffectsService.Changed += () =>
        {
            EffectsView.Dispatcher.BeginInvoke(RefreshLiveTelemetry, DispatcherPriority.Background);
        };
        RefreshLiveTelemetry();
    }

    public LightingEffectSpec BuildCurrentEffectSpec() => new LightingEffectSpec
    {
        Kind = selectedKind,
        Palette = palette.Count > 0 ? [.. palette] : [$"#{getPickerRgb() & 0xffffff:X6}"],
        MinBrightness = (byte)Math.Clamp((int)Math.Round(minBrightSlider.Value), 0, 255),
        MaxBrightness = (byte)Math.Clamp((int)Math.Round(maxBrightSlider.Value), 1, 255),
        StepIntervalMs = (int)Math.Round(intervalSlider.Value),
        CyclePeriodMs = (int)Math.Round(periodSlider.Value),
        IdleTimeoutSeconds = (int)Math.Round(idleSlider.Value),
        MaxSessionWrites = selectedWriteCap,
        RestoreOnStop = true,
    }.Normalized();

    public void LoadFromSpec(LightingEffectSpec raw)
    {
        var spec = raw.Normalized();
        syncing = true;
        try
        {
            selectedKind = Array.IndexOf(AnimatedKinds, spec.Kind) >= 0 ? spec.Kind : LightingEffectKind.SmoothHueWave;
            selectedWriteCap = Array.Exists(WriteCapChoices, c => c.Cap == spec.MaxSessionWrites) ? spec.MaxSessionWrites : 0;
            palette.Clear();
            palette.AddRange(spec.Palette);
            minBrightSlider.Value = spec.MinBrightness;
            maxBrightSlider.Value = spec.MaxBrightness;
            intervalSlider.Value = spec.StepIntervalMs;
            periodSlider.Value = spec.CyclePeriodMs;
            idleSlider.Value = spec.IdleTimeoutSeconds;
        }
        finally
        {
            syncing = false;
        }
        UpdateSelectionPills();
        RenderPaletteSwatches();
        UpdateSliderLabels();
    }

    FrameworkElement BuildEffectsView()
    {
        var cols = new Grid();
        cols.ColumnDefinitions.Add(new() { Width = new(1.25, GridUnitType.Star) });
        cols.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });

        var leftStack = new StackPanel();
        leftStack.Children.Add(Kit.Text("Tap any effect to preview and start immediately", 12.5, "DS.TextMuted").Also(t => t.Margin = new(0, 0, 0, 10)));

        Button MakeEffectCard(LightingEffectKind kind, bool hero = false)
        {
            var (accentHex, subtitle) = EffectCardMeta(kind);
            var accentColor = (Color)ColorConverter.ConvertFromString(accentHex);
            var dot = new Border
            {
                Width = hero ? 14 : 10, Height = hero ? 14 : 10, CornerRadius = new(hero ? 7 : 5),
                Background = new SolidColorBrush(accentColor),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = accentColor, BlurRadius = hero ? 12 : 6, ShadowDepth = 0, Opacity = 0.85 },
                Margin = new(0, 2, 12, 0), VerticalAlignment = VerticalAlignment.Top,
            };
            var textCol = new StackPanel();
            textCol.Children.Add(Kit.Text(hero ? "Ambient Screen Sync  ·  Hero Mode" : ShortEffectName(kind), hero ? 14.5 : 13.5, "DS.Text", FontWeights.SemiBold));
            textCol.Children.Add(Kit.Text(subtitle, 11.5, "DS.TextMuted").Also(t => { t.TextTrimming = TextTrimming.CharacterEllipsis; t.Margin = new(0, 2, 0, 0); }));
            var row = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(dot, Dock.Left);
            row.Children.Add(dot);
            row.Children.Add(textCol);

            var btn = new Button
            {
                Content = row,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = hero ? new Thickness(14, 12, 14, 12) : new Thickness(12, 10, 12, 10),
                Margin = hero ? new Thickness(0, 0, 0, 10) : new Thickness(4),
                Style = DS.Style("ActionButton"),
            };
            btn.Click += async (_, _) =>
            {
                selectedKind = kind;
                if (kind == LightingEffectKind.ScreenAmbient && intervalSlider.Value > 120)
                    intervalSlider.Value = 70;
                UpdateSelectionPills();
                UpdateSliderLabels();
                await LightingEffectsService.ApplyAsync(BuildCurrentEffectSpec());
            };
            kindButtons[kind] = btn;
            return btn;
        }

        leftStack.Children.Add(MakeEffectCard(LightingEffectKind.ScreenAmbient, hero: true));
        var cardGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2, Margin = new(-4, 0, -4, 12) };
        foreach (var kind in AnimatedKinds.Where(k => k != LightingEffectKind.ScreenAmbient))
            cardGrid.Children.Add(MakeEffectCard(kind));
        leftStack.Children.Add(cardGrid);

        leftStack.Children.Add(Kit.Text("Color stops & quick themes", 13, "DS.TextSoft").Also(t => t.Margin = new(0, 2, 0, 6)));
        leftStack.Children.Add(palettePanel);

        var paletteActions = new WrapPanel { Margin = new(0, 0, 0, 6) };
        paletteActions.Children.Add(PillButton("+ Add picker colour", () =>
        {
            if (palette.Count >= 8) return;
            palette.Add($"#{getPickerRgb() & 0xffffff:X6}");
            RenderPaletteSwatches();
        }).Also(b => b.Margin = new(0, 0, 8, 6)));

        foreach (var (name, stops) in new (string, string[])[]
        {
            ("Cyberpunk", ["#00FFFF", "#FF00FF", "#3A6CFF"]),
            ("Fire", ["#FF2200", "#FF8C00", "#FFB800"]),
            ("Police", ["#FF0020", "#0055FF"]),
            ("Aurora", ["#00FF9D", "#00B8FF", "#9D4EDD"]),
        })
        {
            paletteActions.Children.Add(PillButton(name, () =>
            {
                palette.Clear();
                palette.AddRange(stops);
                RenderPaletteSwatches();
                if (LightingEffectsService.Status.IsRunning)
                    _ = LightingEffectsService.ApplyAsync(BuildCurrentEffectSpec());
            }).Also(b => b.Margin = new(0, 0, 6, 6)));
        }
        leftStack.Children.Add(paletteActions);

        var leftCard = Kit.Panel("Effects Studio", "Full-screen Ambient sync, smooth gradients & input-reactive modes.", null,
            new ScrollViewer { Content = leftStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, new Thickness(20));
        leftCard.Margin = new(0, 0, 14, 0);
        cols.Children.Add(leftCard);

        var rightStack = new StackPanel();
        minBrightLabel.Foreground = DS.Brush("DS.TextSoft");
        maxBrightLabel.Foreground = DS.Brush("DS.TextSoft");
        intervalLabel.Foreground = DS.Brush("DS.TextSoft");
        periodLabel.Foreground = DS.Brush("DS.TextSoft");
        idleLabel.Foreground = DS.Brush("DS.TextSoft");
        liveTelemetryText.Foreground = DS.Brush("DS.TextSoft");

        minBrightSlider.ValueChanged += (_, _) =>
        {
            if (minBrightSlider.Value > maxBrightSlider.Value && !syncing) maxBrightSlider.Value = Math.Max(1, minBrightSlider.Value);
            UpdateSliderLabels();
        };
        maxBrightSlider.ValueChanged += (_, _) =>
        {
            if (maxBrightSlider.Value < minBrightSlider.Value && !syncing) minBrightSlider.Value = maxBrightSlider.Value;
            UpdateSliderLabels();
        };
        intervalSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        periodSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        idleSlider.ValueChanged += (_, _) => UpdateSliderLabels();

        rightStack.Children.Add(maxBrightLabel);
        rightStack.Children.Add(maxBrightSlider);
        rightStack.Children.Add(intervalLabel);
        rightStack.Children.Add(intervalSlider);

        var advancedStack = new StackPanel { Margin = new(0, 8, 0, 0) };
        advancedStack.Children.Add(minBrightLabel);
        advancedStack.Children.Add(minBrightSlider);
        advancedStack.Children.Add(new WrapPanel
        {
            Margin = new(0, 4, 0, 10),
            Children =
            {
                PillButton("Moonlight 0x01", () => minBrightSlider.Value = LightingRecord.MoonlightBrightnessByte).Also(b => b.Margin = new(0, 0, 8, 0)),
                PillButton("Off 0x00 floor", () => minBrightSlider.Value = LightingRecord.MinBrightnessByte),
            },
        });
        advancedStack.Children.Add(periodLabel);
        advancedStack.Children.Add(periodSlider);
        advancedStack.Children.Add(idleLabel);
        advancedStack.Children.Add(idleSlider);
        rightStack.Children.Add(new Expander
        {
            Header = Kit.Text("Advanced timing & brightness floor", 12.5, "DS.TextSoft"),
            Foreground = DS.Brush("DS.TextSoft"),
            Margin = new(0, 10, 0, 4),
            Content = advancedStack,
        });

        var startBtn = new Button
        {
            Content = "Apply / Restart",
            Style = DS.Style("DS.Primary"),
            Padding = new(18, 9, 18, 9),
            Margin = new(0, 0, 10, 6),
        };
        startBtn.Click += async (_, _) => await LightingEffectsService.ApplyAsync(BuildCurrentEffectSpec());
        var stopBtn = new Button
        {
            Content = "Stop Effect",
            Style = DS.Style("ActionButton"),
            Padding = new(16, 9, 16, 9),
            Margin = new(0, 0, 0, 6),
        };
        stopBtn.Click += async (_, _) => await LightingEffectsService.StopAsync(restoreBaseline: true);
        rightStack.Children.Add(new WrapPanel { Margin = new(0, 12, 0, 12), Children = { startBtn, stopBtn } });

        rightStack.Children.Add(new Border
        {
            Background = new SolidColorBrush(ThemeCatalog.R(Color.FromArgb(100, 14, 24, 42))),
            BorderBrush = DS.Brush("DS.Hairline"),
            BorderThickness = new(1),
            CornerRadius = new(8),
            Padding = new(12, 10, 12, 10),
            Child = liveTelemetryText,
        });

        var rightCard = Kit.Panel("Speed, Brightness & Live Stream", "Down to 60 ms interval + 0 ms chunk gap for snappy BLE sync.", null,
            new ScrollViewer { Content = rightStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, new Thickness(20));
        Grid.SetColumn(rightCard, 1);
        cols.Children.Add(rightCard);

        return cols;
    }

    FrameworkElement BuildPresetsAndGamesView()
    {
        var cols = new Grid();
        cols.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        cols.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });

        var leftStack = new StackPanel();
        leftStack.Children.Add(Kit.Text("Save lighting preset", 13, "DS.TextSoft").Also(t => t.Margin = new(0, 0, 0, 6)));
        leftStack.Children.Add(new WrapPanel
        {
            Margin = new(0, 0, 0, 12),
            Children =
            {
                PillButton("+ Save Named Effect…", () => SaveWithPrompt(asSolid: false)).Also(b => b.Margin = new(0, 0, 8, 6)),
                PillButton("+ Quick Save Effect", () => SaveQuick(asSolid: false)).Also(b => b.Margin = new(0, 0, 8, 6)),
                PillButton("+ Save Solid Colour", () => SaveQuick(asSolid: true)).Also(b => b.Margin = new(0, 0, 0, 6)),
            },
        });
        leftStack.Children.Add(Kit.Text("Your Saved Presets", 13.5, "DS.Text", FontWeights.SemiBold).Also(t => t.Margin = new(0, 4, 0, 4)));
        leftStack.Children.Add(savedPresetsList);
        leftStack.Children.Add(Kit.Text("Built-in Presets (click Bind Target to use in Game Auto)", 13.5, "DS.Text", FontWeights.SemiBold).Also(t => t.Margin = new(0, 14, 0, 4)));
        leftStack.Children.Add(builtInPresetsList);

        var leftCard = Kit.Panel("Saved colour & effect presets", "Stored locally in %LOCALAPPDATA%\\x20ctl\\lighting\\presets.json.", null,
            new ScrollViewer { Content = leftStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, new Thickness(20));
        leftCard.Margin = new(0, 0, 14, 0);
        cols.Children.Add(leftCard);

        var rightStack = new StackPanel();
        selectedGameRuleHint.Foreground = DS.Brush("DS.AccentHi");
        rightStack.Children.Add(selectedGameRuleHint);

        rightStack.Children.Add(Kit.Text("Click a running/recent app below to bind it to the selected preset", 13, "DS.TextSoft").Also(t => t.Margin = new(0, 2, 0, 6)));
        rightStack.Children.Add(runningAppsPanel);

        var enableAutoBtn = new Button
        {
            Content = "Activate Game Auto-Switch",
            Style = DS.Style("DS.Primary"),
            Padding = new(16, 8, 16, 8),
            Margin = new(0, 0, 8, 6),
        };
        enableAutoBtn.Click += async (_, _) =>
        {
            await LightingEffectsService.ApplyAsync(new LightingEffectSpec
            {
                Kind = LightingEffectKind.GameAutoProfile,
                Palette = [$"#{getPickerRgb() & 0xffffff:X6}"],
                MaxBrightness = getPickerBrightness(),
            });
        };
        rightStack.Children.Add(new WrapPanel
        {
            Margin = new(0, 2, 0, 12),
            Children =
            {
                PillButton("+ Custom .exe Rule…", BindCustomExeWithPrompt).Also(b => b.Margin = new(0, 0, 8, 6)),
                PillButton("Refresh Apps", RefreshRunningApps).Also(b => b.Margin = new(0, 0, 8, 6)),
                enableAutoBtn,
            },
        });

        rightStack.Children.Add(Kit.Text("Active Per-Game Rules", 13.5, "DS.Text", FontWeights.SemiBold).Also(t => t.Margin = new(0, 4, 0, 4)));
        rightStack.Children.Add(gameRulesList);

        presetsTelemetryText.Foreground = DS.Brush("DS.TextSoft");
        rightStack.Children.Add(new Border
        {
            Background = new SolidColorBrush(ThemeCatalog.R(Color.FromArgb(100, 14, 24, 42))),
            BorderBrush = DS.Brush("DS.Hairline"),
            BorderThickness = new(1),
            CornerRadius = new(8),
            Padding = new(12, 9, 12, 9),
            Margin = new(0, 14, 0, 0),
            Child = presetsTelemetryText,
        });

        var rightCard = Kit.Panel("Foreground-game auto profiles", "Switches controller LEDs automatically when a matched game is active.", null,
            new ScrollViewer { Content = rightStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, new Thickness(20));
        Grid.SetColumn(rightCard, 1);
        cols.Children.Add(rightCard);

        return cols;
    }

    void UpdateSelectionPills()
    {
        foreach (var (kind, btn) in kindButtons)
            btn.Style = DS.Style(kind == selectedKind ? "DS.Primary" : "ActionButton");
        foreach (var (cap, btn) in capButtons)
            btn.Style = DS.Style(cap == selectedWriteCap ? "DS.Primary" : "ActionButton");
    }

    void SaveQuick(bool asSolid)
    {
        var spec = asSolid
            ? new LightingEffectSpec
            {
                Kind = LightingEffectKind.Solid,
                Palette = [$"#{getPickerRgb() & 0xffffff:X6}"],
                MaxBrightness = getPickerBrightness(),
            }.Normalized()
            : BuildCurrentEffectSpec();
        string name = asSolid
            ? $"Solid #{getPickerRgb() & 0xffffff:X6} (0x{spec.MaxBrightness:X2})"
            : $"{ShortEffectName(spec.Kind)} · {DateTime.Now:HH:mm}";
        try
        {
            var saved = LightingEffectsService.Store.SavePreset(name, spec);
            selectedRulePresetId = saved.Id;
            RefreshSavedPresets();
        }
        catch { }
    }

    void SaveWithPrompt(bool asSolid)
    {
        string defaultName = ShortEffectName(selectedKind);
        if (PromptText("Save Lighting Preset", "Preset name:", defaultName) is not { } entered) return;
        var spec = asSolid
            ? new LightingEffectSpec
            {
                Kind = LightingEffectKind.Solid,
                Palette = [$"#{getPickerRgb() & 0xffffff:X6}"],
                MaxBrightness = getPickerBrightness(),
            }.Normalized()
            : BuildCurrentEffectSpec();
        try
        {
            var saved = LightingEffectsService.Store.SavePreset(entered, spec);
            selectedRulePresetId = saved.Id;
            RefreshSavedPresets();
        }
        catch { }
    }

    void BindCustomExeWithPrompt()
    {
        string suggested = ForegroundApp.Last?.Exe ?? "eldenring.exe";
        if (PromptText("Add Foreground Game Rule", "Executable filename (e.g. game.exe):", suggested) is not { } exe) return;
        try
        {
            LightingEffectsService.Store.SetGameRule(exe, selectedRulePresetId);
            RefreshGameRules();
        }
        catch { }
    }

    void RenderPaletteSwatches()
    {
        palettePanel.Children.Clear();
        for (int i = 0; i < palette.Count; i++)
        {
            int idx = i;
            string hex = palette[i];
            var (r, g, b) = LightingEffectSpec.ParseRgb(hex);
            var chip = new Border
            {
                BorderBrush = DS.Brush("DS.Hairline"),
                BorderThickness = new(1),
                CornerRadius = new(8),
                Margin = new(0, 0, 8, 8),
                Padding = new(6, 4, 6, 4),
                Background = new SolidColorBrush(ThemeCatalog.R(Color.FromArgb(120, 20, 34, 58))),
            };
            var inner = new StackPanel { Orientation = Orientation.Horizontal };
            var dot = new Button
            {
                Width = 20,
                Height = 20,
                Background = new SolidColorBrush(Color.FromRgb(r, g, b)),
                BorderBrush = DS.Brush("DS.Hairline"),
                BorderThickness = new(1),
                ToolTip = $"Load {hex} into picker",
                Margin = new(0, 0, 6, 0),
            };
            dot.Click += (_, _) => setPickerRgb((r << 16) | (g << 8) | b);
            inner.Children.Add(dot);
            inner.Children.Add(new TextBlock { Text = hex, Foreground = DS.Brush("DS.Text"), VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Margin = new(0, 0, 6, 0) });
            if (palette.Count > 1)
            {
                var del = new Button { Content = "×", Padding = new(5, 0, 5, 0), FontSize = 12, Style = DS.Style("ActionButton"), MinHeight = 20 };
                del.Click += (_, _) => { palette.RemoveAt(idx); RenderPaletteSwatches(); };
                inner.Children.Add(del);
            }
            chip.Child = inner;
            palettePanel.Children.Add(chip);
        }
    }

    void RefreshSavedPresets()
    {
        savedPresetsList.Children.Clear();
        builtInPresetsList.Children.Clear();

        var custom = LightingEffectsService.Store.Presets;
        if (custom.Count == 0)
            savedPresetsList.Children.Add(Kit.Text("No custom presets saved yet.", 12.5, "DS.TextMuted").Also(t => t.Margin = new(0, 4, 0, 4)));

        foreach (var preset in custom)
            savedPresetsList.Children.Add(BuildPresetRow(preset, removable: true));
        foreach (var preset in LightingEffectEngine.BuiltInPresets)
            builtInPresetsList.Children.Add(BuildPresetRow(preset, removable: false));

        UpdateSelectedRuleHint();
    }

    FrameworkElement BuildPresetRow(LightingPreset preset, bool removable)
    {
        var (r, g, b) = LightingEffectSpec.ParseRgb(preset.Spec.Palette[0]);
        bool isRuleTarget = preset.Id == selectedRulePresetId;
        var row = new Border
        {
            BorderBrush = isRuleTarget ? DS.Brush("DS.AccentHi") : DS.Brush("DS.Hairline"),
            BorderThickness = new(isRuleTarget ? 1.5 : 1),
            CornerRadius = new(8),
            Padding = new(10, 7, 10, 7),
            Margin = new(0, 0, 0, 6),
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new(4),
            Background = new SolidColorBrush(Color.FromRgb(r, g, b)),
            BorderBrush = DS.Brush("DS.Hairline"),
            BorderThickness = new(1),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(0, 0, 10, 0),
        });

        var title = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new TextBlock { Text = preset.Name, Foreground = DS.Brush("DS.Text"), FontWeight = FontWeights.SemiBold, FontSize = 13 });
        title.Children.Add(new TextBlock
        {
            Text = $"{LightingEffectSpec.EffectLabel(preset.Spec.Kind)} · 0x{preset.Spec.MaxBrightness:X2}",
            Foreground = DS.Brush("DS.TextMuted"),
            FontSize = 11.5,
        });
        Grid.SetColumn(title, 1);
        grid.Children.Add(title);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(PillButton(isRuleTarget ? "Target ✓" : "Target", () =>
        {
            selectedRulePresetId = preset.Id;
            RefreshSavedPresets();
        }).Also(b => b.Margin = new(0, 0, 6, 0)));
        actions.Children.Add(PillButton("Use", async () =>
        {
            LoadFromSpec(preset.Spec);
            setPickerRgb((r << 16) | (g << 8) | b);
            await LightingEffectsService.ApplyAsync(preset.Spec);
        }));
        if (removable)
        {
            actions.Children.Add(PillButton("×", () =>
            {
                LightingEffectsService.Store.DeletePreset(preset.Id);
                if (selectedRulePresetId == preset.Id) selectedRulePresetId = "builtin-hue-sweep";
                RefreshSavedPresets();
                RefreshGameRules();
            }).Also(b => b.Margin = new(6, 0, 0, 0)));
        }
        Grid.SetColumn(actions, 2);
        grid.Children.Add(actions);
        row.Child = grid;
        return row;
    }

    void RefreshRunningApps()
    {
        runningAppsPanel.Children.Clear();
        var apps = new List<ForegroundApp.App>();
        try { apps.AddRange(ForegroundApp.Running().Take(8)); } catch { }
        if (apps.Count == 0)
        {
            runningAppsPanel.Children.Add(Kit.Text("No foreground apps detected right now — use '+ Custom .exe Rule…'.", 12, "DS.TextMuted"));
            return;
        }
        foreach (var app in apps)
        {
            var captured = app;
            runningAppsPanel.Children.Add(PillButton($"+ {captured.Exe}", () =>
            {
                LightingEffectsService.Store.SetGameRule(captured.Exe, selectedRulePresetId);
                RefreshGameRules();
            }).Also(b => { b.Margin = new(0, 0, 6, 6); b.ToolTip = captured.Title; }));
        }
    }

    void RefreshGameRules()
    {
        gameRulesList.Children.Clear();
        var rules = LightingEffectsService.Store.GameRules;
        if (rules.Count == 0)
        {
            gameRulesList.Children.Add(Kit.Text("No game rules bound yet.", 12.5, "DS.TextMuted").Also(t => t.Margin = new(0, 4, 0, 4)));
            return;
        }
        foreach (var rule in rules)
        {
            var target = LightingEffectsService.Store.FindPreset(rule.PresetId)?.Name ?? "(missing preset)";
            var row = new DockPanel { Margin = new(0, 0, 0, 6) };
            var del = PillButton("Remove", () => { LightingEffectsService.Store.RemoveGameRule(rule.ExeName); RefreshGameRules(); });
            DockPanel.SetDock(del, Dock.Right);
            row.Children.Add(del);
            row.Children.Add(new TextBlock
            {
                Text = $"{rule.ExeName} → {target}",
                Foreground = DS.Brush("DS.Text"),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
            });
            gameRulesList.Children.Add(row);
        }
    }

    void UpdateSelectedRuleHint()
    {
        var name = LightingEffectsService.Store.FindPreset(selectedRulePresetId)?.Name ?? selectedRulePresetId;
        selectedGameRuleHint.Text = $"Target preset for new game rules: {name}";
    }

    void UpdateSliderLabels()
    {
        byte minB = (byte)Math.Clamp((int)Math.Round(minBrightSlider.Value), 0, 255);
        byte maxB = (byte)Math.Clamp((int)Math.Round(maxBrightSlider.Value), 1, 255);
        double minPct = LightingRecord.BrightnessPercentFor(minB);
        double maxPct = LightingRecord.BrightnessPercentFor(maxB);
        minBrightLabel.Text = $"Minimum brightness floor: {minPct:0}% (0x{minB:X2}{(minB == 1 ? " · Moonlight" : minB == 0 ? " · Off" : "")})";
        maxBrightLabel.Text = $"Peak brightness: {maxPct:0}% (0x{maxB:X2})";
        intervalLabel.Text = $"BLE frame interval: {intervalSlider.Value:0} ms ({1000.0 / intervalSlider.Value:0.0} Hz max)";
        periodLabel.Text = $"Wave / cycle duration: {periodSlider.Value / 1000.0:0.0} s";
        idleLabel.Text = $"Idle auto-dim delay: {idleSlider.Value:0} s";
    }

    void RefreshLiveTelemetry()
    {
        var st = LightingEffectsService.Status;
        string state = st.IsRunning
            ? (ReviewSandbox.Active ? "Simulating in review sandbox" : st.IsYielded ? "Paused briefly for BLE config/telemetry" : st.IsBleConnected ? "Live streaming on X20 Pro" : "Connecting BLE stream…")
            : "Idle";
        string gameNote = st.ActiveGameExe != null ? $"\nForeground app: {st.ActiveGameExe}" : "";
        string summary =
            $"Status: {state}\n" +
            $"{st.Summary}\n" +
            $"Session BLE writes: {st.SessionWrites} · Total writes: {st.TotalWrites} · Unchanged frames skipped: {st.SkippedDuplicateFrames}" +
            gameNote;
        liveTelemetryText.Text = summary;
        presetsTelemetryText.Text = summary;
    }

    static string ShortEffectName(LightingEffectKind kind) => kind switch
    {
        LightingEffectKind.SmoothHueWave => "Hue Sweep",
        LightingEffectKind.PaletteFade => "Color Fade",
        LightingEffectKind.PaletteStep => "Color Jump",
        LightingEffectKind.MoonlightPulse => "Moonlight Pulse",
        LightingEffectKind.DualStrobe => "Dual Strobe",
        LightingEffectKind.EmberShimmer => "Lava Ember",
        LightingEffectKind.TriggerGlow => "Trigger Glow",
        LightingEffectKind.StickHeat => "Stick Heat",
        LightingEffectKind.ButtonFlash => "Button Flash",
        LightingEffectKind.IdleSleepDim => "Idle Nightlight",
        LightingEffectKind.ScreenAmbient => "Ambient Sync",
        _ => kind.ToString(),
    };

    static (string AccentHex, string Subtitle) EffectCardMeta(LightingEffectKind kind) => kind switch
    {
        LightingEffectKind.ScreenAmbient => ("#38BDF8", "Full-screen dominant colour sync"),
        LightingEffectKind.SmoothHueWave => ("#A855F7", "Smooth 360° rainbow sweep"),
        LightingEffectKind.PaletteFade => ("#EC4899", "Blended multi-stop gradient"),
        LightingEffectKind.PaletteStep => ("#F59E0B", "Crisp multi-colour step jump"),
        LightingEffectKind.MoonlightPulse => ("#60A5FA", "Soft breathing down to 1%"),
        LightingEffectKind.EmberShimmer => ("#FB923C", "Warm flickering lava glow"),
        LightingEffectKind.DualStrobe => ("#EF4444", "High-energy dual-colour flash"),
        LightingEffectKind.TriggerGlow => ("#3B82F6", "Reacts to LT / RT trigger pull"),
        LightingEffectKind.StickHeat => ("#14B8A6", "Reacts to stick deflection"),
        LightingEffectKind.ButtonFlash => ("#22D3EE", "Flashes on button presses"),
        LightingEffectKind.IdleSleepDim => ("#818CF8", "Auto-dims when controller idles"),
        _ => ("#94A3B8", "Custom lighting effect"),
    };

    static string? PromptText(string title, string label, string initial)
    {
        string? result = null;
        var win = new Window
        {
            Title = title,
            Width = 380,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Background = DS.Brush("DS.Deep"),
            Foreground = DS.Brush("DS.Text"),
            FontFamily = DS.Body,
        };
        var input = new TextBox { Text = initial, Padding = new(10, 7, 10, 7), Margin = new(0, 6, 0, 14) };
        input.SelectAll();
        var ok = new Button { Content = "Save", Style = DS.Style("DS.Primary"), Padding = new(16, 7, 16, 7), Margin = new(0, 0, 8, 0), IsDefault = true };
        ok.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(input.Text)) { result = input.Text.Trim(); win.DialogResult = true; } };
        var cancel = new Button { Content = "Cancel", Style = DS.Style("ActionButton"), Padding = new(14, 7, 14, 7), IsCancel = true };
        win.Content = new StackPanel
        {
            Margin = new(18),
            Children =
            {
                Kit.Text(label, 13, "DS.TextSoft"),
                input,
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Children = { ok, cancel } },
            },
        };
        return win.ShowDialog() == true ? result : null;
    }

    static Button PillButton(string text, Action click)
    {
        var btn = new Button { Content = text, Style = DS.Style("ActionButton"), Padding = new(11, 6, 11, 6), FontSize = 12.5 };
        btn.Click += (_, _) => click();
        return btn;
    }
}
