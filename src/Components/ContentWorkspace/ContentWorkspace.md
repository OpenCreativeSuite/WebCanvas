# ContentWorkspace

The record workspace. It switches between a content map and a side-by-side record list/editor, with search, create, edit, duplicate and delete support.

## Integration
It reads the selected schema and entity from StudioSession, and calls the content CRUD methods. OpenContentMap and OpenContentRecords control the view switch. WorkspaceHost mounts it for StudioMode.Content.
