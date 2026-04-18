#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JUICE
{
    [CustomEditor(typeof(JuicePlayer))]
    public class JuicePlayerEditor : Editor
    {
        // All registered feedback types — add new ones here
        private static readonly List<(string category, Type type, string label)> FeedbackRegistry =
            new List<(string, Type, string)>
            {
                // Camera
                ("Camera", typeof(CameraShakeFeedback), "📷  Camera Shake"),
                // Transform
                ("Transform", typeof(ScaleFeedback), "📐  Scale / Squash & Stretch"),
                ("Transform", typeof(PositionFeedback), "🐢  Position"),
                ("Transform", typeof(RotationFeedback), "🔄  Rotation"),
                // Renderer
                ("Renderer", typeof(FlickerFeedback), "💄  Flicker"),
                // Audio
                ("Audio", typeof(SoundFeedback), "🔊  Sound"),
                // Particles
                ("Particles", typeof(ParticlesFeedback), "✨  Particles"),
                // UI
                ("UI", typeof(FlashFeedback), "⚡  Flash"),
                ("UI", typeof(ImageFeedback), "🌶  Image"),
                ("UI", typeof(CanvasGroupFeedback), "🌶  Canvas Group"),
                // Light
                ("Light", typeof(LightFeedback), "💡  Light"),
                // Time
                ("Time", typeof(TimeScaleFeedback), "⌚  Time Scale"),
                // Animation
                ("Animation", typeof(AnimationParameterFeedback), "💃  Animation Parameter"),
                // Events & Control
                ("Events", typeof(UnityEventFeedback), "📅  Unity Event"),
                ("Control", typeof(SetActiveFeedback), "📦  Set Active"),
            };

        private readonly Dictionary<int, bool> _foldouts = new Dictionary<int, bool>();

        private SerializedProperty _feedbacksProp;
        private SerializedProperty _playOnAwakeProp;
        private SerializedProperty _sequentialProp;

        // Palette
        private static readonly Color BgDark = new Color(0.14f, 0.14f, 0.16f, 1f);
        private static readonly Color HeaderBg = new Color(0.10f, 0.10f, 0.12f, 1f);
        private static readonly Color ItemBg = new Color(0.18f, 0.18f, 0.20f, 1f);
        private static readonly Color ItemBgOff = new Color(0.14f, 0.14f, 0.15f, 1f);
        private static readonly Color Divider = new Color(0.08f, 0.08f, 0.09f, 1f);
        private static readonly Color AccentGreen = new Color(0.25f, 0.82f, 0.35f, 1f);
        private static readonly Color AccentRed = new Color(0.88f, 0.28f, 0.28f, 1f);
        private static readonly Color AccentOrange = new Color(0.95f, 0.55f, 0.15f, 1f);

        private void OnEnable()
        {
            _feedbacksProp = serializedObject.FindProperty("Feedbacks");
            _playOnAwakeProp = serializedObject.FindProperty("PlayOnAwake");
            _sequentialProp = serializedObject.FindProperty("Sequential");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawOptions();
            EditorGUILayout.Space(6);
            DrawFeedbackList();
            EditorGUILayout.Space(4);
            DrawAddButton();
            EditorGUILayout.Space(8);
            DrawPlayStopButtons();

            serializedObject.ApplyModifiedProperties();
        }

        // ─── Header ──────────────────────────────────────────────────
        private void DrawHeader()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, HeaderBg);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = Color.white;

            var subStyle = new GUIStyle(EditorStyles.miniLabel);
            subStyle.normal.textColor = new Color(0.55f, 0.55f, 0.6f);

            int count = _feedbacksProp.arraySize;
            int enabled = 0;
            for (int i = 0; i < count; i++)
            {
                var item = _feedbacksProp.GetArrayElementAtIndex(i).managedReferenceValue as Feedback;
                if (item != null && item.Enabled) enabled++;
            }

            GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width, 22), "🍊  JUICE Player", titleStyle);
            GUI.Label(new Rect(rect.x + 14, rect.y + 27, rect.width, 14), $"{enabled} / {count} feedbacks active",
                subStyle);
        }

        // ─── Options ─────────────────────────────────────────────────
        private void DrawOptions()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_playOnAwakeProp, GUILayout.ExpandWidth(true));
                EditorGUILayout.PropertyField(_sequentialProp, GUILayout.ExpandWidth(true));
            }
        }

        // ─── Feedback List ───────────────────────────────────────────
        private void DrawFeedbackList()
        {
            int toDelete = -1;
            int toMoveUp = -1;
            int toMoveDn = -1;

            for (int i = 0; i < _feedbacksProp.arraySize; i++)
            {
                SerializedProperty item = _feedbacksProp.GetArrayElementAtIndex(i);
                if (item.managedReferenceValue == null) continue;

                Feedback fb = item.managedReferenceValue as Feedback;
                bool on = fb != null && fb.Enabled;

                // Row background
                Rect bg = GUILayoutUtility.GetRect(0, 2, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(bg, on ? ItemBg : ItemBgOff);

                // Header row
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Enable toggle
                    bool newOn = EditorGUILayout.Toggle(on, GUILayout.Width(16));
                    if (newOn != on)
                    {
                        fb.Enabled = newOn;
                        EditorUtility.SetDirty(target);
                    }

                    if (!_foldouts.ContainsKey(i)) _foldouts[i] = false;

                    // Delay badge
                    if (fb != null && fb.Delay > 0f)
                    {
                        var badgeStyle = new GUIStyle(EditorStyles.miniLabel);
                        badgeStyle.normal.textColor = AccentOrange;
                        GUILayout.Label($"+{fb.Delay:0.##}s", badgeStyle, GUILayout.Width(38));
                    }

                    // Foldout label
                    _foldouts[i] = EditorGUILayout.Foldout(_foldouts[i], fb?.DisplayLabel ?? "Feedback", true,
                        FoldoutStyle(on));
                    GUILayout.FlexibleSpace();

                    // Reorder
                    if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(22))) toMoveUp = i;
                    if (GUILayout.Button("▼", EditorStyles.miniButtonRight, GUILayout.Width(22))) toMoveDn = i;

                    // Delete
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = AccentRed;
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22))) toDelete = i;
                    GUI.backgroundColor = prevBg;
                }

                // Expanded fields
                if (_foldouts.TryGetValue(i, out bool folded) && folded)
                {
                    EditorGUILayout.Space(2);
                    using (new EditorGUI.IndentLevelScope(1))
                        DrawFeedbackFields(item);
                    EditorGUILayout.Space(4);
                }

                // Divider
                Rect div = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(div, Divider);
            }

            if (toDelete >= 0)
            {
                _feedbacksProp.DeleteArrayElementAtIndex(toDelete);
                _foldouts.Remove(toDelete);
            }

            if (toMoveUp > 0) _feedbacksProp.MoveArrayElement(toMoveUp, toMoveUp - 1);
            if (toMoveDn >= 0 && toMoveDn < _feedbacksProp.arraySize - 1)
                _feedbacksProp.MoveArrayElement(toMoveDn, toMoveDn + 1);
        }

        private void DrawFeedbackFields(SerializedProperty feedbackProp)
        {
            var prop = feedbackProp.Copy();
            var end = feedbackProp.GetEndProperty();
            if (!prop.NextVisible(true)) return;
            do
            {
                if (SerializedProperty.EqualContents(prop, end)) break;
                // Skip Label and Enabled — they live in the header row
                if (prop.name == "Label" || prop.name == "Enabled") continue;
                EditorGUILayout.PropertyField(prop, true);
            } while (prop.NextVisible(false));
        }

        // ─── Add Button ──────────────────────────────────────────────
        private void DrawAddButton()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.45f, 0.8f, 1f);
                if (GUILayout.Button("＋  Add Feedback", GUILayout.Height(28), GUILayout.Width(170)))
                {
                    GenericMenu menu = new GenericMenu();
                    string lastCategory = "";
                    foreach (var (cat, type, label) in FeedbackRegistry)
                    {
                        if (cat != lastCategory)
                        {
                            menu.AddSeparator("");
                            lastCategory = cat;
                        }

                        var capturedType = type;
                        menu.AddItem(new GUIContent($"{cat}/{label}"), false, () => AddFeedback(capturedType));
                    }

                    menu.ShowAsContext();
                }

                GUI.backgroundColor = prevBg;
                GUILayout.FlexibleSpace();
            }
        }

        private void AddFeedback(Type feedbackType)
        {
            serializedObject.Update();
            int index = _feedbacksProp.arraySize;
            _feedbacksProp.InsertArrayElementAtIndex(index);
            _feedbacksProp.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(feedbackType);
            _foldouts[index] = true;
            serializedObject.ApplyModifiedProperties();
        }

        // ─── Play / Stop ─────────────────────────────────────────────
        private void DrawPlayStopButtons()
        {
            JuicePlayer player = (JuicePlayer)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
                {
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = Application.isPlaying ? AccentGreen : Color.grey;
                    if (GUILayout.Button(player.IsPlaying ? "⏸  Playing..." : "▶  Play", GUILayout.Height(32)))
                        player.Play();

                    GUI.backgroundColor = Application.isPlaying ? AccentRed : Color.grey;
                    if (GUILayout.Button("■  Stop", GUILayout.Height(32), GUILayout.Width(80)))
                        player.Stop();

                    GUI.backgroundColor = prevBg;
                }
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to preview feedbacks.", MessageType.None);
        }

        // ─── Helpers ─────────────────────────────────────────────────
        private static GUIStyle FoldoutStyle(bool enabled)
        {
            var s = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            Color c = enabled ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            s.normal.textColor = c;
            s.onNormal.textColor = c;
            s.focused.textColor = c;
            s.onFocused.textColor = c;
            s.active.textColor = c;
            s.onActive.textColor = c;
            s.hover.textColor = c;
            s.onHover.textColor = c;
            return s;
        }
    }
#endif
}