using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using X20Ctl.Product.Nocturne;

namespace X20Ctl.Product;

public partial class StudioScene
{
    private Border? nocturneDiagPanel;

    /// <summary>The real Diagnostics body lives in one place at a time; give it back before ShowManagement re-adds it.</summary>
    private void NocturneRelease()
    {
        if (nocturneDiagPanel?.Child is ScrollViewer sv) sv.Content = null;
    }

    private void NocturneShowDiagnostics()
    {
        if (nocturnePageHost == null) return;
        nocturnePageHost.Children.Clear();

        var board = new Canvas { Width = 1600, Height = 900, ClipToBounds = true };
        board.Children.Add(NocturneKit.CreateCityBackdrop());

        board.Children.Add(CreateNocturneHeader("Studio"));
        var sidebar = CreateNocturneSidebar("Diagnostics");
        Canvas.SetLeft(sidebar, 24);
        Canvas.SetTop(sidebar, 74);
        board.Children.Add(sidebar);

        var title = NocturneKit.NocturneTitle("PROTOCOL", "DIAGNOSTICS", "Read-only input capture · Use Esc or the mouse to leave");
        Canvas.SetLeft(title, 300);
        Canvas.SetTop(title, 70);
        board.Children.Add(title);

        FrameworkElement body;
        if (Diagnostics is not { } diagnostics)
            body = new TextBlock { Text = "Diagnostics isn't available in this window.", FontFamily = NocturneKit.Body, FontSize = 15, Foreground = NocturneKit.WhiteBrush, Margin = new Thickness(24) };
        else
        {
            if (diagnostics.Body.Parent is Panel owner) owner.Children.Remove(diagnostics.Body);
            body = new ScrollViewer { Content = diagnostics.Body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        nocturneDiagPanel = new Border
        {
            Width = 1180,
            Height = 560,
            Background = new SolidColorBrush(Color.FromArgb(0xEE, 0x01, 0x12, 0x3E)),
            BorderBrush = NocturneKit.LightBlueBrush,
            BorderThickness = new Thickness(1.5),
            Padding = new Thickness(8),
            Child = body
        };
        Canvas.SetLeft(nocturneDiagPanel, 330);
        Canvas.SetTop(nocturneDiagPanel, 250);
        board.Children.Add(nocturneDiagPanel);

        var bottomBar = CreateNocturneBottomBar();
        Canvas.SetLeft(bottomBar, 0);
        Canvas.SetTop(bottomBar, 856);
        board.Children.Add(bottomBar);

        nocturnePageHost.Children.Add(board);
    }
}