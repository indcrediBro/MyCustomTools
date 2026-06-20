// MarkdownInspector.cs
// Unity Editor entry-point — [CustomEditor(typeof(TextAsset))].
//
// Unity imports .md files as TextAsset. We check the extension in OnEnable
// and only activate for .md / .markdown; all other TextAssets fall through to
// Unity's own TextAssetInspector via reflection.
//
// Pipeline:
//   Raw text  ->  MarkdownParser.Parse()  ->  List<MdBlock>  ->  MarkdownRenderer.DrawBlocks()

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextAsset))]
public class MarkdownInspector : Editor
{
    bool          _isMd;
    string        _assetPath;
    string        _text;
    List<MdBlock> _blocks;
    Vector2       _scroll;
    bool          _showSource;

    // Fallback editor for non-.md TextAssets
    Editor _defaultEditor;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _assetPath = AssetDatabase.GetAssetPath(target);
        _isMd = _assetPath.EndsWith(".md",       StringComparison.OrdinalIgnoreCase)
             || _assetPath.EndsWith(".markdown",  StringComparison.OrdinalIgnoreCase);

        if (_isMd)
        {
            MarkdownRenderer.Invalidate();
            _text   = (target as TextAsset)?.text ?? "";
            _blocks = MarkdownParser.Parse(_text);
        }
    }

    void OnDisable()
    {
        if (_defaultEditor != null)
        {
            DestroyImmediate(_defaultEditor);
            _defaultEditor = null;
        }
    }

    // ── Inspector GUI ─────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        if (!_isMd)
        {
            DrawFallbackInspector();
            return;
        }

        // Toolbar: filename | Preview | Source
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(System.IO.Path.GetFileName(_assetPath),
                        MarkdownRenderer.ToolbarTitle, GUILayout.ExpandWidth(true));
        if (GUILayout.Toggle(!_showSource, "Preview", EditorStyles.toolbarButton, GUILayout.Width(58)))
            _showSource = false;
        if (GUILayout.Toggle(_showSource,  "Source",  EditorStyles.toolbarButton, GUILayout.Width(54)))
            _showSource = true;
        EditorGUILayout.EndHorizontal();

        // Content
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_showSource)
            GUILayout.TextArea(_text, MarkdownRenderer.SourceStyle);
        else
            MarkdownRenderer.DrawBlocks(_blocks);
        EditorGUILayout.EndScrollView();
    }

    public override bool UseDefaultMargins() => !_isMd;
    public override bool HasPreviewGUI()     => false;

    protected override void OnHeaderGUI()
    {
        if (!_isMd) base.OnHeaderGUI();
    }

    // ── Fallback for non-.md TextAssets ──────────────────────────────────────

    void DrawFallbackInspector()
    {
        if (_defaultEditor == null)
        {
            var t = Type.GetType("UnityEditor.TextAssetInspector, UnityEditor");
            if (t != null) _defaultEditor = CreateEditor(target, t);
        }
        if (_defaultEditor != null) _defaultEditor.OnInspectorGUI();
        else DrawDefaultInspector();
    }
}
#endif