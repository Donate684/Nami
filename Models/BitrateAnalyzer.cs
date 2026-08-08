using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nami.Models;

public class BitrateDataPoint
{
    public double TimeSeconds { get; set; }
    public double BitrateMbps { get; set; }
    public bool HasIFrame { get; set; }
}

public class BitrateAnalysisResult
{
    public List<BitrateDataPoint> DataPoints { get; set; } = new();
    public double AverageBitrateMbps { get; set; }
    public double MaxBitrateMbps { get; set; }
}

public class BitrateAnalyzer
{
    public async Task<BitrateAnalysisResult> AnalyzeAsync(string filePath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Video file not found", filePath);

        var result = new BitrateAnalysisResult();
        
        // We will collect size per second (in bytes)
        var bytesPerSecond = new Dictionary<int, long>();
        var iFramesPerSecond = new Dictionary<int, bool>();
        
        // Estimate total duration for progress
        double totalDurationSeconds = await GetDurationAsync(filePath);
        
        // ffprobe -v error -select_streams v:0 -show_entries packet=size,pts_time,flags -of csv=p=0 "file"
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v error -select_streams v:0 -show_packets -show_entries packet=pts_time,size,flags -of csv=p=0 \"{filePath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        long maxTimeReported = 0;

        using (var reader = process.StandardOutput)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Format: pts_time,size,flags (e.g., 0.041708,23045,K_)
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    double ptsTime = -1;
                    long size = -1;
                    
                    if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedTime) &&
                        long.TryParse(parts[1], out long parsedSize))
                    {
                        ptsTime = parsedTime;
                        size = parsedSize;
                    }
                    else if (long.TryParse(parts[0], out parsedSize) && 
                             double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out parsedTime))
                    {
                        ptsTime = parsedTime;
                        size = parsedSize;
                    }

                    if (ptsTime >= 0 && size >= 0)
                    {
                        int second = (int)Math.Floor(ptsTime);
                        
                        if (!bytesPerSecond.ContainsKey(second))
                            bytesPerSecond[second] = 0;
                        
                        bytesPerSecond[second] += size;

                        if (parts.Length >= 3 && parts[2].Contains('K'))
                        {
                            iFramesPerSecond[second] = true;
                        }

                        // Progress update
                        if (progress != null && second > maxTimeReported)
                        {
                            maxTimeReported = second;
                            if (totalDurationSeconds > 0)
                            {
                                double prog = Math.Min(100.0, (second / totalDurationSeconds) * 100.0);
                                progress.Report(prog);
                            }
                        }
                    }
                }
            }
        }

        await process.WaitForExitAsync(cancellationToken);

        if (bytesPerSecond.Count == 0)
            return result;

        int maxSecond = bytesPerSecond.Keys.Max();
        long totalBytes = 0;

        for (int i = 0; i <= maxSecond; i++)
        {
            long bytes = bytesPerSecond.ContainsKey(i) ? bytesPerSecond[i] : 0;
            totalBytes += bytes;

            // bytes * 8 = bits. bits / 1_000_000 = Mbps
            double mbps = (bytes * 8.0) / 1_000_000.0;
            
            result.DataPoints.Add(new BitrateDataPoint
            {
                TimeSeconds = i,
                BitrateMbps = mbps,
                HasIFrame = iFramesPerSecond.ContainsKey(i)
            });

            if (mbps > result.MaxBitrateMbps)
                result.MaxBitrateMbps = mbps;
        }

        if (result.DataPoints.Count > 0)
            result.AverageBitrateMbps = (totalBytes * 8.0 / 1_000_000.0) / result.DataPoints.Count;

        return result;
    }

    private async Task<double> GetDurationAsync(string filePath)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (double.TryParse(output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double duration))
            {
                return duration;
            }
        }
        catch
        {
            // Ignore errors
        }
        return 0;
    }
}
