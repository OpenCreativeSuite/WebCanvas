# BlocksWorkspace

The reusable block library and block editor. It lists Theme and Content blocks, filters them, exposes the core prefab kit and the widget palette, and edits the selected block on a canvas.

## Integration
Block operations live on StudioSession.Blocks and StudioSession.Widgets. BlocksWorkspace drives EditingBlockId, opens block tabs and renders PrefabKit and WidgetPalette. WorkspaceHost mounts it for StudioMode.Blocks.
