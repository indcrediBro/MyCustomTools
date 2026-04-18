// ============================================================
//  GamePrefsEditor.cs
//  MUST be inside an Editor/ folder, e.g.:
//      Assets/Editor/GamePrefsEditor.cs
//
//  Adds  Tools → GamePrefs  to the Unity menu bar.
// ============================================================

#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only utility tools for the GamePrefs save system.<br/>
/// Access via: <b>Tools → GamePrefs</b>
/// </summary>
public static class GamePrefsEditor
{
    private const string Menu = "Tools/GamePrefs/";

    // ── Inspect ────────────────────────────────────────────────────────────

    [MenuItem(Menu + "Open Save Folder")]
    private static void OpenSaveFolder()
    {
        var dir = GamePrefs.GetSaveDirectory();
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        EditorUtility.RevealInFinder(dir);
    }

    [MenuItem(Menu + "Print Active Slot to Console")]
    private static void PrintActiveSlot()
    {
        var slot = GamePrefs.ActiveSlot;
        var path = GamePrefs.GetFilePath(slot);

        if (!File.Exists(path))
        {
            Debug.Log($"[GamePrefs] No save file for slot {slot}.");
            return;
        }

        var raw = File.ReadAllText(path, Encoding.UTF8);
        Debug.Log($"[GamePrefs] Slot {slot}  |  {path}\n\n{raw}");
    }

    [MenuItem(Menu + "Print All Slots Info")]
    private static void PrintAllSlotsInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[GamePrefs] Slot overview  (MaxSaveSlots={GamePrefsSettings.MaxSaveSlots})");
        sb.AppendLine($"  Save folder : {GamePrefs.GetSaveDirectory()}");
        sb.AppendLine($"  Active slot : {GamePrefs.ActiveSlot}");
        sb.AppendLine($"  Dirty       : {GamePrefs.IsDirty}");
        sb.AppendLine();

        for (int i = 0; i < GamePrefsSettings.MaxSaveSlots; i++)
        {
            var path   = GamePrefs.GetFilePath(i);
            var exists = File.Exists(path);
            var bak    = path + GamePrefsSettings.BackupExtension;
            sb.AppendLine($"  Slot {i}: {(exists ? "EXISTS" : "empty")}  " +
                          $"{(exists ? new FileInfo(path).LastWriteTime.ToString("g") : "")}" +
                          $"{(File.Exists(bak) ? "  [backup present]" : "")}");
        }

        Debug.Log(sb.ToString());
    }

    // ── Save / Load ────────────────────────────────────────────────────────

    [MenuItem(Menu + "Save Now")]
    private static void SaveNow() => GamePrefs.Save();

    [MenuItem(Menu + "Load Now")]
    private static void LoadNow() => GamePrefs.Load();

    // ── Clear ──────────────────────────────────────────────────────────────

    [MenuItem(Menu + "Clear Active Slot")]
    private static void ClearActiveSlot()
    {
        var slot = GamePrefs.ActiveSlot;
        if (!Confirm($"Delete all data in slot {slot}?")) return;
        DeleteSlotFile(slot);
        GamePrefs.DeleteAll();
        Debug.Log($"[GamePrefs] Slot {slot} cleared.");
    }

    [MenuItem(Menu + "Clear ALL Slots")]
    private static void ClearAllSlots()
    {
        if (!Confirm($"Delete save files for ALL {GamePrefsSettings.MaxSaveSlots} slot(s)?")) return;
        int deleted = 0;
        for (int i = 0; i < GamePrefsSettings.MaxSaveSlots; i++)
            if (DeleteSlotFile(i)) deleted++;
        GamePrefs.DeleteAll();
        Debug.Log($"[GamePrefs] {deleted} save file(s) deleted.");
    }

    // ── Backup ─────────────────────────────────────────────────────────────

    [MenuItem(Menu + "Restore Active Slot from Backup")]
    private static void RestoreFromBackup()
    {
        var slot = GamePrefs.ActiveSlot;
        var path = GamePrefs.GetFilePath(slot);
        var bak  = path + GamePrefsSettings.BackupExtension;

        if (!File.Exists(bak))
        {
            EditorUtility.DisplayDialog("GamePrefs", $"No backup file found for slot {slot}.", "OK");
            return;
        }

        if (!Confirm($"Restore slot {slot} from backup? Current save will be overwritten.")) return;
        File.Copy(bak, path, overwrite: true);
        GamePrefs.Load();
        Debug.Log($"[GamePrefs] Slot {slot} restored from backup.");
    }

    [MenuItem(Menu + "Restore Active Slot from Backup", validate = true)]
    private static bool ValidateRestoreFromBackup() =>
        File.Exists(GamePrefs.GetFilePath(GamePrefs.ActiveSlot) + GamePrefsSettings.BackupExtension);

    // ── Copy slot ──────────────────────────────────────────────────────────

    [MenuItem(Menu + "Copy Slot 0 → Slot 1")]
    private static void CopySlot0To1() => ConfirmCopySlot(0, 1);

    [MenuItem(Menu + "Copy Slot 0 → Slot 1", validate = true)]
    private static bool ValidateCopySlot0To1() => GamePrefsSettings.MaxSaveSlots > 1;

    // ── Slot quick-switch ──────────────────────────────────────────────────

    [MenuItem(Menu + "Switch to Slot 0")]
    private static void Slot0() => SwitchSlot(0);

    [MenuItem(Menu + "Switch to Slot 1")]
    private static void Slot1() => SwitchSlot(1);

    [MenuItem(Menu + "Switch to Slot 2")]
    private static void Slot2() => SwitchSlot(2);

    [MenuItem(Menu + "Switch to Slot 1", validate = true)]
    private static bool ValidateSlot1() => GamePrefsSettings.MaxSaveSlots > 1;

    [MenuItem(Menu + "Switch to Slot 2", validate = true)]
    private static bool ValidateSlot2() => GamePrefsSettings.MaxSaveSlots > 2;

    // ── Helpers ────────────────────────────────────────────────────────────

    private static void SwitchSlot(int slot)
    {
        GamePrefs.SetActiveSlot(slot);
        Debug.Log($"[GamePrefs] Switched to slot {slot}.");
    }

    private static void ConfirmCopySlot(int from, int to)
    {
        if (!Confirm($"Copy slot {from} to slot {to}? Slot {to} will be overwritten.")) return;
        GamePrefs.CopySlot(from, to);
    }

    private static bool DeleteSlotFile(int slot)
    {
        var path = GamePrefs.GetFilePath(slot);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        var bak = path + GamePrefsSettings.BackupExtension;
        if (File.Exists(bak)) File.Delete(bak);
        return true;
    }

    private static bool Confirm(string message) =>
        EditorUtility.DisplayDialog("GamePrefs", message + "\nThis cannot be undone.", "Confirm", "Cancel");
}
#endif