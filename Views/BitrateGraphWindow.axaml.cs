using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nami.Models;
using Nami.ViewModels;
using ScottPlot;
using ScottPlot.Avalonia;

namespace Nami.Views;

public partial class BitrateGraphWindow : Window
{
    private AvaPlot _avaPlot = null!;

    public BitrateGraphWindow()
    {
        InitializeComponent();
        
        _avaPlot = this.FindControl<AvaPlot>("AvaPlot1")!;
        
        if (_avaPlot != null)
        {
            _avaPlot.Plot.FigureBackground.Color = Color.FromHex("#1C1C1E");
            _avaPlot.Plot.DataBackground.Color = Color.FromHex("#2C2C2E");
            _avaPlot.Plot.Axes.Color(Color.FromHex("#A0A0A0"));
            _avaPlot.Plot.Axes.Bottom.Label.Text = "Время (сек)";
            _avaPlot.Plot.Axes.Left.Label.Text = "Битрейт (Mbps)";
            _avaPlot.Plot.Grid.MajorLineColor = Color.FromHex("#3A3A3C");
            
            _avaPlot.PointerPressed += AvaPlot_PointerPressed;
        }

        DataContextChanged += OnDataContextChanged;
    }

    private BitrateAnalysisResult? _lastResult;

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is BitrateGraphViewModel vm)
        {
            vm.OnAnalysisCompleted += result =>
            {
                _lastResult = result;
                Dispatcher.UIThread.InvokeAsync(() => DrawPlot(result));
            };
        }
    }

    private void DrawPlot(BitrateAnalysisResult result)
    {
        if (_avaPlot == null || result.DataPoints.Count == 0) return;

        _avaPlot.Plot.Clear();

        double[] xs = new double[result.DataPoints.Count];
        double[] ys = new double[result.DataPoints.Count];
        
        int iFrameCount = result.DataPoints.Count(p => p.HasIFrame);
        double[] iFrameXs = new double[iFrameCount];
        double[] iFrameYs = new double[iFrameCount];

        int index = 0;
        int iFrameIndex = 0;
        foreach (var pt in result.DataPoints)
        {
            xs[index] = pt.TimeSeconds;
            ys[index] = pt.BitrateMbps;
            
            if (pt.HasIFrame)
            {
                iFrameXs[iFrameIndex] = pt.TimeSeconds;
                iFrameYs[iFrameIndex] = pt.BitrateMbps;
                iFrameIndex++;
            }
            
            index++;
        }

        // Draw main bitrate line
        var line = _avaPlot.Plot.Add.Scatter(xs, ys);
        line.LineWidth = 2;
        line.MarkerSize = 0;
        line.Color = Color.FromHex("#7C3AED");

        // Draw I-Frames
        if (iFrameCount > 0)
        {
            var iFrames = _avaPlot.Plot.Add.Scatter(iFrameXs, iFrameYs);
            iFrames.LineWidth = 0;
            iFrames.MarkerSize = 4;
            iFrames.Color = Color.FromHex("#DB2777");
            iFrames.LegendText = "I-Frames";
        }

        // Average line
        var avgLine = _avaPlot.Plot.Add.HorizontalLine(result.AverageBitrateMbps);
        avgLine.Color = Colors.Green;
        avgLine.LineWidth = 2;
        avgLine.LinePattern = LinePattern.Dashed;
        avgLine.LegendText = "Средний битрейт";

        var legend = _avaPlot.Plot.ShowLegend();
        legend.BackgroundColor = Color.FromHex("#B31C1C1E"); // 70% opacity dark background
        legend.OutlineStyle.Color = Color.FromHex("#3A3A3C");
        legend.FontColor = Colors.White;

        _avaPlot.Plot.Axes.AutoScale();
        _avaPlot.Refresh();
    }

    private async void AvaPlot_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_lastResult == null || DataContext is not BitrateGraphViewModel vm || string.IsNullOrEmpty(vm.VideoFilePath)) 
            return;

        var pos = e.GetCurrentPoint(_avaPlot).Position;
        Pixel mousePixel = new Pixel((float)pos.X, (float)pos.Y);
        Coordinates mouseLocation = _avaPlot.Plot.GetCoordinates(mousePixel);

        double timeSeconds = mouseLocation.X;
        
        // Find closest data point
        var closestPt = _lastResult.DataPoints.OrderBy(p => System.Math.Abs(p.TimeSeconds - timeSeconds)).FirstOrDefault();
        if (closestPt == null || closestPt.TimeSeconds < 0) return;

        timeSeconds = closestPt.TimeSeconds;

        ShowPreviewOverlay(timeSeconds, closestPt.BitrateMbps, pos.X);
        await LoadFramePreviewAsync(vm.VideoFilePath, timeSeconds);
    }

    private void ShowPreviewOverlay(double timeSeconds, double bitrateMbps, double mouseX)
    {
        var previewBorder = this.FindControl<Border>("PreviewBorder");
        var previewTitle = this.FindControl<TextBlock>("PreviewTitle");
        var previewBitrate = this.FindControl<TextBlock>("PreviewBitrate");
        var previewImage = this.FindControl<Avalonia.Controls.Image>("PreviewImage");
        var loadingText = this.FindControl<TextBlock>("PreviewLoadingText");

        if (previewBorder == null || previewTitle == null || previewBitrate == null || previewImage == null || loadingText == null)
            return;

        if (mouseX > _avaPlot.Bounds.Width / 2)
        {
            previewBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            previewBorder.Margin = new Avalonia.Thickness(40, 0, 0, 0);
        }
        else
        {
            previewBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
            previewBorder.Margin = new Avalonia.Thickness(0, 0, 40, 0);
        }

        var timeSpan = System.TimeSpan.FromSeconds(timeSeconds);
        previewTitle.Text = $"Кадр на {timeSpan:hh\\:mm\\:ss}";
        previewBitrate.Text = $"Битрейт: {bitrateMbps:F2} Mbps";
        previewImage.Source = null;
        loadingText.IsVisible = true;
        previewBorder.IsVisible = true;
    }

    private async System.Threading.Tasks.Task LoadFramePreviewAsync(string videoPath, double timeSeconds)
    {
        string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nami_preview_{System.Guid.NewGuid()}.jpg");
        
        try
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -ss {timeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} -i \"{videoPath}\" -vframes 1 -q:v 2 \"{tempFile}\"",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
            process.Start();
            await process.WaitForExitAsync();

            if (System.IO.File.Exists(tempFile))
            {
                var previewImage = this.FindControl<Avalonia.Controls.Image>("PreviewImage");
                var loadingText = this.FindControl<TextBlock>("PreviewLoadingText");
                
                if (previewImage != null && loadingText != null)
                {
                    using (var stream = System.IO.File.OpenRead(tempFile))
                    {
                        var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
                        Dispatcher.UIThread.Post(() => 
                        {
                            previewImage.Source = bitmap;
                            loadingText.IsVisible = false;
                        });
                    }
                }
            }
            else
            {
                var loadingText = this.FindControl<TextBlock>("PreviewLoadingText");
                if (loadingText != null)
                {
                    Dispatcher.UIThread.Post(() => loadingText.Text = "Кадр не найден");
                }
            }
        }
        catch (System.Exception)
        {
            var loadingText = this.FindControl<TextBlock>("PreviewLoadingText");
            if (loadingText != null)
            {
                Dispatcher.UIThread.Post(() => loadingText.Text = "Ошибка загрузки");
            }
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            }
        }
    }

    private void OnClosePreviewClick(object? sender, RoutedEventArgs e)
    {
        var previewBorder = this.FindControl<Border>("PreviewBorder");
        if (previewBorder != null) previewBorder.IsVisible = false;
    }


}
