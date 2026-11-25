# DesignWorkspace

The design workspace frame: undo/redo, viewport presets, zoom controls, canvas preview toggle, compiled Preview action and the canvas body.

## Integration
It reads CurrentPage and the design preferences from StudioSession. Preview calls PreviewPage, which builds the site and opens PreviewWorkspace. WorkspaceHost mounts it for StudioMode.Design.
