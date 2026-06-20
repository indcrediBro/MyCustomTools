#if UNITY_EDITOR
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ObjectPooling.Editor
{
    /// <summary>
    /// Editor window that shows live stats for every active pool.
    /// Open via:  Window > Object Pooling > Pool Debugger
    /// </summary>
    public class PoolDebuggerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private double  _nextRefresh;
        private const double RefreshInterval = 0.25;

        // We read the private _pools dictionary via reflection to avoid
        // exposing internals in the public API.
        private static readonly FieldInfo PoolsField =
            typeof(Pool).GetField("_pools",
                BindingFlags.NonPublic | BindingFlags.Static);

        [MenuItem("Window/Object Pooling/Pool Debugger")]
        public static void Open() =>
            GetWindow<PoolDebuggerWindow>("Pool Debugger");

        private void OnEnable()  => EditorApplication.update += Repaint;
        private void OnDisable() => EditorApplication.update -= Repaint;

        private void OnGUI()
        {
            // Header
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Pool Debugger", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear All", GUILayout.Width(80)))
                    Pool.ClearAll();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live pool data.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);

            // Table header
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Prefab",   GUILayout.Width(200));
                GUILayout.Label("Active",   GUILayout.Width(60));
                GUILayout.Label("Inactive", GUILayout.Width(60));
                GUILayout.Label("Total",    GUILayout.Width(60));
                GUILayout.FlexibleSpace();
                GUILayout.Label("Actions",  GUILayout.Width(80));
            }

            // Rows
            var pools = PoolsField?.GetValue(null)
                as Dictionary<GameObject, UnityEngine.Pool.ObjectPool<GameObject>>;

            if (pools == null || pools.Count == 0)
            {
                EditorGUILayout.HelpBox("No pools active yet.", MessageType.None);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var kvp in pools)
            {
                var prefab = kvp.Key;
                var pool   = kvp.Value;

                int active   = pool.CountActive;
                int inactive = pool.CountInactive;
                int total    = active + inactive;

                using (new EditorGUILayout.HorizontalScope())
                {
                    // Prefab ping
                    if (GUILayout.Button(prefab != null ? prefab.name : "(destroyed)",
                            EditorStyles.linkLabel, GUILayout.Width(200)))
                    {
                        if (prefab != null)
                            EditorGUIUtility.PingObject(prefab);
                    }

                    Color prev = GUI.color;
                    GUI.color = active > 0 ? new Color(1f, 0.85f, 0.3f) : Color.white;
                    GUILayout.Label(active.ToString(),   GUILayout.Width(60));
                    GUI.color = prev;

                    GUILayout.Label(inactive.ToString(), GUILayout.Width(60));

                    GUI.color = total > 0 ? Color.cyan : Color.gray;
                    GUILayout.Label(total.ToString(),    GUILayout.Width(60));
                    GUI.color = prev;

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Clear", GUILayout.Width(80)) && prefab != null)
                        Pool.ClearPool(prefab);
                }

                // Capacity bar
                float fill = total > 0 ? (float)active / total : 0f;
                var barRect = GUILayoutUtility.GetRect(0, 4, GUILayout.ExpandWidth(true));
                barRect.x     += 4;
                barRect.width -= 8;
                EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
                var fillRect = new Rect(barRect.x, barRect.y, barRect.width * fill, barRect.height);
                EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.8f, 0.4f));

                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
