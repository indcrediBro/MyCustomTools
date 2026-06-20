// MdBlock.cs
// Shared data model used by MarkdownParser, MarkdownRenderer, and MarkdownInspector.
// Place this file (and the other three) inside any Editor/ folder in your project.

#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>All recognised block-level element kinds.</summary>
public enum BlockKind
{
    H1, H2, H3, H4, H5, H6,   // ATX and Setext headings
    Paragraph,                  // Normal prose
    CodeFence,                  // ``` … ``` or ~~~ … ~~~
    BlockQuote,                 // > …
    BulletItem,                 // - / * / +
    OrderedItem,                // 1. …
    TaskItem,                   // - [ ] / - [x]
    Table,                      // GFM pipe table
    HorizontalRule,             // --- / *** / ___
    Image,                      // ![alt](src)
    Blank,                      // Empty / whitespace-only line
}

/// <summary>
/// One logical block parsed from a Markdown file.
/// Different fields are populated depending on <see cref="Kind"/>.
/// </summary>
public class MdBlock
{
    // ── Universal ─────────────────────────────────────────────────────────────
    public BlockKind Kind;

    /// <summary>Primary text content (heading text, paragraph text, quote text, …).</summary>
    public string Text = "";

    // ── List items ────────────────────────────────────────────────────────────
    /// <summary>Nesting depth (0 = top-level). One indent level ≈ 2 or 4 spaces / 1 tab.</summary>
    public int  Depth = 0;

    /// <summary>For ordered items: the number that appeared in source. -1 = unordered.</summary>
    public int  OrderIndex = -1;

    /// <summary>For task items: true = [x], false = [ ].</summary>
    public bool TaskChecked = false;

    // ── Code fences ───────────────────────────────────────────────────────────
    /// <summary>Optional language tag after the opening fence (e.g. "csharp", "json").</summary>
    public string Language = "";

    // ── Tables ────────────────────────────────────────────────────────────────
    /// <summary>Header row cells.</summary>
    public List<string> TableHeaders = null;

    /// <summary>
    /// Body rows. Each inner list is one row; each string is one cell.
    /// </summary>
    public List<List<string>> TableRows = null;

    /// <summary>Column alignments parsed from the delimiter row (Left / Center / Right).</summary>
    public List<ColAlign> TableAligns = null;

    // ── Images ────────────────────────────────────────────────────────────────
    /// <summary>Alt text from ![alt](…).</summary>
    public string AltText = "";

    /// <summary>Image URL / path from ![…](src).</summary>
    public string ImageSrc = "";
}

/// <summary>Column alignment for GFM tables.</summary>
public enum ColAlign { Left, Center, Right }
#endif