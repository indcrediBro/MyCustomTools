# IncredibleAttributes

> A zero-inheritance, Unity 6+ inspector attribute library — the spiritual successor to NaughtyAttributes with a cleaner API, better performance, and more features.

[![Unity 6+](https://img.shields.io/badge/unity-6%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)
[![Namespace](https://img.shields.io/badge/namespace-IncredibleAttributes-blueviolet.svg)]()

---

## ✨ Why IncredibleAttributes?

| Feature | NaughtyAttributes | IncredibleAttributes |
|---|---|---|
| No inheritance needed | ❌ (needs `NaughtyInspector` for some) | ✅ Works on every MonoBehaviour / ScriptableObject |
| Unity version | 2019.4+ | **6+** |
| Reflection caching | ❌ | ✅ Per-type cache, fast re-draws |
| Public API for custom editors | Limited | ✅ `IncredibleEditorGUI.*` static helpers |
| New attributes | — | `[Title]`, `[GUIColor]`, `[Suffix]`, `[Prefix]`, `[Indent]`, `[FilePath]`, `[FolderPath]`, `[InlineEditor]`, `[ButtonGroup]`, dynamic `[ProgressBar]` |
| `[ProgressBar]` with dynamic max | ❌ | ✅ |
| `[ButtonGroup]` (horizontal row) | ❌ | ✅ |
| `[OnValueChanged]` with typed arg | ❌ | ✅ |

---

## 📦 Installation

### Via Package Manager (Git URL)
In Unity: **Window → Package Manager → + → Add package from git URL**
```
https://github.com/YOUR_NAME/IncredibleAttributes.git
```

### Manual
Copy the `com.incredibleattributes` folder into your project's `Packages/` directory.

---

## 🚀 Quick Start

```csharp
using IncredibleAttributes;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Title("Stats")]
    [ProgressBar("Health", 100f, EColor.Red)]
    public float health = 75f;

    [MinValue(0f), Suffix("m/s")]
    public float speed = 5f;

    [ShowIf("hasShield")]
    public float shieldStrength = 30f;

    public bool hasShield;

    [Button("Heal to Full", EButtonEnableMode.Playmode)]
    private void HealToFull() => health = 100f;
}
```

No inheritance. No setup. Just works. ✅

---

## 📋 Attribute Reference

### 🎨 Drawer Attributes
Draw the field differently.

| Attribute | Description |
|---|---|
| `[AnimatorParam("animator")]` | Dropdown of Animator parameters |
| `[CurveRange(x, y, w, h, EColor)]` | Constrained AnimationCurve with colour |
| `[Dropdown("valuesName")]` | Dropdown from field / property / method |
| `[EnumFlags]` | Multi-select enum flags dropdown |
| `[Expandable]` | Inline ScriptableObject editor (foldout) |
| `[InlineEditor]` | Inline ScriptableObject editor (always open) |
| `[FilePath(extensions, relative)]` | String with file browser button |
| `[FolderPath(relative)]` | String with folder browser button |
| `[GUIColor(EColor)]` | Tint the entire field row |
| `[HorizontalLine(EColor, height)]` | Coloured separator line |
| `[InfoBox("text", EInfoBoxType)]` | HelpBox above field |
| `[InputAxis]` | Dropdown of Input Manager axes |
| `[Indent(levels)]` | Indent the field N levels |
| `[Layer]` | Layer dropdown (int or string) |
| `[MinMaxSlider(min, max)]` | Dual-handle range slider → Vector2 |
| `[Prefix("$")]` | Static label before the widget |
| `[ProgressBar("label", max, EColor)]` | Filled progress bar |
| `[ProgressBar("label", "maxFieldName", EColor)]` | Dynamic max |
| `[ReorderableList]` | Polished drag-to-reorder list |
| `[ResizableTextArea]` | Auto-growing text area |
| `[Scene]` | Build-settings scene picker |
| `[ShowAssetPreview(w, h)]` | Thumbnail below object field |
| `[SortingLayer]` | Sorting layer dropdown |
| `[Suffix("m/s")]` | Static label after the widget |
| `[Tag]` | Unity tag dropdown |
| `[Title("text", "subtitle")]` | Bold section header |

### 🔧 Meta Attributes
Modify how/when a field is displayed. Stack multiple.

| Attribute | Description |
|---|---|
| `[BoxGroup("name")]` | Group fields in a labelled box |
| `[Foldout("name")]` | Collapsible foldout group |
| `[ShowIf("condition")]` | Show field when condition is true |
| `[HideIf("condition")]` | Hide field when condition is true |
| `[EnableIf("condition")]` | Enable field when condition is true |
| `[DisableIf("condition")]` | Disable field when condition is true |
| `[Label("Custom Name")]` | Override the display name |
| `[OnValueChanged("Method")]` | Callback on inspector change |
| `[ReadOnly]` | Non-editable display |

Conditions can be a **bool field**, **bool property**, or **zero-param bool method**.  
Chain with `EConditionOperator.And` / `EConditionOperator.Or`:
```csharp
[ShowIf(EConditionOperator.And, "isAlive", "hasWeapon")]
public float attackDamage;
```

### ✅ Validator Attributes
Show errors in the inspector.

| Attribute | Description |
|---|---|
| `[MinValue(0)]` | Clamps to minimum |
| `[MaxValue(100)]` | Clamps to maximum |
| `[Required]` | Error if null/empty |
| `[Required("Custom message")]` | Error with custom text |
| `[ValidateInput("Method", "msg")]` | Custom bool validator |

### 🔘 Method Attributes

| Attribute | Description |
|---|---|
| `[Button]` | Click-to-invoke button |
| `[Button("Label", EButtonEnableMode)]` | Button with custom label and enable mode |
| `[ButtonGroup("group")]` | Horizontal row of buttons |
| `[GUIColor(EColor)]` | Tint a button's background |
| `[ShowNativeProperty]` | Display a C# property read-only |
| `[ShowNonSerializedField]` | Display a non-serialized field read-only |

`EButtonEnableMode`: `Always`, `Editor` (not in Play Mode), `Playmode` (only in Play Mode).

### 🔲 Special Attributes

| Attribute | Description |
|---|---|
| `[AllowNesting]` | Enable meta attributes inside nested structs/classes |

---

## 🛠️ Custom Editor Integration

You don't need to inherit anything. But if you **do** write your own `CustomEditor`, use the static API:

```csharp
[CustomEditor(typeof(MyThing))]
public class MyThingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Option A – draw everything automatically:
        IncredibleEditorGUI.DrawAllProperties(serializedObject, target);

        // Option B – draw selectively:
        var sp = serializedObject.FindProperty("speed");
        var fi = target.GetType().GetField("speed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        IncredibleEditorGUI.PropertyField(sp, fi, target);

        serializedObject.ApplyModifiedProperties();
    }
}
```

Helper methods available directly:
```csharp
IncredibleEditorGUI.HorizontalLine(Color.gray);
IncredibleEditorGUI.Title("Section Name", "subtitle");
IncredibleEditorGUI.InfoBox("Something went wrong", EInfoBoxType.Error);
```

---

## 🎨 EColor Values

`Clear`, `White`, `Black`, `Gray`, `Red`, `Pink`, `Orange`, `Yellow`, `Green`, `Teal`, `Blue`, `Indigo`, `Violet`, `Purple`

Convert to `UnityEngine.Color` anytime: `EColor.Green.ToColor()`

---

## 📁 Package Structure

```
com.incredibleattributes/
├── package.json
├── Runtime/
│   ├── IncredibleAttributes.Runtime.asmdef
│   ├── Core/
│   │   ├── EEnums.cs                  ← EColor, EConditionOperator, EInfoBoxType, EButtonEnableMode
│   │   └── DropdownList.cs            ← DropdownList<T>
│   └── Attributes/
│       ├── Drawer/                    ← All [DrawerAttribute] definitions
│       ├── Meta/                      ← BoxGroup, Foldout, ShowIf, EnableIf, Label, ReadOnly …
│       ├── Validator/                 ← MinValue, MaxValue, Required, ValidateInput, AllowNesting
│       └── Special/
└── Editor/
    ├── IncredibleAttributes.Editor.asmdef
    ├── Core/
    │   ├── ReflectionUtility.cs       ← Cached reflection helpers
    │   ├── PropertyUtility.cs         ← Condition evaluation, validation
    │   ├── IncredibleEditor.cs        ← Universal auto-editor + static draw methods
    │   └── IncredibleEditorGUI.cs     ← Public API for custom editors
    └── Drawers/
        ├── Property/
        │   ├── DropdownTypeDrawers.cs  ← Tag, Layer, SortingLayer, Scene, InputAxis
        │   ├── ValueDrawers.cs         ← AnimatorParam, EnumFlags, Dropdown, MinMaxSlider
        │   └── VisualDrawers.cs        ← ProgressBar, TextArea, AssetPreview, Expandable, CurveRange …
        └── Decorator/
            └── DecoratorDrawers.cs     ← HorizontalLine, InfoBox, Title
```

---

## 📄 License

MIT — do whatever you want, just keep the copyright notice.
