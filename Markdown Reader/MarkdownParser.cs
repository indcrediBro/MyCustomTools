// MarkdownParser.cs
// Single-pass line-scanner that converts raw Markdown text into a List<MdBlock>.
// Supports: headings (ATX + Setext), paragraphs, fenced code blocks (``` / ~~~),
// blockquotes, bullet lists (nested), ordered lists (nested), task lists,
// GFM pipe tables, horizontal rules, standalone images, and blank lines.
//
// No third-party libraries required.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class MarkdownParser
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    public static List<MdBlock> Parse(string src)
    {
        if (string.IsNullOrEmpty(src))
            return new List<MdBlock>();

        var  lines  = src.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var  result = new List<MdBlock>();
        int  i      = 0;

        while (i < lines.Length)
        {
            string ln = lines[i];

            // ── Fenced code block ─────────────────────────────────────────────
            if (IsFenceStart(ln, out string fence, out string lang))
            {
                i++;
                var buf = new List<string>();
                while (i < lines.Length && !IsFenceEnd(lines[i], fence))
                    buf.Add(lines[i++]);
                if (i < lines.Length) i++; // consume closing fence
                result.Add(new MdBlock
                {
                    Kind     = BlockKind.CodeFence,
                    Text     = string.Join("\n", buf),
                    Language = lang,
                });
                continue;
            }

            // ── Blank line ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(ln))
            {
                result.Add(new MdBlock { Kind = BlockKind.Blank });
                i++;
                continue;
            }

            // ── ATX heading  (#  ##  ###  …  ######) ─────────────────────────
            {
                var m = Regex.Match(ln, @"^(#{1,6})\s+(.+?)(?:\s+#+\s*)?$");
                if (m.Success)
                {
                    result.Add(new MdBlock
                    {
                        Kind = LevelToHeadingKind(m.Groups[1].Value.Length),
                        Text = m.Groups[2].Value.Trim(),
                    });
                    i++;
                    continue;
                }
            }

            // ── Setext heading (next line is === or ---) ─────────────────────
            if (i + 1 < lines.Length)
            {
                string next = lines[i + 1];
                if (Regex.IsMatch(next, @"^=+\s*$") && ln.Trim().Length > 0)
                {
                    result.Add(new MdBlock { Kind = BlockKind.H1, Text = ln.Trim() });
                    i += 2;
                    continue;
                }
                if (Regex.IsMatch(next, @"^-{2,}\s*$") && ln.Trim().Length > 0
                    && !IsListItem(ln, out _, out _, out _, out _))
                {
                    result.Add(new MdBlock { Kind = BlockKind.H2, Text = ln.Trim() });
                    i += 2;
                    continue;
                }
            }

            // ── Horizontal rule ───────────────────────────────────────────────
            if (Regex.IsMatch(ln, @"^\s*(\*\s*){3,}$") ||
                Regex.IsMatch(ln, @"^\s*(-\s*){3,}$")  ||
                Regex.IsMatch(ln, @"^\s*(_\s*){3,}$"))
            {
                result.Add(new MdBlock { Kind = BlockKind.HorizontalRule });
                i++;
                continue;
            }

            // ── Blockquote ────────────────────────────────────────────────────
            if (ln.TrimStart().StartsWith(">"))
            {
                // Collect consecutive quote lines and merge them
                var qLines = new List<string>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith(">"))
                {
                    var stripped = Regex.Replace(lines[i], @"^\s*>\s?", "");
                    qLines.Add(stripped);
                    i++;
                }
                result.Add(new MdBlock
                {
                    Kind = BlockKind.BlockQuote,
                    Text = string.Join("\n", qLines),
                });
                continue;
            }

            // ── GFM Table ─────────────────────────────────────────────────────
            if (IsTableLine(ln) && i + 1 < lines.Length && IsTableDelimiter(lines[i + 1]))
            {
                var headers = SplitTableRow(ln);
                var aligns  = ParseTableAligns(lines[i + 1]);
                i += 2;
                var rows = new List<List<string>>();
                while (i < lines.Length && IsTableLine(lines[i]))
                {
                    rows.Add(SplitTableRow(lines[i]));
                    i++;
                }
                result.Add(new MdBlock
                {
                    Kind         = BlockKind.Table,
                    TableHeaders = headers,
                    TableRows    = rows,
                    TableAligns  = aligns,
                });
                continue;
            }

            // ── List item (bullet or ordered, with nesting + task) ────────────
            if (IsListItem(ln, out int depth, out int ordIndex, out bool taskChecked, out string itemText))
            {
                BlockKind kind = ordIndex >= 0 ? BlockKind.OrderedItem
                               : itemText.StartsWith("[ ] ") || itemText.StartsWith("[x] ") || itemText.StartsWith("[X] ")
                                   ? BlockKind.TaskItem : BlockKind.BulletItem;

                if (kind == BlockKind.TaskItem)
                {
                    bool chk = itemText.StartsWith("[x]", StringComparison.OrdinalIgnoreCase);
                    itemText = itemText.Substring(4); // strip "[ ] " or "[x] "
                    result.Add(new MdBlock
                    {
                        Kind         = BlockKind.TaskItem,
                        Text         = itemText,
                        Depth        = depth,
                        TaskChecked  = chk,
                    });
                }
                else
                {
                    result.Add(new MdBlock
                    {
                        Kind       = kind,
                        Text       = itemText,
                        Depth      = depth,
                        OrderIndex = ordIndex,
                    });
                }
                i++;
                continue;
            }

            // ── Standalone image  ![alt](src) ─────────────────────────────────
            {
                var m = Regex.Match(ln.Trim(), @"^!\[([^\]]*)\]\(([^)]+)\)\s*$");
                if (m.Success)
                {
                    result.Add(new MdBlock
                    {
                        Kind     = BlockKind.Image,
                        AltText  = m.Groups[1].Value,
                        ImageSrc = m.Groups[2].Value,
                    });
                    i++;
                    continue;
                }
            }

            // ── Paragraph ─────────────────────────────────────────────────────
            // Collect consecutive non-special lines into one paragraph block
            {
                var pLines = new List<string>();
                while (i < lines.Length)
                {
                    string cur = lines[i];
                    if (string.IsNullOrWhiteSpace(cur)) break;
                    if (IsFenceStart(cur, out _, out _))  break;
                    if (cur.TrimStart().StartsWith(">"))  break;
                    if (IsListItem(cur, out _, out _, out _, out _)) break;
                    if (Regex.IsMatch(cur, @"^(#{1,6})\s")) break;
                    if (Regex.IsMatch(cur, @"^\s*(\*\s*){3,}$") ||
                        Regex.IsMatch(cur, @"^\s*(-\s*){3,}$")  ||
                        Regex.IsMatch(cur, @"^\s*(_\s*){3,}$"))  break;

                    // Setext underline on the *next* line — don't consume the heading line here
                    if (i + 1 < lines.Length &&
                        (Regex.IsMatch(lines[i + 1], @"^=+\s*$") ||
                         Regex.IsMatch(lines[i + 1], @"^-{2,}\s*$")))
                        break;

                    pLines.Add(cur);
                    i++;
                }
                if (pLines.Count > 0)
                {
                    result.Add(new MdBlock
                    {
                        Kind = BlockKind.Paragraph,
                        // Join with a space so soft-wrapped lines become one paragraph
                        Text = string.Join(" ", pLines),
                    });
                }
            }
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    static BlockKind LevelToHeadingKind(int level)
    {
        switch (level)
        {
            case 1:  return BlockKind.H1;
            case 2:  return BlockKind.H2;
            case 3:  return BlockKind.H3;
            case 4:  return BlockKind.H4;
            case 5:  return BlockKind.H5;
            default: return BlockKind.H6;
        }
    }

    static bool IsFenceStart(string ln, out string fence, out string lang)
    {
        var m = Regex.Match(ln, @"^(`{3,}|~{3,})(\w*)");
        if (m.Success)
        {
            fence = m.Groups[1].Value.Substring(0, 3); // normalise to 3 chars
            lang  = m.Groups[2].Value;
            return true;
        }
        fence = lang = "";
        return false;
    }

    static bool IsFenceEnd(string ln, string fence) =>
        ln.StartsWith(fence);

    static bool IsListItem(string ln, out int depth, out int ordIndex,
                           out bool taskChecked, out string text)
    {
        depth = 0; ordIndex = -1; taskChecked = false; text = "";

        // Count leading spaces/tabs for nesting depth
        int spaces = 0;
        foreach (char c in ln)
        {
            if (c == ' ')  spaces++;
            else if (c == '\t') spaces += 2;
            else break;
        }
        depth = spaces / 2;

        string trimmed = ln.TrimStart();

        // Unordered
        var mu = Regex.Match(trimmed, @"^[*\-+]\s+(.+)$");
        if (mu.Success)
        {
            text = mu.Groups[1].Value;
            // Task list?
            var mt = Regex.Match(text, @"^\[([ xX])\]\s+(.+)$");
            if (mt.Success)
            {
                taskChecked = mt.Groups[1].Value.Equals("x", StringComparison.OrdinalIgnoreCase);
                text        = mt.Groups[2].Value;
                return true;
            }
            return true;
        }

        // Ordered
        var mo = Regex.Match(trimmed, @"^(\d+)[.)]\s+(.+)$");
        if (mo.Success)
        {
            ordIndex = int.Parse(mo.Groups[1].Value);
            text     = mo.Groups[2].Value;
            return true;
        }

        return false;
    }

    // ── Table helpers ─────────────────────────────────────────────────────────

    static bool IsTableLine(string ln) =>
        ln.Contains("|") && !string.IsNullOrWhiteSpace(ln);

    static bool IsTableDelimiter(string ln) =>
        Regex.IsMatch(ln, @"^\|?[\s\-:|]+\|[\s\-:|]*\|?$");

    static List<string> SplitTableRow(string ln)
    {
        // Strip leading/trailing pipes and whitespace
        string s = ln.Trim();
        if (s.StartsWith("|")) s = s.Substring(1);
        if (s.EndsWith("|"))   s = s.Substring(0, s.Length - 1);
        var cells = s.Split('|');
        var result = new List<string>(cells.Length);
        foreach (var c in cells)
            result.Add(c.Trim());
        return result;
    }

    static List<ColAlign> ParseTableAligns(string delimRow)
    {
        var cells  = SplitTableRow(delimRow);
        var aligns = new List<ColAlign>(cells.Count);
        foreach (var cell in cells)
        {
            string c = cell.Trim();
            bool left  = c.StartsWith(":");
            bool right = c.EndsWith(":");
            if (left && right) aligns.Add(ColAlign.Center);
            else if (right)    aligns.Add(ColAlign.Right);
            else               aligns.Add(ColAlign.Left);
        }
        return aligns;
    }
}
#endif