# DevelopmentWorkspace

The Development workspace host. It switches between Overview, Theme, Plugins, APIs and Build, and provides the shared development page frame.

## Integration
It reads StudioSession.DevSection, updates it with SelectDevSection, and mounts the matching sub-view. WorkspaceHost selects it for StudioMode.Development.
