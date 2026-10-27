using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace CathLab.Infrastructure.BackgroundServices;

/// <summary>Singleton holding the PACS ingestion period (persisted to App_Data/ingestion-settings.json).</summary>
public class IngestionSettings
{
    public const int MinMinutes = 1;
    public const int MaxMinutes = 1440;   // 24 h
    public const int DefaultMinutes = 15;

    private readonly string _path;
    private readonly object _gate = new();
    private CancellationTokenSource _changed = new();
    private int _minutes = DefaultMinutes;
    private DateTime? _lastRunUtc;

    private class Stored { public int IntervalMinutes { get; set; } }

    public IngestionSettings(IHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "ingestion-settings.json");
        try
        {
            if (File.Exists(_path))
            {
                var s = JsonSerializer.Deserialize<Stored>(File.ReadAllText(_path));
                if (s is { IntervalMinutes: >= MinMinutes and <= MaxMinutes })
                    _minutes = s.IntervalMinutes;
            }
        }
        catch { /* unreadable file -> keep default */ }
    }

    public int IntervalMinutes { get { lock (_gate) return _minutes; } }
    public TimeSpan Interval => TimeSpan.FromMinutes(IntervalMinutes);
    public DateTime? LastRunUtc { get { lock (_gate) return _lastRunUtc; } }
    public DateTime? NextRunUtc => LastRunUtc is { } l ? l + Interval : null;

    public void MarkRun(DateTime utc) { lock (_gate) _lastRunUtc = utc; }

    /// <summary>Token cancelled whenever the interval changes (wakes the worker's wait).</summary>
    public CancellationToken GetChangeToken() { lock (_gate) return _changed.Token; }

    public void SetInterval(int minutes)
    {
        if (minutes < MinMinutes || minutes > MaxMinutes)
            throw new ArgumentOutOfRangeException(nameof(minutes));

        CancellationTokenSource old;
        lock (_gate)
        {
            _minutes = minutes;
            File.WriteAllText(_path, JsonSerializer.Serialize(new Stored { IntervalMinutes = minutes }));
            old = _changed;
            _changed = new CancellationTokenSource();
        }
        old.Cancel();
    }
}