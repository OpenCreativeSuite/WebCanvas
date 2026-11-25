# RichTextEditor

A watered-down version of the page editor for `richtext` data fields. Instead of
showing raw HTML, it presents a visual sheet of paragraphs, headings, quotes,
lists and code blocks.

## Integration
`ContentWorkspace` uses this component for `FieldTypes.RichText`. Every block edit
serialises to simple HTML and writes it back through `SetEntityValue`. The parser
also accepts existing plain-text values and upgrades them to paragraphs on the
next edit. Detail-page `{{field}}` tokens emit rich-text HTML without escaping.
