using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace Nami.Views;

public partial class AlbumNameDialogWindow : Window
{
    public AlbumNameDialogWindow()
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
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var text = this.FindControl<TextBox>("AlbumNameTextBox")?.Text?.Trim();
        Close(text);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
