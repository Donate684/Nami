using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Nami.Models;

namespace Nami.ViewModels;

public partial class BitrateGraphViewModel : ViewModelBase
{
    private readonly BitrateAnalyzer _analyzer;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _videoFilePath = "";

    [ObservableProperty]
    private string _statusText = "Готов к анализу";

    [ObservableProperty]
    private bool _isAnalyzing = false;

    [ObservableProperty]
    private double _analysisProgress = 0;

    public event Action<BitrateAnalysisResult>? OnAnalysisCompleted;

    public BitrateGraphViewModel()
    {
        _analyzer = new BitrateAnalyzer();
    }

    public async Task StartAnalysisAsync(string filePath)
    {
        VideoFilePath = filePath;
        IsAnalyzing = true;
        StatusText = "Анализ видеофайла...";
        AnalysisProgress = 0;
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<double>(p => AnalysisProgress = p);
            var result = await _analyzer.AnalyzeAsync(filePath, progress, _cts.Token);
            
            StatusText = $"Анализ завершен. Средний битрейт: {result.AverageBitrateMbps:F2} Mbps, Максимальный: {result.MaxBitrateMbps:F2} Mbps";
            
            OnAnalysisCompleted?.Invoke(result);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Анализ отменен";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }
}
