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
            ("CONTROLLER CHECK", "", "Controller Check", () => Check.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)), ""),
            ("WRITE SCAN", "", "Write Scan", () => WriteScan.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)), ""),
            ("VISUAL THEMES", "", "Browse Themes", () => ShowThemes(true), ""),
            ("FEEDBACK & SUPPORT", "", "Open Feedback Form", () => ShowFeedback(true), ""),
            ("SOFTWARE UPDATES", "", "Check for Updates", () => ShowAbout(true), ""),
            ("OPENING CINEMATIC", "", "Replay Intro", () => Replay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)), ""),
            ("APPLICATION SOUNDS", "", "Sound Settings", () => ShowAbout(true), ""),
            ("ABOUT & CREDITS", "", "About", () => ShowAbout(true), "")
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

        var allThemes = ThemeCatalog.All.Select(theme => (Id: theme.Id, Name: theme.Name, Accent: theme.Accent.ToString())).ToArray();

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
