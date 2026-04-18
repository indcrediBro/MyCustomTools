// ============================================================
//  GamePrefs.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Usage — identical pattern to Unity's PlayerPrefs:
//      GamePrefs.SetInt("score", 100);
//      int s = GamePrefs.GetInt("score", 0);
//
//  Configure everything in GamePrefsSettings.cs.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
#pragma warning disable CS0162
/// <summary>
/// Drop-in replacement for Unity's PlayerPrefs.<br/>
/// Saves data as JSON (optionally AES-encrypted) with backup protection,
/// multiple save slots, events, and support for bool / Vector2 / Vector3
/// and any <c>[Serializable]</c> type.<br/><br/>
/// Configure all behaviour in <see cref="GamePrefsSettings"/>.
/// </summary>
public static class GamePrefs
{
    // ── State ──────────────────────────────────────────────────────────────

    private static Dictionary<string, string> _data;
    private static int  _activeSlot = 0;
    private static bool _isDirty    = false;

    // Lazy-load: first access triggers Load() automatically.
    private static Dictionary<string, string> Data
    {
        get { if (_data == null) Load(); return _data; }
    }

    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>Fired after a successful save. Argument is the slot index.</summary>
    public static event Action<int> OnSaved;

    /// <summary>Fired after a successful load. Argument is the slot index.</summary>
    public static event Action<int> OnLoaded;

    /// <summary>Fired when the active slot changes. Arguments are (oldSlot, newSlot).</summary>
    public static event Action<int, int> OnSlotChanged;

    // ── Properties ─────────────────────────────────────────────────────────

    /// <summary>Index of the currently active save slot.</summary>
    public static int ActiveSlot => _activeSlot;

    /// <summary>True if there are unsaved changes in memory.</summary>
    public static bool IsDirty => _isDirty;

    // ── Slot management ────────────────────────────────────────────────────

    /// <summary>
    /// Switch to a different save slot.<br/>
    /// By default the current slot is saved first if there are pending changes.
    /// </summary>
    /// <param name="slot">0 … <see cref="GamePrefsSettings.MaxSaveSlots"/> − 1</param>
    /// <param name="saveCurrentFirst">Set false to discard unsaved changes.</param>
    public static void SetActiveSlot(int slot, bool saveCurrentFirst = true)
    {
        ValidateSlot(slot);
        if (saveCurrentFirst && _isDirty) Save();
        int prev   = _activeSlot;
        _activeSlot = slot;
        _data       = null; // force reload on next access
        OnSlotChanged?.Invoke(prev, slot);
    }

    /// <summary>Returns true if a save file exists for the given slot.</summary>
    public static bool SlotExists(int slot)
    {
        ValidateSlot(slot);
        return File.Exists(GetFilePath(slot));
    }

    /// <summary>
    /// Copy all data from one slot to another (overwrites the destination).
    /// </summary>
    public static void CopySlot(int fromSlot, int toSlot)
    {
        ValidateSlot(fromSlot);
        ValidateSlot(toSlot);

        var src  = GetFilePath(fromSlot);
        var dest = GetFilePath(toSlot);

        if (!File.Exists(src))
        {
            Debug.LogWarning($"[GamePrefs] CopySlot: source slot {fromSlot} has no save file.");
            return;
        }

        EnsureDirectory(dest);
        if (GamePrefsSettings.CreateBackup) WriteBackup(dest);
        File.Copy(src, dest, overwrite: true);
        if(GamePrefsSettings.DEBUG)Debug.Log($"[GamePrefs] Copied slot {fromSlot} → slot {toSlot}");
    }

    // ── int ────────────────────────────────────────────────────────────────

    public static void SetInt(string key, int value) =>
        SetRaw(key, value.ToString());

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (!TryGetRaw(key, out var raw)) return defaultValue;
        return int.TryParse(raw, out var v) ? v : defaultValue;
    }

    // ── float ──────────────────────────────────────────────────────────────

    public static void SetFloat(string key, float value) =>
        SetRaw(key, value.ToString("R")); // "R" = full round-trip precision

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        if (!TryGetRaw(key, out var raw)) return defaultValue;
        return float.TryParse(raw,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v : defaultValue;
    }

    // ── string ─────────────────────────────────────────────────────────────

    public static void SetString(string key, string value) =>
        SetRaw(key, value ?? string.Empty);

    public static string GetString(string key, string defaultValue = "")
    {
        if (!TryGetRaw(key, out var raw)) return defaultValue;
        return raw;
    }

    // ── bool ───────────────────────────────────────────────────────────────

    public static void SetBool(string key, bool value) =>
        SetRaw(key, value ? "1" : "0");

    public static bool GetBool(string key, bool defaultValue = false)
    {
        if (!TryGetRaw(key, out var raw)) return defaultValue;
        return raw == "1";
    }

    // ── Vector2 ────────────────────────────────────────────────────────────

    public static void SetVector2(string key, Vector2 value) =>
        SetObject(key, new SVector2(value));

    public static Vector2 GetVector2(string key, Vector2 defaultValue = default)
        => HasKey(key) ? GetObject<SVector2>(key).ToVector2() : defaultValue;

    // ── Vector3 ────────────────────────────────────────────────────────────

    public static void SetVector3(string key, Vector3 value) =>
        SetObject(key, new SVector3(value));

    public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
        => HasKey(key) ? GetObject<SVector3>(key).ToVector3() : defaultValue;

    // ── Generic object ─────────────────────────────────────────────────────

    /// <summary>
    /// Store any <c>[Serializable]</c> class or struct using JsonUtility.
    /// </summary>
    public static void SetObject<T>(string key, T value) =>
        SetRaw(key, JsonUtility.ToJson(value));

    /// <summary>
    /// Retrieve and deserialise a stored object.
    /// Returns <paramref name="defaultValue"/> when the key is missing or corrupt.
    /// </summary>
    public static T GetObject<T>(string key, T defaultValue = default)
    {
        if (!TryGetRaw(key, out var raw)) return defaultValue;
        try   { return JsonUtility.FromJson<T>(raw); }
        catch { return defaultValue; }
    }

    // ── Key management ─────────────────────────────────────────────────────

    public static bool HasKey(string key) => Data.ContainsKey(key);

    public static void DeleteKey(string key)
    {
        if (Data.Remove(key)) _isDirty = true;
    }

    /// <summary>Erase all keys from the active slot in memory.
    /// Call <see cref="Save"/> to persist the deletion.</summary>
    public static void DeleteAll()
    {
        Data.Clear();
        _isDirty = true;
    }

    // ── Persistence ────────────────────────────────────────────────────────

    /// <summary>Write the active slot to disk immediately.</summary>
    public static void Save()
    {
        try
        {
            var path = GetFilePath(_activeSlot);
            EnsureDirectory(path);

            if (GamePrefsSettings.CreateBackup) WriteBackup(path);

            var wrapper = SaveDataWrapper.FromDictionary(Data);
            var json    = JsonUtility.ToJson(wrapper, prettyPrint: true);

            if (GamePrefsSettings.UseEncryption) json = Encrypt(json);

            File.WriteAllText(path, json, Encoding.UTF8);
            _isDirty = false;

            if(GamePrefsSettings.DEBUG)Debug.Log($"[GamePrefs] Saved slot {_activeSlot} → {path}");
            OnSaved?.Invoke(_activeSlot);
        }
        catch (Exception e)
        {
            if(GamePrefsSettings.DEBUG)Debug.LogError($"[GamePrefs] Save failed: {e.Message}");
        }
    }

    /// <summary>Load the active slot from disk. Starts fresh if no file exists.</summary>
    public static void Load()
    {
        _data = new Dictionary<string, string>();
        var path = GetFilePath(_activeSlot);

        if (!File.Exists(path))
        {
            if(GamePrefsSettings.DEBUG)Debug.Log($"[GamePrefs] No save at '{path}'. Starting fresh.");
            return;
        }

        try
        {
            var raw = File.ReadAllText(path, Encoding.UTF8);
            if (GamePrefsSettings.UseEncryption) raw = Decrypt(raw);

            var wrapper = JsonUtility.FromJson<SaveDataWrapper>(raw);
            _data = wrapper?.ToDictionary() ?? new Dictionary<string, string>();

            // Warn if the save was written by a different version of the system.
            if (wrapper != null && wrapper.version != GamePrefsSettings.SaveVersion)
                if(GamePrefsSettings.DEBUG)Debug.LogWarning($"[GamePrefs] Save version mismatch: " +
                                                            $"file={wrapper.version}, current={GamePrefsSettings.SaveVersion}. " +
                                                            "Consider adding a migration step.");

            if(GamePrefsSettings.DEBUG)Debug.Log($"[GamePrefs] Loaded slot {_activeSlot} ← {path}");
            OnLoaded?.Invoke(_activeSlot);
        }
        catch (Exception e)
        {
            if(GamePrefsSettings.DEBUG)Debug.LogError($"[GamePrefs] Load failed: {e.Message}. Starting fresh.");
            _data = new Dictionary<string, string>();
        }
    }

    // ── Path helpers (internal: used by Editor script) ─────────────────────

    public static string GetFilePath(int slot)
    {
        var name = $"{GamePrefsSettings.BaseFileName}_slot{slot}{GamePrefsSettings.FileExtension}";
        return Path.Combine(GetSaveDirectory(), name);
    }

    public static string GetSaveDirectory()
    {
#if UNITY_EDITOR
        var root = GamePrefsSettings.UseProjectFolderInEditor
            ? Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
            : Application.persistentDataPath;
#else
        var root = Application.persistentDataPath;
#endif
        return Path.Combine(root, GamePrefsSettings.SaveFolder);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static void SetRaw(string key, string value)
    {
        Data[key] = value;
        _isDirty  = true;
    }

    private static bool TryGetRaw(string key, out string value) =>
        Data.TryGetValue(key, out value);

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static void WriteBackup(string filePath)
    {
        if (!File.Exists(filePath)) return;
        var backupPath = filePath + GamePrefsSettings.BackupExtension;
        try   { File.Copy(filePath, backupPath, overwrite: true); }
        catch (Exception e) { Debug.LogWarning($"[GamePrefs] Backup failed: {e.Message}"); }
    }

    private static void ValidateSlot(int slot)
    {
        if (slot < 0 || slot >= GamePrefsSettings.MaxSaveSlots)
            throw new ArgumentOutOfRangeException(nameof(slot),
                $"Slot must be 0–{GamePrefsSettings.MaxSaveSlots - 1}.");
    }

    // ── AES-128 (random IV per save, prepended to ciphertext) ──────────────

    private static byte[] KeyBytes =>
        PadOrTruncate(Encoding.UTF8.GetBytes(GamePrefsSettings.EncryptionKey), 16);

    private static byte[] PadOrTruncate(byte[] src, int length)
    {
        var result = new byte[length];
        Buffer.BlockCopy(src, 0, result, 0, Math.Min(src.Length, length));
        return result;
    }

    private static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = KeyBytes;
        aes.GenerateIV(); // unique IV every save — much more secure than a fixed IV

        var plain  = Encoding.UTF8.GetBytes(plainText);
        var cipher = aes.CreateEncryptor().TransformFinalBlock(plain, 0, plain.Length);

        // Layout: [16-byte IV][ciphertext] → base64
        var combined = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, combined, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(combined);
    }

    private static string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = KeyBytes;

        var combined = Convert.FromBase64String(cipherText);

        // Extract the leading 16-byte IV
        var iv = new byte[16];
        Buffer.BlockCopy(combined, 0, iv, 0, 16);
        aes.IV = iv;

        var plain = aes.CreateDecryptor()
                       .TransformFinalBlock(combined, 16, combined.Length - 16);
        return Encoding.UTF8.GetString(plain);
    }

    // ── Serialisation helpers ──────────────────────────────────────────────

    [Serializable]
    private class SaveDataWrapper
    {
        public int    version  = GamePrefsSettings.SaveVersion;
        public string savedAt  = "";
        public List<string> keys   = new();
        public List<string> values = new();

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>(keys.Count);
            for (int i = 0; i < keys.Count && i < values.Count; i++)
                dict[keys[i]] = values[i];
            return dict;
        }

        public static SaveDataWrapper FromDictionary(Dictionary<string, string> dict)
        {
            var w = new SaveDataWrapper
            {
                version = GamePrefsSettings.SaveVersion,
                savedAt = DateTime.UtcNow.ToString("u")
            };
            foreach (var kvp in dict)
            {
                w.keys.Add(kvp.Key);
                w.values.Add(kvp.Value);
            }
            return w;
        }
    }

    [Serializable]
    private class SVector2
    {
        public float x, y;
        public SVector2(Vector2 v) { x = v.x; y = v.y; }
        public Vector2 ToVector2() => new(x, y);
    }

    [Serializable]
    private class SVector3
    {
        public float x, y, z;
        public SVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3 ToVector3() => new(x, y, z);
    }
}
#pragma warning restore CS0162