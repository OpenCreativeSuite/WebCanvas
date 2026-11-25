# ContextSidebar

The contextual left sidebar. Its heading and body change with the active workspace: pages in Design, block tools in Blocks, types in Data, and so on.

## Integration
It reads StudioSession.ActiveMode and renders the matching navigation or picker. Actions such as new page, new block, new type and mode-specific selection all go through the session. The app shell shows it when the sidebar preference is enabled.
