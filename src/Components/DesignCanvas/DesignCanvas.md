# DesignCanvas

The live editing canvas. It renders the current page through SiteRenderer, applies viewport/zoom sizing and provides the drag/drop surface, wireframe hints and pointer-anchored drop preview.

## Integration
It reads CurrentPage, DropHint, DropBounds and canvas editing state from StudioSession. Widget drops and block insertions call the session's widget methods. The canvas also switches to a non-editing preview when canvas editing is off.
