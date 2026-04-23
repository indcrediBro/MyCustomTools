/**
 * IncredibleAttributes — Demo Component
 *
 * Attach this to any GameObject to see every attribute in action.
 * This file lives outside the package; put it anywhere in your Assets/ folder.
 */

using System.Collections.Generic;
using IncredibleAttributes;
using UnityEngine;

public class IncredibleAttributesDemo : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  TITLES & SECTION HEADERS
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Character Stats", "Core values for the player character")]
    [ProgressBar("Health", "maxHealth", EColor.Red)]
    public float health = 75f;

    [HideInInspector] public float maxHealth = 100f;

    [ProgressBar("Mana", 100f, EColor.Blue)]
    public float mana = 40f;

    [ProgressBar("Stamina", 200f, EColor.Green)]
    public float stamina = 180f;

    // ─────────────────────────────────────────────────────────────────────────
    //  SUFFIX / PREFIX / GUI COLOR
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Movement")]
    [GUIColor(EColor.Yellow)]
    [Suffix("m/s")]
    [MinValue(0f)]
    public float speed = 5f;

    [Prefix("x")]
    [MinValue(0f), MaxValue(10f)]
    public float jumpMultiplier = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  MIN MAX SLIDER
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Spawn Settings")]
    [MinMaxSlider(0f, 30f)]
    public Vector2 spawnDelay = new Vector2(1f, 5f);

    // ─────────────────────────────────────────────────────────────────────────
    //  DROPDOWN
    // ─────────────────────────────────────────────────────────────────────────

    [Dropdown("_difficultyOptions")]
    public int difficulty = 1;

    private DropdownList<int> _difficultyOptions = new()
    {
        { "Easy",     0 },
        { "Normal",   1 },
        { "Hard",     2 },
        { "Nightmare",3 },
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  SHOW IF / HIDE IF
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Shield System")]
    public bool hasShield;

    [ShowIf("hasShield")]
    [GUIColor(EColor.Teal)]
    [Suffix("HP")]
    public float shieldStrength = 30f;

    [ShowIf("hasShield")]
    [MinMaxSlider(0f, 10f)]
    public Vector2 shieldRechargeWindow = new Vector2(2f, 4f);

    [HideIf("hasShield")]
    [InfoBox("Enable the shield to see shield settings.", EInfoBoxType.Normal)]
    public bool _noShieldHint; // dummy field for the InfoBox placement

    // ─────────────────────────────────────────────────────────────────────────
    //  ENABLE IF / DISABLE IF
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Damage Multiplier")]
    public bool useDamageMultiplier;

    [EnableIf("useDamageMultiplier")]
    [MinValue(0.1f), MaxValue(10f)]
    public float damageMultiplier = 1f;

    [DisableIf("useDamageMultiplier")]
    [InfoBox("Enable damage multiplier above to unlock damage cap.", EInfoBoxType.Warning)]
    public float damageCap = 999f;

    // ─────────────────────────────────────────────────────────────────────────
    //  BOX GROUP
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Audio")]
    [BoxGroup("Volumes")]
    [Suffix("%")]
    [MinValue(0f), MaxValue(100f)]
    public float musicVolume = 70f;

    [BoxGroup("Volumes")]
    [Suffix("%")]
    [MinValue(0f), MaxValue(100f)]
    public float sfxVolume = 100f;

    // ─────────────────────────────────────────────────────────────────────────
    //  FOLDOUT
    // ─────────────────────────────────────────────────────────────────────────

    [Foldout("Debug Info")]
    [ReadOnly] public int frameCount;

    [Foldout("Debug Info")]
    [ReadOnly] public float sessionTime;

    [Foldout("Debug Info")]
    public bool showDebugGizmos;

    // ─────────────────────────────────────────────────────────────────────────
    //  UNITY PICKERS
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Unity Pickers")]
    [Tag]    public string enemyTag    = "Enemy";
    [Layer]  public int groundLayer    = 0;
    [Scene]  public string mainMenu    = "MainMenu";
    [SortingLayer] public string uiLayer = "UI";

    // ─────────────────────────────────────────────────────────────────────────
    //  REORDERABLE LIST
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Inventory")]
    [ReorderableList]
    public List<string> inventory = new() { "Sword", "Shield", "Potion" };

    // ─────────────────────────────────────────────────────────────────────────
    //  RESIZABLE TEXT AREA
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Notes")]
    [ResizableTextArea]
    public string designNotes = "Put your designer notes here.\nThis text area grows as you type.";

    // ─────────────────────────────────────────────────────────────────────────
    //  CURVE RANGE
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Animation Curves")]
    [CurveRange(0f, 0f, 1f, 1f, EColor.Green)]
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [CurveRange(EColor.Orange)]
    public AnimationCurve damageFalloff = AnimationCurve.Linear(0, 1, 1, 0);

    // ─────────────────────────────────────────────────────────────────────────
    //  FILE / FOLDER PATHS
    // ─────────────────────────────────────────────────────────────────────────

    [Title("File Paths")]
    [FilePath(extensions: "json,csv")]
    public string dataFilePath = "";

    [FolderPath]
    public string outputFolder = "";

    // ─────────────────────────────────────────────────────────────────────────
    //  ENUM FLAGS
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Flags")]
    [EnumFlags]
    public DemoFlags activeFlags = DemoFlags.CanJump | DemoFlags.CanRun;

    // ─────────────────────────────────────────────────────────────────────────
    //  REQUIRED / VALIDATE INPUT
    // ─────────────────────────────────────────────────────────────────────────

    [Title("References")]
    [Required("You must assign the player camera!")]
    public Camera playerCamera;

    [ValidateInput("IsPositive", "Speed must be greater than zero.")]
    public float validateSpeed = 1f;
    private bool IsPositive(float v) => v > 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  LABEL / ON VALUE CHANGED / HORIZONTAL LINE
    // ─────────────────────────────────────────────────────────────────────────

    [HorizontalLine(EColor.Purple, 2f)]
    [Label("Max Enemy Count")]
    [OnValueChanged("OnEnemyCountChanged")]
    public int veryLongEnemyCountVariableName = 10;

    private void OnEnemyCountChanged()
        => Debug.Log($"[Demo] Enemy count changed → {veryLongEnemyCountVariableName}");

    // ─────────────────────────────────────────────────────────────────────────
    //  NON-SERIALIZED FIELD / NATIVE PROPERTY
    // ─────────────────────────────────────────────────────────────────────────

    [ShowNonSerializedField]
    private int _internalFrameCounter = 0;

    [ShowNonSerializedField]
    private const float Gravity = -9.81f;

    [ShowNativeProperty]
    public bool IsAlive => health > 0f;

    [ShowNativeProperty]
    public int InventorySize => inventory?.Count ?? 0;

    // ─────────────────────────────────────────────────────────────────────────
    //  BUTTONS
    // ─────────────────────────────────────────────────────────────────────────

    [GUIColor(EColor.Green)]
    [Button("Heal to Full", EButtonEnableMode.Playmode)]
    private void HealToFull()
    {
        health = maxHealth;
        Debug.Log("[Demo] Healed to full!");
    }

    [GUIColor(EColor.Red)]
    [Button("Deal 10 Damage", EButtonEnableMode.Playmode)]
    private void TakeDamage() => health = Mathf.Max(0f, health - 10f);

    [ButtonGroup("Scene Tools")]
    private void LogTransform()
        => Debug.Log($"[Demo] Position: {transform.position}  Rotation: {transform.eulerAngles}");

    [ButtonGroup("Scene Tools")]
    private void ResetTransform()
    {
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
    }

    [Button("Log All Values", EButtonEnableMode.Always)]
    private void LogAll()
    {
        Debug.Log($"[Demo] Health={health}/{maxHealth}  Speed={speed}  Difficulty={difficulty}  " +
                  $"Shield={hasShield}  Inventory=[{string.Join(", ", inventory)}]");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity update — just to show non-serialized field updating in Play Mode
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        _internalFrameCounter++;
        frameCount  = _internalFrameCounter;
        sessionTime = Time.timeSinceLevelLoad;
    }
}

[System.Flags]
public enum DemoFlags
{
    None      = 0,
    CanJump   = 1 << 0,
    CanRun    = 1 << 1,
    CanDodge  = 1 << 2,
    CanGlide  = 1 << 3,
}
