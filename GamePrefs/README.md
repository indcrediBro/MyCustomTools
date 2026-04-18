# GamePrefs

A drop-in replacement for Unity's `PlayerPrefs` that saves typed data as JSON files instead of the Windows registry / binary pref store. Designed to be reusable across projects with a single config file.

---

## File placement

```
Assets/
  Scripts/
    GamePrefs/
      GamePrefsSettings.cs    ← configure everything here
      GamePrefs.cs            ← the API you call in code
      GamePrefsAutoSave.cs    ← no scene setup needed
  Editor/
    GamePrefsEditor.cs        ← must be inside an Editor/ folder
```

> **Note on naming** — the static class is called `GamePrefs`, so calls look like `GamePrefs.SetInt(…)`, identical in style to `PlayerPrefs.SetInt(…)`. There is no double-naming; there is no namespace wrapper.

---

## Quick start

```csharp
// Works exactly like PlayerPrefs — just swap the prefix.
GamePrefs.SetInt("score",   100);
GamePrefs.SetFloat("vol",   0.8f);
GamePrefs.SetString("name", "Hero");
GamePrefs.SetBool("done",   true);

int   score = GamePrefs.GetInt("score",   0);
float vol   = GamePrefs.GetFloat("vol",   1f);
string name = GamePrefs.GetString("name", "Unknown");
bool  done  = GamePrefs.GetBool("done",   false);
```

Data is kept in memory until you call `Save()` — just like PlayerPrefs. Enable `UseAutoSave` in settings to save automatically on quit.

---

## Extras over PlayerPrefs

### Vector types
```csharp
GamePrefs.SetVector2("spawnPos", transform.position);
Vector2 pos = GamePrefs.GetVector2("spawnPos", Vector2.zero);

GamePrefs.SetVector3("lastPos", transform.position);
Vector3 pos = GamePrefs.GetVector3("lastPos", Vector3.zero);
```

### Custom [Serializable] types
```csharp
[Serializable]
public class PlayerData { public string name; public int level; }

GamePrefs.SetObject("player", new PlayerData { name = "Hero", level = 5 });
PlayerData data = GamePrefs.GetObject<PlayerData>("player");
```

### Save slots
```csharp
GamePrefs.SetActiveSlot(1);             // switch (auto-saves current slot first)
GamePrefs.SetActiveSlot(2, saveCurrentFirst: false); // switch, discard changes
bool exists = GamePrefs.SlotExists(1);  // check before loading
GamePrefs.CopySlot(0, 1);              // duplicate slot 0 into slot 1
int current = GamePrefs.ActiveSlot;    // → 2
```

### Manual persistence
```csharp
GamePrefs.Save();       // write active slot to disk now
GamePrefs.Load();       // reload active slot from disk
bool dirty = GamePrefs.IsDirty;  // true when there are unsaved changes
```

### Events
```csharp
GamePrefs.OnSaved       += slot => Debug.Log($"Saved slot {slot}");
GamePrefs.OnLoaded      += slot => Debug.Log($"Loaded slot {slot}");
GamePrefs.OnSlotChanged += (prev, next) => RefreshSlotUI(next);
```

### Key management
```csharp
GamePrefs.HasKey("score");
GamePrefs.DeleteKey("score");
GamePrefs.DeleteAll();    // clears memory; call Save() to persist deletion
```

---

## Configuration (`GamePrefsSettings.cs`)

| Constant | Default | Effect |
|---|---|---|
| `SaveFolder` | `"SaveData"` | Folder name inside the base path |
| `BaseFileName` | `"gameprefs"` | Save file prefix |
| `UseProjectFolderInEditor` | `true` | Save to project root in editor |
| `UseEncryption` | `false` | AES-128 with random IV per save |
| `EncryptionKey` | `"ChangeMe!Key1234"` | 16-char key — **change before shipping** |
| `CreateBackup` | `true` | Copy `.bak` before every write |
| `UseAutoSave` | `false` | Save on quit + mobile background |
| `MaxSaveSlots` | `3` | Number of save slots |
| `SaveVersion` | `1` | Written into every save for migration checks |

---

## Save file location

| Context | Path |
|---|---|
| Editor (`UseProjectFolderInEditor = true`) | `[ProjectRoot]/SaveData/gameprefs_slot0.json` |
| Editor (`UseProjectFolderInEditor = false`) | `Application.persistentDataPath/SaveData/…` |
| Standalone build | `Application.persistentDataPath/SaveData/…` |

---

## Editor tools (`Tools → GamePrefs`)

| Menu item | What it does |
|---|---|
| Open Save Folder | Reveals the save directory in Explorer/Finder |
| Print Active Slot to Console | Dumps the raw JSON of the active slot |
| Print All Slots Info | Lists all slots with timestamps and backup status |
| Save Now / Load Now | Trigger save or load from the menu |
| Clear Active Slot | Deletes the active slot file + clears memory |
| Clear ALL Slots | Deletes every slot file |
| Restore from Backup | Replaces the active slot with its `.bak` file |
| Copy Slot 0 → Slot 1 | Copies slot data between slots |
| Switch to Slot N | Changes the active slot |

---

## Encryption notes

- AES-128, enabled by setting `UseEncryption = true` in `GamePrefsSettings`.
- A **random IV is generated on every save** and stored alongside the ciphertext — identical data produces different output each time, preventing pattern analysis.
- Change `EncryptionKey` to a unique 16-character string per project before shipping. The default key is intentionally generic and should not be used in production.
- Saves made with encryption **cannot** be loaded without it (and vice versa). Decide before you start accumulating save data.

---

## Backup system

When `CreateBackup = true` (default), every `Save()` call first copies the existing file to `gameprefs_slot0.json.bak`. If a save is interrupted or corrupted you can restore the backup via `Tools → GamePrefs → Restore Active Slot from Backup` or manually in code:

```csharp
// Restore slot 0 from its backup (example — normally done via the Editor menu)
var path = GamePrefs.GetFilePath(0);
File.Copy(path + ".bak", path, overwrite: true);
GamePrefs.Load();
```

---

## Save versioning

`SaveVersion` is written into every file. On load, the system warns when the version in the file doesn't match the current constant:

```
[GamePrefs] Save version mismatch: file=1, current=2. Consider adding a migration step.
```

Increment `SaveVersion` whenever you make breaking changes to your saved data structure, and add a migration code path in your game's boot sequence.

---

## Requirements

- Unity 2021.2 or later (uses C# 9 target-typed `new()`)
- No third-party packages — uses only `UnityEngine.JsonUtility` and `System.Security.Cryptography`
