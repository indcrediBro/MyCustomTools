// ============================================================
//  SettingsUIController.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Attach to the root of your Settings UI hierarchy.
//
//  Inspector wiring checklist:
//    ✅ settingsPanel      — root Canvas/Panel to show/hide
//    ✅ audioPanel         — sub-panel for Audio tab
//    ✅ graphicsPanel      — sub-panel for Graphics tab
//    ✅ gameplayPanel      — sub-panel for Gameplay tab
//    ✅ accessibilityPanel — sub-panel for Accessibility tab
//    ✅ applyButton        — "Apply" button
//    ✅ cancelButton       — "Cancel" / "Close" button
//    ✅ resetButton        — "Reset to Defaults" button
//    ✅ (tab buttons)      — one per tab
//    ○  pendingIndicator   — optional "● Unsaved changes" object
//    ○  toggleKey          — key to open/close (default: Escape)
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Settings
{
    /// <summary>
    /// Manages the settings UI panel: opening/closing, tab switching,
    /// and wiring Apply / Cancel / Reset buttons to <see cref="SettingsManager"/>.
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Root Panel")] [SerializeField]
        private GameObject settingsPanel;

        [Header("Tab Panels (assign all that you use)")] [SerializeField]
        private GameObject audioPanel;

        [SerializeField] private GameObject graphicsPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject accessibilityPanel;

        [Header("Action Buttons")] [SerializeField]
        private Button applyButton;

        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetButton;

        [Header("Tab Buttons")] [SerializeField]
        private Button audioTabButton;

        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button gameplayTabButton;
        [SerializeField] private Button accessibilityTabButton;

        [Header("Optional UX")]
        [Tooltip("Enable/disable this object when there are unsaved pending changes.")]
        [SerializeField]
        private GameObject pendingIndicator;

        [Tooltip("Keyboard shortcut to open/close the settings panel. Set to None to disable.")] [SerializeField]
        private KeyCode toggleKey = KeyCode.Escape;

        [Tooltip("Which tab to show when the panel is first opened.")] [SerializeField]
        private StartTab defaultTab = StartTab.Audio;

        public enum StartTab
        {
            Audio,
            Graphics,
            Gameplay,
            Accessibility
        }

        // ── Private ────────────────────────────────────────────────────────────

        private GameObject[] _allTabPanels;

        // Stored delegates so we can unsubscribe properly (lambdas can't be unsubscribed).
        private Action _onApplied;
        private Action _onReverted;
        private Action<object> _onPendingChanged;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _allTabPanels = new[]
            {
                audioPanel, graphicsPanel, gameplayPanel, accessibilityPanel
            };

            // Action buttons
            applyButton?.onClick.AddListener(OnApplyClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);
            resetButton?.onClick.AddListener(OnResetClicked);

            // Tab buttons — use local function to capture the correct panel reference
            WireTabButton(audioTabButton, audioPanel);
            WireTabButton(graphicsTabButton, graphicsPanel);
            WireTabButton(gameplayTabButton, gameplayPanel);
            WireTabButton(accessibilityTabButton, accessibilityPanel);

            // SettingsManager events
            _onApplied = RefreshPendingIndicator;
            _onReverted = RefreshPendingIndicator;
            _onPendingChanged = _ => RefreshPendingIndicator();

            SettingsManager.OnApplied += _onApplied;
            SettingsManager.OnReverted += _onReverted;
            SettingsManager.OnPendingChanged += _onPendingChanged;
        }

        private void OnDestroy()
        {
            applyButton?.onClick.RemoveListener(OnApplyClicked);
            cancelButton?.onClick.RemoveListener(OnCancelClicked);
            resetButton?.onClick.RemoveListener(OnResetClicked);

            SettingsManager.OnApplied -= _onApplied;
            SettingsManager.OnReverted -= _onReverted;
            SettingsManager.OnPendingChanged -= _onPendingChanged;
        }

        private void Start()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            ShowTab(TabToPanel(defaultTab));
            RefreshPendingIndicator();
        }

        private void Update()
        {
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
                TogglePanel();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Show the settings panel.</summary>
        public void OpenSettings()
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(true);
            RefreshPendingIndicator();
        }

        /// <summary>Hide the settings panel without applying or reverting changes.</summary>
        public void CloseSettings()
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(false);
        }

        /// <summary>Toggle the settings panel open/closed.</summary>
        public void TogglePanel()
        {
            if (settingsPanel == null) return;
            if (settingsPanel.activeSelf) CloseSettings();
            else OpenSettings();
        }

        /// <summary>Programmatically switch to a specific tab.</summary>
        public void ShowTab(StartTab tab) => ShowTab(TabToPanel(tab));

        // ── Button handlers ────────────────────────────────────────────────────

        private void OnApplyClicked()
        {
            SettingsManager.Apply();
            CloseSettings();
        }

        private void OnCancelClicked()
        {
            SettingsManager.Revert(); // discard pending changes
            CloseSettings();
        }

        private void OnResetClicked()
        {
            SettingsManager.ResetToDefaults();
            // Intentionally don't close — let the user review before hitting Apply.
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void ShowTab(GameObject target)
        {
            foreach (var panel in _allTabPanels)
                if (panel != null)
                    panel.SetActive(panel == target);
        }

        private void WireTabButton(Button btn, GameObject targetPanel)
        {
            if (btn == null || targetPanel == null) return;
            btn.onClick.AddListener(() => ShowTab(targetPanel));
        }

        private void RefreshPendingIndicator()
        {
            bool hasPending = SettingsManager.HasPendingChanges;

            if (pendingIndicator != null)
                pendingIndicator.SetActive(hasPending);

            // Disable Apply when nothing has changed (prevents accidental re-saves)
            if (applyButton != null)
                applyButton.interactable = hasPending;
        }

        private GameObject TabToPanel(StartTab tab) => tab switch
        {
            StartTab.Audio => audioPanel,
            StartTab.Graphics => graphicsPanel,
            StartTab.Gameplay => gameplayPanel,
            StartTab.Accessibility => accessibilityPanel,
            _ => audioPanel,
        };
    }
}