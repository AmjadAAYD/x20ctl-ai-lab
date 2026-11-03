using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using X20Ctl.Product.Nocturne;

namespace X20Ctl.Product;

public partial class StudioScene
{
    private PcModePage? nocturnePcSource;

    private void NocturneShowPcMode()
    {
        if (nocturnePageHost == null) return;
        nocturnePageHost.Children.Clear();

        var board = new Canvas { Width = 1600, Height = 900, ClipToBounds = true };
        board.Children.Add(NocturneKit.CreateCityBackdrop());

        board.Children.Add(CreateNocturneHeader("Studio"));
        var sidebar = CreateNocturneSidebar("PC Mode");
        Canvas.SetLeft(sidebar, 24);
        Canvas.SetTop(sidebar, 74);
        board.Children.Add(sidebar);

        var title = NocturneKit.NocturneTitle("PC MODE", "ROUTING", "Route controller input through the saved PC profile to a virtual Xbox device. Nothing is written to the controller.");
        Canvas.SetLeft(title, 300);
        Canvas.SetTop(title, 70);
        board.Children.Add(title);

        var original = ManagementBodyHost.Children.OfType<ScrollViewer>().FirstOrDefault();
        var source = original?.Content as PcModePage ?? nocturnePcSource;
        if (original?.Content is PcModePage page)
        {
            nocturnePcSource = page;
            original.Content = null;
        }

        FrameworkElement body;
        if (source == null)
            body = new TextBlock { Text = "PC mode isn't available in this window.", FontFamily = NocturneKit.Body, FontSize = 15, Foreground = NocturneKit.WhiteBrush, Margin = new Thickness(24) };
        else
        {
            if (source.Parent is ContentControl holder) holder.Content = null;
            else if (source.Parent is Decorator frame) frame.Child = null;
            else if (source.Parent is Panel owner) owner.Children.Remove(source);
            body = new ScrollViewer { Content = source, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        var panel = new Border
        {
            Width = 1180,
            Height = 560,
            Background = new SolidColorBrush(Color.FromArgb(0xEE, 0x01, 0x12, 0x3E)),
            BorderBrush = NocturneKit.LightBlueBrush,
            BorderThickness = new Thickness(1.5),
            Padding = new Thickness(8),
            Child = body
        };
        Canvas.SetLeft(panel, 330);
        Canvas.SetTop(panel, 250);
        board.Children.Add(panel);

        var bottomBar = CreateNocturneBottomBar();
        Canvas.SetLeft(bottomBar, 0);
        Canvas.SetTop(bottomBar, 856);
        board.Children.Add(bottomBar);

        nocturnePageHost.Children.Add(board);
    }
}