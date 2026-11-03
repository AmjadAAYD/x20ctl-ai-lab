using System.Windows;
using System.Windows.Controls;
using X20Ctl.Product.Nocturne;
namespace X20Ctl.Product;
public partial class StudioScene
{
    private Border? nocturneLightingPanel;
    private void NocturneShowLighting()
    {
        if (nocturnePageHost == null || rgbBody is not FrameworkElement body) return;
        if (body.Parent is Panel owner) owner.Children.Remove(body);
        nocturnePageHost.Children.Clear();
        var board = new Canvas { Width = 1600, Height = 900 };
        board.Children.Add(NocturneKit.CreateCityBackdrop());
        board.Children.Add(CreateNocturneHeader("Studio"));
        var sidebar = CreateNocturneSidebar("Lighting"); Canvas.SetLeft(sidebar, 24); Canvas.SetTop(sidebar, 74); board.Children.Add(sidebar);
        nocturneLightingPanel = new Border { Width = 1200, Height = 720, Padding = new Thickness(12), Background = NocturneKit.VoidBrush, Child = body };
        Canvas.SetLeft(nocturneLightingPanel, 300); Canvas.SetTop(nocturneLightingPanel, 100); board.Children.Add(nocturneLightingPanel);
        var bottom = CreateNocturneBottomBar(); Canvas.SetTop(bottom, 856); board.Children.Add(bottom);
        nocturnePageHost.Children.Add(board);
    }
}
