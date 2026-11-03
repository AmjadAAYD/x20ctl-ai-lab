using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using X20Ctl.Product.Nocturne;

namespace X20Ctl.Product;

public sealed partial class ProductSettings
{
    internal FrameworkElement? nocturneRoot;
    private string nocturneView = "hub";

    private void BuildNocturne()
    {
        if (pageGrid != null) pageGrid.Visibility = Visibility.Collapsed;
        hub.Visibility = Visibility.Collapsed;
        themes.Visibility = Visibility.Collapsed;
        footer.Visibility = Visibility.Collapsed;

        var canvas = new Canvas { Width = 1600, Height = 900, ClipToBounds = true };
        nocturneRoot = new Viewbox { Child = canvas, Stretch = Stretch.Uniform };
        Children.Add(nocturneRoot);

        NocturneSync();
    }

    internal void NocturneSync()
    {
        if (nocturneRoot is not Viewbox box || box.Child is not Canvas board) return;
        board.Children.Clear();

        nocturneView = pcMode.Visibility == Visibility.Visible ? "pcmode" :
                       feedback.Visibility == Visibility.Visible ? "feedback" :
                       about.Visibility == Visibility.Visible ? "about" :
                       themes.Visibility == Visibility.Visible ? "themes" : "hub";

        bool realView = nocturneView is "about" or "feedback" or "pcmode";
        box.Visibility = realView ? Visibility.Collapsed : Visibility.Visible;
        if (pageGrid != null) pageGrid.Visibility = realView ? Visibility.Visible : Visibility.Collapsed;
        if (nocturneView == "themes") RenderNocturneThemes(board);
        else if (nocturneView == "hub") RenderNocturneHub(board);
    }

    private void RenderNocturneHub(Canvas board)
    {
        var frame = new NocturneToolFrame("TOOLS & CONFIGURATION", "Diagnostics, updates, themes and preferences", () => Back.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));
        var canvas = frame.ContentArea;

        var items = new (string Title, string Desc, string ActionLabel, Action Action, string Icon)[]
        {
            ("CONTROLLER CHECK", "Verify button inputs, triggers, and sensor diagnostics over Bluetooth or USB.", "Run Diagnostic Check", () => Check.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)), ""),
            ("WRITE SCAN", "Inspect controller registers and compare stored profiles with local firmware records.", "Inspect Firmware Scan", () => WriteScan.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)), ""),
            ("VISUAL THEMES", "Choose the app theme.", "Browse Themes", () => ShowThemes(true), ""),
            ("FEEDBACK & SUPPORT", "Report bugs, propose new feature requests, or view diagnostic logs.", "Open Feedback Form", () => ShowFeedback(true), ""),
            ("SOFTWARE UPDATES", "Check for newer preview builds and driver package updates on GitHub.", "Check for Updates", () => ShowAbout(true), ""),
            ("OPENING CINEMATIC", "Replay the theme opening intro film and brand animation.", "Replay Intro", () => Replay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)), ""),
            ("APPLICATION SOUNDS", "Configure interface feedback chime volume and audio effects.", "Sound Settings", () => ShowAbout(true), ""),
            ("ABOUT & CREDITS", "License terms, third-party libraries, privacy policies, and release notes.", "View Release Notes", () => ShowAbout(true), "")
        };

        var grid = new UniformGrid { Columns = 4, Rows = 2, Width = 1440, Height = 640 };
        foreach (var item in items)
        {
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            // No icons: the glyphs here did not match their tools (a play arrow for Controller Check, blanks for Updates and Feedback).
            sp.Children.Add(new TextBlock { Text = item.Title, FontFamily = NocturneKit.Display, FontWeight = FontWeights.Black, FontStyle = FontStyles.Italic, FontSize = 17, Foreground = NocturneKit.VoidTextBrush, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) });

            var btn = NocturneKit.NocturneButton(item.ActionLabel, item.Action, isPrimary: false, height: 32);
            sp.Children.Add(btn);

            var card = NocturneKit.NocturneWhiteCard(sp, 330, 270);
            card.Margin = new Thickness(8);
            grid.Children.Add(card);
        }

        Canvas.SetLeft(grid, 80);
        Canvas.SetTop(grid, 40);
        canvas.Children.Add(grid);

        board.Children.Add(frame);
    }

    private void RenderNocturneThemes(Canvas board)
    {
        var frame = new NocturneToolFrame("VISUAL THEMES", "Choose the app theme", () => ShowThemes(false));
        var canvas = frame.ContentArea;

        var allThemes = new (string Id, string Name, string Accent, string Sub)[]
        {
            ("default", "DEFAULT", "#2563EB", "Classic Studio"),
            ("nocturne", "NOCTURNE", "#0056FB", "Electric Cobalt"),
            ("tide", "TIDE", "#28B6F0", "Persona 3 Blue"),
            ("pulse", "PULSE", "#00FFCC", "Cyber Waveform"),
            ("spotlight", "SPOTLIGHT", "#EAB308", "Noir Warm Glow"),
            ("nexus", "NEXUS", "#38BDF8", "Holo Matrix"),
            ("rebel", "REBEL", "#F43F5E", "Cyberpunk Edge"),
            ("volt", "VOLT", "#FACC15", "High Voltage"),
            ("blossom", "YOZAKURA", "#F472B6", "Night Blossom"),
            ("dusk", "DUSK", "#FB923C", "Twilight Orange")
        };

        var grid = new UniformGrid { Columns = 4, Rows = 3, Width = 1360, Height = 640 };
        string curTheme = ThemeCatalog.Current.Id;

        foreach (var t in allThemes)
        {
            bool isActive = (t.Id == curTheme);
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            var bar = new Border
            {
                Width = 220,
                Height = 8,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(t.Accent)),
                Margin = new Thickness(0, 0, 0, 10),
                RenderTransform = NocturneKit.CreateSlant(-12),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            sp.Children.Add(bar);

            sp.Children.Add(new TextBlock { Text = t.Name, FontFamily = NocturneKit.Display, FontWeight = FontWeights.Black, FontStyle = FontStyles.Italic, FontSize = 14, Foreground = NocturneKit.VoidTextBrush, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = t.Sub, FontFamily = NocturneKit.Body, FontSize = 11.5, Foreground = NocturneKit.SoftBrush, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 10) });

            string targetId = t.Id;
            var selBtn = NocturneKit.NocturneButton(isActive ? "CURRENT THEME" : "SELECT", () =>
            {
                if (targetId != curTheme)
                {
                    ThemeCatalog.Apply(ThemeCatalog.Find(targetId));
                    RequestThemeRestart();
                }
            }, isPrimary: isActive, height: 32, tag: $"theme:choose:{t.Id}");
            sp.Children.Add(selBtn);

            var card = NocturneKit.NocturneWhiteCard(sp, 310, 190);
            card.Margin = new Thickness(8);
            grid.Children.Add(card);
        }

        Canvas.SetLeft(grid, 120);
        Canvas.SetTop(grid, 40);
        canvas.Children.Add(grid);

        board.Children.Add(frame);
    }
}
