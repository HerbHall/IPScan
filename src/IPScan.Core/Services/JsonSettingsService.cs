using System.Text.Json;
using System.Text.Json.Serialization;
using IPScan.Core.Models;
using Microsoft.Extensions.Logging;

namespace IPScan.Core.Services;

/// <summary>
/// JSON file-based implementation of settings service.
/// </summary>
public class JsonSettingsService : ISettingsService
{
    private readonly string _filePath;
    private readonly ILogger<JsonSettingsService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private AppSettings? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonSettingsService(ILogger<JsonSettingsService> logger, string? filePath = null)
    {
        _logger = logger;
        _filePath = filePath ?? GetDefaultFilePath();
    }

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "IPScan", "settings.json");
    }

    /// <inheritdoc />
    public async Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache != null)
                return _cache;

            if (!File.Exists(_filePath))
            {
                _cache = new AppSettings();
                _logger.LogDebug("Settings file not found, using defaults");
                return _cache;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                _cache = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                _logger.LogDebug("Loaded settings from {Path}", _filePath);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse settings file, using defaults");
                _cache = new AppSettings();
            }

            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
            _cache = settings;
            _logger.LogDebug("Saved settings to {Path}", _filePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new AppSettings();
        await SaveSettingsAsync(defaults, cancellationToken);
        _logger.LogInformation("Settings reset to defaults");
    }
}
