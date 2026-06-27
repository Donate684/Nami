using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nami.Views;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        InitializeComponent();
    }

    public DialogWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled)
            return;

        var position = e.GetPosition(this);
        if (position.Y <= 34)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private void OnYesClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnNoClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
