using UnityEditor;
using UnityEngine;

namespace IncredibleAttributes.Editor
{
    // ──────────────────────────────────────────────────────────────────────────
    //  HORIZONTAL LINE  (decorator)
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(HorizontalLineAttribute))]
    internal class HorizontalLineDecoratorDrawer : DecoratorDrawer
    {
        private const float MarginV = 4f;

        public override float GetHeight()
        {
            var attr = (HorizontalLineAttribute)attribute;
            return attr.Height + MarginV * 2f;
        }

        public override void OnGUI(Rect position)
        {
            var attr = (HorizontalLineAttribute)attribute;
            var line = new Rect(position.x, position.y + MarginV, position.width, attr.Height);
            EditorGUI.DrawRect(line, attr.Color);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  INFO BOX  (decorator — always visible version)
    //  Note: the conditional InfoBox (with ConditionName) is handled by
    //  IncredibleEditor's main loop because decorators don't have access to
    //  the serialized object. Unconditional InfoBoxes work here fine.
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    internal class InfoBoxDecoratorDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            var attr = (InfoBoxAttribute)attribute;
            // Skip height for conditional boxes — they appear inline via the main editor
            if (!string.IsNullOrEmpty(attr.ConditionName)) return 0f;
            return HelpBoxHeight(attr.Text) + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position)
        {
            var attr = (InfoBoxAttribute)attribute;
            if (!string.IsNullOrEmpty(attr.ConditionName)) return;

            var type = attr.Type switch
            {
                EInfoBoxType.Warning => MessageType.Warning,
                EInfoBoxType.Error   => MessageType.Error,
                _                   => MessageType.Info
            };
            position.height -= EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.HelpBox(position, attr.Text, type);
        }

        private static float HelpBoxHeight(string text)
        {
            var style = EditorStyles.helpBox;
            float w   = EditorGUIUtility.currentViewWidth - 38f;
            float h   = style.CalcHeight(new GUIContent(text), w);
            return Mathf.Max(h, EditorGUIUtility.singleLineHeight * 1.5f);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  TITLE  (decorator)
    // ──────────────────────────────────────────────────────────────────────────

    [CustomPropertyDrawer(typeof(TitleAttribute))]
    internal class TitleDecoratorDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            var attr = (TitleAttribute)attribute;
            float h  = EditorGUIUtility.singleLineHeight + 4f;     // title
            if (!string.IsNullOrEmpty(attr.Subtitle))
                h += EditorGUIUtility.singleLineHeight;             // subtitle
            if (attr.Line)
                h += 3f;                                            // line
            return h + 4f;                                          // spacing
        }

        public override void OnGUI(Rect position)
        {
            var attr = (TitleAttribute)attribute;
            float y  = position.y + 4f;

            // Title
            var titleRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, attr.Text, EditorStyles.boldLabel);
            y += EditorGUIUtility.singleLineHeight + 1f;

            // Subtitle
            if (!string.IsNullOrEmpty(attr.Subtitle))
            {
                var subStyle = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
                EditorGUI.LabelField(new Rect(position.x, y, position.width,
                                              EditorGUIUtility.singleLineHeight), attr.Subtitle, subStyle);
                y += EditorGUIUtility.singleLineHeight;
            }

            // Line
            if (attr.Line)
                EditorGUI.DrawRect(new Rect(position.x, y + 1f, position.width, 1f),
                                   new Color(0.35f, 0.35f, 0.35f, 0.7f));
        }
    }
}
