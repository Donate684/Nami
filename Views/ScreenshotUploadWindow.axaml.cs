using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace Nami.Views;

public partial class ScreenshotUploadWindow : Window
{
    public ScreenshotUploadWindow()
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

    private void OnOpenImageBanClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://imageban.ru/",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore exceptions opening browser
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        string text = UrlsTextBox.Text ?? string.Empty;
        Close(text);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
