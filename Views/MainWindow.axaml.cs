using Avalonia.Controls;
using Avalonia.Input;

namespace Nami.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled)
            return;

        // If the click is on the top 34 pixels (TitleBar height)
        var position = e.GetPosition(this);
        if (position.Y <= 34)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized 
                    ? WindowState.Normal 
                    : WindowState.Maximized;
            }
            else
            {
                BeginMoveDrag(e);
            }
            e.Handled = true;
        }
    }
}