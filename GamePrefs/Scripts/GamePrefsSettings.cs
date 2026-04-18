// ============================================================
//  GamePrefsSettings.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  This is the only file you need to touch to configure the
//  save system for a new project.
// ============================================================

/// <summary>
/// Central configuration for the GamePrefs save system.
/// Modify constants here — no other file needs changing.
/// </summary>
public static class GamePrefsSettings
{
    public const bool DEBUG = false;
    // ── Save Location ──────────────────────────────────────────────────────

    /// <summary>
    /// Sub-folder where save files live.<br/>
    ///   Editor : [ProjectRoot]/SaveData/<br/>
    ///   Builds : Application.persistentDataPath/SaveData/
    /// </summary>
    public const string SaveFolder = "SaveData";

    /// <summary>Base file name — slot suffix and extension are appended automatically.</summary>
    public const string BaseFileName = "gameprefs";

    /// <summary>File extension used for save files.</summary>
    public const string FileExtension = ".json";

    /// <summary>
    /// When true and running inside the Unity Editor, saves go to the project
    /// root so you can inspect them alongside your assets.<br/>
    /// No effect in standalone builds (always uses persistentDataPath).
    /// </summary>
    public const bool UseProjectFolderInEditor = true;

    // ── Encryption ─────────────────────────────────────────────────────────

    /// <summary>
    /// Encrypt save files with AES-128. A unique random IV is generated
    /// per save and stored inside the file — identical data produces
    /// different ciphertext on every write.
    /// </summary>
    public const bool UseEncryption = false;

    /// <summary>
    /// AES-128 encryption key. Must be 16 ASCII characters
    /// (longer strings are truncated, shorter are zero-padded).<br/>
    /// <b>Change this before shipping your game.</b>
    /// </summary>
    public const string EncryptionKey = "ChangeMe!Key1234";   // 16 chars

    // ── Backup ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Copy the existing save to .bak before overwriting it.
    /// Guards against data loss from crashes or corrupt writes.
    /// </summary>
    public const bool CreateBackup = true;

    /// <summary>Extension appended to backup files.</summary>
    public const string BackupExtension = ".bak";

    // ── Auto-save ──────────────────────────────────────────────────────────

    /// <summary>
    /// Automatically save the active slot on application quit and on
    /// mobile backgrounding. Zero scene setup — bootstraps itself.
    /// </summary>
    public const bool UseAutoSave = true;

    // ── Slots ──────────────────────────────────────────────────────────────

    /// <summary>Total number of save slots (0-indexed).</summary>
    public const int MaxSaveSlots = 3;

    // ── Versioning ─────────────────────────────────────────────────────────

    /// <summary>
    /// Written into every save file header. Increment when your save
    /// format changes so you can detect and migrate old saves at load time.
    /// </summary>
    public const int SaveVersion = 1;
}