// ============================================================
//  SettingsDropdownBinder.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Works with both TMP_Dropdown (TextMeshPro) and the legacy
//  UnityEngine.UI.Dropdown — whichever component is present.
//
//  Inspector setup:
//    1. Pick an IntSetting from the Setting dropdown
//    2. Choose one population strategy:
//       a) Leave Options empty to keep existing dropdown content.
//       b) Fill Options manually to override entries.
//       c) Tick AutoPopulateResolutions     (use with Graphics_ResolutionIndex)
//       d) Tick AutoPopulateQualityLevels   (use with Graphics_QualityLevel)
//       e) Tick AutoPopulateAntiAliasing    (use with Graphics_AntiAliasingLevel)
// ============================================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Settings
{
    /// <summary>
    /// Binds a <see cref="TMP_Dropdown"/> (or legacy <see cref="Dropdown"/>) to an integer
    /// setting in <see cref="SettingsManager"/> using the typed <see cref="IntSetting"/> enum.<br/>
    /// Supports auto-population with screen resolutions, Unity quality levels, and MSAA presets.
    /// </summary>
    public class SettingsDropdownBinder : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Binding")] [Tooltip("Which int setting to bind to this dropdown.")]
        public IntSetting Setting = IntSetting.Graphics_QualityLevel;

        [Header("Options — choose one strategy")]
        [Tooltip("Manually specify dropdown labels. Leave empty to keep existing dropdown content.")]
        public List<string> Options = new();

        [Tooltip("Auto-fill with all Screen.resolutions entries (width × height @ hz).\n" +
                 "Best used with Setting = Graphics_ResolutionIndex.")]
        public bool AutoPopulateResolutions = false;

        [Tooltip("Auto-fill with QualitySettings.names.\n" +
                 "Best used with Setting = Graphics_QualityLevel.")]
        public bool AutoPopulateQualityLevels = false;

        [Tooltip("Auto-fill with Off / 2× / 4× / 8× MSAA options.\n" +
                 "Best used with Setting = Graphics_AntiAliasingLevel.")]
        public bool AutoPopulateAntiAliasing = false;

        // ── Private ────────────────────────────────────────────────────────────

        private TMP_Dropdown _tmpDropdown;
        private Dropdown _legacyDropdown;
        private bool _isSyncing;

        // MSAA: dropdown index → Unity sample count
        private static readonly int[] MsaaSampleCounts = { 0, 2, 4, 8 };

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _tmpDropdown = GetComponent<TMP_Dropdown>();
            _legacyDropdown = GetComponent<Dropdown>();

            if (_tmpDropdown == null && _legacyDropdown == null)
            {
                Debug.LogError("[SettingsDropdownBinder] No TMP_Dropdown or Dropdown found on this GameObject.", this);
                return;
            }

            PopulateDropdown();
        }

        private void OnEnable()
        {
            PullFromManager();
            if (_tmpDropdown != null) _tmpDropdown.onValueChanged.AddListener(OnDropdownChanged);
            if (_legacyDropdown != null) _legacyDropdown.onValueChanged.AddListener(OnDropdownChanged);
            SettingsManager.OnReverted += PullFromManager;
            SettingsManager.OnPendingChanged += OnExternalChanged;
        }

        private void OnDisable()
        {
            if (_tmpDropdown != null) _tmpDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            if (_legacyDropdown != null) _legacyDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            SettingsManager.OnReverted -= PullFromManager;
            SettingsManager.OnPendingChanged -= OnExternalChanged;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnDropdownChanged(int index)
        {
            if (_isSyncing) return;

            int value = AutoPopulateAntiAliasing
                ? (index >= 0 && index < MsaaSampleCounts.Length ? MsaaSampleCounts[index] : 0)
                : index;

            SettingsManager.SetInt(Setting, value);
        }

        private void OnExternalChanged(object changed)
        {
            if (changed is IntSetting i && i == Setting)
                PullFromManager();
        }

        // ── Sync ──────────────────────────────────────────────────────────────

        private void PullFromManager()
        {
            _isSyncing = true;

            int storedValue = SettingsManager.GetInt(Setting);
            int dropdownIndex = storedValue;

            if (AutoPopulateAntiAliasing)
            {
                dropdownIndex = System.Array.IndexOf(MsaaSampleCounts, storedValue);
                if (dropdownIndex < 0) dropdownIndex = 0;
            }

            if (_tmpDropdown != null) _tmpDropdown.SetValueWithoutNotify(dropdownIndex);
            if (_legacyDropdown != null) _legacyDropdown.SetValueWithoutNotify(dropdownIndex);

            _isSyncing = false;
        }

        // ── Population ────────────────────────────────────────────────────────

        private void PopulateDropdown()
        {
            List<string> entries = null;

            if (AutoPopulateResolutions) entries = BuildResolutionOptions();
            else if (AutoPopulateQualityLevels) entries = new List<string>(QualitySettings.names);
            else if (AutoPopulateAntiAliasing) entries = new List<string> { "Off", "2×  MSAA", "4×  MSAA", "8×  MSAA" };
            else if (Options is { Count: > 0 }) entries = Options;

            if (entries != null) SetDropdownOptions(entries);
        }

        private static List<string> BuildResolutionOptions()
        {
            var list = new List<string>();
            foreach (var r in Screen.resolutions)
            {
#if UNITY_2022_2_OR_NEWER
                list.Add($"{r.width} × {r.height}  @  {r.refreshRateRatio.value:F0} Hz");
#else
            list.Add($"{r.width} × {r.height}  @  {r.refreshRate} Hz");
#endif
            }

            return list;
        }

        private void SetDropdownOptions(List<string> opts)
        {
            if (_tmpDropdown != null)
            {
                _tmpDropdown.ClearOptions();
                _tmpDropdown.AddOptions(opts);
            }

            if (_legacyDropdown != null)
            {
                _legacyDropdown.ClearOptions();
                _legacyDropdown.AddOptions(opts);
            }
        }
    }
}