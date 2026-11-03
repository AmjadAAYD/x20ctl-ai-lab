using System.Windows;
using System.Windows.Controls;
using X20Ctl.Product.Nocturne;
namespace X20Ctl.Product;
public partial class StudioScene
{
    private Border? nocturneScreenPanel;
    private void NocturneShowScreen()
    {
        if (nocturnePageHost == null || Screen?.Body is not FrameworkElement body) return;
        if (body.Parent is Panel owner) owner.Children.Remove(body);
        nocturnePageHost.Children.Clear();
        var board = new Canvas { Width = 1600, Height = 900 };
        board.Children.Add(NocturneKit.CreateCityBackdrop());
        board.Children.Add(CreateNocturneHeader("Studio"));
        var sidebar = CreateNocturneSidebar("Screen"); Canvas.SetLeft(sidebar, 24); Canvas.SetTop(sidebar, 74); board.Children.Add(sidebar);
        nocturneScreenPanel = new Border { Width = 1200, Height = 720, Padding = new Thickness(12), Background = NocturneKit.VoidBrush, Child = body };
        Canvas.SetLeft(nocturneScreenPanel, 300); Canvas.SetTop(nocturneScreenPanel, 100); board.Children.Add(nocturneScreenPanel);
        var bottom = CreateNocturneBottomBar(); Canvas.SetTop(bottom, 856); board.Children.Add(bottom);
        nocturnePageHost.Children.Add(board);
    }
}
