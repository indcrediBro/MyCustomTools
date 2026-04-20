// ============================================================
//  SettingsToggleBinder.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Add to any GameObject that also has a Unity Toggle component.
//
//  Inspector setup:
//    1. Pick a BoolSetting from the Setting dropdown
//    2. Tick Invert if the toggle's ON state should store false
//       (e.g. a "Sound Enabled" toggle binding to Audio_MuteAll)
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace Core.Settings
{
    /// <summary>
    /// Binds a <see cref="Toggle"/> to a bool setting in <see cref="SettingsManager"/>
    /// using the typed <see cref="BoolSetting"/> enum.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class SettingsToggleBinder : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Binding")] [Tooltip("Which bool setting to bind to this toggle.")]
        public BoolSetting Setting = BoolSetting.Audio_MuteAll;

        [Tooltip("When true, the stored value is inverted relative to the toggle state.\n\n" +
                 "Example: a 'Sound Enabled' toggle should bind to Audio_MuteAll with Invert = true,\n" +
                 "so that toggle ON  → MuteAll = false\n" +
                 "and      toggle OFF → MuteAll = true.")]
        public bool Invert = false;

        // ── Private ────────────────────────────────────────────────────────────

        private Toggle _toggle;
        private bool _isSyncing;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake() => _toggle = GetComponent<Toggle>();

        private void OnEnable()
        {
            PullFromManager();
            _toggle.onValueChanged.AddListener(OnToggleChanged);
            SettingsManager.OnReverted += PullFromManager;
            SettingsManager.OnPendingChanged += OnExternalChanged;
        }

        private void OnDisable()
        {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            SettingsManager.OnReverted -= PullFromManager;
            SettingsManager.OnPendingChanged -= OnExternalChanged;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnToggleChanged(bool uiValue)
        {
            if (_isSyncing) return;
            SettingsManager.SetBool(Setting, Invert ? !uiValue : uiValue);
        }

        private void OnExternalChanged(object changed)
        {
            if (changed is BoolSetting b && b == Setting)
                PullFromManager();
        }

        // ── Sync ──────────────────────────────────────────────────────────────

        private void PullFromManager()
        {
            _isSyncing = true;
            bool stored = SettingsManager.GetBool(Setting);
            _toggle.SetIsOnWithoutNotify(Invert ? !stored : stored);
            _isSyncing = false;
        }
    }
}