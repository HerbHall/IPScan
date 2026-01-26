using IPScan.Core.Models;

namespace IPScan.Core.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the application settings.
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}
