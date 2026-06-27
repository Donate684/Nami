using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace Nami.Views;

public partial class CoverUploadWindow : Window
{
    public CoverUploadWindow()
    {
        InitializeComponent();
    }

    public CoverUploadWindow(string statsText, string? defaultUrl = null)
    {
        InitializeComponent();
        StatsTextBlock.Text = statsText;
        if (!string.IsNullOrEmpty(defaultUrl))
        {
            CoverUrlTextBox.Text = defaultUrl;
        }
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
            // Ignore
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        string text = CoverUrlTextBox.Text ?? string.Empty;
        Close(text.Trim());
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
