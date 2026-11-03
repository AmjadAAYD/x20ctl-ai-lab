using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using X20Ctl.Product.Nocturne;

namespace X20Ctl.Product;

public partial class StudioScene
{
    private string nocturneMouseGroup = "Mouse";
    private bool nocturneMouseWired;

    private void NocturneShowMouse()
    {
        if (nocturnePageHost == null) return;
        nocturnePageHost.Children.Clear();

        var board = new Canvas { Width = 1600, Height = 900, ClipToBounds = true };
        board.Children.Add(NocturneKit.CreateCityBackdrop());

        // Header & Sidebar
        board.Children.Add(CreateNocturneHeader("Studio"));
        var sidebar = CreateNocturneSidebar("Mouse");
        Canvas.SetLeft(sidebar, 24);
        Canvas.SetTop(sidebar, 74);
        board.Children.Add(sidebar);

        // Title Block
        var title = NocturneKit.NocturneTitle("MOUSE", "EMULATION", "Controller desktop mouse emulation and custom button mapping · Local draft");
        Canvas.SetLeft(title, 300);
        Canvas.SetTop(title, 70);
        board.Children.Add(title);

        var mouse = Mouse;
        if (mouse == null)
        {
            var none = new TextBlock { Text = "Mouse emulation unavailable for this device.", FontFamily = NocturneKit.Body, FontSize = 15, Foreground = NocturneKit.InkBrush, TextWrapping = TextWrapping.Wrap, Width = 520 };
            var noneCard = NocturneKit.NocturneWhiteCard(none, width: 620, height: 200);
            Canvas.SetLeft(noneCard, 320);
            Canvas.SetTop(noneCard, 260);
            board.Children.Add(noneCard);
            nocturnePageHost.Children.Add(board);
            return;
        }
        if (!nocturneMouseWired)
        {
            nocturneMouseWired = true;
            mouse.Watch(() => NocturneLater());
        }

        // ==========================================
        // LEFT STAGE: Inputs List on Slanted White Card
        // ==========================================
        var leftStack = new StackPanel { Width = 520 };
        leftStack.Children.Add(new TextBlock
        {
            Text = "CONTROLLER CONTROLS",
            FontFamily = NocturneKit.Display,
            FontWeight = FontWeights.Black,
            FontStyle = FontStyles.Italic,
            FontSize = 24,
            Foreground = NocturneKit.InkBrush,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 460 };
        var inputsStack = new StackPanel();

        foreach (var inKey in mouse.Inputs)
        {
            bool isSelected = (mouse.SelectedKey == inKey);
            var rowBorder = new Border
            {
                Background = isSelected ? NocturneKit.ActiveBlueBrush : new SolidColorBrush(Color.FromArgb(0x12, 0x01, 0x00, 0x42)),
                BorderBrush = isSelected ? NocturneKit.CobaltBrush : new SolidColorBrush(Color.FromArgb(0x30, 0x01, 0x00, 0x42)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                RenderTransform = NocturneKit.CreateSlant(-12),
                RenderTransformOrigin = new Point(0.5, 0.5),
                Tag = $"theme:mouse:input:{inKey.ToLowerInvariant()}"
            };

            var rGrid = new Grid
            {
                RenderTransform = NocturneKit.CreateSlant(12),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            rGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            rGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            rGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var keyBadge = new Border
            {
                MinWidth = 34,
                Height = 20,
                Background = isSelected ? NocturneKit.WhiteBrush : new SolidColorBrush(Color.FromArgb(0xbb, 0x00, 0x56, 0xfb)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            keyBadge.Child = new TextBlock { Text = inKey.Replace("DPAD_", "").Replace("SELECT", "SEL"), FontFamily = NocturneKit.Display, FontWeight = FontWeights.Bold, FontSize = 10, Foreground = isSelected ? NocturneKit.InkBrush : NocturneKit.WhiteBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(keyBadge, 0);

            var keyName = new TextBlock { Text = mouse.LabelFor(inKey), FontFamily = NocturneKit.Display, FontWeight = FontWeights.Bold, FontSize = 12, Foreground = isSelected ? NocturneKit.WhiteBrush : NocturneKit.InkBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            Grid.SetColumn(keyName, 1);

            var actLabel = new TextBlock { Text = mouse.ActionFor(inKey), FontFamily = NocturneKit.Body, FontSize = 11.5, Foreground = isSelected ? NocturneKit.WhiteBrush : new SolidColorBrush(Color.FromArgb(0xbb, 0x01, 0x00, 0x42)), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(actLabel, 2);

            rGrid.Children.Add(keyBadge);
            rGrid.Children.Add(keyName);
            rGrid.Children.Add(actLabel);
            rowBorder.Child = rGrid;

            string capturedKey = inKey;
            rowBorder.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                mouse.PickKey(capturedKey);
                NocturneShowMouse();
            };
            inputsStack.Children.Add(rowBorder);
        }
        scroll.Content = inputsStack;
        leftStack.Children.Add(scroll);

        var leftCard = NocturneKit.NocturneWhiteCard(leftStack, width: 570, height: 570);
        Canvas.SetLeft(leftCard, 300);
        Canvas.SetTop(leftCard, 220);
        board.Children.Add(leftCard);

        // ==========================================
        // RIGHT STAGE: Action Assignment Deck
        // ==========================================
        var rightStack = new StackPanel { Width = 520 };

        string curInputName = mouse.LabelFor(mouse.SelectedKey);
        rightStack.Children.Add(new TextBlock
        {
            Text = $"ASSIGN: {curInputName.ToUpperInvariant()}",
            FontFamily = NocturneKit.Display,
            FontWeight = FontWeights.Black,
            FontStyle = FontStyles.Italic,
            FontSize = 24,
            Foreground = NocturneKit.InkBrush,
            Margin = new Thickness(0, 0, 0, 10)
        });

        // Category Tabs
        if (!mouse.Groups.Any(g => g.Group.Equals(nocturneMouseGroup, StringComparison.OrdinalIgnoreCase)))
            nocturneMouseGroup = mouse.Groups.FirstOrDefault(g => g.Actions.Contains(mouse.CurrentAction)).Group ?? mouse.Groups[0].Group;
        var catSp = new WrapPanel { Width = 500, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var grp in mouse.Groups)
        {
            bool isCurGrp = grp.Group.Equals(nocturneMouseGroup, StringComparison.OrdinalIgnoreCase);
            var gBtn = NocturneKit.NocturneChip(grp.Group, isCurGrp, () =>
            {
                nocturneMouseGroup = grp.Group;
                NocturneShowMouse();
            });
            if (isCurGrp)
            {
                // Selected chips are white, which vanishes on this white card.
                gBtn.Background = NocturneKit.ActiveBlueBrush;
                gBtn.BorderBrush = NocturneKit.CobaltBrush;
                if (gBtn.Child is TextBlock gText) gText.Foreground = NocturneKit.WhiteBrush;
            }
            catSp.Children.Add(gBtn);
        }
        rightStack.Children.Add(catSp);

        // Action Chips Grid
        var curGrp = mouse.Groups.FirstOrDefault(g => g.Group.Equals(nocturneMouseGroup, StringComparison.OrdinalIgnoreCase));
        if (curGrp.Actions != null)
        {
            var chipsWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 14), Height = 180 };
            string curAct = mouse.ActionFor(mouse.SelectedKey);

            foreach (var act in curGrp.Actions)
            {
                bool isCurrentAction = (MouseWorkbench.Friendly(act) == curAct);
                var chip = new Border
                {
                    Padding = new Thickness(10, 5, 10, 5),
                    Background = isCurrentAction ? NocturneKit.ActiveBlueBrush : new SolidColorBrush(Color.FromArgb(0x18, 0x01, 0x00, 0x42)),
                    BorderBrush = isCurrentAction ? NocturneKit.CobaltBrush : new SolidColorBrush(Color.FromArgb(0x30, 0x01, 0x00, 0x42)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(2),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    RenderTransform = NocturneKit.CreateSlant(-12),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    Tag = $"theme:mouse:action:{act}"
                };
                chip.Child = new TextBlock
                {
                    Text = MouseWorkbench.Friendly(act),
                    FontFamily = NocturneKit.Display,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = isCurrentAction ? NocturneKit.WhiteBrush : NocturneKit.InkBrush,
                    RenderTransform = NocturneKit.CreateSlant(12),
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                var assignAct = act;
                chip.MouseLeftButtonUp += (_, e) =>
                {
                    e.Handled = true;
                    mouse.Assign(assignAct);
                    NocturneShowMouse();
                };
                chipsWrap.Children.Add(chip);
            }
            rightStack.Children.Add(chipsWrap);
        }

        // Thin rule
        rightStack.Children.Add(new Rectangle { Height = 1, Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x01, 0x00, 0x42)), Margin = new Thickness(0, 0, 0, 14) });

        // Speed Slider
        rightStack.Children.Add(new TextBlock { Text = "POINTER SPEED", FontFamily = NocturneKit.Display, FontWeight = FontWeights.Bold, FontSize = 12, Foreground = NocturneKit.InkBrush, Margin = new Thickness(0, 4, 0, 4) });
        var speedSlider = mouse.SpeedSlider;
        if (speedSlider.Parent is Panel sp0) sp0.Children.Remove(speedSlider); else if (speedSlider.Parent is ContentControl sc0) sc0.Content = null; else if (speedSlider.Parent is Decorator sd0) sd0.Child = null;
        speedSlider.Width = 480;
        speedSlider.HorizontalAlignment = HorizontalAlignment.Left;
        speedSlider.Margin = new Thickness(0, 0, 0, 16);
        rightStack.Children.Add(speedSlider);

        var rightCard = NocturneKit.NocturneWhiteCard(rightStack, width: 570, height: 570);
        Canvas.SetLeft(rightCard, 960);
        Canvas.SetTop(rightCard, 220);
        board.Children.Add(rightCard);

        // Bottom Bar
        var bottomBar = CreateNocturneBottomBar();
        Canvas.SetLeft(bottomBar, 0);
        Canvas.SetTop(bottomBar, 856);
        board.Children.Add(bottomBar);

        nocturnePageHost.Children.Add(board);
    }
}
