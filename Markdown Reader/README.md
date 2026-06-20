# Unity Markdown Inspector

Renders `.md` files directly in the Unity Inspector. Click any Markdown file in the Project window and get a styled, readable preview — no packages, no setup.

---

## Installation

Drop all four scripts into any `Editor/` folder in your project:

```
Assets/
└── Editor/
    ├── MdBlock.cs
    ├── MarkdownParser.cs
    ├── MarkdownRenderer.cs
    └── MarkdownInspector.cs
```

Unity recompiles automatically. No other steps needed.

---

## Usage

Select any `.md` file in the Project window. The Inspector shows the rendered preview immediately.

Use the two buttons in the Inspector toolbar to switch views:

- **Preview** — rendered Markdown
- **Source** — raw text

All other asset types (`.txt`, `.json`, etc.) are unaffected and show their normal Inspector.

---

## Supported Markdown

| Element | Syntax |
|---|---|
| Headings | `# H1` through `###### H6`, and Setext style (`===` / `---`) |
| Bold | `**bold**` or `__bold__` |
| Italic | `*italic*` or `_italic_` |
| Bold + italic | `***bold italic***` |
| Strikethrough | `~~text~~` |
| Inline code | `` `code` `` |
| Fenced code block | ```` ``` ```` with optional language tag |
| Blockquote | `> text` |
| Bullet list | `- item`, `* item`, or `+ item` — nested |
| Ordered list | `1. item` — nested |
| Task list | `- [ ] todo` and `- [x] done` |
| Table | GFM pipe tables with column alignment |
| Horizontal rule | `---`, `***`, or `___` |
| Link | `[label](url)` — shown in blue, not clickable |
| Image | `![alt](path)` — loaded from project assets if path resolves |

---

## File Structure

| File | Role |
|---|---|
| `MdBlock.cs` | Data model — `BlockKind` enum and `MdBlock` class |
| `MarkdownParser.cs` | Converts raw Markdown text into a `List<MdBlock>` |
| `MarkdownRenderer.cs` | Draws blocks as IMGUI, owns all styles and rich-text conversion |
| `MarkdownInspector.cs` | Unity entry point — `[CustomEditor(typeof(TextAsset))]` |

---

## Limitations

- Links are not clickable — Unity's IMGUI does not support hyperlink interaction.
- Remote images (`https://…`) are not fetched — a placeholder is shown instead.
- Strikethrough is approximated as muted + bracketed text (no `<s>` tag in Unity rich-text).
- Nested blockquotes are flattened to one level.
- Inline HTML is shown as plain text.

---

## Compatibility

- Unity 2020.1 or newer
- Any render pipeline (Editor-only, stripped from builds)
- No dependencies

---

## License

MIT — free to use and modify in personal and commercial projects.
