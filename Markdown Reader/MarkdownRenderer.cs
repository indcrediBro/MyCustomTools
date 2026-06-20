// MarkdownRenderer.cs
// IMGUI renderer for a List<MdBlock> produced by MarkdownParser.
// Owns all GUIStyle construction and inline Markdown -> Unity rich-text conversion.
//
// Table alignment fix: column widths are calculated once from the available
// inspector width and applied as fixed GUILayout.Width() per cell, so all
// columns stay equal-width and properly aligned.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MarkdownRenderer
{
    // ── Style sheet ───────────────────────────────────────────────────────────

    static bool _ready;

    static GUIStyle _h1, _h2, _h3, _h4, _h5, _h6;
    static GUIStyle _para;
    static GUIStyle _quote;
    static GUIStyle _code, _codeBg;
    static GUIStyle _bulletMark, _orderedMark;
    static GUIStyle _tableHeader, _tableCell, _tableOddRow;
    static GUIStyle _langBadge;
    static GUIStyle _toolbarTitle;
    static GUIStyle _imageCaption;
    static GUIStyle _sourceStyle;

    static Texture2D _codeBgTex, _tableHeaderBgTex, _tableOddBgTex;

    // Palette
    static Color _textCol, _muteCol, _codeCol, _codeBgCol, _accentCol;
    static Color _headCol, _linkCol, _tableHeaderBg, _tableOddBg, _tableBorder, _hrCol;

    // ── Public accessors ──────────────────────────────────────────────────────

    public static GUIStyle ToolbarTitle => _ready ? _toolbarTitle : BuildAndReturn();
    public static GUIStyle SourceStyle  => _ready ? _sourceStyle  : BuildAndReturn();

    static GUIStyle BuildAndReturn() { Build(); return _toolbarTitle; }

    public static void Invalidate() => _ready = false;

    // ── Entry point ───────────────────────────────────────────────────────────

    public static void DrawBlocks(List<MdBlock> blocks)
    {
        if (!_ready) Build();
        GUILayout.Space(8);
        foreach (var b in blocks)
            DrawBlock(b);
        GUILayout.Space(10);
    }

    // ── Block dispatch ────────────────────────────────────────────────────────

    static void DrawBlock(MdBlock b)
    {
        switch (b.Kind)
        {
            case BlockKind.H1: DrawHeading(b.Text, _h1, true,  24); break;
            case BlockKind.H2: DrawHeading(b.Text, _h2, true,  16); break;
            case BlockKind.H3: DrawHeading(b.Text, _h3, false, 10); break;
            case BlockKind.H4: DrawHeading(b.Text, _h4, false,  6); break;
            case BlockKind.H5: DrawHeading(b.Text, _h5, false,  4); break;
            case BlockKind.H6: DrawHeading(b.Text, _h6, false,  4); break;

            case BlockKind.HorizontalRule: DrawHR();            break;
            case BlockKind.Blank:          GUILayout.Space(5); break;

            case BlockKind.Paragraph:  DrawParagraph(b.Text);  break;
            case BlockKind.BlockQuote: DrawBlockQuote(b.Text); break;
            case BlockKind.CodeFence:  DrawCodeFence(b);       break;

            case BlockKind.BulletItem:  DrawBulletItem(b);     break;
            case BlockKind.OrderedItem: DrawOrderedItem(b);    break;
            case BlockKind.TaskItem:    DrawTaskItem(b);       break;

            case BlockKind.Table: DrawTable(b);                break;
            case BlockKind.Image: DrawImage(b);                break;
        }
    }

    // ── Heading ───────────────────────────────────────────────────────────────

    static void DrawHeading(string text, GUIStyle style, bool rule, int spaceAbove)
    {
        GUILayout.Space(spaceAbove);
        GUILayout.Label(Rich(text), style);
        if (rule)
        {
            var r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                        GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(new Rect(r.x + 4, r.y, r.width - 8, 1), _hrCol);
        }
        GUILayout.Space(4);
    }

    // ── Horizontal rule ───────────────────────────────────────────────────────

    static void DrawHR()
    {
        GUILayout.Space(6);
        var r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                    GUILayout.Height(1), GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(new Rect(r.x + 4, r.y, r.width - 8, 1), _hrCol);
        GUILayout.Space(6);
    }

    // ── Paragraph ─────────────────────────────────────────────────────────────

    static void DrawParagraph(string text)
    {
        GUILayout.Label(Rich(text), _para);
        GUILayout.Space(2);
    }

    // ── Blockquote ────────────────────────────────────────────────────────────

    static void DrawBlockQuote(string text)
    {
        foreach (var ln in text.Split('\n'))
        {
            EditorGUILayout.BeginHorizontal();
            // Accent bar — GetRect gives us a rect we can draw into
            var bar = GUILayoutUtility.GetRect(4, EditorGUIUtility.singleLineHeight,
                          GUILayout.Width(4), GUILayout.ExpandHeight(false));
            // Extend bar to cover full label height after layout
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, 3, bar.height + 2), _accentCol);
            GUILayout.Space(8);
            GUILayout.Label(Rich(ln), _quote, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.Space(2);
    }

    // ── Fenced code block ─────────────────────────────────────────────────────

    static void DrawCodeFence(MdBlock b)
    {
        GUILayout.Space(4);
        EditorGUILayout.BeginVertical(_codeBg);
        if (!string.IsNullOrEmpty(b.Language))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(b.Language.ToUpper(), _langBadge);
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.Label(b.Text, _code);
        EditorGUILayout.EndVertical();
        GUILayout.Space(4);
    }

    // ── List items ────────────────────────────────────────────────────────────

    static void DrawBulletItem(MdBlock b)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10 + b.Depth * 16f);
        string bullet = b.Depth == 0 ? "•" : b.Depth == 1 ? "◦" : "▸";
        GUILayout.Label(bullet, _bulletMark, GUILayout.Width(14));
        GUILayout.Label(Rich(b.Text), _para, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
    }

    static void DrawOrderedItem(MdBlock b)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10 + b.Depth * 16f);
        GUILayout.Label(b.OrderIndex + ".", _orderedMark, GUILayout.Width(28));
        GUILayout.Label(Rich(b.Text), _para, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
    }

    static void DrawTaskItem(MdBlock b)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10 + b.Depth * 16f);

        var boxStyle = new GUIStyle(_bulletMark);
        boxStyle.normal.textColor = b.TaskChecked ? new Color(.3f, .85f, .45f) : _muteCol;
        GUILayout.Label(b.TaskChecked ? "☑" : "☐", boxStyle, GUILayout.Width(18));

        var labelStyle = new GUIStyle(_para);
        if (b.TaskChecked) labelStyle.normal.textColor = _muteCol;
        GUILayout.Label(Rich(b.Text), labelStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
    }

    // ── Table ─────────────────────────────────────────────────────────────────
    // Column widths are fixed and equal so cells line up correctly.
    // The available width is the inspector width minus left/right padding.

    static void DrawTable(MdBlock b)
    {
        if (b.TableHeaders == null || b.TableHeaders.Count == 0) return;

        GUILayout.Space(6);

        int   cols     = b.TableHeaders.Count;
        float pad      = 12f; // left + right inspector margin
        float colW     = (EditorGUIUtility.currentViewWidth - pad) / cols;

        // ── Header row ────────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        for (int c = 0; c < cols; c++)
        {
            var style = new GUIStyle(_tableHeader)
            {
                alignment = ColToAnchor(b.TableAligns, c, header: true)
            };
            GUILayout.Label(Rich(b.TableHeaders[c]), style, GUILayout.Width(colW));
        }
        EditorGUILayout.EndHorizontal();

        // Header bottom border
        DrawRowBorder(1f, _tableBorder);

        // ── Body rows ─────────────────────────────────────────────────────────
        if (b.TableRows != null)
        {
            for (int r = 0; r < b.TableRows.Count; r++)
            {
                var row = b.TableRows[r];

                if (r % 2 == 1)
                    EditorGUILayout.BeginHorizontal(_tableOddRow);
                else
                    EditorGUILayout.BeginHorizontal();

                for (int c = 0; c < cols; c++)
                {
                    string cellText = (c < row.Count) ? row[c] : "";
                    var style = new GUIStyle(_tableCell)
                    {
                        alignment = ColToAnchor(b.TableAligns, c, header: false)
                    };
                    GUILayout.Label(Rich(cellText), style, GUILayout.Width(colW));
                }

                EditorGUILayout.EndHorizontal();
                DrawRowBorder(1f, new Color(_tableBorder.r, _tableBorder.g, _tableBorder.b, 0.25f));
            }
        }

        GUILayout.Space(6);
    }

    static void DrawRowBorder(float h, Color col)
    {
        var r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                    GUILayout.Height(h), GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, col);
    }

    static TextAnchor ColToAnchor(List<ColAlign> aligns, int col, bool header)
    {
        ColAlign a = (aligns != null && col < aligns.Count) ? aligns[col] : ColAlign.Left;
        switch (a)
        {
            case ColAlign.Center: return header ? TextAnchor.MiddleCenter : TextAnchor.UpperCenter;
            case ColAlign.Right:  return header ? TextAnchor.MiddleRight  : TextAnchor.UpperRight;
            default:              return header ? TextAnchor.MiddleLeft   : TextAnchor.UpperLeft;
        }
    }

    // ── Image ─────────────────────────────────────────────────────────────────

    static void DrawImage(MdBlock b)
    {
        GUILayout.Space(4);
        string src = b.ImageSrc ?? "";
        Texture2D tex = null;

        if (!string.IsNullOrEmpty(src) && !src.StartsWith("http://") && !src.StartsWith("https://"))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(src);
            if (tex == null && !src.StartsWith("Assets/"))
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + src);
        }

        if (tex != null)
        {
            float maxW  = EditorGUIUtility.currentViewWidth - 24f;
            float scale = Mathf.Min(1f, maxW / tex.width);
            var   rect  = GUILayoutUtility.GetRect(tex.width * scale, tex.height * scale,
                              GUILayout.MaxWidth(maxW));
            EditorGUI.DrawPreviewTexture(rect, tex);
        }
        else
        {
            // Placeholder
            var ph = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter };
            ph.normal.textColor = _muteCol;
            string label = string.IsNullOrEmpty(b.AltText) ? $"[ image: {src} ]"
                                                           : $"[ {b.AltText} ]";
            GUILayout.Label(label, ph, GUILayout.Height(40), GUILayout.ExpandWidth(true));
        }

        if (!string.IsNullOrEmpty(b.AltText))
            GUILayout.Label(b.AltText, _imageCaption);

        GUILayout.Space(4);
    }

    // ── Inline Markdown → Unity rich-text ────────────────────────────────────

    public static string Rich(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        s = Regex.Replace(s, @"\*\*\*(.+?)\*\*\*", "<b><i>$1</i></b>");
        s = Regex.Replace(s, @"___(.+?)___",        "<b><i>$1</i></b>");
        s = Regex.Replace(s, @"\*\*(.+?)\*\*",      "<b>$1</b>");
        s = Regex.Replace(s, @"__(.+?)__",           "<b>$1</b>");
        s = Regex.Replace(s, @"\*(.+?)\*",           "<i>$1</i>");
        s = Regex.Replace(s, @"_(.+?)_",             "<i>$1</i>");
        s = Regex.Replace(s, @"~~(.+?)~~",
            m => $"<color=#{Hex(_muteCol)}>[{m.Groups[1].Value}]</color>");
        s = Regex.Replace(s, @"`(.+?)`",
            m => $"<color=#{Hex(_codeCol)}><b>{EscRich(m.Groups[1].Value)}</b></color>");
        s = Regex.Replace(s, @"!\[([^\]]*)\]\(([^)]+)\)",
            m => $"<color=#{Hex(_muteCol)}>[image: {m.Groups[1].Value}]</color>");
        s = Regex.Replace(s, @"\[([^\]]+)\]\([^)]+\)",
            m => $"<color=#{Hex(_linkCol)}><b>{m.Groups[1].Value}</b></color>");
        return s;
    }

    static string Hex(Color c)
    {
        Color32 c32 = c;
        return c32.r.ToString("X2") + c32.g.ToString("X2") + c32.b.ToString("X2");
    }

    static string EscRich(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // ── Style builder ─────────────────────────────────────────────────────────

    static void Build()
    {
        _ready = true;
        bool dark = EditorGUIUtility.isProSkin;

        _textCol       = dark ? new Color(.88f, .88f, .88f) : new Color(.10f, .10f, .10f);
        _muteCol       = dark ? new Color(.55f, .55f, .55f) : new Color(.45f, .45f, .45f);
        _headCol       = dark ? new Color(.96f, .96f, .96f) : new Color(.08f, .08f, .08f);
        _codeCol       = dark ? new Color(.72f, .88f, .65f) : new Color(.15f, .40f, .15f);
        _codeBgCol     = dark ? new Color(.13f, .13f, .13f) : new Color(.93f, .93f, .93f);
        _accentCol     = dark ? new Color(.30f, .60f, 1.00f): new Color(.20f, .50f, .90f);
        _linkCol       = dark ? new Color(.45f, .72f, .98f) : new Color(.10f, .40f, .85f);
        _tableHeaderBg = dark ? new Color(.22f, .22f, .26f) : new Color(.80f, .82f, .88f);
        _tableOddBg    = dark ? new Color(.18f, .18f, .20f) : new Color(.94f, .94f, .97f);
        _tableBorder   = dark ? new Color(.35f, .35f, .40f) : new Color(.68f, .70f, .75f);
        _hrCol         = dark ? new Color(.38f, .38f, .38f) : new Color(.68f, .68f, .68f);

        // Monospace font — fall back through common system fonts
        Font mono = null;
        foreach (var n in new[] { "JetBrains Mono","Fira Code","Cascadia Code",
                                   "Courier New","Consolas","Courier","Monaco" })
        {
            mono = Font.CreateDynamicFontFromOSFont(n, 12);
            if (mono != null) break;
        }

        // Headings
        _h1 = Lbl(22, FontStyle.Bold,         _headCol);
        _h2 = Lbl(18, FontStyle.Bold,         _headCol);
        _h3 = Lbl(15, FontStyle.Bold,         _headCol);
        _h4 = Lbl(13, FontStyle.Bold,         _headCol);
        _h5 = Lbl(12, FontStyle.BoldAndItalic,_headCol);
        _h6 = Lbl(12, FontStyle.Italic,       _muteCol);

        // Body
        _para         = Lbl(12, FontStyle.Normal, _textCol);
        _para.padding = new RectOffset(4, 4, 1, 1);

        // Blockquote
        _quote         = Lbl(12, FontStyle.Italic, _muteCol);
        _quote.padding = new RectOffset(6, 4, 1, 1);

        // List markers
        _bulletMark           = Lbl(13, FontStyle.Normal, _muteCol);
        _bulletMark.alignment = TextAnchor.UpperCenter;

        _orderedMark           = Lbl(12, FontStyle.Bold, _muteCol);
        _orderedMark.alignment = TextAnchor.UpperRight;
        _orderedMark.padding   = new RectOffset(0, 4, 1, 1);

        // Code
        _code = new GUIStyle(EditorStyles.label)
        {
            font     = mono,
            fontSize = 12,
            wordWrap = true,
            richText = false,
            padding  = new RectOffset(10, 10, 8, 8),
        };
        _code.normal.textColor = _codeCol;

        _codeBgTex = Tex(_codeBgCol);
        _codeBg    = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(0,0,0,0), margin = new RectOffset(4,4,2,2) };
        _codeBg.normal.background = _codeBgTex;

        // Language badge
        _langBadge           = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, alignment = TextAnchor.MiddleRight };
        _langBadge.padding   = new RectOffset(4, 8, 2, 0);
        _langBadge.normal.textColor = _muteCol;

        // Table
        _tableHeaderBgTex = Tex(_tableHeaderBg);
        _tableOddBgTex    = Tex(_tableOddBg);

        _tableHeader = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 12,
            wordWrap  = false,
            richText  = true,
            padding   = new RectOffset(6, 6, 5, 5),
            alignment = TextAnchor.MiddleLeft,
            clipping  = TextClipping.Clip,
        };
        _tableHeader.normal.textColor  = _headCol;
        _tableHeader.normal.background = _tableHeaderBgTex;

        _tableCell = new GUIStyle(EditorStyles.label)
        {
            fontSize  = 12,
            wordWrap  = false,
            richText  = true,
            padding   = new RectOffset(6, 6, 4, 4),
            alignment = TextAnchor.UpperLeft,
            clipping  = TextClipping.Clip,
        };
        _tableCell.normal.textColor = _textCol;

        _tableOddRow = new GUIStyle();
        _tableOddRow.normal.background = _tableOddBgTex;

        // Image caption
        _imageCaption = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize  = 10,
            fontStyle = FontStyle.Italic,
            wordWrap  = true,
        };
        _imageCaption.normal.textColor = _muteCol;

        // Toolbar title
        _toolbarTitle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };

        // Source view
        _sourceStyle = new GUIStyle(EditorStyles.textArea)
        {
            font     = mono,
            fontSize = 12,
            wordWrap = true,
            richText = false,
        };
    }

    static GUIStyle Lbl(int size, FontStyle style, Color col)
    {
        var s = new GUIStyle(EditorStyles.label)
        {
            fontSize  = size,
            fontStyle = style,
            wordWrap  = true,
            richText  = true,
        };
        s.normal.textColor = col;
        return s;
    }

    static Texture2D Tex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }
}
#endif