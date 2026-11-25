# HtmlEditorWorkspace

The dedicated editor for `html` page nodes. It shows two plain code fields â€” one
for the HTML fragment and one for its CSS â€” beside a live webview preview.

## Integration
The Design inspector opens this view with `StudioSession.OpenHtmlEditor`. HTML
and CSS save through `SetHtmlBlockHtml` / `SetHtmlBlockCss`. When the webview is
ready, its document size is measured and stored as `width`/`height` attributes so
the design canvas can reserve the block's default space. `Back to page` returns to
the page's Design tab.

