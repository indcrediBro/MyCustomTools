// ============================================================
//  SettingsSliderBinder.cs
//  Place anywhere in Assets/ (except an Editor/ folder).
//
//  Add to any GameObject that also has a Unity Slider component.
//
//  Inspector setup:
//    1. Choose Type (Float or Int)
//    2. Set the matching Setting enum value in the Inspector
//    3. Set Min / Max to the valid range
//    4. Optionally wire a ValueLabel (TMP_Text) for live readout
//    5. Adjust LabelFormat if needed
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Settings
{
    /// <summary>
    /// Binds a <see cref="Slider"/> to a float or integer setting in <see cref="SettingsManager"/>
    /// using typed enums (<see cref="FloatSetting"/> / <see cref="IntSetting"/>).
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class SettingsSliderBinder : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        public enum BindingType
        {
            Float,
            Int
        }

        [Header("Binding")]
        [Tooltip("Float = fractional value (volume, sensitivity). Int = whole number (frame rate, quality index).")]
        public BindingType Type = BindingType.Float;

        [Tooltip("Which float setting to bind. Only used when Type = Float.")]
        public FloatSetting FloatPath = FloatSetting.Audio_MasterVolume;

        [Tooltip("Which int setting to bind. Only used when Type = Int.")]
        public IntSetting IntPath = IntSetting.Graphics_TargetFrameRate;

        [Header("Range")] public float MinValue = 0f;
        public float MaxValue = 1f;

        [Header("Live Label (optional)")] [Tooltip("TMP_Text to display the current value. Leave empty to skip.")]
        public TMP_Text ValueLabel;

        [Tooltip("C# format string.\n" +
                 "  \"{0:P0}\" → 80%\n" +
                 "  \"{0:F1}\" → 0.8\n" +
                 "  \"{0:F0} FPS\" → 60 FPS")]
        public string LabelFormat = "{0:F2}";

        // ── Private ────────────────────────────────────────────────────────────

        private Slider _slider;
        private bool _isSyncing;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = MinValue;
            _slider.maxValue = MaxValue;
            _slider.wholeNumbers = (Type == BindingType.Int);
        }

        private void OnEnable()
        {
            PullFromManager();
            _slider.onValueChanged.AddListener(OnSliderMoved);
            SettingsManager.OnReverted += PullFromManager;
            SettingsManager.OnPendingChanged += OnExternalChanged;
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(OnSliderMoved);
            SettingsManager.OnReverted -= PullFromManager;
            SettingsManager.OnPendingChanged -= OnExternalChanged;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnSliderMoved(float value)
        {
            if (_isSyncing) return;

            if (Type == BindingType.Int)
                SettingsManager.SetInt(IntPath, Mathf.RoundToInt(value));
            else
                SettingsManager.SetFloat(FloatPath, value);

            UpdateLabel(value);
        }

        private void OnExternalChanged(object changed)
        {
            bool isOurs = Type == BindingType.Int
                ? changed is IntSetting i && i == IntPath
                : changed is FloatSetting f && f == FloatPath;

            if (isOurs) PullFromManager();
        }

        // ── Sync ──────────────────────────────────────────────────────────────

        private void PullFromManager()
        {
            _isSyncing = true;

            float v = Type == BindingType.Int
                ? SettingsManager.GetInt(IntPath)
                : SettingsManager.GetFloat(FloatPath);

            _slider.SetValueWithoutNotify(v);
            UpdateLabel(v);
            _isSyncing = false;
        }

        private void UpdateLabel(float value)
        {
            if (ValueLabel == null) return;
            object formatted = Type == BindingType.Int ? (object)Mathf.RoundToInt(value) : value;
            ValueLabel.text = string.Format(LabelFormat, formatted);
        }
    }
}