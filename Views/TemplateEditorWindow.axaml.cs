using Avalonia.Controls;
using Avalonia.Input;

namespace Nami.Views;

public partial class TemplateEditorWindow : Window
{
    public TemplateEditorWindow()
    {
        InitializeComponent();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled)
            return;

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
