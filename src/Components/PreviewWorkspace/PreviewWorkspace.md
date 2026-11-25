# PreviewWorkspace

A compiled page preview rendered by Donkeh's embedded Chromium webview. It shows the page as the published site sees it, including scripts and embeds.

## Integration
DesignWorkspace.PreviewPage builds the site first, then opens this tab. The tab title stores the page id; PreviewTarget resolves the compiled file and the local HTTP URL served by PreviewServer. The preview toolbar can rebuild or close the tab.
