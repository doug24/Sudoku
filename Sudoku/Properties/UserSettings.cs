using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sudoku.Properties;

/// <summary>
/// Modern JSON-based user settings stored in %AppData%\Roaming\Sudoku\settings.json.
/// Replaces the legacy System.Configuration.ApplicationSettingsBase implementation.
/// </summary>
public class UserSettings
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sudoku");

    private static readonly string SettingsFilePath =
        Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static UserSettings? _default;

    /// <summary>Gets the singleton settings instance, loading from disk (or migrating from legacy) on first access.</summary>
    public static UserSettings Default => _default ??= Load();

    // ── Settings properties ──────────────────────────────────────────────────

    public int SectionLayout { get; set; } = -1;
    public string PuzzleSymmetry { get; set; } = "MIRROR";
    public string PuzzleDifficulty { get; set; } = "INTERMEDIATE";
    public bool HighlightIncorrect { get; set; } = true;
    public bool CleanPencilMarks { get; set; } = true;
    public bool NumberFirstMode { get; set; } = true;
    public bool EnableNumberHighlight { get; set; } = true;
    public bool ShowTimer { get; set; } = false;
    public bool DarkMode { get; set; } = false;
    public bool ShowKillerCalculator { get; set; } = true;

    // ── Persistence ──────────────────────────────────────────────────────────

    private static UserSettings Load()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
            }
            catch
            {
                // Corrupt file — fall through to defaults (or migration)
            }
        }

        // No JSON file yet: attempt one-time migration from legacy settings
        return MigrateFromLegacy();
    }

    /// <summary>Saves the current settings to disk.</summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSettings] Failed to save settings: {ex.Message}");
        }
    }

    // ── Legacy migration ─────────────────────────────────────────────────────

    private static UserSettings MigrateFromLegacy()
    {
        var settings = new UserSettings();

        try
        {
            // Upgrade moves the stored values forward across application version changes
            // so the properties reflect what the user last saved.
            Settings.Default.Upgrade();

            settings.SectionLayout = Settings.Default.SectionLayout;
            settings.PuzzleSymmetry = Settings.Default.PuzzleSymmetry;
            settings.PuzzleDifficulty = Settings.Default.PuzzleDifficulty;
            settings.HighlightIncorrect = Settings.Default.HighlightIncorrect;
            settings.CleanPencilMarks = Settings.Default.CleanPencilMarks;
            settings.NumberFirstMode = Settings.Default.NumberFirstMode;
            settings.EnableNumberHighlight = Settings.Default.EnableNumberHighlight;
            settings.ShowTimer = Settings.Default.ShowTimer;
            settings.DarkMode = Settings.Default.DarkMode;
            settings.ShowKillerCalculator = Settings.Default.ShowKillerCalculator;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSettings] Legacy migration failed: {ex.Message}");
        }

        // Persist immediately so next launch reads from JSON
        settings.Save();
        return settings;
    }
}
