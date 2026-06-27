using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace Nami.Views;

public class ScreenshotItem : IDisposable
{
    public byte[] ImageData { get; }
    public Bitmap Thumbnail { get; }

    public ScreenshotItem(byte[] imageData)
    {
        ImageData = imageData;
        using var stream = new System.IO.MemoryStream(imageData);
        Thumbnail = Bitmap.DecodeToWidth(stream, 240);
    }

    public void Dispose()
    {
        Thumbnail?.Dispose();
    }
}

public partial class ScreenshotSelectionWindow : Window
{
    private List<ScreenshotItem> _items = new();

    public ScreenshotSelectionWindow()
    {
        InitializeComponent();
        Closed += OnWindowClosed;
    }

    private void OnTitleBarPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
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

    public ScreenshotSelectionWindow(IEnumerable<byte[]> imageDatas)
    {
        InitializeComponent();
        Closed += OnWindowClosed;
        
        foreach (var data in imageDatas)
        {
            try
            {
                _items.Add(new ScreenshotItem(data));
            }
            catch
            {
                // Skip invalid images
            }
        }

        ScreenshotsListBox.ItemsSource = _items;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        foreach (var item in _items)
        {
            item.Dispose();
        }
        _items.Clear();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int count = ScreenshotsListBox.SelectedItems?.Count ?? 0;
        SelectionStatusTextBlock.Text = $"Выбрано: {count} скриншотов";
        ConfirmButton.Content = $"Подтвердить ({count}/4)";
        ConfirmButton.IsEnabled = (count == 4);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        var selected = ScreenshotsListBox.SelectedItems
            ?.Cast<ScreenshotItem>()
            .Select(item => item.ImageData)
            .ToList();

        if (selected != null && selected.Count == 4)
        {
            Close(selected);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
